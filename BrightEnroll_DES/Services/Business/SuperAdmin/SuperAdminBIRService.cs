using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.SuperAdmin;

public class SuperAdminBIRService
{
    private readonly SuperAdminDbContext _context;
    private readonly ILogger<SuperAdminBIRService>? _logger;

    public SuperAdminBIRService(SuperAdminDbContext context, ILogger<SuperAdminBIRService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<SuperAdminBIRInfo?> GetBIRInfoAsync()
    {
        try
        {
            return await _context.SuperAdminBIRInfo.FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading SuperAdmin BIR information");
            return null;
        }
    }

    public async Task<SuperAdminBIRInfo> SaveBIRInfoAsync(SuperAdminBIRInfo birInfo, int? updatedBy = null)
    {
        try
        {
            var existing = await _context.SuperAdminBIRInfo.FirstOrDefaultAsync();
            
            if (existing != null)
            {
                existing.TinNumber = birInfo.TinNumber;
                existing.BusinessName = birInfo.BusinessName;
                existing.BusinessAddress = birInfo.BusinessAddress;
                existing.RegistrationType = birInfo.RegistrationType;
                existing.VatRate = birInfo.VatRate;
                existing.IsVatRegistered = birInfo.IsVatRegistered;
                existing.UpdatedAt = DateTime.Now;
                existing.UpdatedBy = updatedBy;

                _context.SuperAdminBIRInfo.Update(existing);
            }
            else
            {
                birInfo.UpdatedAt = DateTime.Now;
                birInfo.UpdatedBy = updatedBy;
                _context.SuperAdminBIRInfo.Add(birInfo);
            }

            await _context.SaveChangesAsync();
            _logger?.LogInformation("SuperAdmin BIR information saved successfully");

            return existing ?? birInfo;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving SuperAdmin BIR information");
            throw;
        }
    }

    public decimal GetVATRate()
    {
        try
        {
            var birInfo = _context.SuperAdminBIRInfo.FirstOrDefault();
            return birInfo?.VatRate ?? 0.12m;
        }
        catch
        {
            return 0.12m;
        }
    }
}
