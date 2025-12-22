using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Academic;

public class SchoolYearService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SchoolYearService>? _logger;

    public SchoolYearService(AppDbContext context, ILogger<SchoolYearService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    // Gets all available school years from database
    public async Task<List<string>> GetAvailableSchoolYearsAsync()
    {
        try
        {
            // Clear change tracker to ensure fresh data
            _context.ChangeTracker.Clear();
            
            var schoolYears = await _context.SchoolYears
                .AsNoTracking()
                .OrderByDescending(sy => sy.SchoolYearName)
                .Select(sy => sy.SchoolYearName)
                .ToListAsync();
            
            return schoolYears;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting available school years: {Message}", ex.Message);
            // Return empty list instead of fallback to allow proper error handling
            return new List<string>();
        }
    }

    // Gets all school years as entities for UI display
    public async Task<List<SchoolYear>> GetAllSchoolYearsAsync()
    {
        try
        {
            // Clear change tracker to ensure fresh data
            _context.ChangeTracker.Clear();
            
            return await _context.SchoolYears
                .AsNoTracking()
                .OrderByDescending(sy => sy.SchoolYearName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting all school years: {Message}", ex.Message);
            return new List<SchoolYear>();
        }
    }

    // Gets the currently active/open school year
    public async Task<SchoolYear?> GetActiveSchoolYearAsync()
    {
        try
        {
            // Clear change tracker to ensure fresh data
            _context.ChangeTracker.Clear();
            
            return await _context.SchoolYears
                .AsNoTracking()
                .FirstOrDefaultAsync(sy => sy.IsActive && sy.IsOpen);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting active school year: {Message}", ex.Message);
            return null;
        }
    }

    // Gets the currently active school year name as string
    public async Task<string?> GetActiveSchoolYearNameAsync()
    {
        var activeYear = await GetActiveSchoolYearAsync();
        return activeYear?.SchoolYearName;
    }

    // Checks if a school year is open
    public async Task<bool> IsSchoolYearOpenAsync(string schoolYear)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(schoolYear))
                return false;

            var sy = await _context.SchoolYears
                .FirstOrDefaultAsync(s => s.SchoolYearName == schoolYear);
            
            // If school year exists in database, return its IsOpen status
            if (sy != null)
            {
                return sy.IsOpen;
            }
            
            var activeSchoolYear = await GetActiveSchoolYearAsync();
            if (activeSchoolYear != null && activeSchoolYear.SchoolYearName == schoolYear)
            {
                return activeSchoolYear.IsOpen;
            }
            
            // Default to false if school year doesn't exist and isn't active
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking if school year is open: {Message}", ex.Message);
            return false;
        }
    }

    // Adds a new school year to the database
    public async Task<bool> AddSchoolYearAsync(string schoolYear, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(schoolYear))
                return false;

            if (!System.Text.RegularExpressions.Regex.IsMatch(schoolYear, @"^\d{4}-\d{4}$"))
                return false;

            // Check if school year already exists
            var exists = await _context.SchoolYears
                .AnyAsync(sy => sy.SchoolYearName == schoolYear);

            if (exists)
                return false;

            var newSchoolYear = new SchoolYear
            {
                SchoolYearName = schoolYear,
                IsActive = false,
                IsOpen = false,
                StartDate = startDate,
                EndDate = endDate,
                CreatedAt = DateTime.Now
            };

            _context.SchoolYears.Add(newSchoolYear);
            await _context.SaveChangesAsync();
            
            // Clear change tracker to ensure fresh data on next query
            _context.ChangeTracker.Clear();

            _logger?.LogInformation("School year {SchoolYear} added successfully", schoolYear);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error adding school year {SchoolYear}: {Message}", schoolYear, ex.Message);
            return false;
        }
    }

    // Removes a school year from the database
    public async Task<bool> RemoveSchoolYearAsync(string schoolYear)
    {
        try
        {
            var sy = await _context.SchoolYears
                .FirstOrDefaultAsync(s => s.SchoolYearName == schoolYear);

            if (sy == null)
                return false;

            // Don't allow removing active/open school year
            if (sy.IsActive || sy.IsOpen)
            {
                _logger?.LogWarning("Cannot remove active/open school year {SchoolYear}", schoolYear);
                return false;
            }

            _context.SchoolYears.Remove(sy);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("School year {SchoolYear} removed successfully", schoolYear);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error removing school year {SchoolYear}: {Message}", schoolYear, ex.Message);
            return false;
        }
    }

    // Opens a school year and closes all others
    public async Task<bool> OpenSchoolYearAsync(string schoolYear)
    {
        try
        {
            var sy = await _context.SchoolYears
                .FirstOrDefaultAsync(s => s.SchoolYearName == schoolYear);

            if (sy == null)
            {
                _logger?.LogWarning("School year {SchoolYear} not found", schoolYear);
                return false;
            }

            // Close all other school years
            var allSchoolYears = await _context.SchoolYears.ToListAsync();
            foreach (var year in allSchoolYears)
            {
                if (year.SchoolYearId != sy.SchoolYearId)
                {
                    bool wasOpen = year.IsOpen;
                    year.IsActive = false;
                    year.IsOpen = false;
                    if (wasOpen) 
                    {
                        year.ClosedAt = DateTime.Now;
                    }
                }
            }

            // Open the selected school year
            sy.IsActive = true;
            sy.IsOpen = true;
            sy.OpenedAt = DateTime.Now;
            sy.ClosedAt = null;

            await _context.SaveChangesAsync();

            _logger?.LogInformation("School year {SchoolYear} opened successfully", schoolYear);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error opening school year {SchoolYear}: {Message}", schoolYear, ex.Message);
            return false;
        }
    }

    // Closes a school year
    public async Task<bool> CloseSchoolYearAsync(string schoolYear)
    {
        try
        {
            var sy = await _context.SchoolYears
                .FirstOrDefaultAsync(s => s.SchoolYearName == schoolYear);

            if (sy == null)
            {
                _logger?.LogWarning("School year {SchoolYear} not found", schoolYear);
                return false;
            }

            sy.IsActive = false;
            sy.IsOpen = false;
            sy.ClosedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger?.LogInformation("School year {SchoolYear} closed successfully", schoolYear);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error closing school year {SchoolYear}: {Message}", schoolYear, ex.Message);
            return false;
        }
    }

    public string GetCurrentSchoolYear()
    {
        var currentYear = DateTime.Now.Year;
        var currentMonth = DateTime.Now.Month;

        if (currentMonth >= 6)
        {
            return $"{currentYear}-{currentYear + 1}";
        }
        else
        {
            return $"{currentYear - 1}-{currentYear}";
        }
    }
    public async Task RemoveFinishedSchoolYearsAsync()
    {
        try
        {
            var currentYear = DateTime.Now.Year;
            var currentMonth = DateTime.Now.Month;
            int currentEndYear;
            if (currentMonth >= 6)
            {
                currentEndYear = currentYear + 1;
            }
            else
            {
                currentEndYear = currentYear;
            }

            const int MIN_DAYS_SINCE_CLOSING = 30;

            // Get all school years
            var allSchoolYears = await _context.SchoolYears.ToListAsync();

            // Find and remove finished school years (not active/open)
            var finishedYears = allSchoolYears.Where(sy =>
            {
                if (string.IsNullOrWhiteSpace(sy.SchoolYearName))
                    return false;

                // Don't delete if still active or open
                if (sy.IsActive || sy.IsOpen)
                    return false;

                var parts = sy.SchoolYearName.Split('-');
                if (parts.Length != 2)
                    return false;

                if (int.TryParse(parts[1], out int endYear))
                {
                    if (endYear >= currentEndYear)
                        return false;

                    if (sy.ClosedAt.HasValue)
                    {
                        var daysSinceClosed = (DateTime.Now - sy.ClosedAt.Value).Days;
                        if (daysSinceClosed < MIN_DAYS_SINCE_CLOSING)
                        {
                            return false;
                        }
                    }
                    else if (sy.EndDate.HasValue)
                    {
                        var daysSinceEndDate = (DateTime.Now - sy.EndDate.Value).Days;
                        if (daysSinceEndDate < MIN_DAYS_SINCE_CLOSING)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }

                    return true;
                }

                return false;
            }).ToList();

            if (finishedYears.Any())
            {
                _context.SchoolYears.RemoveRange(finishedYears);
                await _context.SaveChangesAsync();
                _logger?.LogInformation("Removed {Count} finished school years that have been closed for at least {Days} days", 
                    finishedYears.Count, MIN_DAYS_SINCE_CLOSING);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error removing finished school years: {Message}", ex.Message);
        }
    }

}
