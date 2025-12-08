using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BrightEnroll_DES.Services.Business.Notifications;

/// <summary>
/// Service for managing notifications
/// </summary>
public class NotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Creates a new notification
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
        var notification = new Notification
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

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return notification.NotificationId;
    }

    /// <summary>
    /// Gets all unread notifications - optimized for performance
    /// </summary>
    public async Task<List<Notification>> GetUnreadNotificationsAsync()
    {
        return await _context.Notifications
            .AsNoTracking() // Faster - no change tracking needed for read-only
            .Where(n => !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all notifications (read and unread) - optimized for performance
    /// </summary>
    public async Task<List<Notification>> GetAllNotificationsAsync(int? limit = null)
    {
        // Optimize: Use AsNoTracking for read-only queries, select only needed fields
        var query = _context.Notifications
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
        return await _context.Notifications
            .AsNoTracking() // Faster - no change tracking needed
            .Where(n => !n.IsRead)
            .CountAsync();
    }

    /// <summary>
    /// Marks a notification as read
    /// </summary>
    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
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
        var unreadNotifications = await _context.Notifications
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
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Gets notification by ID
    /// </summary>
    public async Task<Notification?> GetNotificationByIdAsync(int notificationId)
    {
        return await _context.Notifications
            .Include(n => n.CreatedByUser)
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId);
    }

    /// <summary>
    /// Marks notifications as read by reference type and ID
    /// </summary>
    public async Task MarkNotificationsAsReadByReferenceAsync(string referenceType, int referenceId)
    {
        var notifications = await _context.Notifications
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

