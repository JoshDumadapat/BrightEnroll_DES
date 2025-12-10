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

    // Gets financial summary: collections, expenses, and outstanding balances
    public async Task<FinancialSummary> GetFinancialSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        // Calculate total collections from StudentPayments table (read-only query)
        // Use date-only comparison to ensure we capture all payments for the full day
        var paymentsQuery = _context.StudentPayments.AsQueryable();
        if (fromDate.HasValue)
        {
            var fromDateOnly = fromDate.Value.Date;
            paymentsQuery = paymentsQuery.Where(p => p.CreatedAt >= fromDateOnly);
        }
        if (toDate.HasValue)
        {
            var toDateOnly = toDate.Value.Date.AddDays(1).AddTicks(-1); // End of day
            paymentsQuery = paymentsQuery.Where(p => p.CreatedAt <= toDateOnly);
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

        // Get expenses from Expenses table (read-only query)
        // Use date-only comparison to ensure we capture all expenses for the full day
        var expensesQuery = _context.Expenses.AsQueryable();
        if (fromDate.HasValue)
        {
            var fromDateOnly = fromDate.Value.Date;
            expensesQuery = expensesQuery.Where(e => e.ExpenseDate >= fromDateOnly);
        }
        if (toDate.HasValue)
        {
            var toDateOnly = toDate.Value.Date.AddDays(1).AddTicks(-1); // End of day
            expensesQuery = expensesQuery.Where(e => e.ExpenseDate <= toDateOnly);
        }

        var totalExpenses = await expensesQuery
            .Where(e => e.Status == "Approved")
            .SumAsync(e => (decimal?)e.Amount) ?? 0.00m;

        // Get payroll expenses (read-only query)
        // Payroll expense = GrossSalary (what company pays employees) + TotalCompanyContribution (company's share of benefits)
        // Use date-only comparison to ensure we capture all payroll for the full day
        var payrollQuery = _context.PayrollTransactions
            .Where(pt => pt.Status == "Paid")
            .AsQueryable();
        
        if (fromDate.HasValue)
        {
            var fromDateOnly = fromDate.Value.Date;
            payrollQuery = payrollQuery.Where(pt => 
                (pt.PaymentDate.HasValue && pt.PaymentDate.Value >= fromDateOnly) ||
                (!pt.PaymentDate.HasValue && pt.CreatedAt >= fromDateOnly));
        }
        if (toDate.HasValue)
        {
            var toDateOnly = toDate.Value.Date.AddDays(1).AddTicks(-1); // End of day
            payrollQuery = payrollQuery.Where(pt => 
                (pt.PaymentDate.HasValue && pt.PaymentDate.Value <= toDateOnly) ||
                (!pt.PaymentDate.HasValue && pt.CreatedAt <= toDateOnly));
        }

        var totalPayrollExpenses = await payrollQuery
            .SumAsync(pt => (decimal?)(pt.GrossSalary + pt.TotalCompanyContribution)) ?? 0.00m;

        // Total expenses = regular expenses + payroll expenses
        var totalAllExpenses = totalExpenses + totalPayrollExpenses;
        var netIncome = totalCollections - totalAllExpenses;

        return new FinancialSummary
        {
            TotalCollections = totalCollections,
            Outstanding = outstanding,
            TotalExpenses = totalAllExpenses, // Includes both regular expenses and payroll
            NetIncome = netIncome
        };
    }

    public async Task<List<ExpenseByCategory>> GetExpensesByCategoryAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        // Use date-only comparison to ensure we capture all expenses for the full day
        var query = _context.Expenses.AsQueryable();

        if (fromDate.HasValue)
        {
            var fromDateOnly = fromDate.Value.Date;
            query = query.Where(e => e.ExpenseDate >= fromDateOnly);
        }

        if (toDate.HasValue)
        {
            var toDateOnly = toDate.Value.Date.AddDays(1).AddTicks(-1); // End of day
            query = query.Where(e => e.ExpenseDate <= toDateOnly);
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

    // Gets financial data grouped by time period for charts (daily, monthly, or yearly)
    public async Task<List<FinancialTimeSeries>> GetFinancialTimeSeriesAsync(DateTime? fromDate = null, DateTime? toDate = null, string period = "monthly", string? schoolYear = null)
    {
        // Fetch collections from StudentPayments table (read-only query)
        // If no date filters provided, fetch ALL payments (not just last 6 months)
        // Use date-only comparison to ensure we capture all payments for the full day
        var paymentsQuery = _context.StudentPayments.AsQueryable();
        
        // Filter by school year if provided
        if (!string.IsNullOrEmpty(schoolYear))
        {
            // Get student IDs for the selected school year
            var studentIdsForSchoolYear = await _context.StudentSectionEnrollments
                .Where(e => e.SchoolYear == schoolYear)
                .Select(e => e.StudentId)
                .Distinct()
                .ToListAsync();
            
            paymentsQuery = paymentsQuery.Where(p => studentIdsForSchoolYear.Contains(p.StudentId));
        }
        
        if (fromDate.HasValue)
        {
            var fromDateOnly = fromDate.Value.Date;
            paymentsQuery = paymentsQuery.Where(p => p.CreatedAt >= fromDateOnly);
        }
        if (toDate.HasValue)
        {
            var toDateOnly = toDate.Value.Date.AddDays(1).AddTicks(-1); // End of day
            paymentsQuery = paymentsQuery.Where(p => p.CreatedAt <= toDateOnly);
        }
        // If both are null, fetch all payments (no date filter)

        // Fetch expenses from Expenses table (read-only query)
        // Use date-only comparison to ensure we capture all expenses for the full day
        var expensesQuery = _context.Expenses
            .Where(e => e.Status == "Approved")
            .AsQueryable();
        
        if (fromDate.HasValue)
        {
            var fromDateOnly = fromDate.Value.Date;
            expensesQuery = expensesQuery.Where(e => e.ExpenseDate >= fromDateOnly);
        }
        if (toDate.HasValue)
        {
            var toDateOnly = toDate.Value.Date.AddDays(1).AddTicks(-1); // End of day
            expensesQuery = expensesQuery.Where(e => e.ExpenseDate <= toDateOnly);
        }
        // If both are null, fetch all approved expenses (no date filter)

        // Fetch payroll expenses (read-only query)
        // Payroll expense = GrossSalary + TotalCompanyContribution (company's total cost)
        // Use date-only comparison to ensure we capture all payroll for the full day
        var payrollQuery = _context.PayrollTransactions
            .Where(pt => pt.Status == "Paid")
            .AsQueryable();
        
        if (fromDate.HasValue)
        {
            var fromDateOnly = fromDate.Value.Date;
            payrollQuery = payrollQuery.Where(pt => 
                (pt.PaymentDate.HasValue && pt.PaymentDate.Value >= fromDateOnly) ||
                (!pt.PaymentDate.HasValue && pt.CreatedAt >= fromDateOnly));
        }
        if (toDate.HasValue)
        {
            var toDateOnly = toDate.Value.Date.AddDays(1).AddTicks(-1); // End of day
            payrollQuery = payrollQuery.Where(pt => 
                (pt.PaymentDate.HasValue && pt.PaymentDate.Value <= toDateOnly) ||
                (!pt.PaymentDate.HasValue && pt.CreatedAt <= toDateOnly));
        }
        // If both are null, fetch all paid payroll transactions (no date filter)

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

            // Get payroll expenses grouped by date
            var payrollExpenses = await payrollQuery
                .GroupBy(pt => (pt.PaymentDate ?? pt.CreatedAt).Date)
                .Select(g => new { Date = g.Key, Amount = g.Sum(pt => pt.GrossSalary + pt.TotalCompanyContribution) })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var allDates = payments.Select(p => p.Date)
                .Union(expenses.Select(e => e.Date))
                .Union(payrollExpenses.Select(pe => pe.Date))
                .OrderBy(d => d)
                .Distinct()
                .ToList();

            foreach (var date in allDates)
            {
                var payment = payments.FirstOrDefault(p => p.Date == date);
                var expense = expenses.FirstOrDefault(e => e.Date == date);
                var payrollExpense = payrollExpenses.FirstOrDefault(pe => pe.Date == date);
                
                var totalExpenses = (expense?.Amount ?? 0) + (payrollExpense?.Amount ?? 0);
                
                result.Add(new FinancialTimeSeries
                {
                    Period = date.ToString("MMM dd"),
                    PeriodKey = date,
                    Collections = payment?.Amount ?? 0,
                    Expenses = totalExpenses,
                    NetIncome = (payment?.Amount ?? 0) - totalExpenses
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

            // Get payroll expenses grouped by year and month
            var payrollExpenses = await payrollQuery
                .GroupBy(pt => new { Year = (pt.PaymentDate ?? pt.CreatedAt).Year, Month = (pt.PaymentDate ?? pt.CreatedAt).Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Amount = g.Sum(pt => pt.GrossSalary + pt.TotalCompanyContribution) })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            var allPeriods = payments.Select(p => new { p.Year, p.Month })
                .Union(expenses.Select(e => new { e.Year, e.Month }))
                .Union(payrollExpenses.Select(pe => new { pe.Year, pe.Month }))
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .Distinct()
                .ToList();

            foreach (var periodItem in allPeriods)
            {
                var payment = payments.FirstOrDefault(p => p.Year == periodItem.Year && p.Month == periodItem.Month);
                var expense = expenses.FirstOrDefault(e => e.Year == periodItem.Year && e.Month == periodItem.Month);
                var payrollExpense = payrollExpenses.FirstOrDefault(pe => pe.Year == periodItem.Year && pe.Month == periodItem.Month);
                
                var totalExpenses = (expense?.Amount ?? 0) + (payrollExpense?.Amount ?? 0);
                var periodDate = new DateTime(periodItem.Year, periodItem.Month, 1);
                
                result.Add(new FinancialTimeSeries
                {
                    Period = periodDate.ToString("MMM"), // Just month abbreviation (Jan, Feb, Mar, etc.)
                    PeriodKey = periodDate,
                    Collections = payment?.Amount ?? 0,
                    Expenses = totalExpenses,
                    NetIncome = (payment?.Amount ?? 0) - totalExpenses
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

            // Get payroll expenses grouped by year
            var payrollExpenses = await payrollQuery
                .GroupBy(pt => (pt.PaymentDate ?? pt.CreatedAt).Year)
                .Select(g => new { Year = g.Key, Amount = g.Sum(pt => pt.GrossSalary + pt.TotalCompanyContribution) })
                .OrderBy(x => x.Year)
                .ToListAsync();

            var allYears = payments.Select(p => p.Year)
                .Union(expenses.Select(e => e.Year))
                .Union(payrollExpenses.Select(pe => pe.Year))
                .OrderBy(y => y)
                .Distinct()
                .ToList();

            foreach (var year in allYears)
            {
                var payment = payments.FirstOrDefault(p => p.Year == year);
                var expense = expenses.FirstOrDefault(e => e.Year == year);
                var payrollExpense = payrollExpenses.FirstOrDefault(pe => pe.Year == year);
                
                var totalExpenses = (expense?.Amount ?? 0) + (payrollExpense?.Amount ?? 0);
                var periodDate = new DateTime(year, 1, 1);
                
                result.Add(new FinancialTimeSeries
                {
                    Period = year.ToString(),
                    PeriodKey = periodDate,
                    Collections = payment?.Amount ?? 0,
                    Expenses = totalExpenses,
                    NetIncome = (payment?.Amount ?? 0) - totalExpenses
                });
            }
        }

        return result;
    }

    // Gets payment status distribution for pie chart from ledger data
    public async Task<List<PaymentStatusDistribution>> GetPaymentStatusDistributionAsync(DateTime? fromDate = null, DateTime? toDate = null, string? schoolYear = null)
    {
        // Fetch payment status from StudentLedger table (actual payment data)
        // This ensures we get real-time payment status based on actual payments vs charges
        var ledgersQuery = _context.StudentLedgers.AsQueryable();

        // Filter by school year if provided
        if (!string.IsNullOrEmpty(schoolYear))
        {
            ledgersQuery = ledgersQuery.Where(l => l.SchoolYear == schoolYear);
        }

        // If date filters are provided, filter by student registration date
        if (fromDate.HasValue || toDate.HasValue)
        {
            var studentIdsQuery = _context.Students.AsQueryable();
            
            if (fromDate.HasValue)
            {
                var fromDateOnly = fromDate.Value.Date;
                studentIdsQuery = studentIdsQuery.Where(s => s.DateRegistered.Date >= fromDateOnly);
            }
            
            if (toDate.HasValue)
            {
                var toDateOnly = toDate.Value.Date.AddDays(1).AddTicks(-1);
                studentIdsQuery = studentIdsQuery.Where(s => s.DateRegistered.Date <= toDateOnly);
            }

            var filteredStudentIds = await studentIdsQuery.Select(s => s.StudentId).ToListAsync();
            ledgersQuery = ledgersQuery.Where(l => filteredStudentIds.Contains(l.StudentId));
        }

        // Get all ledgers with their status
        var ledgers = await ledgersQuery
            .Select(l => new { l.StudentId, l.Status, l.SchoolYear })
            .ToListAsync();

        // Group by status and count
        // Use ledger status (calculated from actual payments) instead of student.PaymentStatus field
        var statusGroups = ledgers
            .GroupBy(l => l.Status ?? "Unpaid")
            .Select(g => new PaymentStatusDistribution
            {
                Status = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(s => s.Count)
            .ToList();

        // If no ledgers found but school year is specified, return empty result
        // Otherwise, if no school year filter, get status from students who have no ledger yet
        if (!statusGroups.Any() && string.IsNullOrEmpty(schoolYear))
        {
            // Fallback: use student PaymentStatus field for students without ledgers
            var studentsQuery = _context.Students.AsQueryable();
            
            if (fromDate.HasValue)
            {
                var fromDateOnly = fromDate.Value.Date;
                studentsQuery = studentsQuery.Where(s => s.DateRegistered.Date >= fromDateOnly);
            }
            
            if (toDate.HasValue)
            {
                var toDateOnly = toDate.Value.Date.AddDays(1).AddTicks(-1);
                studentsQuery = studentsQuery.Where(s => s.DateRegistered.Date <= toDateOnly);
            }

            var studentsWithoutLedgers = await studentsQuery
                .Where(s => !_context.StudentLedgers.Any(l => l.StudentId == s.StudentId))
                .ToListAsync();

            var fallbackGroups = studentsWithoutLedgers
                .GroupBy(s => s.PaymentStatus ?? "Unpaid")
                .Select(g => new PaymentStatusDistribution
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            statusGroups = fallbackGroups;
        }

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

