using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.SuperAdmin;

public class SuperAdminBIRFilingService
{
    private readonly SuperAdminDbContext _context;
    private readonly ILogger<SuperAdminBIRFilingService>? _logger;

    public SuperAdminBIRFilingService(SuperAdminDbContext context, ILogger<SuperAdminBIRFilingService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<List<SuperAdminBIRFiling>> GetAllFilingsAsync()
    {
        try
        {
            return await _context.SuperAdminBIRFilings
                .OrderByDescending(f => f.DueDate)
                .ThenByDescending(f => f.FilingDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading BIR filings");
            return new List<SuperAdminBIRFiling>();
        }
    }

    public async Task<SuperAdminBIRFiling?> GetFilingByIdAsync(int filingId)
    {
        try
        {
            return await _context.SuperAdminBIRFilings
                .FirstOrDefaultAsync(f => f.FilingId == filingId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading BIR filing {FilingId}", filingId);
            return null;
        }
    }

    public async Task<SuperAdminBIRFiling> CreateFilingAsync(SuperAdminBIRFiling filing, int? createdBy = null)
    {
        try
        {
            filing.CreatedAt = DateTime.Now;
            filing.CreatedBy = createdBy;
            filing.Status = DetermineStatus(filing);

            _context.SuperAdminBIRFilings.Add(filing);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("BIR filing created: {FilingType} for period {Period}", filing.FilingType, filing.Period);
            return filing;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating BIR filing");
            throw;
        }
    }

    public async Task<SuperAdminBIRFiling> UpdateFilingAsync(SuperAdminBIRFiling filing, int? updatedBy = null)
    {
        try
        {
            var existing = await _context.SuperAdminBIRFilings
                .FirstOrDefaultAsync(f => f.FilingId == filing.FilingId);

            if (existing == null)
            {
                throw new Exception($"BIR filing {filing.FilingId} not found");
            }

            existing.FilingType = filing.FilingType;
            existing.Period = filing.Period;
            existing.FilingDate = filing.FilingDate;
            existing.DueDate = filing.DueDate;
            existing.Amount = filing.Amount;
            existing.ReferenceNumber = filing.ReferenceNumber;
            existing.Notes = filing.Notes;
            existing.Status = DetermineStatus(filing);
            existing.UpdatedAt = DateTime.Now;
            existing.UpdatedBy = updatedBy;

            _context.SuperAdminBIRFilings.Update(existing);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("BIR filing updated: {FilingId}", filing.FilingId);
            return existing;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating BIR filing");
            throw;
        }
    }

    public async Task DeleteFilingAsync(int filingId)
    {
        try
        {
            var filing = await _context.SuperAdminBIRFilings
                .FirstOrDefaultAsync(f => f.FilingId == filingId);

            if (filing == null)
            {
                throw new Exception($"BIR filing {filingId} not found");
            }

            _context.SuperAdminBIRFilings.Remove(filing);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("BIR filing deleted: {FilingId}", filingId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting BIR filing");
            throw;
        }
    }

    public async Task<List<SuperAdminBIRFiling>> GetUpcomingFilingsAsync(int daysAhead = 30)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(daysAhead);
            return await _context.SuperAdminBIRFilings
                .Where(f => f.DueDate >= DateTime.Now && f.DueDate <= cutoffDate && f.Status != "Filed")
                .OrderBy(f => f.DueDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading upcoming BIR filings");
            return new List<SuperAdminBIRFiling>();
        }
    }

    public async Task<List<SuperAdminBIRFiling>> GetOverdueFilingsAsync()
    {
        try
        {
            return await _context.SuperAdminBIRFilings
                .Where(f => f.DueDate < DateTime.Now && f.Status != "Filed" && f.Status != "Late Filed")
                .OrderBy(f => f.DueDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading overdue BIR filings");
            return new List<SuperAdminBIRFiling>();
        }
    }

    private string DetermineStatus(SuperAdminBIRFiling filing)
    {
        if (filing.Status == "Filed")
            return "Filed";

        if (filing.FilingDate > filing.DueDate && filing.Status != "Filed")
            return "Late Filed";

        if (filing.DueDate < DateTime.Now && filing.Status != "Filed")
            return "Overdue";

        return "Pending";
    }
}
