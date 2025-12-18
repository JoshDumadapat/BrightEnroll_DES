using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models.SuperAdmin;
using Microsoft.EntityFrameworkCore;

namespace BrightEnroll_DES.Services.Business.SuperAdmin;

/// <summary>
/// Service for managing SuperAdmin notifications
/// </summary>
public class SuperAdminNotificationService
{
    private readonly SuperAdminDbContext _context;

    public SuperAdminNotificationService(SuperAdminDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Creates a new SuperAdmin notification
    /// </summary>
    public async Task<int> CreateNotificationAsync(
        string notificationType,
        string title,
        string? message,
        string referenceType,
        int? referenceId,
        string? actionUrl = null,
        string priority = "Normal",
        int? createdBy = null)
    {
        var notification = new SuperAdminNotification
        {
            NotificationType = notificationType,
            Title = title,
            Message = message,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            IsRead = false,
            CreatedAt = DateTime.Now,
            ActionUrl = actionUrl,
            Priority = priority,
            CreatedBy = createdBy
        };

        _context.SuperAdminNotifications.Add(notification);
        await _context.SaveChangesAsync();

        return notification.NotificationId;
    }

    /// <summary>
    /// Gets all unread notifications - optimized for performance
    /// </summary>
    public async Task<List<SuperAdminNotification>> GetUnreadNotificationsAsync()
    {
        return await _context.SuperAdminNotifications
            .AsNoTracking() // Faster - no change tracking needed for read-only
            .Where(n => !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all notifications (read and unread) - optimized for performance
    /// </summary>
    public async Task<List<SuperAdminNotification>> GetAllNotificationsAsync(int? limit = null)
    {
        // Optimize: Use AsNoTracking for read-only queries, select only needed fields
        var query = _context.SuperAdminNotifications
            .AsNoTracking() // Faster - no change tracking needed for read-only
            .OrderByDescending(n => n.CreatedAt)
            .AsQueryable();

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync();
    }

    /// <summary>
    /// Gets unread notification count - optimized for performance
    /// </summary>
    public async Task<int> GetUnreadCountAsync()
    {
        return await _context.SuperAdminNotifications
            .AsNoTracking() // Faster - no change tracking needed
            .Where(n => !n.IsRead)
            .CountAsync();
    }

    /// <summary>
    /// Marks a notification as read
    /// </summary>
    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _context.SuperAdminNotifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId);
        
        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Marks all notifications as read
    /// </summary>
    public async Task MarkAllAsReadAsync()
    {
        var unreadNotifications = await _context.SuperAdminNotifications
            .Where(n => !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes a notification
    /// </summary>
    public async Task DeleteNotificationAsync(int notificationId)
    {
        var notification = await _context.SuperAdminNotifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId);
        
        if (notification != null)
        {
            _context.SuperAdminNotifications.Remove(notification);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Gets notification by ID
    /// </summary>
    public async Task<SuperAdminNotification?> GetNotificationByIdAsync(int notificationId)
    {
        return await _context.SuperAdminNotifications
            .Include(n => n.CreatedByUser)
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId);
    }

    /// <summary>
    /// Marks notifications as read by reference type and ID
    /// </summary>
    public async Task MarkNotificationsAsReadByReferenceAsync(string referenceType, int referenceId)
    {
        var notifications = await _context.SuperAdminNotifications
            .Where(n => n.ReferenceType == referenceType && 
                       n.ReferenceId.HasValue && 
                       n.ReferenceId.Value == referenceId &&
                       !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.Now;
        }

        if (notifications.Any())
        {
            await _context.SaveChangesAsync();
        }
    }
}
