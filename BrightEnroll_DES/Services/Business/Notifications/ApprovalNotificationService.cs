using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BrightEnroll_DES.Services.Business.Notifications;

/// <summary>
/// Service for tracking pending approvals and notifications
/// </summary>
public class ApprovalNotificationService
{
    private readonly AppDbContext _context;
    private readonly NotificationService? _notificationService;

    public ApprovalNotificationService(AppDbContext context, NotificationService? notificationService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _notificationService = notificationService;
    }

    /// <summary>
    /// Gets the total count of pending approvals (expenses + payroll + journal entries)
    /// Always counts actual pending items from the database, not notifications
    /// </summary>
    public async Task<int> GetPendingApprovalsCountAsync()
    {
        try
        {
            // Clear change tracker to ensure we get fresh data from database
            // This is important after approvals/rejections to get accurate counts
            _context.ChangeTracker.Clear();
            
            // Always count actual pending items from database (not notifications)
            // This ensures the count matches what's actually displayed in the Approvals tab
            // and decreases correctly when items are approved/rejected
            var expenseCount = await _context.Expenses
                .Where(e => e.Status == "Pending")
                .CountAsync();

            // Count payroll transactions with "Pending Approval" status
            var payrollCount = await _context.PayrollTransactions
                .Where(pt => pt.Status == "Pending Approval")
                .CountAsync();

            var journalEntryCount = await _context.JournalEntries
                .Where(je => je.Status == "Draft")
                .CountAsync();

            return expenseCount + payrollCount + journalEntryCount;
        }
        catch (Exception)
        {
            // Return 0 on error to prevent UI issues
            return 0;
        }
    }

    /// <summary>
    /// Gets all unread notifications
    /// </summary>
    public async Task<List<Notification>> GetUnreadNotificationsAsync()
    {
        if (_notificationService != null)
        {
            return await _notificationService.GetUnreadNotificationsAsync();
        }
        return new List<Notification>();
    }

    /// <summary>
    /// Gets pending expenses count
    /// </summary>
    public async Task<int> GetPendingExpensesCountAsync()
    {
        return await _context.Expenses
            .Where(e => e.Status == "Pending")
            .CountAsync();
    }

    /// <summary>
    /// Gets pending journal entries count
    /// </summary>
    public async Task<int> GetPendingJournalEntriesCountAsync()
    {
        return await _context.JournalEntries
            .Where(je => je.Status == "Draft")
            .CountAsync();
    }

    /// <summary>
    /// Gets pending payroll count
    /// </summary>
    public async Task<int> GetPendingPayrollCountAsync()
    {
        return await _context.PayrollTransactions
            .Where(pt => pt.Status == "Pending Approval")
            .CountAsync();
    }
}

