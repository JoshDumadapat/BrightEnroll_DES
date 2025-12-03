using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BrightEnroll_DES.Services.QuestPDF;

public class EnrollmentStatisticsService
{
    private readonly AppDbContext _context;

    public EnrollmentStatisticsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EnrollmentStatistics> GetEnrollmentStatisticsAsync()
    {
        var allStudents = await _context.Students
            .Include(s => s.Requirements)
            .ToListAsync();
        var allPayments = await _context.StudentPayments.ToListAsync();

        // Count by status
        var newApplicants = allStudents.Count(s => s.Status == "Pending" || s.Status == "New Applicant");
        var forEnrollment = allStudents.Count(s => s.Status == "For Payment" || s.Status == "Partially Paid");
        var reEnrollment = allStudents.Count(s => s.StudentType != null && 
            (s.StudentType.Contains("Re-enrolled", StringComparison.OrdinalIgnoreCase) || 
             s.StudentType.Contains("Returnee", StringComparison.OrdinalIgnoreCase)));
        var enrolled = allStudents.Count(s => s.Status == "Enrolled");

        // Payment statistics
        var totalPaymentsCollected = allPayments.Sum(p => p.Amount);
        var fullyPaidCount = allStudents.Count(s => s.PaymentStatus == "Fully Paid" || s.PaymentStatus == "Paid");
        var partiallyPaidCount = allStudents.Count(s => s.PaymentStatus == "Partially Paid");
        var unpaidCount = allStudents.Count(s => s.PaymentStatus == "Unpaid" || string.IsNullOrWhiteSpace(s.PaymentStatus));

        // Enrollment by grade
        var enrollmentByGrade = allStudents
            .Where(s => s.Status == "Enrolled" && !string.IsNullOrWhiteSpace(s.GradeLevel))
            .GroupBy(s => s.GradeLevel ?? "Unknown")
            .Select(g => new GradeEnrollmentStat
            {
                GradeLevel = g.Key,
                Count = g.Count()
            })
            .OrderBy(g => g.GradeLevel)
            .ToList();

        // Enrollment by type
        var newStudents = allStudents.Count(s => s.StudentType != null && 
            s.StudentType.Contains("New", StringComparison.OrdinalIgnoreCase) && 
            !s.StudentType.Contains("Re-enrolled", StringComparison.OrdinalIgnoreCase));
        var transferee = allStudents.Count(s => s.StudentType != null && 
            s.StudentType.Contains("Transferee", StringComparison.OrdinalIgnoreCase));
        var returnee = allStudents.Count(s => s.StudentType != null && 
            s.StudentType.Contains("Returnee", StringComparison.OrdinalIgnoreCase));

        // Document verification
        var verifiedDocuments = allStudents.Count(s => 
            s.Requirements != null && 
            s.Requirements.Any(r => r.Status == "verified"));
        var pendingDocuments = allStudents.Count(s => 
            s.Requirements == null || 
            !s.Requirements.Any(r => r.Status == "verified"));

        // LRN statistics
        var withLRN = allStudents.Count(s => !string.IsNullOrWhiteSpace(s.Lrn) && s.Lrn != "N/A");
        var withoutLRN = allStudents.Count(s => string.IsNullOrWhiteSpace(s.Lrn) || s.Lrn == "N/A");

        return new EnrollmentStatistics
        {
            TotalStudents = allStudents.Count,
            NewApplicants = newApplicants,
            ForEnrollment = forEnrollment,
            ReEnrollment = reEnrollment,
            Enrolled = enrolled,
            TotalPaymentsCollected = totalPaymentsCollected,
            FullyPaidCount = fullyPaidCount,
            PartiallyPaidCount = partiallyPaidCount,
            UnpaidCount = unpaidCount,
            EnrollmentByGrade = enrollmentByGrade,
            NewStudents = newStudents,
            Transferee = transferee,
            Returnee = returnee,
            VerifiedDocuments = verifiedDocuments,
            PendingDocuments = pendingDocuments,
            WithLRN = withLRN,
            WithoutLRN = withoutLRN,
            GeneratedDate = DateTime.Now
        };
    }

    public async Task<EnrollmentStatistics> GetNewApplicantsStatisticsAsync()
    {
        var students = await _context.Students
            .Include(s => s.Requirements)
            .Where(s => s.Status == "Pending" || s.Status == "New Applicant")
            .ToListAsync();

        var payments = await _context.StudentPayments
            .Where(p => students.Select(s => s.StudentId).Contains(p.StudentId))
            .ToListAsync();

        return new EnrollmentStatistics
        {
            TotalStudents = students.Count,
            NewApplicants = students.Count,
            TotalPaymentsCollected = payments.Sum(p => p.Amount),
            EnrollmentByGrade = students
                .Where(s => !string.IsNullOrWhiteSpace(s.GradeLevel))
                .GroupBy(s => s.GradeLevel ?? "Unknown")
                .Select(g => new GradeEnrollmentStat { GradeLevel = g.Key, Count = g.Count() })
                .OrderBy(g => g.GradeLevel)
                .ToList(),
            GeneratedDate = DateTime.Now
        };
    }

    public async Task<EnrollmentStatistics> GetForEnrollmentStatisticsAsync()
    {
        var students = await _context.Students
            .Include(s => s.Requirements)
            .Where(s => s.Status == "For Payment" || s.Status == "Partially Paid")
            .ToListAsync();

        var payments = await _context.StudentPayments
            .Where(p => students.Select(s => s.StudentId).Contains(p.StudentId))
            .ToListAsync();

        return new EnrollmentStatistics
        {
            TotalStudents = students.Count,
            ForEnrollment = students.Count,
            TotalPaymentsCollected = payments.Sum(p => p.Amount),
            FullyPaidCount = students.Count(s => s.PaymentStatus == "Fully Paid"),
            PartiallyPaidCount = students.Count(s => s.PaymentStatus == "Partially Paid"),
            UnpaidCount = students.Count(s => s.PaymentStatus == "Unpaid" || string.IsNullOrWhiteSpace(s.PaymentStatus)),
            EnrollmentByGrade = students
                .Where(s => !string.IsNullOrWhiteSpace(s.GradeLevel))
                .GroupBy(s => s.GradeLevel ?? "Unknown")
                .Select(g => new GradeEnrollmentStat { GradeLevel = g.Key, Count = g.Count() })
                .OrderBy(g => g.GradeLevel)
                .ToList(),
            GeneratedDate = DateTime.Now
        };
    }

    public async Task<EnrollmentStatistics> GetReEnrollmentStatisticsAsync()
    {
        var students = await _context.Students
            .Include(s => s.Requirements)
            .Where(s => s.StudentType != null && 
                (s.StudentType.Contains("Re-enrolled", StringComparison.OrdinalIgnoreCase) || 
                 s.StudentType.Contains("Returnee", StringComparison.OrdinalIgnoreCase)))
            .ToListAsync();

        var payments = await _context.StudentPayments
            .Where(p => students.Select(s => s.StudentId).Contains(p.StudentId))
            .ToListAsync();

        return new EnrollmentStatistics
        {
            TotalStudents = students.Count,
            ReEnrollment = students.Count,
            TotalPaymentsCollected = payments.Sum(p => p.Amount),
            EnrollmentByGrade = students
                .Where(s => !string.IsNullOrWhiteSpace(s.GradeLevel))
                .GroupBy(s => s.GradeLevel ?? "Unknown")
                .Select(g => new GradeEnrollmentStat { GradeLevel = g.Key, Count = g.Count() })
                .OrderBy(g => g.GradeLevel)
                .ToList(),
            GeneratedDate = DateTime.Now
        };
    }

    public async Task<EnrollmentStatistics> GetEnrolledStatisticsAsync()
    {
        var students = await _context.Students
            .Include(s => s.Requirements)
            .Where(s => s.Status == "Enrolled")
            .ToListAsync();

        var payments = await _context.StudentPayments
            .Where(p => students.Select(s => s.StudentId).Contains(p.StudentId))
            .ToListAsync();

        return new EnrollmentStatistics
        {
            TotalStudents = students.Count,
            Enrolled = students.Count,
            TotalPaymentsCollected = payments.Sum(p => p.Amount),
            EnrollmentByGrade = students
                .Where(s => !string.IsNullOrWhiteSpace(s.GradeLevel))
                .GroupBy(s => s.GradeLevel ?? "Unknown")
                .Select(g => new GradeEnrollmentStat { GradeLevel = g.Key, Count = g.Count() })
                .OrderBy(g => g.GradeLevel)
                .ToList(),
            WithLRN = students.Count(s => !string.IsNullOrWhiteSpace(s.Lrn) && s.Lrn != "N/A"),
            WithoutLRN = students.Count(s => string.IsNullOrWhiteSpace(s.Lrn) || s.Lrn == "N/A"),
            VerifiedDocuments = students.Count(s => 
                s.Requirements != null && s.Requirements.Any(r => r.Status == "verified")),
            GeneratedDate = DateTime.Now
        };
    }
}

public class EnrollmentStatistics
{
    public int TotalStudents { get; set; }
    public int NewApplicants { get; set; }
    public int ForEnrollment { get; set; }
    public int ReEnrollment { get; set; }
    public int Enrolled { get; set; }
    public decimal TotalPaymentsCollected { get; set; }
    public int FullyPaidCount { get; set; }
    public int PartiallyPaidCount { get; set; }
    public int UnpaidCount { get; set; }
    public List<GradeEnrollmentStat> EnrollmentByGrade { get; set; } = new();
    public int NewStudents { get; set; }
    public int Transferee { get; set; }
    public int Returnee { get; set; }
    public int VerifiedDocuments { get; set; }
    public int PendingDocuments { get; set; }
    public int WithLRN { get; set; }
    public int WithoutLRN { get; set; }
    public DateTime GeneratedDate { get; set; }
}

public class GradeEnrollmentStat
{
    public string GradeLevel { get; set; } = string.Empty;
    public int Count { get; set; }
}

