using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BrightEnroll_DES.Services.Business.Reports;

public class FinancialReportService
{
    private readonly AppDbContext _context;

    public FinancialReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<FinancialSummary> GetFinancialSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        // Calculate total collections from actual student payment records
        var paymentsQuery = _context.StudentPayments.AsQueryable();
        if (fromDate.HasValue)
        {
            paymentsQuery = paymentsQuery.Where(p => p.CreatedAt >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            paymentsQuery = paymentsQuery.Where(p => p.CreatedAt <= toDate.Value);
        }

        var totalCollections = await paymentsQuery
            .SumAsync(p => (decimal?)p.Amount) ?? 0.00m;

        // Calculate outstanding balances: (Total Fees - Total Payments) for all students
        var outstanding = 0.00m;

        // Get all students with their grade levels
        var students = await _context.Students
            .Where(s => !string.IsNullOrWhiteSpace(s.GradeLevel))
            .ToListAsync();

        // Get all active fees grouped by grade level name
        var fees = await _context.Fees
            .Include(f => f.GradeLevel)
            .Where(f => f.IsActive)
            .ToListAsync();

        // Get total payments per student
        var studentPayments = await _context.StudentPayments
            .GroupBy(p => p.StudentId)
            .Select(g => new { StudentId = g.Key, TotalPaid = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(x => x.StudentId, x => x.TotalPaid);

        // Calculate outstanding for each student
        foreach (var student in students)
        {
            // Find matching fee for student's grade level
            if (string.IsNullOrWhiteSpace(student.GradeLevel)) continue;
            var studentGradeLevel = student.GradeLevel.Trim();
            var fee = fees.FirstOrDefault(f =>
            {
                var gradeName = f.GradeLevel?.GradeLevelName?.Trim() ?? "";
                
                // Exact match
                if (gradeName.Equals(studentGradeLevel, StringComparison.OrdinalIgnoreCase))
                    return true;
                
                // Match without "Grade " prefix
                var gradeNameNoPrefix = gradeName.Replace("Grade ", "", StringComparison.OrdinalIgnoreCase).Trim();
                var studentGradeNoPrefix = studentGradeLevel.Replace("Grade ", "", StringComparison.OrdinalIgnoreCase).Trim();
                if (gradeNameNoPrefix.Equals(studentGradeNoPrefix, StringComparison.OrdinalIgnoreCase))
                    return true;
                
                // Match just the number (e.g., "1" matches "Grade 1")
                if (int.TryParse(gradeNameNoPrefix, out var gradeNum) && 
                    int.TryParse(studentGradeNoPrefix, out var studentNum) && 
                    gradeNum == studentNum)
                    return true;
                
                return false;
            });

            if (fee != null)
            {
                decimal totalFee = fee.TuitionFee + fee.MiscFee + fee.OtherFee;
                decimal totalPaid = studentPayments.GetValueOrDefault(student.StudentId, 0m);
                decimal studentOutstanding = totalFee - totalPaid;
                
                if (studentOutstanding > 0)
                {
                    outstanding += studentOutstanding;
                }
            }
        }

        // Get expenses
        var expensesQuery = _context.Expenses.AsQueryable();
        if (fromDate.HasValue)
        {
            expensesQuery = expensesQuery.Where(e => e.ExpenseDate >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            expensesQuery = expensesQuery.Where(e => e.ExpenseDate <= toDate.Value);
        }

        var totalExpenses = await expensesQuery
            .Where(e => e.Status == "Approved")
            .SumAsync(e => (decimal?)e.Amount) ?? 0.00m;

        var netIncome = totalCollections - totalExpenses;

        return new FinancialSummary
        {
            TotalCollections = totalCollections,
            Outstanding = outstanding,
            TotalExpenses = totalExpenses,
            NetIncome = netIncome
        };
    }

    public async Task<List<ExpenseByCategory>> GetExpensesByCategoryAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Expenses.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.ExpenseDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.ExpenseDate <= toDate.Value);
        }

        var expensesByCategory = await query
            .Where(e => e.Status == "Approved")
            .GroupBy(e => e.Category ?? "Uncategorized")
            .Select(g => new ExpenseByCategory
            {
                Category = g.Key,
                Amount = g.Sum(e => e.Amount),
                Count = g.Count()
            })
            .OrderByDescending(e => e.Amount)
            .ToListAsync();

        return expensesByCategory;
    }

    // Get financial data grouped by time period for line charts
    public async Task<List<FinancialTimeSeries>> GetFinancialTimeSeriesAsync(DateTime? fromDate = null, DateTime? toDate = null, string period = "monthly", string? schoolYear = null)
    {
        var defaultFromDate = fromDate ?? DateTime.Now.AddMonths(-6);
        var defaultToDate = toDate ?? DateTime.Now;

        var paymentsQuery = _context.StudentPayments
            .Where(p => p.CreatedAt >= defaultFromDate && p.CreatedAt <= defaultToDate)
            .AsQueryable();

        // Filter payments by school year if provided
        if (!string.IsNullOrEmpty(schoolYear))
        {
            var studentIdsForSchoolYear = await _context.StudentSectionEnrollments
                .Where(e => e.SchoolYear == schoolYear)
                .Select(e => e.StudentId)
                .Distinct()
                .ToListAsync();
            
            paymentsQuery = paymentsQuery.Where(p => studentIdsForSchoolYear.Contains(p.StudentId));
        }

        var expensesQuery = _context.Expenses
            .Where(e => e.ExpenseDate >= defaultFromDate && e.ExpenseDate <= defaultToDate && e.Status == "Approved")
            .AsQueryable();

        List<FinancialTimeSeries> result = new List<FinancialTimeSeries>();

        if (period == "daily")
        {
            var payments = await paymentsQuery
                .GroupBy(p => p.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Amount = g.Sum(p => p.Amount) })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var expenses = await expensesQuery
                .GroupBy(e => e.ExpenseDate.Date)
                .Select(g => new { Date = g.Key, Amount = g.Sum(e => e.Amount) })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var allDates = payments.Select(p => p.Date)
                .Union(expenses.Select(e => e.Date))
                .OrderBy(d => d)
                .Distinct()
                .ToList();

            foreach (var date in allDates)
            {
                var payment = payments.FirstOrDefault(p => p.Date == date);
                var expense = expenses.FirstOrDefault(e => e.Date == date);
                result.Add(new FinancialTimeSeries
                {
                    Period = date.ToString("MMM dd"),
                    PeriodKey = date,
                    Collections = payment?.Amount ?? 0,
                    Expenses = expense?.Amount ?? 0,
                    NetIncome = (payment?.Amount ?? 0) - (expense?.Amount ?? 0)
                });
            }
        }
        else if (period == "monthly")
        {
            var payments = await paymentsQuery
                .GroupBy(p => new { Year = p.CreatedAt.Year, Month = p.CreatedAt.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Amount = g.Sum(p => p.Amount) })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            var expenses = await expensesQuery
                .GroupBy(e => new { Year = e.ExpenseDate.Year, Month = e.ExpenseDate.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Amount = g.Sum(e => e.Amount) })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            var allPeriods = payments.Select(p => new { p.Year, p.Month })
                .Union(expenses.Select(e => new { e.Year, e.Month }))
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .Distinct()
                .ToList();

            foreach (var periodItem in allPeriods)
            {
                var payment = payments.FirstOrDefault(p => p.Year == periodItem.Year && p.Month == periodItem.Month);
                var expense = expenses.FirstOrDefault(e => e.Year == periodItem.Year && e.Month == periodItem.Month);
                var periodDate = new DateTime(periodItem.Year, periodItem.Month, 1);
                result.Add(new FinancialTimeSeries
                {
                    Period = periodDate.ToString("MMM"), // Just month abbreviation (Jan, Feb, Mar, etc.)
                    PeriodKey = periodDate,
                    Collections = payment?.Amount ?? 0,
                    Expenses = expense?.Amount ?? 0,
                    NetIncome = (payment?.Amount ?? 0) - (expense?.Amount ?? 0)
                });
            }
        }
        else if (period == "yearly")
        {
            var payments = await paymentsQuery
                .GroupBy(p => p.CreatedAt.Year)
                .Select(g => new { Year = g.Key, Amount = g.Sum(p => p.Amount) })
                .OrderBy(x => x.Year)
                .ToListAsync();

            var expenses = await expensesQuery
                .GroupBy(e => e.ExpenseDate.Year)
                .Select(g => new { Year = g.Key, Amount = g.Sum(e => e.Amount) })
                .OrderBy(x => x.Year)
                .ToListAsync();

            var allYears = payments.Select(p => p.Year)
                .Union(expenses.Select(e => e.Year))
                .OrderBy(y => y)
                .Distinct()
                .ToList();

            foreach (var year in allYears)
            {
                var payment = payments.FirstOrDefault(p => p.Year == year);
                var expense = expenses.FirstOrDefault(e => e.Year == year);
                var periodDate = new DateTime(year, 1, 1);
                result.Add(new FinancialTimeSeries
                {
                    Period = year.ToString(),
                    PeriodKey = periodDate,
                    Collections = payment?.Amount ?? 0,
                    Expenses = expense?.Amount ?? 0,
                    NetIncome = (payment?.Amount ?? 0) - (expense?.Amount ?? 0)
                });
            }
        }

        return result;
    }

    // Get payment status distribution for pie chart
    public async Task<List<PaymentStatusDistribution>> GetPaymentStatusDistributionAsync(DateTime? fromDate = null, DateTime? toDate = null, string? schoolYear = null)
    {
        // Get students based on filters
        var query = _context.Students.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.DateRegistered >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.DateRegistered <= toDate.Value);
        }

        // Filter by school year if provided
        List<string> studentIdsForSchoolYear = new();
        Dictionary<string, string> enrollmentStatusMap = new();
        
        if (!string.IsNullOrEmpty(schoolYear))
        {
            // Get all enrollments for this school year to get student IDs and their enrollment statuses
            var enrollments = await _context.StudentSectionEnrollments
                .Where(e => e.SchoolYear == schoolYear)
                .Select(e => new { e.StudentId, e.Status })
                .ToListAsync();
            
            studentIdsForSchoolYear = enrollments.Select(e => e.StudentId).Distinct().ToList();
            
            // Create a map of student enrollment statuses (prioritize payment-related statuses)
            foreach (var enrollment in enrollments)
            {
                if (!string.IsNullOrWhiteSpace(enrollment.StudentId))
                {
                    // Map enrollment status to payment status
                    var paymentStatus = enrollment.Status switch
                    {
                        "Fully Paid" => "Fully Paid",
                        "Partially Paid" => "Partially Paid",
                        "For Payment" => "Unpaid",
                        "Unpaid" => "Unpaid",
                        _ => null // Will be determined from ledger
                    };
                    
                    if (paymentStatus != null && (!enrollmentStatusMap.ContainsKey(enrollment.StudentId) || 
                        enrollmentStatusMap[enrollment.StudentId] == "Unpaid"))
                    {
                        enrollmentStatusMap[enrollment.StudentId] = paymentStatus;
                    }
                }
            }
            
            query = query.Where(s => studentIdsForSchoolYear.Contains(s.StudentId));
        }

        var students = await query.Select(s => s.StudentId).ToListAsync();

        // Early return if no students found
        if (students == null || !students.Any())
        {
            return new List<PaymentStatusDistribution>();
        }

        // Filter out any null student IDs
        var validStudentIds = students.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
        
        if (!validStudentIds.Any())
        {
            return new List<PaymentStatusDistribution>();
        }

        // Get all ledgers for these students, filtered by school year if provided
        // Ensure _context is not null (should be injected, but safety check)
        if (_context == null)
        {
            return new List<PaymentStatusDistribution>();
        }

        List<StudentLedger> ledgers;
        try
        {
            // Ensure validStudentIds is not empty to avoid query issues
            if (!validStudentIds.Any())
            {
                ledgers = new List<StudentLedger>();
            }
            else
            {
                // Build the query safely
                var ledgersQuery = _context.StudentLedgers
                    .Include(l => l.Charges)
                    .Include(l => l.Payments)
                    .Where(l => validStudentIds.Contains(l.StudentId));

                // Apply school year filter if provided
                if (!string.IsNullOrEmpty(schoolYear))
                {
                    ledgersQuery = ledgersQuery.Where(l => l.SchoolYear == schoolYear);
                }

                // Execute the query
                ledgers = await ledgersQuery.ToListAsync();
                
                // Ensure result is not null
                if (ledgers == null)
                {
                    ledgers = new List<StudentLedger>();
                }
            }
        }
        catch (Exception)
        {
            // If query fails for any reason, return empty list to prevent crash
            // Allow report generation even if ledger query fails
            ledgers = new List<StudentLedger>();
        }

        // Calculate payment status for each student based on their ledger(s) or enrollment status
        var studentStatusMap = new Dictionary<string, string>();

        // Use validStudentIds to avoid null references
        foreach (var studentId in validStudentIds)
        {
            // Get all ledgers for this student (filtered by school year if provided)
            var studentLedgers = ledgers.Where(l => l.StudentId == studentId).ToList();

            if (!studentLedgers.Any())
            {
                // No ledger - check enrollment status first, then default to Unpaid
                if (enrollmentStatusMap.ContainsKey(studentId))
                {
                    studentStatusMap[studentId] = enrollmentStatusMap[studentId];
                }
                else
                {
                    studentStatusMap[studentId] = "Unpaid";
                }
                continue;
            }

            // Calculate totals across all ledgers for this student
            decimal totalCharges = studentLedgers.Sum(l => l.Charges?.Sum(c => c.Amount) ?? 0m);
            decimal totalPayments = studentLedgers.Sum(l => l.Payments?.Sum(p => p.Amount) ?? 0m);
            decimal balance = totalCharges - totalPayments;

            // Determine payment status based on calculated values
            string paymentStatus;
            if (totalCharges == 0)
            {
                // No charges yet - check enrollment status or default to Unpaid
                if (enrollmentStatusMap.ContainsKey(studentId))
                {
                    paymentStatus = enrollmentStatusMap[studentId];
                }
                else
                {
                    paymentStatus = "Unpaid";
                }
            }
            else if (totalPayments == 0)
            {
                paymentStatus = "Unpaid";
            }
            else if (balance > 0)
            {
                paymentStatus = "Partially Paid";
            }
            else
            {
                paymentStatus = "Fully Paid";
            }

            studentStatusMap[studentId] = paymentStatus;
        }

        // Group by status and count
        var statusGroups = studentStatusMap
            .GroupBy(kvp => kvp.Value)
            .Select(g => new PaymentStatusDistribution
            {
                Status = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(s => s.Count)
            .ToList();

        return statusGroups;
    }
}

public class FinancialTimeSeries
{
    public string Period { get; set; } = string.Empty;
    public DateTime PeriodKey { get; set; }
    public decimal Collections { get; set; }
    public decimal Expenses { get; set; }
    public decimal NetIncome { get; set; }
}

public class PaymentStatusDistribution
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class FinancialSummary
{
    public decimal TotalCollections { get; set; }
    public decimal Outstanding { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetIncome { get; set; }
}

public class ExpenseByCategory
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

