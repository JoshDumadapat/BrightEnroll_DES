using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace BrightEnroll_DES.Services.Cashier;

// Service for cashier dashboard and payment operations
public class CashierService
{
    private readonly AppDbContext _context;

    public CashierService(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // Gets today's collections (total payments made today)
    public async Task<decimal> GetTodayCollectionsAsync()
    {
        try
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var total = await _context.Payments
                .Where(p => p.TransactionDate >= today && 
                           p.TransactionDate < tomorrow && 
                           !p.IsVoid)
                .SumAsync(p => p.Amount);

            return total;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get today's collections: {ex.Message}", ex);
        }
    }

    // Gets count of pending payments (students with partial or unpaid status)
    public async Task<int> GetPendingPaymentsCountAsync()
    {
        try
        {
            var count = await _context.StudentAccounts
                .Where(a => a.IsActive && 
                           (a.PaymentStatus == "Partial" || a.PaymentStatus == "Unpaid"))
                .CountAsync();

            return count;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get pending payments count: {ex.Message}", ex);
        }
    }

    // Gets count of transactions today
    public async Task<int> GetTransactionsTodayCountAsync()
    {
        try
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var count = await _context.Payments
                .Where(p => p.TransactionDate >= today && 
                           p.TransactionDate < tomorrow && 
                           !p.IsVoid)
                .CountAsync();

            return count;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get transactions today count: {ex.Message}", ex);
        }
    }

    // Gets total outstanding balance (sum of all unpaid balances)
    public async Task<decimal> GetOutstandingBalanceAsync()
    {
        try
        {
            var total = await _context.StudentAccounts
                .Where(a => a.IsActive && a.Balance > 0)
                .SumAsync(a => a.Balance);

            return total;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get outstanding balance: {ex.Message}", ex);
        }
    }

    // Gets recent transactions (last 10 transactions)
    public async Task<List<TransactionItem>> GetRecentTransactionsAsync(int count = 10)
    {
        try
        {
            var transactions = await _context.Payments
                .Include(p => p.Student)
                .Where(p => !p.IsVoid)
                .OrderByDescending(p => p.TransactionDate)
                .Take(count)
                .Select(p => new TransactionItem
                {
                    StudentName = p.Student != null 
                        ? $"{p.Student.FirstName} {p.Student.LastName}" 
                        : "Unknown",
                    PaymentType = p.PaymentType,
                    Amount = p.Amount,
                    ORNumber = p.ORNumber,
                    TimeStamp = p.TransactionDate
                })
                .ToListAsync();

            return transactions;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get recent transactions: {ex.Message}", ex);
        }
    }

    // Generates a new OR number using stored procedure
    public async Task<string> GenerateORNumberAsync()
    {
        try
        {
            var orNumberParam = new SqlParameter("@or_number", System.Data.SqlDbType.VarChar, 50)
            {
                Direction = System.Data.ParameterDirection.Output
            };

            await _context.Database.ExecuteSqlRawAsync("EXEC [dbo].[sp_GenerateORNumber] @or_number OUTPUT", orNumberParam);

            var orNumber = orNumberParam.Value?.ToString() ?? string.Empty;
            
            if (string.IsNullOrEmpty(orNumber))
            {
                throw new Exception("Failed to generate OR number");
            }

            return orNumber;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to generate OR number: {ex.Message}", ex);
        }
    }

    // Gets dashboard summary data
    public async Task<DashboardSummary> GetDashboardSummaryAsync()
    {
        try
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var todayCollections = await _context.Payments
                .Where(p => p.TransactionDate >= today && 
                           p.TransactionDate < tomorrow && 
                           !p.IsVoid)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var pendingPayments = await _context.StudentAccounts
                .Where(a => a.IsActive && 
                           (a.PaymentStatus == "Partial" || a.PaymentStatus == "Unpaid"))
                .CountAsync();

            var transactionsToday = await _context.Payments
                .Where(p => p.TransactionDate >= today && 
                           p.TransactionDate < tomorrow && 
                           !p.IsVoid)
                .CountAsync();

            var outstandingBalance = await _context.StudentAccounts
                .Where(a => a.IsActive && a.Balance > 0)
                .SumAsync(a => (decimal?)a.Balance) ?? 0;

            var recentTransactions = await GetRecentTransactionsAsync(10);

            return new DashboardSummary
            {
                TodayCollections = todayCollections,
                PendingPayments = pendingPayments,
                TransactionsToday = transactionsToday,
                OutstandingBalance = outstandingBalance,
                RecentTransactions = recentTransactions
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get dashboard summary: {ex.Message}", ex);
        }
    }

    // Gets all payment transactions with optional filtering
    public async Task<List<PaymentTransaction>> GetPaymentTransactionsAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? paymentType = null,
        string? searchTerm = null)
    {
        try
        {
            var query = _context.Payments
                .Include(p => p.Student)
                .Where(p => !p.IsVoid)
                .AsQueryable();

            // Apply date filters
            if (dateFrom.HasValue)
            {
                query = query.Where(p => p.TransactionDate.Date >= dateFrom.Value.Date);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(p => p.TransactionDate.Date <= dateTo.Value.Date);
            }

            // Apply payment type filter
            if (!string.IsNullOrWhiteSpace(paymentType))
            {
                query = query.Where(p => p.PaymentType == paymentType);
            }

            // Apply search term filter (student name, OR number, or student ID)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p =>
                    (p.Student != null && 
                     (p.Student.FirstName.Contains(searchTerm) || 
                      p.Student.LastName.Contains(searchTerm) ||
                      p.Student.MiddleName != null && p.Student.MiddleName.Contains(searchTerm))) ||
                    p.ORNumber.Contains(searchTerm) ||
                    p.StudentId.Contains(searchTerm));
            }

            var transactions = await query
                .OrderByDescending(p => p.TransactionDate)
                .Select(p => new PaymentTransaction
                {
                    ORNumber = p.ORNumber,
                    TransactionDate = p.TransactionDate,
                    StudentId = p.StudentId,
                    StudentName = p.Student != null
                        ? $"{p.Student.FirstName} {p.Student.MiddleName} {p.Student.LastName}".Trim()
                        : "Unknown",
                    PaymentType = p.PaymentType,
                    PaymentMethod = p.PaymentMethod,
                    Amount = p.Amount
                })
                .ToListAsync();

            return transactions;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get payment transactions: {ex.Message}", ex);
        }
    }

    // Searches for a student by ID or name
    public async Task<StudentAccountInfo?> SearchStudentAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return null;
            }

            // Search by student ID or name
            var student = await _context.Students
                .Where(s => s.StudentId.Contains(searchTerm) ||
                           s.FirstName.Contains(searchTerm) ||
                           s.LastName.Contains(searchTerm) ||
                           (s.MiddleName != null && s.MiddleName.Contains(searchTerm)))
                .FirstOrDefaultAsync();

            if (student == null)
            {
                return null;
            }

            // Get or create student account
            var account = await _context.StudentAccounts
                .Where(a => a.StudentId == student.StudentId && a.IsActive)
                .FirstOrDefaultAsync();

            // If no account exists, create one (assessment will be 0, can be updated later)
            if (account == null)
            {
                account = new StudentAccount
                {
                    StudentId = student.StudentId,
                    SchoolYear = student.SchoolYr,
                    GradeLevel = student.GradeLevel,
                    AssessmentAmount = 0.00m,
                    AmountPaid = 0.00m,
                    PaymentStatus = "Unpaid",
                    IsActive = true
                };

                _context.StudentAccounts.Add(account);
                await _context.SaveChangesAsync();
            }

            // Build full name
            var fullName = $"{student.FirstName} {student.MiddleName} {student.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(student.Suffix))
            {
                fullName += $" {student.Suffix}";
            }

            return new StudentAccountInfo
            {
                AccountId = account.AccountId,
                StudentId = student.StudentId,
                FullName = fullName,
                GradeLevel = account.GradeLevel ?? student.GradeLevel ?? "N/A",
                TotalAssessment = account.AssessmentAmount,
                TotalPaid = account.AmountPaid,
                Balance = account.Balance
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to search student: {ex.Message}", ex);
        }
    }

    // Gets student account by student ID
    public async Task<StudentAccountInfo?> GetStudentAccountAsync(string studentId)
    {
        try
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                return null;
            }

            // Get or create student account
            var account = await _context.StudentAccounts
                .Where(a => a.StudentId == studentId && a.IsActive)
                .FirstOrDefaultAsync();

            // If no account exists, create one
            if (account == null)
            {
                account = new StudentAccount
                {
                    StudentId = student.StudentId,
                    SchoolYear = student.SchoolYr,
                    GradeLevel = student.GradeLevel,
                    AssessmentAmount = 0.00m,
                    AmountPaid = 0.00m,
                    PaymentStatus = "Unpaid",
                    IsActive = true
                };

                _context.StudentAccounts.Add(account);
                await _context.SaveChangesAsync();
            }

            // Build full name
            var fullName = $"{student.FirstName} {student.MiddleName} {student.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(student.Suffix))
            {
                fullName += $" {student.Suffix}";
            }

            return new StudentAccountInfo
            {
                AccountId = account.AccountId,
                StudentId = student.StudentId,
                FullName = fullName,
                GradeLevel = account.GradeLevel ?? student.GradeLevel ?? "N/A",
                TotalAssessment = account.AssessmentAmount,
                TotalPaid = account.AmountPaid,
                Balance = account.Balance
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get student account: {ex.Message}", ex);
        }
    }

    // Processes a payment and saves it to the database
    public async Task<string> ProcessPaymentAsync(ProcessPaymentRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Validate student account exists
            var account = await _context.StudentAccounts
                .FirstOrDefaultAsync(a => a.AccountId == request.AccountId && a.IsActive);

            if (account == null)
            {
                throw new Exception("Student account not found");
            }

            // Validate payment amount doesn't exceed balance (unless it's a full payment)
            if (request.PaymentType != "Full Payment" && request.Amount > account.Balance)
            {
                throw new Exception("Payment amount cannot exceed the outstanding balance");
            }

            // If it's a full payment, set amount to balance
            decimal paymentAmount = request.PaymentType == "Full Payment" 
                ? account.Balance 
                : request.Amount;

            if (paymentAmount <= 0)
            {
                throw new Exception("Payment amount must be greater than zero");
            }

            // Generate OR number
            var orNumber = await GenerateORNumberAsync();

            // Get current user ID (cashier processing the payment)
            var currentUserId = request.ProcessedByUserId;

            // Create payment record
            var payment = new Payment
            {
                ORNumber = orNumber,
                StudentId = account.StudentId,
                AccountId = account.AccountId,
                PaymentType = request.PaymentType,
                PaymentMethod = request.PaymentMethod,
                Amount = paymentAmount,
                ReferenceNumber = string.IsNullOrWhiteSpace(request.ReferenceNumber) ? null : request.ReferenceNumber,
                Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks,
                TransactionDate = DateTime.Now,
                ProcessedBy = currentUserId,
                CreatedBy = request.ProcessedByUserId?.ToString(),
                IsVoid = false
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Reload account to get updated balance (trigger updates amount_paid)
            await _context.Entry(account).ReloadAsync();

            // Update payment status based on balance
            if (account.Balance <= 0)
            {
                account.PaymentStatus = "Fully Paid";
            }
            else if (account.AmountPaid > 0)
            {
                account.PaymentStatus = "Partial";
            }
            else
            {
                account.PaymentStatus = "Unpaid";
            }

            account.UpdatedDate = DateTime.Now;
            account.UpdatedBy = request.ProcessedByUserId?.ToString();

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return orNumber;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new Exception($"Failed to process payment: {ex.Message}", ex);
        }
    }

    // Gets daily collection report (today's transactions)
    public async Task<DailyCollectionReport> GetDailyCollectionReportAsync()
    {
        try
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var transactions = await _context.Payments
                .Include(p => p.Student)
                .Where(p => p.TransactionDate >= today && 
                           p.TransactionDate < tomorrow && 
                           !p.IsVoid)
                .OrderByDescending(p => p.TransactionDate)
                .ToListAsync();

            var totalCollection = transactions.Sum(p => p.Amount);
            var transactionCount = transactions.Count;

            var paymentTypeBreakdown = transactions
                .GroupBy(p => p.PaymentType)
                .Select(g => new PaymentTypeSummary
                {
                    PaymentType = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .OrderByDescending(s => s.TotalAmount)
                .ToList();

            return new DailyCollectionReport
            {
                ReportDate = today,
                TotalCollection = totalCollection,
                TransactionCount = transactionCount,
                Transactions = transactions.Select(p => new PaymentTransaction
                {
                    ORNumber = p.ORNumber,
                    TransactionDate = p.TransactionDate,
                    StudentId = p.StudentId,
                    StudentName = p.Student != null
                        ? $"{p.Student.FirstName} {p.Student.MiddleName} {p.Student.LastName}".Trim()
                        : "Unknown",
                    PaymentType = p.PaymentType,
                    PaymentMethod = p.PaymentMethod,
                    Amount = p.Amount
                }).ToList(),
                PaymentTypeBreakdown = paymentTypeBreakdown
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get daily collection report: {ex.Message}", ex);
        }
    }

    // Gets monthly collection report (current month)
    public async Task<MonthlyCollectionReport> GetMonthlyCollectionReportAsync()
    {
        try
        {
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            var transactions = await _context.Payments
                .Include(p => p.Student)
                .Where(p => p.TransactionDate >= startOfMonth && 
                           p.TransactionDate < endOfMonth && 
                           !p.IsVoid)
                .OrderByDescending(p => p.TransactionDate)
                .ToListAsync();

            var totalCollection = transactions.Sum(p => p.Amount);
            var transactionCount = transactions.Count;

            var dailyBreakdown = transactions
                .GroupBy(p => p.TransactionDate.Date)
                .Select(g => new DailySummary
                {
                    Date = g.Key,
                    TransactionCount = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .OrderByDescending(d => d.Date)
                .ToList();

            var paymentTypeBreakdown = transactions
                .GroupBy(p => p.PaymentType)
                .Select(g => new PaymentTypeSummary
                {
                    PaymentType = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .OrderByDescending(s => s.TotalAmount)
                .ToList();

            return new MonthlyCollectionReport
            {
                Month = now.Month,
                Year = now.Year,
                TotalCollection = totalCollection,
                TransactionCount = transactionCount,
                DailyBreakdown = dailyBreakdown,
                PaymentTypeBreakdown = paymentTypeBreakdown
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get monthly collection report: {ex.Message}", ex);
        }
    }

    // Gets payment summary report (by payment type)
    public async Task<PaymentSummaryReport> GetPaymentSummaryReportAsync(DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        try
        {
            var query = _context.Payments
                .Where(p => !p.IsVoid)
                .AsQueryable();

            if (dateFrom.HasValue)
            {
                query = query.Where(p => p.TransactionDate.Date >= dateFrom.Value.Date);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(p => p.TransactionDate.Date <= dateTo.Value.Date);
            }

            var transactions = await query.ToListAsync();

            var paymentTypeSummary = transactions
                .GroupBy(p => p.PaymentType)
                .Select(g => new PaymentTypeSummary
                {
                    PaymentType = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .OrderByDescending(s => s.TotalAmount)
                .ToList();

            var paymentMethodSummary = transactions
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new PaymentMethodSummary
                {
                    PaymentMethod = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .OrderByDescending(s => s.TotalAmount)
                .ToList();

            return new PaymentSummaryReport
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                TotalCollection = transactions.Sum(p => p.Amount),
                TotalTransactions = transactions.Count,
                PaymentTypeSummary = paymentTypeSummary,
                PaymentMethodSummary = paymentMethodSummary
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get payment summary report: {ex.Message}", ex);
        }
    }

    // Gets account status report (student balances)
    public async Task<AccountStatusReport> GetAccountStatusReportAsync()
    {
        try
        {
            var accounts = await _context.StudentAccounts
                .Include(a => a.Student)
                .Where(a => a.IsActive)
                .OrderBy(a => a.StudentId)
                .ToListAsync();

            var statusBreakdown = accounts
                .GroupBy(a => a.PaymentStatus)
                .Select(g => new StatusSummary
                {
                    Status = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(a => a.AssessmentAmount),
                    TotalPaid = g.Sum(a => a.AmountPaid),
                    TotalBalance = g.Sum(a => a.Balance)
                })
                .ToList();

            return new AccountStatusReport
            {
                TotalStudents = accounts.Count,
                TotalAssessment = accounts.Sum(a => a.AssessmentAmount),
                TotalCollected = accounts.Sum(a => a.AmountPaid),
                TotalOutstanding = accounts.Sum(a => a.Balance),
                StatusBreakdown = statusBreakdown,
                Accounts = accounts.Select(a => new StudentAccountInfo
                {
                    AccountId = a.AccountId,
                    StudentId = a.StudentId,
                    FullName = a.Student != null
                        ? $"{a.Student.FirstName} {a.Student.MiddleName} {a.Student.LastName}".Trim()
                        : "Unknown",
                    GradeLevel = a.GradeLevel ?? "N/A",
                    TotalAssessment = a.AssessmentAmount,
                    TotalPaid = a.AmountPaid,
                    Balance = a.Balance
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get account status report: {ex.Message}", ex);
        }
    }

    // Gets all student accounts with optional filtering
    public async Task<List<StudentAccountInfo>> GetAllStudentAccountsAsync(
        string? gradeLevel = null,
        string? paymentStatus = null,
        string? searchTerm = null)
    {
        try
        {
            var query = _context.StudentAccounts
                .Include(a => a.Student)
                .Where(a => a.IsActive)
                .AsQueryable();

            // Apply grade level filter
            if (!string.IsNullOrWhiteSpace(gradeLevel))
            {
                query = query.Where(a => a.GradeLevel == gradeLevel);
            }

            // Apply payment status filter
            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                query = query.Where(a => a.PaymentStatus == paymentStatus);
            }

            // Apply search term filter (student name or ID)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(a =>
                    (a.Student != null &&
                     (a.Student.FirstName.Contains(searchTerm) ||
                      a.Student.LastName.Contains(searchTerm) ||
                      (a.Student.MiddleName != null && a.Student.MiddleName.Contains(searchTerm)))) ||
                    a.StudentId.Contains(searchTerm));
            }

            var accounts = await query
                .OrderBy(a => a.StudentId)
                .ToListAsync();

            var result = accounts.Select(a =>
            {
                // Build full name
                var fullName = a.Student != null
                    ? $"{a.Student.FirstName} {a.Student.MiddleName} {a.Student.LastName}".Trim()
                    : "Unknown";
                
                if (a.Student != null && !string.IsNullOrWhiteSpace(a.Student.Suffix))
                {
                    fullName += $" {a.Student.Suffix}";
                }

                return new StudentAccountInfo
                {
                    AccountId = a.AccountId,
                    StudentId = a.StudentId,
                    FullName = fullName,
                    GradeLevel = a.GradeLevel ?? a.Student?.GradeLevel ?? "N/A",
                    TotalAssessment = a.AssessmentAmount,
                    TotalPaid = a.AmountPaid,
                    Balance = a.Balance
                };
            }).ToList();

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get student accounts: {ex.Message}", ex);
        }
    }

    // Gets outstanding balance report (unpaid accounts)
    public async Task<OutstandingBalanceReport> GetOutstandingBalanceReportAsync()
    {
        try
        {
            var accounts = await _context.StudentAccounts
                .Include(a => a.Student)
                .Where(a => a.IsActive && a.Balance > 0)
                .OrderByDescending(a => a.Balance)
                .ToListAsync();

            var totalOutstanding = accounts.Sum(a => a.Balance);
            var totalStudents = accounts.Count;

            var byGradeLevel = accounts
                .GroupBy(a => a.GradeLevel ?? "N/A")
                .Select(g => new GradeLevelSummary
                {
                    GradeLevel = g.Key,
                    StudentCount = g.Count(),
                    TotalOutstanding = g.Sum(a => a.Balance)
                })
                .OrderByDescending(g => g.TotalOutstanding)
                .ToList();

            return new OutstandingBalanceReport
            {
                TotalOutstanding = totalOutstanding,
                TotalStudents = totalStudents,
                GradeLevelBreakdown = byGradeLevel,
                Accounts = accounts.Select(a => new StudentAccountInfo
                {
                    AccountId = a.AccountId,
                    StudentId = a.StudentId,
                    FullName = a.Student != null
                        ? $"{a.Student.FirstName} {a.Student.MiddleName} {a.Student.LastName}".Trim()
                        : "Unknown",
                    GradeLevel = a.GradeLevel ?? "N/A",
                    TotalAssessment = a.AssessmentAmount,
                    TotalPaid = a.AmountPaid,
                    Balance = a.Balance
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get outstanding balance report: {ex.Message}", ex);
        }
    }

    // Gets custom date range report
    public async Task<CustomDateRangeReport> GetCustomDateRangeReportAsync(DateTime dateFrom, DateTime dateTo)
    {
        try
        {
            var transactions = await _context.Payments
                .Include(p => p.Student)
                .Where(p => p.TransactionDate.Date >= dateFrom.Date && 
                           p.TransactionDate.Date <= dateTo.Date && 
                           !p.IsVoid)
                .OrderByDescending(p => p.TransactionDate)
                .ToListAsync();

            var totalCollection = transactions.Sum(p => p.Amount);
            var transactionCount = transactions.Count;

            var dailyBreakdown = transactions
                .GroupBy(p => p.TransactionDate.Date)
                .Select(g => new DailySummary
                {
                    Date = g.Key,
                    TransactionCount = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .OrderByDescending(d => d.Date)
                .ToList();

            return new CustomDateRangeReport
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                TotalCollection = totalCollection,
                TransactionCount = transactionCount,
                DailyBreakdown = dailyBreakdown,
                Transactions = transactions.Select(p => new PaymentTransaction
                {
                    ORNumber = p.ORNumber,
                    TransactionDate = p.TransactionDate,
                    StudentId = p.StudentId,
                    StudentName = p.Student != null
                        ? $"{p.Student.FirstName} {p.Student.MiddleName} {p.Student.LastName}".Trim()
                        : "Unknown",
                    PaymentType = p.PaymentType,
                    PaymentMethod = p.PaymentMethod,
                    Amount = p.Amount
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get custom date range report: {ex.Message}", ex);
        }
    }
}

// DTOs for cashier operations
public class TransactionItem
{
    public string StudentName { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ORNumber { get; set; } = string.Empty;
    public DateTime TimeStamp { get; set; }
}

public class PaymentTransaction
{
    public string ORNumber { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class StudentAccountInfo
{
    public int AccountId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public decimal TotalAssessment { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }
}

public class ProcessPaymentRequest
{
    public int AccountId { get; set; }
    public string PaymentType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? Remarks { get; set; }
    public int? ProcessedByUserId { get; set; }
}

public class DashboardSummary
{
    public decimal TodayCollections { get; set; }
    public int PendingPayments { get; set; }
    public int TransactionsToday { get; set; }
    public decimal OutstandingBalance { get; set; }
    public List<TransactionItem> RecentTransactions { get; set; } = new();
}

// Report DTOs
public class DailyCollectionReport
{
    public DateTime ReportDate { get; set; }
    public decimal TotalCollection { get; set; }
    public int TransactionCount { get; set; }
    public List<PaymentTransaction> Transactions { get; set; } = new();
    public List<PaymentTypeSummary> PaymentTypeBreakdown { get; set; } = new();
}

public class MonthlyCollectionReport
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalCollection { get; set; }
    public int TransactionCount { get; set; }
    public List<DailySummary> DailyBreakdown { get; set; } = new();
    public List<PaymentTypeSummary> PaymentTypeBreakdown { get; set; } = new();
}

public class PaymentSummaryReport
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public decimal TotalCollection { get; set; }
    public int TotalTransactions { get; set; }
    public List<PaymentTypeSummary> PaymentTypeSummary { get; set; } = new();
    public List<PaymentMethodSummary> PaymentMethodSummary { get; set; } = new();
}

public class AccountStatusReport
{
    public int TotalStudents { get; set; }
    public decimal TotalAssessment { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalOutstanding { get; set; }
    public List<StatusSummary> StatusBreakdown { get; set; } = new();
    public List<StudentAccountInfo> Accounts { get; set; } = new();
}

public class OutstandingBalanceReport
{
    public decimal TotalOutstanding { get; set; }
    public int TotalStudents { get; set; }
    public List<GradeLevelSummary> GradeLevelBreakdown { get; set; } = new();
    public List<StudentAccountInfo> Accounts { get; set; } = new();
}

public class CustomDateRangeReport
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public decimal TotalCollection { get; set; }
    public int TransactionCount { get; set; }
    public List<DailySummary> DailyBreakdown { get; set; } = new();
    public List<PaymentTransaction> Transactions { get; set; } = new();
}

public class PaymentTypeSummary
{
    public string PaymentType { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
}

public class PaymentMethodSummary
{
    public string PaymentMethod { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
}

public class DailySummary
{
    public DateTime Date { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class StatusSummary
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalBalance { get; set; }
}

public class GradeLevelSummary
{
    public string GradeLevel { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public decimal TotalOutstanding { get; set; }
}

