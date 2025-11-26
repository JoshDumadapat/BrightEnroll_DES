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

    // Gets recent reports for a teacher
    public async Task<List<ReportInfoDto>> GetRecentReportsAsync(int teacherId, int limit = 10)
    {
        try
        {
            var reports = await _context.TeacherReports
                .Where(r => r.TeacherId == teacherId)
                .OrderByDescending(r => r.GeneratedDate)
                .Take(limit)
                .ToListAsync();

            return reports.Select(r => new ReportInfoDto
            {
                ReportId = r.ReportId,
                Title = r.ReportTitle,
                GeneratedDate = r.GeneratedDate,
                ReportType = r.ReportType,
                FilePath = r.FilePath
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching recent reports: {Message}", ex.Message);
            throw new Exception($"Failed to fetch reports: {ex.Message}", ex);
        }
    }

    // Creates a report record in the database
    public async Task<TeacherReport> CreateReportAsync(
        int teacherId,
        string reportType,
        string schoolYear,
        string? gradeLevel = null,
        string? section = null,
        string? subject = null,
        string? gradingPeriod = null,
        string? studentId = null,
        string? filePath = null,
        string? generatedBy = null)
    {
        try
        {
            // Generate report title
            var title = GenerateReportTitle(reportType, gradeLevel, section, subject, gradingPeriod);

            var report = new TeacherReport
            {
                TeacherId = teacherId,
                ReportType = reportType,
                SchoolYear = schoolYear,
                GradeLevel = gradeLevel,
                Section = section,
                Subject = subject,
                GradingPeriod = gradingPeriod,
                StudentId = studentId,
                ReportTitle = title,
                FilePath = filePath,
                GeneratedDate = DateTime.Now,
                GeneratedBy = generatedBy
            };

            _context.TeacherReports.Add(report);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Report created: {ReportId} - {Title}", report.ReportId, title);
            return report;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating report: {Message}", ex.Message);
            throw new Exception($"Failed to create report: {ex.Message}", ex);
        }
    }

    // Generates report title based on parameters
    private string GenerateReportTitle(string reportType, string? gradeLevel, string? section, string? subject, string? gradingPeriod)
    {
        var parts = new List<string>();

        if (reportType == "ClassPerformance")
        {
            parts.Add("Class Performance");
            if (!string.IsNullOrWhiteSpace(gradeLevel))
            {
                parts.Add(gradeLevel);
                if (!string.IsNullOrWhiteSpace(section))
                    parts.Add(section);
            }
        }
        else if (reportType == "StudentGrades")
        {
            if (!string.IsNullOrWhiteSpace(gradeLevel))
            {
                parts.Add(gradeLevel);
                if (!string.IsNullOrWhiteSpace(section))
                    parts.Add(section);
            }
            if (!string.IsNullOrWhiteSpace(subject))
                parts.Add(subject);
        }
        else if (reportType == "SubjectSummary")
        {
            parts.Add("Subject Summary");
        }

        if (!string.IsNullOrWhiteSpace(gradingPeriod) && gradingPeriod != "All")
        {
            parts.Add(gradingPeriod);
        }

        return string.Join(" - ", parts);
    }

    // Gets class performance data for a report
    public async Task<ClassPerformanceReportDto> GetClassPerformanceDataAsync(
        int teacherId,
        string schoolYear,
        string gradeLevel,
        string? section = null,
        string? subject = null)
    {
        try
        {
            // Get class assignment
            var assignmentQuery = _context.ClassAssignments
                .Where(c => c.TeacherId == teacherId 
                    && c.SchoolYear == schoolYear 
                    && c.GradeLevel == gradeLevel
                    && c.Status == "Active");

            if (!string.IsNullOrWhiteSpace(section))
                assignmentQuery = assignmentQuery.Where(c => c.Section == section);
            if (!string.IsNullOrWhiteSpace(subject))
                assignmentQuery = assignmentQuery.Where(c => c.Subject == subject);

            var assignments = await assignmentQuery.ToListAsync();

            if (!assignments.Any())
            {
                return new ClassPerformanceReportDto
                {
                    SchoolYear = schoolYear,
                    GradeLevel = gradeLevel,
                    Section = section ?? "N/A",
                    Subject = subject ?? "All Subjects",
                    TotalStudents = 0,
                    Students = new List<StudentPerformanceDto>()
                };
            }

            // Get students for these classes
            var students = await _context.Students
                .Include(s => s.Guardian)
                .Where(s => s.SchoolYr == schoolYear && s.GradeLevel == gradeLevel)
                .ToListAsync();

            // Get grades for these students
            var studentIds = students.Select(s => s.StudentId).ToList();
            var grades = await _context.StudentGrades
                .Where(g => studentIds.Contains(g.StudentId) 
                    && g.SchoolYear == schoolYear
                    && (!string.IsNullOrWhiteSpace(subject) ? g.Subject == subject : true))
                .ToListAsync();

            // Build performance data
            var studentPerformance = new List<StudentPerformanceDto>();

            foreach (var student in students)
            {
                var studentGrades = grades.Where(g => g.StudentId == student.StudentId).ToList();
                var finalGrades = studentGrades
                    .Select(g => g.FinalGrade)
                    .Where(g => g.HasValue)
                    .Select(g => (double)g!.Value)
                    .ToList();

                var average = finalGrades.Any() ? finalGrades.Average() : 0.0;
                var passed = average >= 75;
                var failed = average < 75 && average > 0;

                studentPerformance.Add(new StudentPerformanceDto
                {
                    StudentId = student.StudentId,
                    Name = $"{student.FirstName} {student.MiddleName} {student.LastName}".Trim(),
                    Average = average,
                    Status = average == 0 ? "No Grades" : (passed ? "Passed" : "Failed")
                });
            }

            return new ClassPerformanceReportDto
            {
                SchoolYear = schoolYear,
                GradeLevel = gradeLevel,
                Section = section ?? "N/A",
                Subject = subject ?? "All Subjects",
                TotalStudents = students.Count,
                PassedCount = studentPerformance.Count(s => s.Status == "Passed"),
                FailedCount = studentPerformance.Count(s => s.Status == "Failed"),
                NoGradesCount = studentPerformance.Count(s => s.Status == "No Grades"),
                AverageGrade = studentPerformance.Where(s => s.Average > 0).Select(s => s.Average).DefaultIfEmpty(0).Average(),
                Students = studentPerformance.OrderBy(s => s.Name).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting class performance data: {Message}", ex.Message);
            throw new Exception($"Failed to get class performance data: {ex.Message}", ex);
        }
    }

    // Gets student grade data for a report
    public async Task<List<StudentGradeReportDto>> GetStudentGradeDataAsync(
        int teacherId,
        string schoolYear,
        string gradeLevel,
        string? section = null,
        string? subject = null,
        string? studentId = null,
        string? gradingPeriod = null)
    {
        try
        {
            var gradesQuery = _context.StudentGrades
                .Include(g => g.Student)
                .Where(g => g.SchoolYear == schoolYear
                    && g.GradeLevel == gradeLevel
                    && (!string.IsNullOrWhiteSpace(subject) ? g.Subject == subject : true)
                    && (!string.IsNullOrWhiteSpace(studentId) ? g.StudentId == studentId : true));

            if (!string.IsNullOrWhiteSpace(section))
            {
                gradesQuery = gradesQuery.Where(g => g.Section == section);
            }

            var grades = await gradesQuery.ToListAsync();

            var result = new List<StudentGradeReportDto>();

            foreach (var grade in grades)
            {
                if (grade.Student == null) continue;

                var reportDto = new StudentGradeReportDto
                {
                    StudentId = grade.StudentId,
                    StudentName = $"{grade.Student.FirstName} {grade.Student.MiddleName} {grade.Student.LastName}".Trim(),
                    Subject = grade.Subject,
                    FirstQuarter = grade.FirstQuarter,
                    SecondQuarter = grade.SecondQuarter,
                    ThirdQuarter = grade.ThirdQuarter,
                    FourthQuarter = grade.FourthQuarter,
                    FinalGrade = grade.FinalGrade,
                    Remarks = grade.Remarks ?? "N/A"
                };

                // Filter by grading period if specified
                if (!string.IsNullOrWhiteSpace(gradingPeriod) && gradingPeriod != "All")
                {
                    if (gradingPeriod == "1st" && !grade.FirstQuarter.HasValue) continue;
                    if (gradingPeriod == "2nd" && !grade.SecondQuarter.HasValue) continue;
                    if (gradingPeriod == "3rd" && !grade.ThirdQuarter.HasValue) continue;
                    if (gradingPeriod == "4th" && !grade.FourthQuarter.HasValue) continue;
                    if (gradingPeriod == "Final" && !grade.FinalGrade.HasValue) continue;
                }

                result.Add(reportDto);
            }

            return result.OrderBy(r => r.StudentName).ThenBy(r => r.Subject).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting student grade data: {Message}", ex.Message);
            throw new Exception($"Failed to get student grade data: {ex.Message}", ex);
        }
    }

    // Gets subject summary data for a report
    public async Task<List<SubjectSummaryReportDto>> GetSubjectSummaryDataAsync(
        int teacherId,
        string schoolYear)
    {
        try
        {
            // Get teacher's class assignments
            var assignments = await _context.ClassAssignments
                .Where(c => c.TeacherId == teacherId 
                    && c.SchoolYear == schoolYear 
                    && c.Status == "Active")
                .ToListAsync();

            var subjects = assignments.Select(a => a.Subject).Distinct().ToList();

            var result = new List<SubjectSummaryReportDto>();

            foreach (var subject in subjects)
            {
                var subjectAssignments = assignments.Where(a => a.Subject == subject).ToList();
                var gradeLevels = subjectAssignments.Select(a => a.GradeLevel).Distinct().ToList();

                foreach (var gradeLevel in gradeLevels)
                {
                    var levelAssignments = subjectAssignments.Where(a => a.GradeLevel == gradeLevel).ToList();

                    // Get students for this grade level
                    var students = await _context.Students
                        .Where(s => s.SchoolYr == schoolYear && s.GradeLevel == gradeLevel)
                        .ToListAsync();

                    // Get grades for this subject and grade level
                    var studentIds = students.Select(s => s.StudentId).ToList();
                    var grades = await _context.StudentGrades
                        .Where(g => studentIds.Contains(g.StudentId) 
                            && g.SchoolYear == schoolYear
                            && g.Subject == subject
                            && g.GradeLevel == gradeLevel)
                        .ToListAsync();

                    var finalGrades = grades
                        .Select(g => g.FinalGrade)
                        .Where(g => g.HasValue)
                        .Select(g => (double)g!.Value)
                        .ToList();

                    result.Add(new SubjectSummaryReportDto
                    {
                        Subject = subject,
                        GradeLevel = gradeLevel,
                        TotalStudents = students.Count,
                        StudentsWithGrades = grades.Select(g => g.StudentId).Distinct().Count(),
                        AverageGrade = finalGrades.Any() ? finalGrades.Average() : 0.0,
                        PassedCount = grades.Count(g => g.Remarks == "PASSED"),
                        FailedCount = grades.Count(g => g.Remarks == "FAILED")
                    });
                }
            }

            return result.OrderBy(r => r.Subject).ThenBy(r => r.GradeLevel).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting subject summary data: {Message}", ex.Message);
            throw new Exception($"Failed to get subject summary data: {ex.Message}", ex);
        }
    }

    // Gets available school years from reports
    public async Task<List<string>> GetAvailableSchoolYearsAsync(int teacherId)
    {
        try
        {
            var schoolYears = await _context.TeacherReports
                .Where(r => r.TeacherId == teacherId)
                .Select(r => r.SchoolYear)
                .Distinct()
                .OrderByDescending(sy => sy)
                .ToListAsync();

            // Also include school years from class assignments
            var assignmentYears = await _context.ClassAssignments
                .Where(c => c.TeacherId == teacherId)
                .Select(c => c.SchoolYear)
                .Distinct()
                .ToListAsync();

            return schoolYears.Union(assignmentYears).Distinct().OrderByDescending(sy => sy).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching school years: {Message}", ex.Message);
            return new List<string>();
        }
    }
}

// DTOs for reports
public class ReportInfoDto
{
    public int ReportId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string? FilePath { get; set; }
}

public class ClassPerformanceReportDto
{
    public string SchoolYear { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public int NoGradesCount { get; set; }
    public double AverageGrade { get; set; }
    public List<StudentPerformanceDto> Students { get; set; } = new();
}

public class StudentPerformanceDto
{
    public string StudentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Average { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class StudentGradeReportDto
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public decimal? FirstQuarter { get; set; }
    public decimal? SecondQuarter { get; set; }
    public decimal? ThirdQuarter { get; set; }
    public decimal? FourthQuarter { get; set; }
    public decimal? FinalGrade { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

public class SubjectSummaryReportDto
{
    public string Subject { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public int StudentsWithGrades { get; set; }
    public double AverageGrade { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
}

