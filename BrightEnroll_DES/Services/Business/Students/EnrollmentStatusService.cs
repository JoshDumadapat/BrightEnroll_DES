using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Students;

// Service for managing student enrollment status
public class EnrollmentStatusService
{
    private readonly AppDbContext _context;
    private readonly ILogger<EnrollmentStatusService>? _logger;

    public EnrollmentStatusService(AppDbContext context, ILogger<EnrollmentStatusService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
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

            student.Status = newStatus;
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Student {StudentId} status updated to {NewStatus}", studentId, newStatus);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating student {StudentId} status to {NewStatus}: {Message}", studentId, newStatus, ex.Message);
            return false;
        }
    }
}

