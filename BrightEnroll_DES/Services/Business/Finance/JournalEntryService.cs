using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using BrightEnroll_DES.Services.Business.Audit;
using BrightEnroll_DES.Services.Authentication;

namespace BrightEnroll_DES.Services.Business.Finance;

/// <summary>
/// Service for creating and managing journal entries (double-entry bookkeeping)
/// </summary>
public class JournalEntryService
{
    private readonly AppDbContext _context;
    private readonly ILogger<JournalEntryService>? _logger;
    private readonly IServiceScopeFactory? _serviceScopeFactory;
    private readonly IAuthService? _authService;

    public JournalEntryService(
        AppDbContext context, 
        ILogger<JournalEntryService>? logger = null,
        IServiceScopeFactory? serviceScopeFactory = null,
        IAuthService? authService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _authService = authService;
    }

    /// <summary>
    /// Creates a journal entry for a student payment
    /// </summary>
    public async Task<int> CreatePaymentJournalEntryAsync(StudentPayment payment, int? createdBy = null)
    {
        try
        {
            // Get Cash account (1000)
            var cashAccount = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountCode == "1000" && a.IsActive);

            if (cashAccount == null)
            {
                throw new InvalidOperationException("Cash account (1000) not found in Chart of Accounts. Please seed the Chart of Accounts first.");
            }

            // Get Tuition Revenue account (4000)
            var revenueAccount = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountCode == "4000" && a.IsActive);

            if (revenueAccount == null)
            {
                throw new InvalidOperationException("Tuition Revenue account (4000) not found in Chart of Accounts. Please seed the Chart of Accounts first.");
            }

            // Generate entry number
            var entryNumber = await GenerateEntryNumberAsync();

            // Create journal entry
            var journalEntry = new JournalEntry
            {
                EntryNumber = entryNumber,
                EntryDate = payment.CreatedAt.Date,
                Description = $"Payment from Student {payment.StudentId} - OR {payment.OrNumber}",
                ReferenceType = "Payment",
                ReferenceId = payment.PaymentId,
                Status = "Posted",
                CreatedBy = createdBy,
                CreatedAt = DateTime.Now
            };

            _context.JournalEntries.Add(journalEntry);
            await _context.SaveChangesAsync(); // Save to get JournalEntryId

            // Create journal entry lines
            var lines = new List<JournalEntryLine>
            {
                // Debit: Cash
                new JournalEntryLine
                {
                    JournalEntryId = journalEntry.JournalEntryId,
                    AccountId = cashAccount.AccountId,
                    LineNumber = 1,
                    DebitAmount = payment.Amount,
                    CreditAmount = 0,
                    Description = $"Payment received - {payment.PaymentMethod}"
                },
                // Credit: Tuition Revenue
                new JournalEntryLine
                {
                    JournalEntryId = journalEntry.JournalEntryId,
                    AccountId = revenueAccount.AccountId,
                    LineNumber = 2,
                    DebitAmount = 0,
                    CreditAmount = payment.Amount,
                    Description = $"Tuition revenue from Student {payment.StudentId}"
                }
            };

            _context.JournalEntryLines.AddRange(lines);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Journal entry {EntryNumber} created for payment {PaymentId}", entryNumber, payment.PaymentId);

            // Audit logging (non-blocking, background task)
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_serviceScopeFactory != null)
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var auditLogService = scope.ServiceProvider.GetRequiredService<AuditLogService>();
                        var authService = scope.ServiceProvider.GetService<IAuthService>();
                        
                        var currentUser = authService?.CurrentUser;
                        var userName = currentUser != null ? $"{currentUser.first_name} {currentUser.last_name}".Trim() : "System";
                        var userRole = currentUser?.user_role ?? "System";
                        var userId = createdBy;
                        
                        await auditLogService.CreateTransactionLogAsync(
                            action: "Create Journal Entry",
                            module: "Finance",
                            description: $"Created journal entry {entryNumber} for payment {payment.PaymentId}: Amount ₱{payment.Amount:N2}",
                            userName: userName,
                            userRole: userRole,
                            userId: userId,
                            entityType: "JournalEntry",
                            entityId: journalEntry.JournalEntryId.ToString(),
                            oldValues: null,
                            newValues: $"EntryNumber: {entryNumber}, PaymentId: {payment.PaymentId}, Amount: ₱{payment.Amount:N2}, Status: Posted",
                            status: "Success",
                            severity: "High"
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to create audit log for journal entry creation: {Message}", ex.Message);
                }
            });

            return journalEntry.JournalEntryId;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating payment journal entry for payment {PaymentId}: {Message}", payment.PaymentId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Creates a journal entry for an approved expense
    /// </summary>
    public async Task<int> CreateExpenseJournalEntryAsync(Expense expense, int? createdBy = null, int? approvedBy = null)
    {
        try
        {
            if (expense.Status != "Approved")
            {
                throw new InvalidOperationException($"Cannot create journal entry for expense with status '{expense.Status}'. Expense must be approved.");
            }

            // Get Cash account (1000)
            var cashAccount = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountCode == "1000" && a.IsActive);

            if (cashAccount == null)
            {
                throw new InvalidOperationException("Cash account (1000) not found in Chart of Accounts.");
            }

            // Map expense category to expense account
            // Default to "5100" (Other Expenses) if category doesn't match
            var expenseAccountCode = MapExpenseCategoryToAccount(expense.Category);
            var expenseAccount = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountCode == expenseAccountCode && a.IsActive);

            if (expenseAccount == null)
            {
                // Fallback to Other Expenses (5100)
                expenseAccount = await _context.ChartOfAccounts
                    .FirstOrDefaultAsync(a => a.AccountCode == "5100" && a.IsActive);

                if (expenseAccount == null)
                {
                    throw new InvalidOperationException($"Expense account not found. Please ensure account {expenseAccountCode} or 5100 exists in Chart of Accounts.");
                }
            }

            // Check if journal entry already exists for this expense
            var existingEntry = await _context.JournalEntries
                .FirstOrDefaultAsync(e => e.ReferenceType == "Expense" && e.ReferenceId == expense.ExpenseId);

            if (existingEntry != null)
            {
                _logger?.LogWarning("Journal entry already exists for expense {ExpenseId}", expense.ExpenseId);
                return existingEntry.JournalEntryId;
            }

            // Generate entry number
            var entryNumber = await GenerateEntryNumberAsync();

            // Create journal entry
            var journalEntry = new JournalEntry
            {
                EntryNumber = entryNumber,
                EntryDate = expense.ExpenseDate,
                Description = $"{expense.Category}: {expense.Description ?? expense.ExpenseCode}",
                ReferenceType = "Expense",
                ReferenceId = expense.ExpenseId,
                Status = "Posted",
                CreatedBy = createdBy,
                ApprovedBy = approvedBy, // Set the approver for audit trail
                CreatedAt = DateTime.Now
            };

            _context.JournalEntries.Add(journalEntry);
            await _context.SaveChangesAsync();

            // Create journal entry lines
            var lines = new List<JournalEntryLine>
            {
                // Debit: Expense Account
                new JournalEntryLine
                {
                    JournalEntryId = journalEntry.JournalEntryId,
                    AccountId = expenseAccount.AccountId,
                    LineNumber = 1,
                    DebitAmount = expense.Amount,
                    CreditAmount = 0,
                    Description = $"{expense.Category}: {expense.Description ?? expense.ExpenseCode}"
                },
                // Credit: Cash
                new JournalEntryLine
                {
                    JournalEntryId = journalEntry.JournalEntryId,
                    AccountId = cashAccount.AccountId,
                    LineNumber = 2,
                    DebitAmount = 0,
                    CreditAmount = expense.Amount,
                    Description = $"Payment - {expense.PaymentMethod}"
                }
            };

            _context.JournalEntryLines.AddRange(lines);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Journal entry {EntryNumber} created for expense {ExpenseId}", entryNumber, expense.ExpenseId);

            return journalEntry.JournalEntryId;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating expense journal entry for expense {ExpenseId}: {Message}", expense.ExpenseId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Creates a journal entry for payroll transaction
    /// </summary>
    public async Task<int> CreatePayrollJournalEntryAsync(PayrollTransaction payroll, int? createdBy = null, int? approvedBy = null)
    {
        try
        {
            if (payroll.Status != "Paid")
            {
                throw new InvalidOperationException($"Cannot create journal entry for payroll with status '{payroll.Status}'. Payroll must be paid.");
            }

            // Get required accounts
            var cashAccount = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountCode == "1000" && a.IsActive);
            var salariesExpenseAccount = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountCode == "5000" && a.IsActive);
            var payrollTaxExpenseAccount = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountCode == "5100" && a.IsActive);
            var accruedPayrollTaxesAccount = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountCode == "2100" && a.IsActive);

            if (cashAccount == null || salariesExpenseAccount == null || payrollTaxExpenseAccount == null || accruedPayrollTaxesAccount == null)
            {
                throw new InvalidOperationException("Required accounts not found in Chart of Accounts. Please seed the Chart of Accounts first.");
            }

            // Check if journal entry already exists
            var existingEntry = await _context.JournalEntries
                .FirstOrDefaultAsync(e => e.ReferenceType == "Payroll" && e.ReferenceId == payroll.TransactionId);

            if (existingEntry != null)
            {
                _logger?.LogWarning("Journal entry already exists for payroll {TransactionId}", payroll.TransactionId);
                return existingEntry.JournalEntryId;
            }

            // Generate entry number
            var entryNumber = await GenerateEntryNumberAsync();

            // Create journal entry
            // Use PaymentDate if available (date approved), otherwise use CreatedAt
            // Always use .Date to ensure date-only comparison in reports
            var entryDate = payroll.PaymentDate.HasValue 
                ? payroll.PaymentDate.Value.Date 
                : payroll.CreatedAt.Date;
                
            var journalEntry = new JournalEntry
            {
                EntryNumber = entryNumber,
                EntryDate = entryDate,
                Description = $"Payroll for {payroll.PayPeriod} - User {payroll.UserId}",
                ReferenceType = "Payroll",
                ReferenceId = payroll.TransactionId,
                Status = "Posted",
                CreatedBy = createdBy,
                ApprovedBy = approvedBy ?? payroll.ApprovedBy, // Set approver for audit trail
                CreatedAt = DateTime.Now
            };

            _context.JournalEntries.Add(journalEntry);
            await _context.SaveChangesAsync();

            // Create journal entry lines
            var lines = new List<JournalEntryLine>();
            int lineNumber = 1;

            // Debit: Salaries Expense (Gross Salary - the full cost to company)
            // This includes both what employee receives (Net Pay) and what company owes (deductions + contributions)
            var totalSalaryExpense = payroll.GrossSalary + payroll.TotalCompanyContribution;
            lines.Add(new JournalEntryLine
            {
                JournalEntryId = journalEntry.JournalEntryId,
                AccountId = salariesExpenseAccount.AccountId,
                LineNumber = lineNumber++,
                DebitAmount = totalSalaryExpense,
                CreditAmount = 0,
                Description = $"Gross salary and company contributions for {payroll.PayPeriod}"
            });

            // Credit: Cash (Net Pay - what employee receives)
            lines.Add(new JournalEntryLine
            {
                JournalEntryId = journalEntry.JournalEntryId,
                AccountId = cashAccount.AccountId,
                LineNumber = lineNumber++,
                DebitAmount = 0,
                CreditAmount = payroll.NetSalary,
                Description = $"Net pay payment"
            });

            // Credit: Accrued Payroll Taxes (Company Contributions + Employee Deductions)
            // This represents the liability - money owed to government agencies
            if (payroll.TotalCompanyContribution > 0 || payroll.TotalDeductions > 0)
            {
                var totalAccrued = payroll.TotalCompanyContribution + payroll.TotalDeductions;
                lines.Add(new JournalEntryLine
                {
                    JournalEntryId = journalEntry.JournalEntryId,
                    AccountId = accruedPayrollTaxesAccount.AccountId,
                    LineNumber = lineNumber++,
                    DebitAmount = 0,
                    CreditAmount = totalAccrued,
                    Description = $"Company contributions and employee deductions"
                });
            }

            _context.JournalEntryLines.AddRange(lines);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Journal entry {EntryNumber} created for payroll {TransactionId}", entryNumber, payroll.TransactionId);

            return journalEntry.JournalEntryId;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating payroll journal entry for transaction {TransactionId}: {Message}", payroll.TransactionId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Creates a single combined journal entry for a batch of payroll transactions
    /// </summary>
    public async Task<int> CreateBatchPayrollJournalEntryAsync(List<PayrollTransaction> payrollTransactions, int? createdBy = null, int? approvedBy = null)
    {
        try
        {
            if (!payrollTransactions.Any())
            {
                throw new InvalidOperationException("Cannot create journal entry for empty batch.");
            }

            // Validate all transactions are paid
            if (payrollTransactions.Any(pt => pt.Status != "Paid"))
            {
                throw new InvalidOperationException("All payroll transactions must be paid before creating journal entry.");
            }

            // Get required accounts
            var cashAccount = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountCode == "1000" && a.IsActive);
            var salariesExpenseAccount = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountCode == "5000" && a.IsActive);
            var payrollTaxExpenseAccount = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountCode == "5100" && a.IsActive);
            var accruedPayrollTaxesAccount = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountCode == "2100" && a.IsActive);

            if (cashAccount == null || salariesExpenseAccount == null || payrollTaxExpenseAccount == null || accruedPayrollTaxesAccount == null)
            {
                throw new InvalidOperationException("Required accounts not found in Chart of Accounts. Please seed the Chart of Accounts first.");
            }

            // Check if journal entry already exists for this batch (use first transaction's batch timestamp as reference)
            var firstTransaction = payrollTransactions.First();
            var batchTimestamp = firstTransaction.BatchTimestamp;
            if (batchTimestamp.HasValue)
            {
                // Extract transaction IDs first (client-side evaluation)
                var transactionIds = payrollTransactions.Select(pt => pt.TransactionId).ToList();
                
                // Check if a journal entry already exists for any transaction in this batch
                // Use Contains which can be translated to SQL
                var existingEntry = await _context.JournalEntries
                    .FirstOrDefaultAsync(e => e.ReferenceType == "Payroll" && 
                        e.ReferenceId.HasValue &&
                        transactionIds.Contains(e.ReferenceId.Value));

                if (existingEntry != null)
                {
                    _logger?.LogWarning("Journal entry already exists for batch payroll");
                    return existingEntry.JournalEntryId;
                }
            }

            // Aggregate totals from all transactions
            var totalGrossSalary = payrollTransactions.Sum(pt => pt.GrossSalary);
            var totalDeductions = payrollTransactions.Sum(pt => pt.TotalDeductions);
            var totalNetSalary = payrollTransactions.Sum(pt => pt.NetSalary);
            var totalCompanyContribution = payrollTransactions.Sum(pt => pt.TotalCompanyContribution);
            var employeeCount = payrollTransactions.Count;
            var payPeriod = firstTransaction.PayPeriod;

            // Generate entry number
            var entryNumber = await GenerateEntryNumberAsync();

            // Create journal entry
            // Use PaymentDate if available (date approved), otherwise use CreatedAt
            // Always use .Date to ensure date-only comparison in reports
            var entryDate = firstTransaction.PaymentDate.HasValue 
                ? firstTransaction.PaymentDate.Value.Date 
                : firstTransaction.CreatedAt.Date;
                
            var journalEntry = new JournalEntry
            {
                EntryNumber = entryNumber,
                EntryDate = entryDate,
                Description = $"Batch Payroll for {employeeCount} employee(s) - Pay Period: {payPeriod}",
                ReferenceType = "Payroll",
                ReferenceId = firstTransaction.TransactionId, // Use first transaction ID as reference
                Status = "Posted",
                CreatedBy = createdBy,
                ApprovedBy = approvedBy ?? firstTransaction.ApprovedBy,
                CreatedAt = DateTime.Now
            };

            _context.JournalEntries.Add(journalEntry);
            await _context.SaveChangesAsync();

            // Create journal entry lines with aggregated amounts
            var lines = new List<JournalEntryLine>();
            int lineNumber = 1;

            // Debit: Salaries Expense (Total Gross Salary)
            lines.Add(new JournalEntryLine
            {
                JournalEntryId = journalEntry.JournalEntryId,
                AccountId = salariesExpenseAccount.AccountId,
                LineNumber = lineNumber++,
                DebitAmount = totalGrossSalary,
                CreditAmount = 0,
                Description = $"Batch gross salary for {employeeCount} employee(s) - {payPeriod}"
            });

            // Debit: Payroll Tax Expense (Total Deductions)
            if (totalDeductions > 0)
            {
                lines.Add(new JournalEntryLine
                {
                    JournalEntryId = journalEntry.JournalEntryId,
                    AccountId = payrollTaxExpenseAccount.AccountId,
                    LineNumber = lineNumber++,
                    DebitAmount = totalDeductions,
                    CreditAmount = 0,
                    Description = $"Batch payroll deductions (SSS, PhilHealth, Pag-IBIG, Tax) for {employeeCount} employee(s)"
                });
            }

            // Credit: Cash (Total Net Pay)
            lines.Add(new JournalEntryLine
            {
                JournalEntryId = journalEntry.JournalEntryId,
                AccountId = cashAccount.AccountId,
                LineNumber = lineNumber++,
                DebitAmount = 0,
                CreditAmount = totalNetSalary,
                Description = $"Batch net pay payment for {employeeCount} employee(s)"
            });

            // Credit: Accrued Payroll Taxes (Total Company Contributions + Total Deductions)
            if (totalCompanyContribution > 0 || totalDeductions > 0)
            {
                var totalAccrued = totalCompanyContribution + totalDeductions;
                lines.Add(new JournalEntryLine
                {
                    JournalEntryId = journalEntry.JournalEntryId,
                    AccountId = accruedPayrollTaxesAccount.AccountId,
                    LineNumber = lineNumber++,
                    DebitAmount = 0,
                    CreditAmount = totalAccrued,
                    Description = $"Batch company contributions and employee deductions for {employeeCount} employee(s)"
                });
            }

            _context.JournalEntryLines.AddRange(lines);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Batch journal entry {EntryNumber} created for {Count} payroll transactions", entryNumber, employeeCount);

            return journalEntry.JournalEntryId;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating batch payroll journal entry: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Generates a unique journal entry number (e.g., JE-2025-001)
    /// </summary>
    private async Task<string> GenerateEntryNumberAsync()
    {
        var year = DateTime.Now.Year;
        var prefix = $"JE-{year}-";

        var lastEntry = await _context.JournalEntries
            .Where(e => e.EntryNumber.StartsWith(prefix))
            .OrderByDescending(e => e.EntryNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastEntry != null)
        {
            var lastNumberStr = lastEntry.EntryNumber.Replace(prefix, "");
            if (int.TryParse(lastNumberStr, out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D3}";
    }

    /// <summary>
    /// Creates a manual journal entry (requires approval)
    /// </summary>
    public async Task<int> CreateManualJournalEntryAsync(
        DateTime entryDate,
        string description,
        List<ManualJournalEntryLine> lines,
        int createdBy,
        string? notes = null)
    {
        try
        {
            // Validate: Debits must equal Credits
            var totalDebits = lines.Sum(l => l.DebitAmount);
            var totalCredits = lines.Sum(l => l.CreditAmount);

            if (Math.Abs(totalDebits - totalCredits) > 0.01m)
            {
                throw new InvalidOperationException($"Journal entry is not balanced. Debits: {totalDebits:N2}, Credits: {totalCredits:N2}. Difference: {Math.Abs(totalDebits - totalCredits):N2}");
            }

            // Validate: Must have at least 2 lines
            if (lines.Count < 2)
            {
                throw new InvalidOperationException("Journal entry must have at least 2 lines (double-entry bookkeeping).");
            }

            // Validate: All accounts exist and are active
            var accountIds = lines.Select(l => l.AccountId).Distinct().ToList();
            var accounts = await _context.ChartOfAccounts
                .Where(a => accountIds.Contains(a.AccountId) && a.IsActive)
                .ToListAsync();

            if (accounts.Count != accountIds.Count)
            {
                throw new InvalidOperationException("One or more accounts are invalid or inactive.");
            }

            // Generate entry number
            var entryNumber = await GenerateEntryNumberAsync();

            // Create journal entry with Status = "Draft" (requires approval)
            var journalEntry = new JournalEntry
            {
                EntryNumber = entryNumber,
                EntryDate = entryDate,
                Description = description,
                ReferenceType = "Manual",
                ReferenceId = null,
                Status = "Draft", // Requires approval
                CreatedBy = createdBy,
                ApprovedBy = null, // Will be set when approved
                CreatedAt = DateTime.Now
            };

            _context.JournalEntries.Add(journalEntry);
            await _context.SaveChangesAsync(); // Save to get JournalEntryId

            // Create journal entry lines
            var journalEntryLines = new List<JournalEntryLine>();
            int lineNumber = 1;

            foreach (var line in lines)
            {
                journalEntryLines.Add(new JournalEntryLine
                {
                    JournalEntryId = journalEntry.JournalEntryId,
                    AccountId = line.AccountId,
                    LineNumber = lineNumber++,
                    DebitAmount = line.DebitAmount,
                    CreditAmount = line.CreditAmount,
                    Description = line.Description
                });
            }

            _context.JournalEntryLines.AddRange(journalEntryLines);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Manual journal entry {EntryNumber} created by user {CreatedBy} (Status: Draft)", entryNumber, createdBy);

            return journalEntry.JournalEntryId;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating manual journal entry: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Approves a draft journal entry
    /// </summary>
    public async Task ApproveJournalEntryAsync(int journalEntryId, int approvedBy, string? approvalNotes = null)
    {
        try
        {
            var entry = await _context.JournalEntries
                .Include(je => je.JournalEntryLines)
                .FirstOrDefaultAsync(je => je.JournalEntryId == journalEntryId);

            if (entry == null)
            {
                throw new InvalidOperationException($"Journal entry {journalEntryId} not found.");
            }

            if (entry.Status != "Draft")
            {
                throw new InvalidOperationException($"Journal entry {entry.EntryNumber} cannot be approved. Current status: {entry.Status}");
            }

            // Verify entry is balanced
            var totalDebits = entry.JournalEntryLines.Sum(l => l.DebitAmount);
            var totalCredits = entry.JournalEntryLines.Sum(l => l.CreditAmount);

            if (Math.Abs(totalDebits - totalCredits) > 0.01m)
            {
                throw new InvalidOperationException($"Journal entry {entry.EntryNumber} is not balanced. Cannot approve.");
            }

            // Update status and approval info
            entry.Status = "Posted";
            entry.ApprovedBy = approvedBy;
            entry.UpdatedAt = DateTime.Now;

            // Add approval notes to description if provided
            if (!string.IsNullOrWhiteSpace(approvalNotes))
            {
                entry.Description = $"{entry.Description} [Approved: {approvalNotes}]";
            }

            await _context.SaveChangesAsync();

            _logger?.LogInformation("Journal entry {EntryNumber} approved by user {ApprovedBy}", entry.EntryNumber, approvedBy);
            
            // Audit logging (non-blocking, background task)
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_serviceScopeFactory != null)
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var auditLogService = scope.ServiceProvider.GetRequiredService<AuditLogService>();
                        var authService = scope.ServiceProvider.GetService<IAuthService>();
                        
                        var currentUser = authService?.CurrentUser;
                        var userName = currentUser != null ? $"{currentUser.first_name} {currentUser.last_name}".Trim() : "System";
                        var userRole = currentUser?.user_role ?? "System";
                        var userId = approvedBy;
                        
                        var totalDebits = entry.JournalEntryLines.Sum(l => l.DebitAmount);
                        var totalCredits = entry.JournalEntryLines.Sum(l => l.CreditAmount);
                        
                        await auditLogService.CreateTransactionLogAsync(
                            action: "Approve Journal Entry",
                            module: "Finance",
                            description: $"Approved journal entry {entry.EntryNumber}: Debits ₱{totalDebits:N2}, Credits ₱{totalCredits:N2}",
                            userName: userName,
                            userRole: userRole,
                            userId: userId,
                            entityType: "JournalEntry",
                            entityId: journalEntryId.ToString(),
                            oldValues: $"Status: Draft",
                            newValues: $"Status: Posted, ApprovedBy: {approvedBy}, Notes: {approvalNotes ?? "N/A"}",
                            status: "Success",
                            severity: "High"
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to create audit log for journal entry approval: {Message}", ex.Message);
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error approving journal entry {JournalEntryId}: {Message}", journalEntryId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Rejects a draft journal entry
    /// </summary>
    public async Task RejectJournalEntryAsync(int journalEntryId, int rejectedBy, string rejectionReason)
    {
        try
        {
            var entry = await _context.JournalEntries
                .FirstOrDefaultAsync(je => je.JournalEntryId == journalEntryId);

            if (entry == null)
            {
                throw new InvalidOperationException($"Journal entry {journalEntryId} not found.");
            }

            if (entry.Status != "Draft")
            {
                throw new InvalidOperationException($"Journal entry {entry.EntryNumber} cannot be rejected. Current status: {entry.Status}");
            }

            // Update status
            entry.Status = "Rejected";
            entry.ApprovedBy = rejectedBy; // Store who rejected it
            entry.UpdatedAt = DateTime.Now;
            entry.Description = $"{entry.Description} [Rejected: {rejectionReason}]";

            await _context.SaveChangesAsync();

            _logger?.LogInformation("Journal entry {EntryNumber} rejected by user {RejectedBy}. Reason: {Reason}", entry.EntryNumber, rejectedBy, rejectionReason);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error rejecting journal entry {JournalEntryId}: {Message}", journalEntryId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets all pending journal entries (Draft status) for approval
    /// </summary>
    public async Task<List<PendingJournalEntry>> GetPendingJournalEntriesAsync()
    {
        var entries = await _context.JournalEntries
            .Include(je => je.CreatedByUser)
            .Include(je => je.JournalEntryLines)
                .ThenInclude(l => l.Account)
            .Where(je => je.Status == "Draft")
            .OrderBy(je => je.CreatedAt)
            .ToListAsync();

        return entries.Select(je => new PendingJournalEntry
        {
            JournalEntryId = je.JournalEntryId,
            EntryNumber = je.EntryNumber,
            EntryDate = je.EntryDate,
            Description = je.Description ?? string.Empty,
            CreatedBy = je.CreatedBy,
            CreatedByName = je.CreatedByUser != null 
                ? $"{je.CreatedByUser.FirstName} {je.CreatedByUser.LastName}" 
                : $"User ID: {je.CreatedBy}",
            CreatedAt = je.CreatedAt,
            TotalDebits = je.JournalEntryLines.Sum(l => l.DebitAmount),
            TotalCredits = je.JournalEntryLines.Sum(l => l.CreditAmount),
            Lines = je.JournalEntryLines.Select(l => new PendingJournalEntryLine
            {
                AccountCode = l.Account?.AccountCode ?? "N/A",
                AccountName = l.Account?.AccountName ?? "Unknown",
                DebitAmount = l.DebitAmount,
                CreditAmount = l.CreditAmount,
                Description = l.Description ?? string.Empty
            }).ToList()
        }).ToList();
    }

    /// <summary>
    /// Maps expense category to appropriate account code
    /// </summary>
    private string MapExpenseCategoryToAccount(string category)
    {
        // Map common expense categories to account codes
        return category.ToLower() switch
        {
            var c when c.Contains("salary") || c.Contains("wage") || c.Contains("payroll") => "5000", // Salaries Expense
            var c when c.Contains("utility") || c.Contains("electric") || c.Contains("water") => "5200", // Utilities Expense
            var c when c.Contains("supply") || c.Contains("material") => "5300", // Supplies Expense
            var c when c.Contains("rent") || c.Contains("lease") => "5400", // Rent Expense
            var c when c.Contains("maintenance") || c.Contains("repair") => "5500", // Maintenance Expense
            var c when c.Contains("office") => "5600", // Office Expense
            _ => "5100" // Other Expenses (default)
        };
    }
}

// Helper classes for manual journal entries
public class ManualJournalEntryLine
{
    public int AccountId { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class PendingJournalEntry
{
    public int JournalEntryId { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public List<PendingJournalEntryLine> Lines { get; set; } = new();
}

public class PendingJournalEntryLine
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string Description { get; set; } = string.Empty;
}

