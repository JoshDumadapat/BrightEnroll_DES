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

