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

    /// <summary>
    /// Gets all available school years from database
    /// </summary>
    public async Task<List<string>> GetAvailableSchoolYearsAsync()
    {
        try
        {
            var schoolYears = await _context.SchoolYears
                .OrderBy(sy => sy.SchoolYearName)
                .Select(sy => sy.SchoolYearName)
                .ToListAsync();
            
            return schoolYears;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting available school years: {Message}", ex.Message);
            // Fallback to current school year calculation
            return new List<string> { GetCurrentSchoolYear() };
        }
    }

    /// <summary>
    /// Gets all school years as entities (for UI display)
    /// </summary>
    public async Task<List<SchoolYear>> GetAllSchoolYearsAsync()
    {
        try
        {
            return await _context.SchoolYears
                .OrderByDescending(sy => sy.SchoolYearName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting all school years: {Message}", ex.Message);
            return new List<SchoolYear>();
        }
    }

    /// <summary>
    /// Gets the currently active/open school year
    /// </summary>
    public async Task<SchoolYear?> GetActiveSchoolYearAsync()
    {
        try
        {
            return await _context.SchoolYears
                .FirstOrDefaultAsync(sy => sy.IsActive && sy.IsOpen);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting active school year: {Message}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Gets the currently active school year name (string)
    /// </summary>
    public async Task<string?> GetActiveSchoolYearNameAsync()
    {
        var activeYear = await GetActiveSchoolYearAsync();
        return activeYear?.SchoolYearName;
    }

    /// <summary>
    /// Checks if a school year is open
    /// </summary>
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
            
            // If school year doesn't exist, check if it's the active school year
            // This handles cases where the school year might be calculated but not yet in DB
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

    /// <summary>
    /// Adds a new school year to the database
    /// </summary>
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

            _logger?.LogInformation("School year {SchoolYear} added successfully", schoolYear);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error adding school year {SchoolYear}: {Message}", schoolYear, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Removes a school year from the database
    /// </summary>
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

    /// <summary>
    /// Opens a school year (closes all others)
    /// </summary>
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
                    year.IsActive = false;
                    year.IsOpen = false;
                    if (year.IsOpen) // Was previously open
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

    /// <summary>
    /// Closes a school year
    /// </summary>
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

    /// <summary>
    /// Gets the current school year based on date calculation (fallback method)
    /// </summary>
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

    // Legacy synchronous methods for backward compatibility
    // NOTE: These methods should be avoided in Blazor - use async versions instead
    // They use Task.Run to minimize deadlock risk, but async/await is preferred
    public List<string> GetAvailableSchoolYears()
    {
        try
        {
            // Use Task.Run to avoid deadlock in Blazor context
            return Task.Run(async () => await GetAvailableSchoolYearsAsync()).GetAwaiter().GetResult();
        }
        catch
        {
            return new List<string> { GetCurrentSchoolYear() };
        }
    }

    public bool AddSchoolYear(string schoolYear)
    {
        try
        {
            // Use Task.Run to avoid deadlock in Blazor context
            return Task.Run(async () => await AddSchoolYearAsync(schoolYear)).GetAwaiter().GetResult();
        }
        catch
        {
            return false;
        }
    }

    public bool RemoveSchoolYear(string schoolYear)
    {
        try
        {
            // Use Task.Run to avoid deadlock in Blazor context
            return Task.Run(async () => await RemoveSchoolYearAsync(schoolYear)).GetAwaiter().GetResult();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Removes finished school years (those that have ended)
    /// </summary>
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

            // Get all school years
            var allSchoolYears = await _context.SchoolYears.ToListAsync();

            // Find and remove finished school years (not active/open)
            var finishedYears = allSchoolYears.Where(sy =>
            {
                if (string.IsNullOrWhiteSpace(sy.SchoolYearName))
                    return false;

                var parts = sy.SchoolYearName.Split('-');
                if (parts.Length != 2)
                    return false;

                if (int.TryParse(parts[1], out int endYear))
                {
                    // Remove if the end year is before the current end year AND not active/open
                    return endYear < currentEndYear && !sy.IsActive && !sy.IsOpen;
                }

                return false;
            }).ToList();

            if (finishedYears.Any())
            {
                _context.SchoolYears.RemoveRange(finishedYears);
                await _context.SaveChangesAsync();
                _logger?.LogInformation("Removed {Count} finished school years", finishedYears.Count);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error removing finished school years: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Legacy synchronous method for backward compatibility
    /// NOTE: Uses Task.Run to avoid deadlocks in Blazor
    /// </summary>
    public void RemoveFinishedSchoolYears()
    {
        try
        {
            // Use Task.Run to avoid deadlock in Blazor context
            Task.Run(async () => await RemoveFinishedSchoolYearsAsync()).GetAwaiter().GetResult();
        }
        catch
        {
            // Silently fail for backward compatibility
        }
    }
}
