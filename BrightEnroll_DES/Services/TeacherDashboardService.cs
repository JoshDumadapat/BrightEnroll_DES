using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services;

// Handles dashboard statistics and data for teachers
public class TeacherDashboardService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TeacherDashboardService>? _logger;

    public TeacherDashboardService(AppDbContext context, ILogger<TeacherDashboardService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    // Gets dashboard statistics for a teacher
    public async Task<TeacherDashboardStatsDto> GetDashboardStatsAsync(int teacherId, string? schoolYear = null)
    {
        try
        {
            // Get current school year if not provided
            if (string.IsNullOrWhiteSpace(schoolYear))
            {
                schoolYear = await GetCurrentSchoolYearAsync(teacherId);
            }

            // Get total classes (active class assignments)
            var totalClasses = await _context.ClassAssignments
                .Where(c => c.TeacherId == teacherId && c.Status == "Active")
                .CountAsync();

            // Get total students in teacher's classes
            var assignments = await _context.ClassAssignments
                .Where(c => c.TeacherId == teacherId && c.Status == "Active")
                .ToListAsync();

            var gradeLevels = assignments.Select(a => a.GradeLevel).Distinct().ToList();
            var schoolYears = assignments.Select(a => a.SchoolYear).Distinct().ToList();

            var totalStudents = await _context.Students
                .Where(s => gradeLevels.Contains(s.GradeLevel ?? "") 
                    && schoolYears.Contains(s.SchoolYr ?? ""))
                .Select(s => s.StudentId)
                .Distinct()
                .CountAsync();

            // Get pending grades count (students without complete grades for current school year)
            var pendingGrades = await GetPendingGradesCountAsync(teacherId, schoolYear);

            // Get classes today count
            var classesToday = await GetClassesTodayCountAsync(teacherId);

            return new TeacherDashboardStatsDto
            {
                TotalClasses = totalClasses,
                TotalStudents = totalStudents,
                PendingGrades = pendingGrades,
                ClassesToday = classesToday
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching dashboard stats: {Message}", ex.Message);
            throw new Exception($"Failed to fetch dashboard statistics: {ex.Message}", ex);
        }
    }

    // Gets today's schedule for a teacher
    public async Task<List<TodayScheduleDto>> GetTodayScheduleAsync(int teacherId)
    {
        try
        {
            var today = DateTime.Now;
            var dayOfWeek = today.DayOfWeek.ToString();

            // Get teacher's active class assignments
            var assignments = await _context.ClassAssignments
                .Include(c => c.Schedules)
                .Where(c => c.TeacherId == teacherId && c.Status == "Active")
                .ToListAsync();

            var todaySchedule = new List<TodayScheduleDto>();

            foreach (var assignment in assignments)
            {
                // Find schedules for today's day of week
                var schedules = assignment.Schedules
                    .Where(s => s.DayOfWeek.Equals(dayOfWeek, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var schedule in schedules)
                {
                    todaySchedule.Add(new TodayScheduleDto
                    {
                        Subject = assignment.Subject,
                        GradeLevel = assignment.GradeLevel,
                        Section = assignment.Section ?? "N/A",
                        Time = $"{schedule.StartTime} - {schedule.EndTime}",
                        Room = assignment.Room ?? "TBA"
                    });
                }
            }

            // Sort by start time
            return todaySchedule
                .OrderBy(s => s.Time)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching today's schedule: {Message}", ex.Message);
            throw new Exception($"Failed to fetch today's schedule: {ex.Message}", ex);
        }
    }

    // Gets current school year for a teacher (most recent active assignment)
    private async Task<string> GetCurrentSchoolYearAsync(int teacherId)
    {
        try
        {
            var schoolYear = await _context.ClassAssignments
                .Where(c => c.TeacherId == teacherId && c.Status == "Active")
                .OrderByDescending(c => c.SchoolYear)
                .Select(c => c.SchoolYear)
                .FirstOrDefaultAsync();

            return schoolYear ?? DateTime.Now.Year + "-" + (DateTime.Now.Year + 1);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting current school year: {Message}", ex.Message);
            return DateTime.Now.Year + "-" + (DateTime.Now.Year + 1);
        }
    }

    // Gets count of pending grades (students without complete grades)
    private async Task<int> GetPendingGradesCountAsync(int teacherId, string schoolYear)
    {
        try
        {
            // Get teacher's class assignments
            var assignments = await _context.ClassAssignments
                .Where(c => c.TeacherId == teacherId && c.Status == "Active" && c.SchoolYear == schoolYear)
                .ToListAsync();

            if (!assignments.Any())
                return 0;

            var gradeLevels = assignments.Select(a => a.GradeLevel).Distinct().ToList();
            var subjects = assignments.Select(a => a.Subject).Distinct().ToList();

            // Get students in these classes
            var students = await _context.Students
                .Where(s => s.SchoolYr == schoolYear && gradeLevels.Contains(s.GradeLevel ?? ""))
                .Select(s => s.StudentId)
                .ToListAsync();

            if (!students.Any())
                return 0;

            // Get students who have incomplete grades (missing at least one subject or quarter)
            var studentsWithGrades = await _context.StudentGrades
                .Where(g => students.Contains(g.StudentId) 
                    && g.SchoolYear == schoolYear
                    && subjects.Contains(g.Subject))
                .Select(g => new { g.StudentId, g.Subject })
                .Distinct()
                .ToListAsync();

            // Count students who are missing grades for at least one subject
            var expectedGradeCount = students.Count * subjects.Count;
            var actualGradeCount = studentsWithGrades.Count;
            var pendingCount = expectedGradeCount - actualGradeCount;

            // Also check for incomplete quarters
            var incompleteGrades = await _context.StudentGrades
                .Where(g => students.Contains(g.StudentId) 
                    && g.SchoolYear == schoolYear
                    && subjects.Contains(g.Subject)
                    && (g.FirstQuarter == null || g.SecondQuarter == null || 
                        g.ThirdQuarter == null || g.FourthQuarter == null))
                .CountAsync();

            return Math.Max(pendingCount, incompleteGrades);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error counting pending grades: {Message}", ex.Message);
            return 0;
        }
    }

    // Gets count of classes scheduled for today
    private async Task<int> GetClassesTodayCountAsync(int teacherId)
    {
        try
        {
            var today = DateTime.Now;
            var dayOfWeek = today.DayOfWeek.ToString();

            // Get teacher's active class assignments with schedules
            var assignments = await _context.ClassAssignments
                .Include(c => c.Schedules)
                .Where(c => c.TeacherId == teacherId && c.Status == "Active")
                .ToListAsync();

            var classesToday = 0;

            foreach (var assignment in assignments)
            {
                var hasScheduleToday = assignment.Schedules
                    .Any(s => s.DayOfWeek.Equals(dayOfWeek, StringComparison.OrdinalIgnoreCase));

                if (hasScheduleToday)
                    classesToday++;
            }

            return classesToday;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error counting classes today: {Message}", ex.Message);
            return 0;
        }
    }
}

// DTOs for dashboard
public class TeacherDashboardStatsDto
{
    public int TotalClasses { get; set; }
    public int TotalStudents { get; set; }
    public int PendingGrades { get; set; }
    public int ClassesToday { get; set; }
}

public class TodayScheduleDto
{
    public string Subject { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
}

