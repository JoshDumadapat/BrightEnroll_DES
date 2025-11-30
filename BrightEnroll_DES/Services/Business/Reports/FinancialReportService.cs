using BrightEnroll_DES.Data;
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
        // Get total collections (from payment records - this would need a payment table)
        // For now, using fees as a proxy
        var totalCollections = 0.00m; // TODO: Calculate from actual payment records
        
        // Get outstanding balances
        var outstanding = 0.00m; // TODO: Calculate from student fees minus payments

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
            .SumAsync(e => e.Amount);

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

