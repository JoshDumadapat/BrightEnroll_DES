using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.HR;

// Service for generating attendance reports
public class AttendanceReportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AttendanceReportService>? _logger;

    public AttendanceReportService(AppDbContext context, ILogger<AttendanceReportService>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    // Generate attendance summary report for period
    public async Task<AttendanceReportDto> GenerateAttendanceReportAsync(string period, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var timeRecords = await _context.TimeRecords
                .Include(tr => tr.User)
                .Where(tr => tr.Period == period)
                .ToListAsync();

            var report = new AttendanceReportDto
            {
                Period = period,
                StartDate = startDate,
                EndDate = endDate,
                TotalEmployees = timeRecords.Count,
                EmployeesWithRecords = timeRecords.Count(tr => tr.RegularHours > 0 || tr.OvertimeHours > 0 || tr.LateMinutes > 0 || tr.TotalDaysAbsent > 0)
            };

            // Calculate aggregate statistics
            report.TotalRegularHours = timeRecords.Sum(tr => tr.RegularHours);
            report.TotalOvertimeHours = timeRecords.Sum(tr => tr.OvertimeHours);
            report.TotalLateMinutes = timeRecords.Sum(tr => tr.LateMinutes);
            report.TotalAbsentDays = timeRecords.Sum(tr => tr.TotalDaysAbsent);
            report.AverageRegularHours = timeRecords.Any() ? timeRecords.Average(tr => tr.RegularHours) : 0;
            report.AverageOvertimeHours = timeRecords.Any() ? timeRecords.Average(tr => tr.OvertimeHours) : 0;
            report.AverageLateMinutes = timeRecords.Any() ? (int)timeRecords.Average(tr => tr.LateMinutes) : 0;
            report.AverageAbsentDays = timeRecords.Any() ? (decimal)timeRecords.Average(tr => tr.TotalDaysAbsent) : 0;

            // Employee details
            report.EmployeeDetails = timeRecords.Select(tr => new AttendanceEmployeeDetail
            {
                EmployeeId = tr.UserId,
                EmployeeName = tr.User != null ? $"{tr.User.FirstName} {tr.User.LastName}" : $"User ID: {tr.UserId}",
                EmployeeSystemId = tr.User?.SystemId ?? "N/A",
                Role = tr.User?.UserRole ?? "N/A",
                RegularHours = tr.RegularHours,
                OvertimeHours = tr.OvertimeHours,
                LateMinutes = tr.LateMinutes,
                AbsentDays = tr.TotalDaysAbsent,
                LeaveDays = tr.LeaveDays
            }).OrderBy(e => e.EmployeeName).ToList();

            // Generate recommendations
            report.Recommendations = GenerateRecommendations(report);

            return report;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error generating attendance report for period {Period}", period);
            throw;
        }
    }

    /// <summary>
    /// Generates recommendations based on attendance patterns
    /// </summary>
    private List<AttendanceRecommendation> GenerateRecommendations(AttendanceReportDto report)
    {
        var recommendations = new List<AttendanceRecommendation>();

        // Check for high late minutes
        if (report.AverageLateMinutes > 30)
        {
            var employeesWithHighLates = report.EmployeeDetails
                .Where(e => e.LateMinutes > 30)
                .ToList();

            if (employeesWithHighLates.Any())
            {
                recommendations.Add(new AttendanceRecommendation
                {
                    Type = "High Lateness",
                    Severity = "High",
                    Title = "High Lateness Detected",
                    Description = $"{employeesWithHighLates.Count} employee(s) have more than 30 minutes of lateness.",
                    Details = string.Join(", ", employeesWithHighLates.Select(e => $"{e.EmployeeName} ({e.LateMinutes} mins)")),
                    Action = "Consider implementing stricter attendance policies or providing time management training. Review work schedules and consider flexible hours if appropriate.",
                    AffectedEmployees = employeesWithHighLates.Select(e => e.EmployeeName).ToList()
                });
            }
        }

        // Check for high absenteeism
        if (report.AverageAbsentDays > 2)
        {
            var employeesWithHighAbsenteeism = report.EmployeeDetails
                .Where(e => e.AbsentDays > 2)
                .ToList();

            if (employeesWithHighAbsenteeism.Any())
            {
                recommendations.Add(new AttendanceRecommendation
                {
                    Type = "High Absenteeism",
                    Severity = "High",
                    Title = "High Absenteeism Detected",
                    Description = $"{employeesWithHighAbsenteeism.Count} employee(s) have more than 2 absent days.",
                    Details = string.Join(", ", employeesWithHighAbsenteeism.Select(e => $"{e.EmployeeName} ({e.AbsentDays} days)")),
                    Action = "Schedule one-on-one meetings to understand reasons for absences. Review leave policies and consider health and wellness programs.",
                    AffectedEmployees = employeesWithHighAbsenteeism.Select(e => e.EmployeeName).ToList()
                });
            }
        }

        // Check for low regular hours (potential underutilization)
        if (report.AverageRegularHours < 150 && report.TotalEmployees > 0)
        {
            var employeesWithLowHours = report.EmployeeDetails
                .Where(e => e.RegularHours < 150)
                .ToList();

            if (employeesWithLowHours.Any())
            {
                recommendations.Add(new AttendanceRecommendation
                {
                    Type = "Low Work Hours",
                    Severity = "Medium",
                    Title = "Low Regular Hours Worked",
                    Description = $"{employeesWithLowHours.Count} employee(s) have less than 150 regular hours.",
                    Details = string.Join(", ", employeesWithLowHours.Select(e => $"{e.EmployeeName} ({e.RegularHours:F2} hrs)")),
                    Action = "Review work schedules and ensure employees are getting adequate hours. Consider redistributing workload if necessary.",
                    AffectedEmployees = employeesWithLowHours.Select(e => e.EmployeeName).ToList()
                });
            }
        }

        // Check for high overtime (potential burnout)
        if (report.AverageOvertimeHours > 20)
        {
            var employeesWithHighOvertime = report.EmployeeDetails
                .Where(e => e.OvertimeHours > 20)
                .ToList();

            if (employeesWithHighOvertime.Any())
            {
                recommendations.Add(new AttendanceRecommendation
                {
                    Type = "High Overtime",
                    Severity = "Medium",
                    Title = "High Overtime Hours",
                    Description = $"{employeesWithHighOvertime.Count} employee(s) have more than 20 overtime hours.",
                    Details = string.Join(", ", employeesWithHighOvertime.Select(e => $"{e.EmployeeName} ({e.OvertimeHours:F2} hrs)")),
                    Action = "Review workload distribution and consider hiring additional staff or redistributing tasks to prevent employee burnout.",
                    AffectedEmployees = employeesWithHighOvertime.Select(e => e.EmployeeName).ToList()
                });
            }
        }

        // Check for perfect attendance
        var employeesWithPerfectAttendance = report.EmployeeDetails
            .Where(e => e.LateMinutes == 0 && e.AbsentDays == 0 && e.RegularHours >= 150)
            .ToList();

        if (employeesWithPerfectAttendance.Any())
        {
            recommendations.Add(new AttendanceRecommendation
            {
                Type = "Recognition",
                Severity = "Low",
                Title = "Perfect Attendance",
                Description = $"{employeesWithPerfectAttendance.Count} employee(s) have perfect attendance.",
                Details = string.Join(", ", employeesWithPerfectAttendance.Select(e => e.EmployeeName)),
                Action = "Consider recognizing these employees for their excellent attendance. This can boost morale and encourage others.",
                AffectedEmployees = employeesWithPerfectAttendance.Select(e => e.EmployeeName).ToList()
            });
        }

        // If no issues found
        if (!recommendations.Any())
        {
            recommendations.Add(new AttendanceRecommendation
            {
                Type = "General",
                Severity = "Low",
                Title = "Attendance Status: Good",
                Description = "Overall attendance is within acceptable ranges.",
                Details = "No significant attendance issues detected for this period.",
                Action = "Continue monitoring attendance patterns and maintain current policies.",
                AffectedEmployees = new List<string>()
            });
        }

        return recommendations.OrderByDescending(r => r.Severity == "High" ? 3 : r.Severity == "Medium" ? 2 : 1).ToList();
    }
}

/// <summary>
/// DTO for attendance report data
/// </summary>
public class AttendanceReportDto
{
    public string Period { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int TotalEmployees { get; set; }
    public int EmployeesWithRecords { get; set; }
    public decimal TotalRegularHours { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalAbsentDays { get; set; }
    public decimal AverageRegularHours { get; set; }
    public decimal AverageOvertimeHours { get; set; }
    public int AverageLateMinutes { get; set; }
    public decimal AverageAbsentDays { get; set; }
    public List<AttendanceEmployeeDetail> EmployeeDetails { get; set; } = new();
    public List<AttendanceRecommendation> Recommendations { get; set; } = new();
}

/// <summary>
/// Employee attendance detail
/// </summary>
public class AttendanceEmployeeDetail
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeSystemId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public int LateMinutes { get; set; }
    public int AbsentDays { get; set; }
    public decimal LeaveDays { get; set; }
}

/// <summary>
/// Attendance recommendation
/// </summary>
public class AttendanceRecommendation
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // Low, Medium, High
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public List<string> AffectedEmployees { get; set; } = new();
}

