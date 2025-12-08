using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Services.Business.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Finance;

/// <summary>
/// Service for managing accounting period closing
/// </summary>
public class PeriodClosingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PeriodClosingService>? _logger;

    public PeriodClosingService(AppDbContext context, ILogger<PeriodClosingService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates an accounting period for the given month/year
    /// </summary>
    public async Task<AccountingPeriod> GetOrCreatePeriodAsync(int year, int month)
    {
        var period = await _context.AccountingPeriods
            .FirstOrDefaultAsync(p => p.PeriodYear == year && p.PeriodMonth == month);

        if (period == null)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            var periodName = startDate.ToString("MMMM yyyy");

            period = new AccountingPeriod
            {
                PeriodYear = year,
                PeriodMonth = month,
                PeriodName = periodName,
                StartDate = startDate,
                EndDate = endDate,
                IsClosed = false,
                CreatedAt = DateTime.Now
            };

            _context.AccountingPeriods.Add(period);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Created accounting period: {PeriodName}", periodName);
        }

        return period;
    }

    /// <summary>
    /// Closes an accounting period (prevents new transactions in that period)
    /// </summary>
    public async Task ClosePeriodAsync(int periodId, int closedBy, string? closingNotes = null)
    {
        try
        {
            var period = await _context.AccountingPeriods
                .FirstOrDefaultAsync(p => p.PeriodId == periodId);

            if (period == null)
            {
                throw new InvalidOperationException($"Accounting period {periodId} not found.");
            }

            if (period.IsClosed)
            {
                throw new InvalidOperationException($"Period {period.PeriodName} is already closed.");
            }

            // Verify all journal entries in the period are posted
            var draftEntries = await _context.JournalEntries
                .Where(je => je.EntryDate >= period.StartDate &&
                           je.EntryDate <= period.EndDate &&
                           je.Status == "Draft")
                .CountAsync();

            if (draftEntries > 0)
            {
                throw new InvalidOperationException($"Cannot close period. There are {draftEntries} draft journal entries that must be approved or rejected first.");
            }

            // Verify trial balance is balanced
            var trialBalanceService = new AccountingReportService(_context);
            var trialBalance = await trialBalanceService.GetTrialBalanceAsync(period.EndDate);

            if (!trialBalance.IsBalanced)
            {
                throw new InvalidOperationException($"Cannot close period. Trial balance is not balanced. Difference: {trialBalance.Difference:N2}");
            }

            // Close the period
            period.IsClosed = true;
            period.ClosedBy = closedBy;
            period.ClosedAt = DateTime.Now;
            period.ClosingNotes = closingNotes;
            period.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger?.LogInformation("Period {PeriodName} closed by user {ClosedBy}", period.PeriodName, closedBy);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error closing period {PeriodId}: {Message}", periodId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Reopens a closed period (for corrections)
    /// </summary>
    public async Task ReopenPeriodAsync(int periodId, int reopenedBy, string? reason = null)
    {
        try
        {
            var period = await _context.AccountingPeriods
                .FirstOrDefaultAsync(p => p.PeriodId == periodId);

            if (period == null)
            {
                throw new InvalidOperationException($"Accounting period {periodId} not found.");
            }

            if (!period.IsClosed)
            {
                throw new InvalidOperationException($"Period {period.PeriodName} is not closed.");
            }

            // Reopen the period
            period.IsClosed = false;
            period.ClosedBy = null;
            period.ClosedAt = null;
            period.ClosingNotes = reason ?? "Period reopened for corrections";
            period.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger?.LogInformation("Period {PeriodName} reopened by user {ReopenedBy}. Reason: {Reason}", period.PeriodName, reopenedBy, reason);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error reopening period {PeriodId}: {Message}", periodId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Checks if a date falls within a closed period
    /// </summary>
    public async Task<bool> IsDateInClosedPeriodAsync(DateTime date)
    {
        return await _context.AccountingPeriods
            .AnyAsync(p => p.IsClosed &&
                          date >= p.StartDate &&
                          date <= p.EndDate);
    }

    /// <summary>
    /// Gets all periods with their closing status
    /// </summary>
    public async Task<List<AccountingPeriod>> GetAllPeriodsAsync(int? year = null)
    {
        var query = _context.AccountingPeriods.AsQueryable();

        if (year.HasValue)
        {
            query = query.Where(p => p.PeriodYear == year.Value);
        }

        return await query
            .OrderByDescending(p => p.PeriodYear)
            .ThenByDescending(p => p.PeriodMonth)
            .ToListAsync();
    }

    /// <summary>
    /// Gets the current open period
    /// </summary>
    public async Task<AccountingPeriod?> GetCurrentPeriodAsync()
    {
        var now = DateTime.Now;
        return await _context.AccountingPeriods
            .FirstOrDefaultAsync(p => p.PeriodYear == now.Year &&
                                     p.PeriodMonth == now.Month &&
                                     !p.IsClosed);
    }
}

