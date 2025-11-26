using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services;

// Handles report generation and management for teachers
public class ReportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReportService>? _logger;

    public ReportService(AppDbContext context, ILogger<ReportService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    // Get distinct school years where the teacher has grades/classes recorded
    public async Task<List<string>> GetAvailableSchoolYearsAsync(int teacherId)
    {
        try
        {
            return await _context.Grades
                .Include(g => g.Class)
                .Where(g => g.Class != null && g.Class.TeacherId == teacherId)
                .Select(g => g.SchoolYear)
                .Distinct()
                .OrderBy(y => y)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting available school years for teacher {TeacherId}", teacherId);
            throw;
        }
    }

    // Get recent reports for a teacher
    public async Task<List<ReportInfo>> GetRecentReportsByTeacherIdAsync(int teacherId, int limit = 10)
    {
        try
        {
            return await _context.Reports
                .Where(r => r.TeacherId == teacherId && r.IsActive)
                .OrderByDescending(r => r.GeneratedDate)
                .Take(limit)
                .Select(r => new ReportInfo
                {
                    ReportId = r.ReportId,
                    Title = r.ReportTitle,
                    GeneratedDate = r.GeneratedDate,
                    ReportType = r.ReportType,
                    FilePath = r.FilePath
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting recent reports for teacher {TeacherId}", teacherId);
            throw;
        }
    }

    // Save a generated report
    public async Task<Report> SaveReportAsync(Report report)
    {
        try
        {
            report.GeneratedDate = DateTime.Now;
            _context.Reports.Add(report);
            await _context.SaveChangesAsync();
            
            _logger?.LogInformation("Report saved with ID: {ReportId}", report.ReportId);
            return report;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving report");
            throw;
        }
    }

    // Get class performance report data
    public async Task<List<ClassPerformanceData>> GetClassPerformanceReportAsync(
        int teacherId, 
        int classId, 
        string schoolYear, 
        string? period = null)
    {
        try
        {
            var query = _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Class)
                .Where(g => g.Class != null && 
                           g.Class.TeacherId == teacherId && 
                           g.ClassId == classId && 
                           g.SchoolYear == schoolYear);

            var grades = await query.ToListAsync();

            var reportData = grades.Select(g => new ClassPerformanceData
            {
                StudentId = g.Student!.StudentId,
                StudentName = $"{g.Student.FirstName} {g.Student.MiddleName} {g.Student.LastName}".Trim() + 
                             (string.IsNullOrEmpty(g.Student.Suffix) ? "" : $" {g.Student.Suffix}"),
                FirstQuarter = g.FirstQuarter.HasValue ? (double?)g.FirstQuarter.Value : null,
                SecondQuarter = g.SecondQuarter.HasValue ? (double?)g.SecondQuarter.Value : null,
                ThirdQuarter = g.ThirdQuarter.HasValue ? (double?)g.ThirdQuarter.Value : null,
                FourthQuarter = g.FourthQuarter.HasValue ? (double?)g.FourthQuarter.Value : null,
                FinalGrade = g.FinalGrade.HasValue ? (double?)g.FinalGrade.Value : null,
                Remarks = g.Remarks ?? string.Empty
            }).ToList();

            // Filter by period if specified
            if (!string.IsNullOrWhiteSpace(period) && period != "All")
            {
                switch (period)
                {
                    case "1st":
                        reportData = reportData.Where(r => r.FirstQuarter.HasValue).ToList();
                        break;
                    case "2nd":
                        reportData = reportData.Where(r => r.SecondQuarter.HasValue).ToList();
                        break;
                    case "3rd":
                        reportData = reportData.Where(r => r.ThirdQuarter.HasValue).ToList();
                        break;
                    case "4th":
                        reportData = reportData.Where(r => r.FourthQuarter.HasValue).ToList();
                        break;
                    case "Final":
                        reportData = reportData.Where(r => r.FinalGrade.HasValue).ToList();
                        break;
                }
            }

            return reportData.OrderBy(r => r.StudentName).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting class performance report for class {ClassId}", classId);
            throw;
        }
    }

    // Get student grade report data
    public async Task<List<StudentGradeReportData>> GetStudentGradeReportAsync(
        int teacherId,
        int? classId,
        string? schoolYear,
        string? subject,
        string? studentSearch,
        string? period = null)
    {
        try
        {
            var query = _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Class)
                .Where(g => g.Class != null && g.Class.TeacherId == teacherId);

            if (classId.HasValue)
            {
                query = query.Where(g => g.ClassId == classId.Value);
            }

            if (!string.IsNullOrWhiteSpace(schoolYear))
            {
                query = query.Where(g => g.SchoolYear == schoolYear);
            }

            if (!string.IsNullOrWhiteSpace(subject))
            {
                query = query.Where(g => g.Class!.Subject == subject);
            }

            if (!string.IsNullOrWhiteSpace(studentSearch))
            {
                query = query.Where(g => 
                    g.Student!.StudentId.Contains(studentSearch) ||
                    g.Student.FirstName.Contains(studentSearch) ||
                    g.Student.LastName.Contains(studentSearch));
            }

            var grades = await query.ToListAsync();

            var reportData = grades.Select(g => new StudentGradeReportData
            {
                StudentId = g.Student!.StudentId,
                StudentName = $"{g.Student.FirstName} {g.Student.MiddleName} {g.Student.LastName}".Trim() + 
                             (string.IsNullOrEmpty(g.Student.Suffix) ? "" : $" {g.Student.Suffix}"),
                Subject = g.Class!.Subject,
                ClassName = $"{g.Class.GradeLevel} - {g.Class.Section}",
                FirstQuarter = g.FirstQuarter.HasValue ? (double?)g.FirstQuarter.Value : null,
                SecondQuarter = g.SecondQuarter.HasValue ? (double?)g.SecondQuarter.Value : null,
                ThirdQuarter = g.ThirdQuarter.HasValue ? (double?)g.ThirdQuarter.Value : null,
                FourthQuarter = g.FourthQuarter.HasValue ? (double?)g.FourthQuarter.Value : null,
                FinalGrade = g.FinalGrade.HasValue ? (double?)g.FinalGrade.Value : null,
                Remarks = g.Remarks ?? string.Empty,
                SchoolYear = g.SchoolYear
            }).ToList();

            // Filter by period if specified
            if (!string.IsNullOrWhiteSpace(period) && period != "All")
            {
                switch (period)
                {
                    case "1st":
                        reportData = reportData.Where(r => r.FirstQuarter.HasValue).ToList();
                        break;
                    case "2nd":
                        reportData = reportData.Where(r => r.SecondQuarter.HasValue).ToList();
                        break;
                    case "3rd":
                        reportData = reportData.Where(r => r.ThirdQuarter.HasValue).ToList();
                        break;
                    case "4th":
                        reportData = reportData.Where(r => r.FourthQuarter.HasValue).ToList();
                        break;
                    case "Final":
                        reportData = reportData.Where(r => r.FinalGrade.HasValue).ToList();
                        break;
                }
            }

            return reportData.OrderBy(r => r.StudentName).ThenBy(r => r.Subject).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting student grade report");
            throw;
        }
    }

    // Get subject summary report data
    public async Task<List<SubjectSummaryData>> GetSubjectSummaryReportAsync(
        int teacherId,
        string? schoolYear)
    {
        try
        {
            var query = _context.Grades
                .Include(g => g.Class)
                .Where(g => g.Class != null && g.Class.TeacherId == teacherId);

            if (!string.IsNullOrWhiteSpace(schoolYear))
            {
                query = query.Where(g => g.SchoolYear == schoolYear);
            }

            var grades = await query.ToListAsync();

            var summaryData = grades
                .GroupBy(g => new { g.Class!.Subject, g.SchoolYear })
                .Select(g => new SubjectSummaryData
                {
                    Subject = g.Key.Subject,
                    SchoolYear = g.Key.SchoolYear,
                    TotalStudents = g.Select(x => x.StudentId).Distinct().Count(),
                    StudentsWithGrades = g.Where(x => x.FinalGrade.HasValue).Select(x => x.StudentId).Distinct().Count(),
                    AverageGrade = g.Where(x => x.FinalGrade.HasValue).Average(x => (double)x.FinalGrade!.Value),
                    PassedCount = g.Where(x => x.Remarks == "PASSED").Select(x => x.StudentId).Distinct().Count(),
                    FailedCount = g.Where(x => x.Remarks == "FAILED").Select(x => x.StudentId).Distinct().Count(),
                    IncompleteCount = g.Where(x => x.Remarks == "INCOMPLETE").Select(x => x.StudentId).Distinct().Count()
                })
                .OrderBy(s => s.Subject)
                .ToList();

            return summaryData;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting subject summary report for teacher {TeacherId}", teacherId);
            throw;
        }
    }

    // Delete a report (soft delete)
    public async Task<bool> DeleteReportAsync(int reportId)
    {
        try
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null)
            {
                return false;
            }

            report.IsActive = false;
            await _context.SaveChangesAsync();
            
            _logger?.LogInformation("Report deleted (soft) with ID: {ReportId}", reportId);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting report {ReportId}", reportId);
            throw;
        }
    }
}

// DTOs for report data
public class ReportInfo
{
    public int ReportId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string? FilePath { get; set; }
}

public class ClassPerformanceData
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public double? FirstQuarter { get; set; }
    public double? SecondQuarter { get; set; }
    public double? ThirdQuarter { get; set; }
    public double? FourthQuarter { get; set; }
    public double? FinalGrade { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

public class StudentGradeReportData
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public double? FirstQuarter { get; set; }
    public double? SecondQuarter { get; set; }
    public double? ThirdQuarter { get; set; }
    public double? FourthQuarter { get; set; }
    public double? FinalGrade { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public string SchoolYear { get; set; } = string.Empty;
}

public class SubjectSummaryData
{
    public string Subject { get; set; } = string.Empty;
    public string SchoolYear { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public int StudentsWithGrades { get; set; }
    public double AverageGrade { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public int IncompleteCount { get; set; }
}

