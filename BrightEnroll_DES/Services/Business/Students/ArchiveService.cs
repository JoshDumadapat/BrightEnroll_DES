using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace BrightEnroll_DES.Services.Business.Students;

// Service for managing archived students and employees
public class ArchiveService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ArchiveService>? _logger;

    public ArchiveService(AppDbContext context, ILogger<ArchiveService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    // Gets all archived students
    public async Task<List<Components.Pages.Admin.Archive.ArchivedStudent>> GetArchivedStudentsAsync()
    {
        try
        {
            // Get students with archived statuses
            // Include both "Application Withdrawn" and "Application Withdraw" (truncated version) for compatibility
            var archivedStatuses = new[] { "Rejected by School", "Application Withdrawn", "Application Withdraw", "Withdrawn", "Graduated", "Transferred" };
            
            // Clear change tracker to ensure fresh data
            _context.ChangeTracker.Clear();
            
            var archivedStudents = await _context.Students
                .Where(s => archivedStatuses.Contains(s.Status ?? ""))
                .OrderByDescending(s => s.DateRegistered)
                .Select(s => new Components.Pages.Admin.Archive.ArchivedStudent
                {
                    Id = s.StudentId,
                    Name = $"{s.FirstName} {s.MiddleName} {s.LastName}".Replace("  ", " ").Trim(),
                    LRN = s.Lrn ?? "N/A",
                    Date = s.DateRegistered.ToString("dd MMM yyyy"),
                    Status = s.Status ?? "N/A",
                    Reason = s.ArchiveReason != null ? s.ArchiveReason.Trim() : "", // Get archive reason from database, trim whitespace
                    ArchivedDate = s.DateRegistered.ToString("dd MMM yyyy")
                })
                .ToListAsync();

            return archivedStudents;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching archived students: {Message}", ex.Message);
            return new List<Components.Pages.Admin.Archive.ArchivedStudent>();
        }
    }

    // Gets all employees whose latest status is Inactive, with their latest inactive reason
    public async Task<List<Components.Pages.Admin.Archive.ArchivedEmployee>> GetArchivedEmployeesAsync()
    {
        try
        {
            const string inactiveStatus = "Inactive";

            // 1. Get all employees with Inactive status directly from the view
            // The view normalizes status to proper case, so we can do a direct comparison
            var inactiveEmployees = await _context.EmployeeDataViews
                .Where(e => e.Status == inactiveStatus)
                .ToListAsync();

            if (!inactiveEmployees.Any())
            {
                return new List<Components.Pages.Admin.Archive.ArchivedEmployee>();
            }

            // 2. Get the user IDs of inactive employees
            var inactiveUserIds = inactiveEmployees.Select(e => e.UserId).ToList();

            // 3. Get the latest inactive status log for each user (for reason and date)
            var latestInactiveLogsByUserId = await _context.UserStatusLogs
                .Where(l => inactiveUserIds.Contains(l.UserId) &&
                            l.NewStatus == inactiveStatus)
                .GroupBy(l => l.UserId)
                .Select(g => g.OrderByDescending(l => l.CreatedAt).First())
                .ToDictionaryAsync(l => l.UserId, l => l);

            // 4. Build the result list
            var result = inactiveEmployees
                .Select(emp =>
                {
                    latestInactiveLogsByUserId.TryGetValue(emp.UserId, out var latestInactiveLog);
                    var archivedDate = latestInactiveLog?.CreatedAt ?? (emp.DateHired ?? DateTime.Now);

                    return new Components.Pages.Admin.Archive.ArchivedEmployee
                    {
                        UserId = emp.UserId,
                        Id = emp.SystemId ?? emp.UserId.ToString(),
                        Name = emp.FullName
                               ?? ($"{emp.FirstName} {emp.MiddleName ?? ""} {emp.LastName}"
                                   .Replace("  ", " ")
                                   .Trim()),
                        Address = emp.FormattedAddress
                                  ?? $"{emp.HouseNo ?? ""} {emp.StreetName ?? ""}, {emp.Barangay ?? ""}, {emp.City ?? ""}, {emp.Province ?? ""}"
                                     .Replace("  ", " ")
                                     .Trim()
                                     .Trim(',', ' '),
                        Contact = emp.ContactNumber ?? string.Empty,
                        Email = emp.Email ?? string.Empty,
                        Role = emp.Role ?? string.Empty,
                        Status = emp.Status ?? inactiveStatus,
                        ArchivedDate = archivedDate.ToString("dd MMM yyyy"),
                        ArchivedReason = latestInactiveLog?.Reason ?? string.Empty
                    };
                })
                .OrderByDescending(e => DateTime.ParseExact(
                    e.ArchivedDate,
                    "dd MMM yyyy",
                    System.Globalization.CultureInfo.InvariantCulture))
                .ToList();

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching archived employees: {Message}", ex.Message);
            return new List<Components.Pages.Admin.Archive.ArchivedEmployee>();
        }
    }

    // Gets archived student by ID
    public async Task<Components.Pages.Admin.Archive.ArchivedStudent?> GetArchivedStudentAsync(string studentId)
    {
        try
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return null;

            return new Components.Pages.Admin.Archive.ArchivedStudent
            {
                Id = student.StudentId,
                Name = $"{student.FirstName} {student.MiddleName} {student.LastName}".Replace("  ", " ").Trim(),
                LRN = student.Lrn ?? "N/A",
                Date = student.DateRegistered.ToString("dd MMM yyyy"),
                Status = student.Status ?? "N/A",
                Reason = !string.IsNullOrWhiteSpace(student.ArchiveReason) ? student.ArchiveReason.Trim() : "", // Get archive reason from database, trim whitespace
                ArchivedDate = student.DateRegistered.ToString("dd MMM yyyy")
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching archived student {StudentId}: {Message}", studentId, ex.Message);
            return null;
        }
    }

    // Checks if student is archived
    public async Task<bool> IsStudentArchivedAsync(string studentId)
    {
        try
        {
            // Include both "Application Withdrawn" and "Application Withdraw" (truncated version) for compatibility
            var archivedStatuses = new[] { "Rejected by School", "Application Withdrawn", "Application Withdraw", "Withdrawn", "Graduated", "Transferred" };
            return await _context.Students
                .AnyAsync(s => s.StudentId == studentId && archivedStatuses.Contains(s.Status ?? ""));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking if student {StudentId} is archived: {Message}", studentId, ex.Message);
            return false;
        }
    }

    // Archives a student by updating their status
    // Note: Status should already be updated by UpdateStudentAsync, this method just confirms archiving
    public async Task ArchiveStudentAsync(string studentId, string archiveReason, string? notes = null, string? archivedBy = null, string? reason = null)
    {
        try
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                _logger?.LogWarning("Student {StudentId} not found for archiving", studentId);
                throw new InvalidOperationException($"Student {studentId} not found for archiving");
            }

            // CRITICAL: Normalize status values to fit database column (VARCHAR(20) or VARCHAR(50))
            // Map "Application Withdrawn" (21 chars) to "Withdrawn" (9 chars) to avoid truncation
            var statusToSet = (archiveReason ?? "").Trim();
            
            // Normalize long status values to shorter equivalents
            if (statusToSet.Equals("Application Withdrawn", StringComparison.OrdinalIgnoreCase) || 
                statusToSet.Equals("Application Withdraw", StringComparison.OrdinalIgnoreCase))
            {
                statusToSet = "Withdrawn";
                _logger?.LogInformation("Normalized 'Application Withdrawn' to 'Withdrawn' for student {StudentId}", studentId);
            }
            
            // Ensure status is max 50 characters (safety check for database column)
            if (statusToSet.Length > 50)
            {
                statusToSet = statusToSet.Substring(0, 50).Trim();
                _logger?.LogWarning("Archive status truncated from '{Original}' to '{Truncated}' to fit VARCHAR(50) column", archiveReason, statusToSet);
            }
            
            // Check if status is already one of the archived statuses - if so, no need to update
            var archivedStatuses = new[] { 
                "Rejected by School", 
                "Application Withdrawn", 
                "Application Withdraw", 
                "Withdrawn", 
                "Graduated", 
                "Transferred" 
            };
            
            var currentStatus = (student.Status ?? "").Trim();
            
            // Normalize current status for comparison (map "Application Withdrawn" to "Withdrawn")
            var currentStatusNormalized = currentStatus;
            if (currentStatus.Equals("Application Withdrawn", StringComparison.OrdinalIgnoreCase) || 
                currentStatus.Equals("Application Withdraw", StringComparison.OrdinalIgnoreCase))
            {
                currentStatusNormalized = "Withdrawn";
            }
            
            // Check if current status (normalized) matches any archived status
            var currentMatchesArchived = archivedStatuses.Any(s => 
            {
                var normalized = s;
                if (s.Equals("Application Withdrawn", StringComparison.OrdinalIgnoreCase) || 
                    s.Equals("Application Withdraw", StringComparison.OrdinalIgnoreCase))
                {
                    normalized = "Withdrawn";
                }
                return normalized.Equals(currentStatusNormalized, StringComparison.OrdinalIgnoreCase);
            });
            
            // If status already matches (normalized), we're done
            // But still save the reason if provided
            if (currentStatusNormalized.Equals(statusToSet, StringComparison.OrdinalIgnoreCase) || currentMatchesArchived)
            {
                // Save the archive reason if provided, even if status doesn't need updating
                // Also update status to ensure it's normalized (e.g., "Application Withdrawn" -> "Withdrawn")
                student.Status = statusToSet;
                
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    student.ArchiveReason = reason.Trim();
                    _logger?.LogInformation("Archive reason saved for student {StudentId} (status already archived): {Reason}", studentId, reason);
                }
                else if (string.IsNullOrWhiteSpace(student.ArchiveReason))
                {
                    // If no reason provided and ArchiveReason is empty, set a default based on status
                    student.ArchiveReason = $"Student archived with status: {statusToSet}";
                    _logger?.LogInformation("Default archive reason set for student {StudentId} (status already archived)", studentId);
                }
                
                await _context.SaveChangesAsync();
                return; // Success - status already correct, no need to update
            }

            // Status doesn't match - update it (shouldn't normally happen, but handle it)
            // Update status to archived status (normalized and guaranteed to fit)
            student.Status = statusToSet;
            
            // Save the archive reason if provided, or set default if empty
            if (!string.IsNullOrWhiteSpace(reason))
            {
                student.ArchiveReason = reason.Trim();
                _logger?.LogInformation("Archive reason saved for student {StudentId}: {Reason}", studentId, reason);
            }
            else if (string.IsNullOrWhiteSpace(student.ArchiveReason))
            {
                // If no reason provided and ArchiveReason is empty, set a default based on status
                student.ArchiveReason = $"Student archived with status: {statusToSet}";
                _logger?.LogInformation("Default archive reason set for student {StudentId}", studentId);
            }
            
            try
            {
                await _context.SaveChangesAsync();
                _logger?.LogInformation("Student {StudentId} status updated to '{Status}' during archiving", studentId, statusToSet);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                var innerEx = dbEx.InnerException;
                var sqlEx = innerEx as Microsoft.Data.SqlClient.SqlException;
                
                // If it's a truncation error, try with "Withdrawn" as fallback
                if (sqlEx != null && sqlEx.Number == 2628 && sqlEx.Message.Contains("status", StringComparison.OrdinalIgnoreCase))
                {
                    // Fallback to shorter status
                    student.Status = "Withdrawn";
                    await _context.SaveChangesAsync();
                    _logger?.LogWarning("Status changed to 'Withdrawn' due to truncation error for student {StudentId}", studentId);
                    return;
                }
                throw; // Re-throw if not a truncation error or if truncation didn't help
            }
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            var innerEx = dbEx.InnerException;
            var sqlEx = innerEx as Microsoft.Data.SqlClient.SqlException;
            
            // Check if this is a status truncation error
            if (sqlEx != null && sqlEx.Number == 2628 && sqlEx.Message.Contains("status", StringComparison.OrdinalIgnoreCase))
            {
                _logger?.LogError(dbEx, "Status column truncation error when archiving student {StudentId}. Please run Update_Status_Column_Size.sql", studentId);
                throw new Exception(
                    $"Failed to archive student: Status value is too long for database column. " +
                    $"Please run 'Database_Scripts/Update_Status_Column_Size.sql' to fix this. " +
                    $"Original error: {sqlEx.Message}", dbEx);
            }
            
            _logger?.LogError(dbEx, "Database error archiving student {StudentId}: {Message}", studentId, dbEx.Message);
            throw new Exception($"Failed to archive student: {dbEx.Message}", dbEx);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error archiving student {StudentId}: {Message}", studentId, ex.Message);
            throw;
        }
    }

    // Updates archive information
    public async Task UpdateArchiveAsync(string studentId, string archiveReason, string? reason = null)
    {
        try
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                _logger?.LogWarning("Student {StudentId} not found for archive update", studentId);
                return;
            }

            // Normalize status value (map "Application Withdrawn" to "Withdrawn")
            var statusToSet = (archiveReason ?? "").Trim();
            if (statusToSet.Equals("Application Withdrawn", StringComparison.OrdinalIgnoreCase) || 
                statusToSet.Equals("Application Withdraw", StringComparison.OrdinalIgnoreCase))
            {
                statusToSet = "Withdrawn";
            }
            
            // Ensure it fits in database column
            if (statusToSet.Length > 50)
            {
                statusToSet = statusToSet.Substring(0, 50).Trim();
            }

            student.Status = statusToSet;
            
            // Update archive reason if provided
            if (!string.IsNullOrWhiteSpace(reason))
            {
                student.ArchiveReason = reason;
                _logger?.LogInformation("Archive reason updated for student {StudentId}", studentId);
            }
            
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Archive updated for student {StudentId} with reason: {Reason}", studentId, statusToSet);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating archive for student {StudentId}: {Message}", studentId, ex.Message);
            throw;
        }
    }
}

