using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Services.Authentication;
using BrightEnroll_DES.Services.Business.Academic;
using BrightEnroll_DES.Services.Business.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Students;

// Service for managing student enrollment status
public class EnrollmentStatusService
{
    private readonly AppDbContext _context;
    private readonly ILogger<EnrollmentStatusService>? _logger;
    private readonly IAuthService? _authService;
    private readonly SchoolYearService? _schoolYearService;
    private readonly StudentLedgerService? _ledgerService;

    public EnrollmentStatusService(
        AppDbContext context, 
        ILogger<EnrollmentStatusService>? logger = null, 
        IAuthService? authService = null,
        SchoolYearService? schoolYearService = null,
        StudentLedgerService? ledgerService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
        _authService = authService;
        _schoolYearService = schoolYearService;
        _ledgerService = ledgerService;
    }

    // Gets students by status (with optional school year filtering)
    public async Task<List<Student>> GetStudentsByStatusAsync(string status, string? schoolYear = null)
    {
        try
        {
            // Get active school year if not provided
            if (string.IsNullOrEmpty(schoolYear) && _schoolYearService != null)
            {
                schoolYear = await _schoolYearService.GetActiveSchoolYearNameAsync();
            }

            var query = _context.Students
                .Include(s => s.Requirements)
                .Where(s => s.Status == status);

            // Filter by school year if provided
            if (!string.IsNullOrEmpty(schoolYear))
            {
                query = query.Where(s => s.SchoolYr == schoolYear);
            }

            // Order by DateRegistered (DateRegistered is non-nullable, so no null check needed)
            return await query
                .OrderByDescending(s => s.DateRegistered)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching students with status {Status}: {Message}", status, ex.Message);
            return new List<Student>();
        }
    }

    // Gets students by multiple statuses (with optional school year filtering)
    // STRICTLY filters by enrollment records for the specified school year
    // For "For Enrollment" tab: Only shows students with enrollments for the active school year
    public async Task<List<Student>> GetStudentsByStatusesAsync(string? schoolYear = null, params string[] statuses)
    {
        try
        {
            // Get active school year if not provided
            if (string.IsNullOrEmpty(schoolYear) && _schoolYearService != null)
            {
                schoolYear = await _schoolYearService.GetActiveSchoolYearNameAsync();
            }

            if (!string.IsNullOrEmpty(schoolYear))
            {
                // STRICT FILTERING: Only get students who have enrollment records for this specific school year
                // This ensures closed school years don't show students from other years
                var enrollmentStudentIds = await _context.StudentSectionEnrollments
                    .Where(e => e.SchoolYear == schoolYear && statuses.Contains(e.Status))
                    .Select(e => e.StudentId)
                    .Distinct()
                    .ToListAsync();

                // Also include students who don't have enrollment records yet but match the status
                // This covers:
                // 1. New applicants who haven't been enrolled yet (status "Pending" or "For Payment")
                // 2. Re-enrolled students who don't have enrollment records yet (Status = "For Payment" but no enrollment record)
                // For re-enrolled students, check if they have enrollments for OTHER school years but not this one
                var allEnrollmentStudentIds = await _context.StudentSectionEnrollments
                    .Where(e => e.SchoolYear == schoolYear)
                    .Select(e => e.StudentId)
                    .Distinct()
                    .ToListAsync();

                var studentsWithoutEnrollments = await _context.Students
                    .Include(s => s.Requirements)
                    .Include(s => s.SectionEnrollments)
                    .Where(s => statuses.Contains(s.Status) && 
                               !allEnrollmentStudentIds.Contains(s.StudentId))
                    .ToListAsync();

                // Further filter: only include if they're registered for this school year OR have no enrollments at all
                // This ensures re-enrolled students (who might have old SchoolYr) are included
                studentsWithoutEnrollments = studentsWithoutEnrollments
                    .Where(s => s.SchoolYr == schoolYear || 
                               s.SchoolYr == null || 
                               (s.SectionEnrollments != null && s.SectionEnrollments.Any() && 
                                !s.SectionEnrollments.Any(e => e.SchoolYear == schoolYear)))
                    .ToList();

                // Get students with enrollments for this school year
                var studentsWithEnrollments = await _context.Students
                    .Include(s => s.Requirements)
                    .Include(s => s.SectionEnrollments)
                    .Where(s => enrollmentStudentIds.Contains(s.StudentId))
                    .ToListAsync();

                // Combine and return
                var allStudents = studentsWithEnrollments.Concat(studentsWithoutEnrollments)
                    .OrderByDescending(s => s.DateRegistered)
                    .ToList();

                return allStudents;
            }
            else
            {
                // No school year filter - return all students with matching statuses
                var query = _context.Students
                    .Include(s => s.Requirements)
                    .Where(s => statuses.Contains(s.Status));

                return await query
                    .OrderByDescending(s => s.DateRegistered)
                    .ToListAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching students with statuses {Statuses}: {Message}", string.Join(", ", statuses), ex.Message);
            return new List<Student>();
        }
    }

    // Updates student status
    public async Task<bool> UpdateStudentStatusAsync(string studentId, string newStatus)
    {
        try
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                _logger?.LogWarning("Student {StudentId} not found for status update", studentId);
                return false;
            }

            // Validate status transition (basic validation)
            var validStatuses = new[] { "Pending", "For Payment", "Eligible", "Enrolled", "Rejected by School", "Application Withdrawn" };
            if (!validStatuses.Contains(newStatus))
            {
                _logger?.LogWarning("Invalid status transition for student {StudentId}: {NewStatus}", studentId, newStatus);
                // Still allow the update, but log warning
            }

            var oldStatus = student.Status ?? "Pending";
            student.Status = newStatus;
            await _context.SaveChangesAsync();

            // If status changed to "For Payment", automatically create ledger for current school year
            if (newStatus == "For Payment" && oldStatus != "For Payment" && _ledgerService != null)
            {
                try
                {
                    await _ledgerService.GetOrCreateLedgerForCurrentSYAsync(studentId, student.GradeLevel);
                    _logger?.LogInformation("Created ledger for student {StudentId} when status changed to For Payment", studentId);
                }
                catch (Exception ledgerEx)
                {
                    _logger?.LogWarning(ledgerEx, "Failed to create ledger for student {StudentId} when status changed to For Payment: {Message}", 
                        studentId, ledgerEx.Message);
                    // Don't fail the status update if ledger creation fails
                }
            }

            // Create status log entry
            try
            {
                var changedByUserId = _authService?.CurrentUser?.user_ID;
                var changedByName = _authService?.CurrentUser != null
                    ? $"{_authService.CurrentUser.first_name} {_authService.CurrentUser.last_name}".Trim()
                    : null;

                var statusLog = new StudentStatusLog
                {
                    StudentId = studentId,
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    ChangedBy = changedByUserId,
                    ChangedByName = changedByName,
                    CreatedAt = DateTime.Now
                };

                _context.StudentStatusLogs.Add(statusLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception logEx)
            {
                _logger?.LogWarning(logEx, "Failed to create status log for student {StudentId}: {Message}", studentId, logEx.Message);
                // Don't fail the status update if logging fails
            }

            _logger?.LogInformation("Student {StudentId} status updated to {NewStatus}", studentId, newStatus);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating student {StudentId} status to {NewStatus}: {Message}", studentId, newStatus, ex.Message);
            return false;
        }
    }

    // Gets the latest status change log for a student
    public async Task<StudentStatusLog?> GetLatestStatusLogAsync(string studentId)
    {
        try
        {
            return await _context.StudentStatusLogs
                .Where(log => log.StudentId == studentId)
                .OrderByDescending(log => log.CreatedAt)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching latest status log for student {StudentId}: {Message}", studentId, ex.Message);
            return null;
        }
    }

    // Gets the latest status change logs for multiple students
    public async Task<Dictionary<string, StudentStatusLog>> GetLatestStatusLogsAsync(IEnumerable<string> studentIds)
    {
        try
        {
            var studentIdList = studentIds.ToList();
            if (!studentIdList.Any())
                return new Dictionary<string, StudentStatusLog>();

            // Get all logs for these students
            var allLogs = await _context.StudentStatusLogs
                .Where(log => studentIdList.Contains(log.StudentId))
                .ToListAsync();

            // Group by student ID and get the latest log for each
            var latestLogs = allLogs
                .GroupBy(log => log.StudentId)
                .Select(g => g.OrderByDescending(log => log.CreatedAt).First())
                .ToList();

            return latestLogs.ToDictionary(log => log.StudentId, log => log);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching status logs for students: {Message}", ex.Message);
            return new Dictionary<string, StudentStatusLog>();
        }
    }

    // Gets the first (earliest) status change log for multiple students (for registration tracking)
    public async Task<Dictionary<string, StudentStatusLog>> GetFirstStatusLogsAsync(IEnumerable<string> studentIds)
    {
        try
        {
            var studentIdList = studentIds.ToList();
            if (!studentIdList.Any())
                return new Dictionary<string, StudentStatusLog>();

            // Get all logs for these students
            var allLogs = await _context.StudentStatusLogs
                .Where(log => studentIdList.Contains(log.StudentId))
                .ToListAsync();

            // Group by student ID and get the first (earliest) log for each
            var firstLogs = allLogs
                .GroupBy(log => log.StudentId)
                .Select(g => g.OrderBy(log => log.CreatedAt).First())
                .ToList();

            return firstLogs.ToDictionary(log => log.StudentId, log => log);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching first status logs for students: {Message}", ex.Message);
            return new Dictionary<string, StudentStatusLog>();
        }
    }
}

