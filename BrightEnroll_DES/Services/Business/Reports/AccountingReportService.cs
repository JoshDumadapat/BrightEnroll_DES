using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BrightEnroll_DES.Services.Business.Reports;

public class AccountingReportService
{
    private readonly AppDbContext _context;

    public AccountingReportService(AppDbContext context)
    {
        _context = context;
    }

    #region Accounts Receivable Report

    public async Task<List<AccountsReceivableRecord>> GetAccountsReceivableAsync(DateTime? asOfDate = null)
    {
        var cutoffDate = asOfDate ?? DateTime.Now;

        // Get all students with their grade levels
        var students = await _context.Students
            .Where(s => !string.IsNullOrWhiteSpace(s.GradeLevel))
            .ToListAsync();

        // Get all active fees grouped by grade level
        var fees = await _context.Fees
            .Include(f => f.GradeLevel)
            .Where(f => f.IsActive)
            .ToListAsync();

        // Get total payments per student up to cutoff date
        var studentPayments = await _context.StudentPayments
            .Where(p => p.CreatedAt <= cutoffDate)
            .GroupBy(p => p.StudentId)
            .Select(g => new { StudentId = g.Key, TotalPaid = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(x => x.StudentId, x => x.TotalPaid);

        // Get last payment date per student
        var lastPaymentDates = await _context.StudentPayments
            .Where(p => p.CreatedAt <= cutoffDate)
            .GroupBy(p => p.StudentId)
            .Select(g => new { StudentId = g.Key, LastPaymentDate = g.Max(p => p.CreatedAt) })
            .ToDictionaryAsync(x => x.StudentId, x => x.LastPaymentDate);

        var arRecords = new List<AccountsReceivableRecord>();

        foreach (var student in students)
        {
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

                // Match just the number
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
                decimal outstanding = totalFee - totalPaid;

                if (outstanding > 0)
                {
                    var daysSinceLastPayment = lastPaymentDates.TryGetValue(student.StudentId, out var lastPaymentDate)
                        ? (cutoffDate - lastPaymentDate).Days
                        : (cutoffDate - student.DateRegistered).Days;

                    string agingBucket = GetAgingBucket(daysSinceLastPayment);

                    arRecords.Add(new AccountsReceivableRecord
                    {
                        StudentId = student.StudentId,
                        StudentName = $"{student.FirstName} {(!string.IsNullOrWhiteSpace(student.MiddleName) ? student.MiddleName + " " : "")}{student.LastName}{(!string.IsNullOrWhiteSpace(student.Suffix) ? " " + student.Suffix : "")}".Trim(),
                        GradeLevel = student.GradeLevel ?? "N/A",
                        TotalFee = totalFee,
                        AmountPaid = totalPaid,
                        OutstandingBalance = outstanding,
                        LastPaymentDate = lastPaymentDate,
                        DaysSinceLastPayment = daysSinceLastPayment,
                        AgingBucket = agingBucket
                    });
                }
            }
        }

        return arRecords.OrderByDescending(r => r.OutstandingBalance).ToList();
    }

    private string GetAgingBucket(int days)
    {
        return days switch
        {
            <= 30 => "Current",
            <= 60 => "31-60 Days",
            <= 90 => "61-90 Days",
            _ => "Over 90 Days"
        };
    }

    #endregion

    #region Income Statement (P&L)

    public async Task<IncomeStatement> GetIncomeStatementAsync(DateTime fromDate, DateTime toDate)
    {
        // Revenue from student payments
        var revenue = await _context.StudentPayments
            .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        // Expenses (only approved)
        var expenses = await _context.Expenses
            .Where(e => e.ExpenseDate >= fromDate && e.ExpenseDate <= toDate && e.Status == "Approved")
            .SumAsync(e => (decimal?)e.Amount) ?? 0m;

        // Expenses by category
        var expensesByCategory = await _context.Expenses
            .Where(e => e.ExpenseDate >= fromDate && e.ExpenseDate <= toDate && e.Status == "Approved")
            .GroupBy(e => e.Category ?? "Uncategorized")
            .Select(g => new ExpenseCategorySummary
            {
                Category = g.Key,
                Amount = g.Sum(e => e.Amount)
            })
            .OrderByDescending(e => e.Amount)
            .ToListAsync();

        var netIncome = revenue - expenses;

        return new IncomeStatement
        {
            PeriodFrom = fromDate,
            PeriodTo = toDate,
            Revenue = revenue,
            TotalExpenses = expenses,
            ExpensesByCategory = expensesByCategory,
            NetIncome = netIncome
        };
    }

    #endregion

    #region Payment History Report

    public async Task<List<PaymentHistoryRecord>> GetPaymentHistoryAsync(DateTime? fromDate = null, DateTime? toDate = null, string? studentId = null)
    {
        var query = _context.StudentPayments
            .Include(p => p.Student)
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(p => p.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(p => p.CreatedAt <= toDate.Value);

        if (!string.IsNullOrWhiteSpace(studentId))
            query = query.Where(p => p.StudentId == studentId);

        var payments = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return payments.Select(p => new PaymentHistoryRecord
        {
            PaymentId = p.PaymentId,
            StudentId = p.StudentId,
            StudentName = p.Student != null
                ? $"{p.Student.FirstName} {(!string.IsNullOrWhiteSpace(p.Student.MiddleName) ? p.Student.MiddleName + " " : "")}{p.Student.LastName}{(!string.IsNullOrWhiteSpace(p.Student.Suffix) ? " " + p.Student.Suffix : "")}".Trim()
                : "N/A",
            GradeLevel = p.Student?.GradeLevel ?? "N/A",
            Amount = p.Amount,
            PaymentMethod = p.PaymentMethod,
            OrNumber = p.OrNumber,
            ProcessedBy = p.ProcessedBy ?? "N/A",
            PaymentDate = p.CreatedAt
        }).ToList();
    }

    #endregion

    #region Expense Analysis Report

    public async Task<ExpenseAnalysisReport> GetExpenseAnalysisAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Expenses.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(e => e.ExpenseDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(e => e.ExpenseDate <= toDate.Value);

        var expenses = await query.ToListAsync();

        // Group by category
        var byCategory = expenses
            .Where(e => e.Status == "Approved")
            .GroupBy(e => e.Category ?? "Uncategorized")
            .Select(g => new ExpenseCategorySummary
            {
                Category = g.Key,
                Amount = g.Sum(e => e.Amount),
                Count = g.Count()
            })
            .OrderByDescending(e => e.Amount)
            .ToList();

        // Group by status
        var byStatus = expenses
            .GroupBy(e => e.Status ?? "Pending")
            .Select(g => new ExpenseStatusSummary
            {
                Status = g.Key,
                Amount = g.Sum(e => e.Amount),
                Count = g.Count()
            })
            .OrderByDescending(e => e.Amount)
            .ToList();

        // Group by payment method
        var byPaymentMethod = expenses
            .Where(e => e.Status == "Approved")
            .GroupBy(e => e.PaymentMethod ?? "Cash")
            .Select(g => new ExpensePaymentMethodSummary
            {
                PaymentMethod = g.Key,
                Amount = g.Sum(e => e.Amount),
                Count = g.Count()
            })
            .OrderByDescending(e => e.Amount)
            .ToList();

        // Monthly trends
        var monthlyTrends = expenses
            .Where(e => e.Status == "Approved")
            .GroupBy(e => new { Year = e.ExpenseDate.Year, Month = e.ExpenseDate.Month })
            .Select(g => new ExpenseMonthlyTrend
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                Amount = g.Sum(e => e.Amount),
                Count = g.Count()
            })
            .OrderBy(e => e.Year)
            .ThenBy(e => e.Month)
            .ToList();

        return new ExpenseAnalysisReport
        {
            TotalExpenses = expenses.Where(e => e.Status == "Approved").Sum(e => e.Amount),
            TotalCount = expenses.Count,
            ByCategory = byCategory,
            ByStatus = byStatus,
            ByPaymentMethod = byPaymentMethod,
            MonthlyTrends = monthlyTrends
        };
    }

    #endregion

    #region Cash Flow Report

    public async Task<CashFlowReport> GetCashFlowReportAsync(DateTime fromDate, DateTime toDate)
    {
        // Cash Inflows (from student payments)
        var cashInflows = await _context.StudentPayments
            .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new CashFlowItem
            {
                Category = g.Key,
                Amount = g.Sum(p => p.Amount),
                Type = "Inflow"
            })
            .ToListAsync();

        // Cash Outflows (from expenses)
        var cashOutflows = await _context.Expenses
            .Where(e => e.ExpenseDate >= fromDate && e.ExpenseDate <= toDate && e.Status == "Approved")
            .GroupBy(e => e.PaymentMethod ?? "Cash")
            .Select(g => new CashFlowItem
            {
                Category = g.Key,
                Amount = g.Sum(e => e.Amount),
                Type = "Outflow"
            })
            .ToListAsync();

        var totalInflow = cashInflows.Sum(i => i.Amount);
        var totalOutflow = cashOutflows.Sum(o => o.Amount);
        var netCashFlow = totalInflow - totalOutflow;

        // Monthly breakdown
        var monthlyCashFlow = new List<MonthlyCashFlow>();

        var months = Enumerable.Range(0, (toDate.Year - fromDate.Year) * 12 + (toDate.Month - fromDate.Month) + 1)
            .Select(m => fromDate.AddMonths(m))
            .Where(d => d <= toDate)
            .ToList();

        foreach (var month in months)
        {
            var monthStart = new DateTime(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var monthInflow = await _context.StudentPayments
                .Where(p => p.CreatedAt >= monthStart && p.CreatedAt <= monthEnd)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var monthOutflow = await _context.Expenses
                .Where(e => e.ExpenseDate >= monthStart && e.ExpenseDate <= monthEnd && e.Status == "Approved")
                .SumAsync(e => (decimal?)e.Amount) ?? 0m;

            monthlyCashFlow.Add(new MonthlyCashFlow
            {
                Month = monthStart,
                MonthName = monthStart.ToString("MMM yyyy"),
                CashIn = monthInflow,
                CashOut = monthOutflow,
                NetCashFlow = monthInflow - monthOutflow
            });
        }

        return new CashFlowReport
        {
            PeriodFrom = fromDate,
            PeriodTo = toDate,
            CashInflows = cashInflows,
            CashOutflows = cashOutflows,
            TotalInflow = totalInflow,
            TotalOutflow = totalOutflow,
            NetCashFlow = netCashFlow,
            MonthlyBreakdown = monthlyCashFlow
        };
    }

    #endregion
}

#region Report Models

public class AccountsReceivableRecord
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public decimal TotalFee { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal OutstandingBalance { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public int DaysSinceLastPayment { get; set; }
    public string AgingBucket { get; set; } = string.Empty;
}

public class IncomeStatement
{
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public decimal Revenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public List<ExpenseCategorySummary> ExpensesByCategory { get; set; } = new();
    public decimal NetIncome { get; set; }
}

public class PaymentHistoryRecord
{
    public int PaymentId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string OrNumber { get; set; } = string.Empty;
    public string ProcessedBy { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
}

public class ExpenseAnalysisReport
{
    public decimal TotalExpenses { get; set; }
    public int TotalCount { get; set; }
    public List<ExpenseCategorySummary> ByCategory { get; set; } = new();
    public List<ExpenseStatusSummary> ByStatus { get; set; } = new();
    public List<ExpensePaymentMethodSummary> ByPaymentMethod { get; set; } = new();
    public List<ExpenseMonthlyTrend> MonthlyTrends { get; set; } = new();
}

public class ExpenseCategorySummary
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class ExpenseStatusSummary
{
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class ExpensePaymentMethodSummary
{
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class ExpenseMonthlyTrend
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class CashFlowReport
{
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public List<CashFlowItem> CashInflows { get; set; } = new();
    public List<CashFlowItem> CashOutflows { get; set; } = new();
    public decimal TotalInflow { get; set; }
    public decimal TotalOutflow { get; set; }
    public decimal NetCashFlow { get; set; }
    public List<MonthlyCashFlow> MonthlyBreakdown { get; set; } = new();
}

public class CashFlowItem
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty; // "Inflow" or "Outflow"
}

public class MonthlyCashFlow
{
    public DateTime Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal CashIn { get; set; }
    public decimal CashOut { get; set; }
    public decimal NetCashFlow { get; set; }
}

#endregion

