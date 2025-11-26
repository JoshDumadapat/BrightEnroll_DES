using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Finance;

// Handles expense recording operations
public class ExpenseService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ExpenseService>? _logger;

    public ExpenseService(AppDbContext context, ILogger<ExpenseService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    // Creates a new expense record from the UI request
    public async Task<Expense> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            // Basic validation/sanitization
            if (string.IsNullOrWhiteSpace(request.ExpenseCode))
                throw new ArgumentException("Expense code is required.", nameof(request.ExpenseCode));

            if (request.Amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.", nameof(request.Amount));

            // If an expense with the same code already exists, treat this as an update instead of insert
            var existingExpense = await _context.Expenses
                .FirstOrDefaultAsync(e => e.ExpenseCode == request.ExpenseCode);

            if (existingExpense != null)
            {
                var updateRequest = new UpdateExpenseRequest
                {
                    Category = request.Category,
                    Description = request.Description,
                    Amount = request.Amount,
                    ExpenseDate = request.ExpenseDate,
                    Payee = request.Payee,
                    OrNumber = request.OrNumber,
                    PaymentMethod = request.PaymentMethod,
                    ApprovedBy = request.ApprovedBy,
                    Status = request.Status,
                    RecordedBy = request.RecordedBy
                };

                return await UpdateExpenseAsync(request.ExpenseCode, updateRequest);
            }

            var expense = new Expense
            {
                ExpenseCode = request.ExpenseCode.Trim(),
                Category = Sanitize(request.Category, 50),
                Description = Sanitize(request.Description, 500, allowNull: true),
                Amount = request.Amount,
                ExpenseDate = request.ExpenseDate,
                Payee = Sanitize(request.Payee, 150, allowNull: true),
                OrNumber = Sanitize(request.OrNumber, 50, allowNull: true),
                PaymentMethod = Sanitize(request.PaymentMethod, 30),
                Status = Sanitize(string.IsNullOrWhiteSpace(request.Status) ? "Pending" : request.Status, 20),
                RecordedBy = Sanitize(request.RecordedBy, 100, allowNull: true),
                ApprovedBy = Sanitize(request.ApprovedBy, 100, allowNull: true),
                CreatedAt = DateTime.Now
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Expense created successfully with code {ExpenseCode}", expense.ExpenseCode);

            return expense;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating expense: {Message}", ex.Message);
            throw;
        }
    }

    // Updates an existing expense record identified by its expense code
    public async Task<Expense> UpdateExpenseAsync(string expenseCode, UpdateExpenseRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(expenseCode))
                throw new ArgumentException("Expense code is required.", nameof(expenseCode));

            var expense = await _context.Expenses
                .FirstOrDefaultAsync(e => e.ExpenseCode == expenseCode);

            if (expense == null)
            {
                throw new InvalidOperationException($"Expense with code '{expenseCode}' was not found.");
            }

            if (request.Amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.", nameof(request.Amount));

            expense.Category = Sanitize(request.Category, 50);
            expense.Description = Sanitize(request.Description, 500, allowNull: true);
            expense.Amount = request.Amount;
            expense.ExpenseDate = request.ExpenseDate;
            expense.Payee = Sanitize(request.Payee, 150, allowNull: true);
            expense.OrNumber = Sanitize(request.OrNumber, 50, allowNull: true);
            expense.PaymentMethod = Sanitize(request.PaymentMethod, 30);
            expense.Status = Sanitize(request.Status, 20);
            expense.RecordedBy = Sanitize(request.RecordedBy, 100, allowNull: true);
            expense.ApprovedBy = Sanitize(request.ApprovedBy, 100, allowNull: true);
            expense.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger?.LogInformation("Expense updated successfully with code {ExpenseCode}", expense.ExpenseCode);
            return expense;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating expense {ExpenseCode}: {Message}", expenseCode, ex.Message);
            throw;
        }
    }

    // Archives an expense (soft delete) by marking its status as Archived
    public async Task ArchiveExpenseAsync(string expenseCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(expenseCode))
                throw new ArgumentException("Expense code is required.", nameof(expenseCode));

            var expense = await _context.Expenses
                .FirstOrDefaultAsync(e => e.ExpenseCode == expenseCode);

            if (expense == null)
            {
                throw new InvalidOperationException($"Expense with code '{expenseCode}' was not found.");
            }

            expense.Status = "Archived";
            expense.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger?.LogInformation("Expense archived successfully with code {ExpenseCode}", expense.ExpenseCode);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error archiving expense {ExpenseCode}: {Message}", expenseCode, ex.Message);
            throw;
        }
    }

    // Gets expenses within an optional date range (for future records tab)
    public async Task<List<Expense>> GetExpensesAsync(DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var query = _context.Expenses
                .Include(e => e.Attachments)
                .AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(e => e.ExpenseDate >= from.Value.Date);
            }

            if (to.HasValue)
            {
                query = query.Where(e => e.ExpenseDate <= to.Value.Date);
            }

            return await query
                .OrderByDescending(e => e.ExpenseDate)
                .ThenByDescending(e => e.ExpenseId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading expenses: {Message}", ex.Message);
            throw;
        }
    }

    private static string? Sanitize(string? input, int maxLength, bool allowNull = false)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return allowNull ? null : string.Empty;
        }

        var trimmed = input.Trim();
        if (trimmed.Length > maxLength)
        {
            trimmed = trimmed.Substring(0, maxLength);
        }

        return trimmed;
    }
}

public class CreateExpenseRequest
{
    public string ExpenseCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Payee { get; set; }
    public string? OrNumber { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? ApprovedBy { get; set; }
    public string Status { get; set; } = "Pending";
    public string? RecordedBy { get; set; }
}

public class UpdateExpenseRequest
{
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Payee { get; set; }
    public string? OrNumber { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? ApprovedBy { get; set; }
    public string Status { get; set; } = "Pending";
    public string? RecordedBy { get; set; }
}


