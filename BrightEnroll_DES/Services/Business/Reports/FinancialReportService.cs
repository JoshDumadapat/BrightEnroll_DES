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

    /// <summary>
    /// Get financial data grouped by time period (daily, monthly, yearly) for line charts
    /// </summary>
    public async Task<List<FinancialTimeSeries>> GetFinancialTimeSeriesAsync(DateTime? fromDate = null, DateTime? toDate = null, string period = "monthly")
    {
        var defaultFromDate = fromDate ?? DateTime.Now.AddMonths(-6);
        var defaultToDate = toDate ?? DateTime.Now;

        var paymentsQuery = _context.StudentPayments
            .Where(p => p.CreatedAt >= defaultFromDate && p.CreatedAt <= defaultToDate)
            .AsQueryable();

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

    /// <summary>
    /// Get payment status distribution for pie chart
    /// </summary>
    public async Task<List<PaymentStatusDistribution>> GetPaymentStatusDistributionAsync(DateTime? fromDate = null, DateTime? toDate = null)
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

        var students = await query.ToListAsync();

        var statusGroups = students
            .GroupBy(s => s.PaymentStatus ?? "Unpaid")
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

