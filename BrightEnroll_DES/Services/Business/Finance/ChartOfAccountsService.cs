using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Finance;

// Service for managing Chart of Accounts
public class ChartOfAccountsService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ChartOfAccountsService>? _logger;

    public ChartOfAccountsService(AppDbContext context, ILogger<ChartOfAccountsService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    // Get all active accounts
    public async Task<List<ChartOfAccount>> GetAllAccountsAsync(bool includeInactive = false)
    {
        try
        {
            var query = _context.ChartOfAccounts.AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(a => a.IsActive);
            }

            return await query
                .Include(a => a.ParentAccount)
                .OrderBy(a => a.AccountCode)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting all accounts: {Message}", ex.Message);
            throw;
        }
    }

    // Get account by code
    public async Task<ChartOfAccount?> GetAccountByCodeAsync(string accountCode)
    {
        try
        {
            return await _context.ChartOfAccounts
                .Include(a => a.ParentAccount)
                .FirstOrDefaultAsync(a => a.AccountCode == accountCode);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting account by code {AccountCode}: {Message}", accountCode, ex.Message);
            throw;
        }
    }

    // Get account by ID
    public async Task<ChartOfAccount?> GetAccountByIdAsync(int accountId)
    {
        try
        {
            return await _context.ChartOfAccounts
                .Include(a => a.ParentAccount)
                .FirstOrDefaultAsync(a => a.AccountId == accountId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting account by ID {AccountId}: {Message}", accountId, ex.Message);
            throw;
        }
    }

    // Get accounts by type
    public async Task<List<ChartOfAccount>> GetAccountsByTypeAsync(string accountType)
    {
        try
        {
            return await _context.ChartOfAccounts
                .Where(a => a.AccountType == accountType && a.IsActive)
                .OrderBy(a => a.AccountCode)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting accounts by type {AccountType}: {Message}", accountType, ex.Message);
            throw;
        }
    }

    // Calculate account balance from journal entries
    public async Task<decimal> GetAccountBalanceAsync(int accountId, DateTime? asOfDate = null)
    {
        try
        {
            var account = await GetAccountByIdAsync(accountId);
            if (account == null)
            {
                return 0;
            }

            var query = _context.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == accountId && l.JournalEntry.Status == "Posted");

            if (asOfDate.HasValue)
            {
                query = query.Where(l => l.JournalEntry.EntryDate <= asOfDate.Value);
            }

            var lines = await query.ToListAsync();

            decimal balance = 0;
            foreach (var line in lines)
            {
                if (account.NormalBalance == "Debit")
                {
                    balance += line.DebitAmount - line.CreditAmount;
                }
                else
                {
                    balance += line.CreditAmount - line.DebitAmount;
                }
            }

            return balance;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error calculating account balance for {AccountId}: {Message}", accountId, ex.Message);
            throw;
        }
    }
}

