using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business;

public class SchoolInformationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SchoolInformationService>? _logger;

    public SchoolInformationService(AppDbContext context, ILogger<SchoolInformationService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<SchoolInformation?> GetSchoolInformationAsync()
    {
        try
        {
            return await _context.SchoolInformation.FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading school information");
            return null;
        }
    }

    public async Task<SchoolInformation> SaveSchoolInformationAsync(SchoolInformation schoolInfo, int? updatedBy = null)
    {
        try
        {
            var existing = await _context.SchoolInformation.FirstOrDefaultAsync();
            
            if (existing != null)
            {
                existing.SchoolName = schoolInfo.SchoolName;
                existing.SchoolCode = schoolInfo.SchoolCode;
                existing.ContactNumber = schoolInfo.ContactNumber;
                existing.Email = schoolInfo.Email;
                existing.Website = schoolInfo.Website;
                existing.HouseNo = schoolInfo.HouseNo;
                existing.StreetName = schoolInfo.StreetName;
                existing.Barangay = schoolInfo.Barangay;
                existing.City = schoolInfo.City;
                existing.Province = schoolInfo.Province;
                existing.Country = schoolInfo.Country;
                existing.ZipCode = schoolInfo.ZipCode;
                existing.BirTin = schoolInfo.BirTin;
                existing.BirBusinessName = schoolInfo.BirBusinessName;
                existing.BirAddress = schoolInfo.BirAddress;
                existing.BirRegistrationType = schoolInfo.BirRegistrationType;
                existing.VatRate = schoolInfo.VatRate;
                existing.IsVatRegistered = schoolInfo.IsVatRegistered;
                existing.UpdatedAt = DateTime.Now;
                existing.UpdatedBy = updatedBy;

                _context.SchoolInformation.Update(existing);
            }
            else
            {
                schoolInfo.UpdatedAt = DateTime.Now;
                schoolInfo.UpdatedBy = updatedBy;
                _context.SchoolInformation.Add(schoolInfo);
            }

            await _context.SaveChangesAsync();
            _logger?.LogInformation("School information saved successfully");

            return existing ?? schoolInfo;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving school information");
            throw;
        }
    }
}
