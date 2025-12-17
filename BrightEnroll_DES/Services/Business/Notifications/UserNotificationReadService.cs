using BrightEnroll_DES.Services.Authentication;

namespace BrightEnroll_DES.Services.Business.Notifications;

// Service to track notification read status per user in memory
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

    // Get current user identifier
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

    // Check if notification is read for current user
    public bool IsNotificationReadForCurrentUser(int notificationId)
    {
        var userIdentifier = GetCurrentUserIdentifier();
        var key = $"{userIdentifier}_{notificationId}";
        
        lock (_lock)
        {
            return _userReadNotifications.ContainsKey(key) && _userReadNotifications[key];
        }
    }

    // Mark notification as read for current user
    public void MarkNotificationAsReadForCurrentUser(int notificationId)
    {
        var userIdentifier = GetCurrentUserIdentifier();
        var key = $"{userIdentifier}_{notificationId}";
        
        lock (_lock)
        {
            _userReadNotifications[key] = true;
        }
    }

    // Mark all notifications as read for current user
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

    // Get unread count for current user from notification list
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

    // Update notification read status for display
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

    // Clear read status for a specific user
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

