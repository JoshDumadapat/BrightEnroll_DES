using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Services.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Students;

// Service for managing student enrollment status
public class EnrollmentStatusService
{
    private readonly AppDbContext _context;
    private readonly ILogger<EnrollmentStatusService>? _logger;
    private readonly IAuthService? _authService;

    public EnrollmentStatusService(AppDbContext context, ILogger<EnrollmentStatusService>? logger = null, IAuthService? authService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
        _authService = authService;
    }

    // Gets students by status
    public async Task<List<Student>> GetStudentsByStatusAsync(string status)
    {
        try
        {
            return await _context.Students
                .Include(s => s.Requirements)
                .Where(s => s.Status == status)
                .OrderByDescending(s => s.DateRegistered)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching students with status {Status}: {Message}", status, ex.Message);
            return new List<Student>();
        }
    }

    // Gets students by multiple statuses
    public async Task<List<Student>> GetStudentsByStatusesAsync(params string[] statuses)
    {
        try
        {
            return await _context.Students
                .Include(s => s.Requirements)
                .Where(s => statuses.Contains(s.Status))
                .OrderByDescending(s => s.DateRegistered)
                .ToListAsync();
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
}

