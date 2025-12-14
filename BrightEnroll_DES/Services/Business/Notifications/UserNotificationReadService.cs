using BrightEnroll_DES.Services.Authentication;

namespace BrightEnroll_DES.Services.Business.Notifications;

/// <summary>
/// Service to track notification read status per user/role in memory (no database changes)
/// </summary>
public class UserNotificationReadService
{
    private readonly IAuthService _authService;
    
    // Dictionary to store read notifications per user: Key = "userId_notificationId", Value = true if read
    private static readonly Dictionary<string, bool> _userReadNotifications = new();
    
    // Lock for thread safety
    private static readonly object _lock = new object();

    public UserNotificationReadService(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    /// <summary>
    /// Gets the current user identifier (user ID)
    /// </summary>
    private string GetCurrentUserIdentifier()
    {
        if (!_authService.IsAuthenticated || _authService.CurrentUser == null)
        {
            return "anonymous";
        }

        var user = _authService.CurrentUser;
        // Use user ID as the identifier
        return user.user_ID.ToString();
    }

    /// <summary>
    /// Checks if a notification is read for the current user
    /// </summary>
    public bool IsNotificationReadForCurrentUser(int notificationId)
    {
        var userIdentifier = GetCurrentUserIdentifier();
        var key = $"{userIdentifier}_{notificationId}";
        
        lock (_lock)
        {
            return _userReadNotifications.ContainsKey(key) && _userReadNotifications[key];
        }
    }

    /// <summary>
    /// Marks a notification as read for the current user
    /// </summary>
    public void MarkNotificationAsReadForCurrentUser(int notificationId)
    {
        var userIdentifier = GetCurrentUserIdentifier();
        var key = $"{userIdentifier}_{notificationId}";
        
        lock (_lock)
        {
            _userReadNotifications[key] = true;
        }
    }

    /// <summary>
    /// Marks all notifications as read for the current user
    /// </summary>
    public void MarkAllNotificationsAsReadForCurrentUser(List<int> notificationIds)
    {
        var userIdentifier = GetCurrentUserIdentifier();
        
        lock (_lock)
        {
            foreach (var notificationId in notificationIds)
            {
                var key = $"{userIdentifier}_{notificationId}";
                _userReadNotifications[key] = true;
            }
        }
    }

    /// <summary>
    /// Gets unread count for the current user from a list of notifications
    /// </summary>
    public int GetUnreadCountForCurrentUser(List<BrightEnroll_DES.Data.Models.Notification> notifications)
    {
        if (notifications == null || !notifications.Any())
            return 0;

        var userIdentifier = GetCurrentUserIdentifier();
        int unreadCount = 0;

        lock (_lock)
        {
            foreach (var notification in notifications)
            {
                var key = $"{userIdentifier}_{notification.NotificationId}";
                // Notification is unread if:
                // 1. Database says it's unread AND
                // 2. User hasn't marked it as read in memory
                if (!notification.IsRead && (!_userReadNotifications.ContainsKey(key) || !_userReadNotifications[key]))
                {
                    unreadCount++;
                }
            }
        }

        return unreadCount;
    }

    /// <summary>
    /// Updates notification read status for display (combines database and user-specific read status)
    /// </summary>
    public void UpdateNotificationReadStatus(List<BrightEnroll_DES.Data.Models.Notification> notifications)
    {
        if (notifications == null || !notifications.Any())
            return;

        var userIdentifier = GetCurrentUserIdentifier();

        lock (_lock)
        {
            foreach (var notification in notifications)
            {
                var key = $"{userIdentifier}_{notification.NotificationId}";
                // Notification is read if database says it's read OR user has marked it as read
                if (_userReadNotifications.ContainsKey(key) && _userReadNotifications[key])
                {
                    notification.IsRead = true;
                }
            }
        }
    }

    /// <summary>
    /// Clears read status for a specific user (useful for testing or cleanup)
    /// </summary>
    public void ClearReadStatusForUser(string userIdentifier)
    {
        lock (_lock)
        {
            var keysToRemove = _userReadNotifications.Keys
                .Where(k => k.StartsWith($"{userIdentifier}_", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _userReadNotifications.Remove(key);
            }
        }
    }
}

