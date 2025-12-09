using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Services.Business.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Finance;

public class ExpenseService
{
    private readonly AppDbContext _context;
    private readonly JournalEntryService _journalEntryService;
    private readonly NotificationService? _notificationService;
    private readonly ILogger<ExpenseService>? _logger;

    public ExpenseService(AppDbContext context, JournalEntryService journalEntryService, ILogger<ExpenseService>? logger = null, NotificationService? notificationService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _journalEntryService = journalEntryService ?? throw new ArgumentNullException(nameof(journalEntryService));
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<Expense> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ExpenseCode))
                throw new ArgumentException("Expense code is required.", nameof(request.ExpenseCode));

            if (request.Amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.", nameof(request.Amount));

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
                Category = Sanitize(request.Category, 50) ?? string.Empty,
                Description = Sanitize(request.Description, 500, allowNull: true),
                Amount = request.Amount,
                ExpenseDate = request.ExpenseDate,
                Payee = Sanitize(request.Payee, 150, allowNull: true),
                OrNumber = Sanitize(request.OrNumber, 50, allowNull: true),
                PaymentMethod = Sanitize(request.PaymentMethod, 30) ?? string.Empty,
                Status = Sanitize(string.IsNullOrWhiteSpace(request.Status) ? "Pending" : request.Status, 20) ?? string.Empty,
                RecordedBy = Sanitize(request.RecordedBy, 100, allowNull: true),
                ApprovedBy = Sanitize(request.ApprovedBy, 100, allowNull: true),
                CreatedAt = DateTime.Now
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            // Create notification if expense is pending approval
            if (expense.Status == "Pending" && _notificationService != null)
            {
                try
                {
                    // Get created by user ID
                    int? createdBy = null;
                    if (!string.IsNullOrWhiteSpace(expense.RecordedBy))
                    {
                        var user = await _context.Users
                            .FirstOrDefaultAsync(u => u.Email == expense.RecordedBy || u.SystemId == expense.RecordedBy ||
                                (u.FirstName + " " + u.LastName) == expense.RecordedBy);
                        createdBy = user?.UserId;
                    }

                    await _notificationService.CreateNotificationAsync(
                        notificationType: "Expense",
                        title: "New Expense Pending Approval",
                        message: $"Expense {expense.ExpenseCode}: {expense.Description} - ₱{expense.Amount:N2}",
                        referenceType: "Expense",
                        referenceId: expense.ExpenseId,
                        actionUrl: "/finance?tab=Approvals",
                        priority: "Normal",
                        createdBy: createdBy
                    );
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to create notification for expense {ExpenseCode}", expense.ExpenseCode);
                    // Don't throw - notification failure shouldn't break expense creation
                }
            }

            _logger?.LogInformation("Expense created successfully with code {ExpenseCode}", expense.ExpenseCode);

            return expense;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating expense: {Message}", ex.Message);
            throw;
        }
    }

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

            // Track status change for journal entry creation
            string oldStatus = expense.Status;
            bool justApproved = oldStatus != "Approved" && request.Status == "Approved";

            expense.Category = Sanitize(request.Category, 50) ?? string.Empty;
            expense.Description = Sanitize(request.Description, 500, allowNull: true);
            expense.Amount = request.Amount;
            expense.ExpenseDate = request.ExpenseDate;
            expense.Payee = Sanitize(request.Payee, 150, allowNull: true);
            expense.OrNumber = Sanitize(request.OrNumber, 50, allowNull: true);
            expense.PaymentMethod = Sanitize(request.PaymentMethod, 30) ?? string.Empty;
            expense.Status = Sanitize(request.Status, 20) ?? string.Empty;
            expense.RecordedBy = Sanitize(request.RecordedBy, 100, allowNull: true);
            expense.ApprovedBy = Sanitize(request.ApprovedBy, 100, allowNull: true);
            expense.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // Create journal entry when expense is approved
            if (justApproved)
            {
                try
                {
                    // Get user IDs for audit trail
                    int? createdBy = null;
                    int? approvedBy = null;

                    // Get created by user ID (from RecordedBy)
                    if (!string.IsNullOrWhiteSpace(expense.RecordedBy))
                    {
                        var createdByUser = await _context.Users
                            .FirstOrDefaultAsync(u => u.Email == expense.RecordedBy || u.SystemId == expense.RecordedBy || 
                                (u.FirstName + " " + u.LastName) == expense.RecordedBy);
                        createdBy = createdByUser?.UserId;
                    }

                    // Get approved by user ID (from ApprovedBy)
                    if (!string.IsNullOrWhiteSpace(request.ApprovedBy))
                    {
                        var approvedByUser = await _context.Users
                            .FirstOrDefaultAsync(u => u.Email == request.ApprovedBy || u.SystemId == request.ApprovedBy ||
                                (u.FirstName + " " + u.LastName) == request.ApprovedBy);
                        approvedBy = approvedByUser?.UserId;
                    }

                    await _journalEntryService.CreateExpenseJournalEntryAsync(expense, createdBy, approvedBy);
                    _logger?.LogInformation("Journal entry created for expense {ExpenseId} with approver {ApprovedBy}", expense.ExpenseId, approvedBy);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the expense update if journal entry creation fails
                    _logger?.LogWarning(ex, "Failed to create journal entry for expense {ExpenseId}: {Message}", expense.ExpenseId, ex.Message);
                }
            }

            _logger?.LogInformation("Expense updated successfully with code {ExpenseCode}", expense.ExpenseCode);
            return expense;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating expense {ExpenseCode}: {Message}", expenseCode, ex.Message);
            throw;
        }
    }

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


