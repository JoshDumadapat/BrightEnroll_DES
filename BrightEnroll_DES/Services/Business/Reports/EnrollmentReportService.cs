using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BrightEnroll_DES.Services.Business.Reports;

public class EnrollmentReportService
{
    private readonly AppDbContext _context;

    public EnrollmentReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EnrollmentSummary> GetEnrollmentSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null, string? gradeLevel = null)
    {
        var query = _context.Students.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.DateRegistered >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.DateRegistered <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(gradeLevel))
        {
            query = query.Where(s => s.GradeLevel == gradeLevel);
        }

        var totalEnrolled = await query.CountAsync(s => s.Status == "Enrolled");
        var newStudents = await query.CountAsync(s => s.Status == "Enrolled" && (s.StudentType == "New" || s.StudentType.Contains("New")));
        var reEnrolled = await query.CountAsync(s => s.Status == "Enrolled" && (s.StudentType == "Re-enrolled" || s.StudentType.Contains("Re-enrolled")));
        var pending = await query.CountAsync(s => s.Status == "Pending");

        return new EnrollmentSummary
        {
            TotalEnrolled = totalEnrolled,
            NewStudents = newStudents,
            ReEnrolled = reEnrolled,
            Pending = pending
        };
    }

    public async Task<List<EnrollmentByGrade>> GetEnrollmentByGradeAsync(string? schoolYear = null)
    {
        var query = _context.Students.AsQueryable();

        if (!string.IsNullOrWhiteSpace(schoolYear))
        {
            query = query.Where(s => s.SchoolYr == schoolYear);
        }

        var enrollmentByGrade = await query
            .Where(s => s.Status == "Enrolled")
            .GroupBy(s => s.GradeLevel ?? "Unknown")
            .Select(g => new EnrollmentByGrade
            {
                GradeLevel = g.Key,
                Count = g.Count()
            })
            .OrderBy(e => e.GradeLevel)
            .ToListAsync();

        return enrollmentByGrade;
    }

    public async Task<List<EnrollmentDetail>> GetEnrollmentDetailsAsync(DateTime? fromDate = null, DateTime? toDate = null, string? gradeLevel = null, string? status = null)
    {
        var query = _context.Students
            .Include(s => s.Guardian)
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.DateRegistered >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.DateRegistered <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(gradeLevel))
        {
            query = query.Where(s => s.GradeLevel == gradeLevel);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(s => s.Status == status);
        }

        var students = await query
            .Select(s => new EnrollmentDetail
            {
                StudentId = s.StudentId,
                Name = $"{s.FirstName} {s.MiddleName} {s.LastName}".Trim(),
                GradeLevel = s.GradeLevel ?? "N/A",
                Section = "N/A", // TODO: Join with sections table when available
                Status = s.Status,
                EnrollmentDate = s.DateRegistered,
                GuardianName = s.Guardian != null ? $"{s.Guardian.FirstName} {s.Guardian.LastName}" : "N/A"
            })
            .ToListAsync();

        return students;
    }

    /// <summary>
    /// Get enrollment data grouped by time period for line charts
    /// </summary>
    public async Task<List<EnrollmentTimeSeries>> GetEnrollmentTimeSeriesAsync(DateTime? fromDate = null, DateTime? toDate = null, string period = "monthly")
    {
        var defaultFromDate = fromDate ?? DateTime.Now.AddMonths(-6);
        var defaultToDate = toDate ?? DateTime.Now;

        var query = _context.Students
            .Where(s => s.DateRegistered >= defaultFromDate && s.DateRegistered <= defaultToDate)
            .AsQueryable();

        List<EnrollmentTimeSeries> result = new List<EnrollmentTimeSeries>();

        if (period == "daily")
        {
            var enrollments = await query
                .GroupBy(s => s.DateRegistered.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            foreach (var enrollment in enrollments)
            {
                result.Add(new EnrollmentTimeSeries
                {
                    Period = enrollment.Date.ToString("MMM dd"),
                    PeriodKey = enrollment.Date,
                    Count = enrollment.Count
                });
            }
        }
        else if (period == "monthly")
        {
            var enrollments = await query
                .GroupBy(s => new { Year = s.DateRegistered.Year, Month = s.DateRegistered.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            foreach (var enrollment in enrollments)
            {
                var periodDate = new DateTime(enrollment.Year, enrollment.Month, 1);
                result.Add(new EnrollmentTimeSeries
                {
                    Period = periodDate.ToString("MMM yyyy"),
                    PeriodKey = periodDate,
                    Count = enrollment.Count
                });
            }
        }
        else if (period == "yearly")
        {
            var enrollments = await query
                .GroupBy(s => s.DateRegistered.Year)
                .Select(g => new { Year = g.Key, Count = g.Count() })
                .OrderBy(x => x.Year)
                .ToListAsync();

            foreach (var enrollment in enrollments)
            {
                var periodDate = new DateTime(enrollment.Year, 1, 1);
                result.Add(new EnrollmentTimeSeries
                {
                    Period = enrollment.Year.ToString(),
                    PeriodKey = periodDate,
                    Count = enrollment.Count
                });
            }
        }

        return result;
    }
}

public class EnrollmentTimeSeries
{
    public string Period { get; set; } = string.Empty;
    public DateTime PeriodKey { get; set; }
    public int Count { get; set; }
}

public class EnrollmentSummary
{
    public int TotalEnrolled { get; set; }
    public int NewStudents { get; set; }
    public int ReEnrolled { get; set; }
    public int Pending { get; set; }
}

public class EnrollmentByGrade
{
    public string GradeLevel { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class EnrollmentDetail
{
    public string StudentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnrollmentDate { get; set; }
    public string GuardianName { get; set; } = string.Empty;
}

