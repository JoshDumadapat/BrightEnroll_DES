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
        // Try to get revenue from General Ledger (Journal Entries) - ERP approach
        // Fallback to direct payments if no journal entries exist yet
        var revenueFromLedger = await _context.JournalEntryLines
            .Include(l => l.Account)
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.EntryDate >= fromDate && 
                       l.JournalEntry.EntryDate <= toDate &&
                       l.JournalEntry.Status == "Posted" &&
                       l.Account.AccountType == "Revenue" &&
                       l.CreditAmount > 0)
            .SumAsync(l => (decimal?)l.CreditAmount) ?? 0m;

        // If no journal entries exist, use direct payments (backward compatibility)
        decimal revenue = revenueFromLedger;
        if (revenueFromLedger == 0)
        {
            revenue = await _context.StudentPayments
                .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;
        }

        // Try to get expenses from General Ledger (Journal Entries) - ERP approach
        // Fallback to direct expenses if no journal entries exist yet
        var expensesFromLedger = await _context.JournalEntryLines
            .Include(l => l.Account)
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.EntryDate >= fromDate && 
                       l.JournalEntry.EntryDate <= toDate &&
                       l.JournalEntry.Status == "Posted" &&
                       l.Account.AccountType == "Expense" &&
                       l.DebitAmount > 0)
            .SumAsync(l => (decimal?)l.DebitAmount) ?? 0m;

        // If no journal entries exist, use direct expenses (backward compatibility)
        decimal expenses = expensesFromLedger;
        if (expensesFromLedger == 0)
        {
            expenses = await _context.Expenses
                .Where(e => e.ExpenseDate >= fromDate && e.ExpenseDate <= toDate && e.Status == "Approved")
                .SumAsync(e => (decimal?)e.Amount) ?? 0m;
        }

        // Expenses by category - Try from General Ledger first, fallback to Expenses table
        List<ExpenseCategorySummary> expensesByCategory;
        
        var expensesByAccount = await _context.JournalEntryLines
            .Include(l => l.Account)
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry.EntryDate >= fromDate && 
                       l.JournalEntry.EntryDate <= toDate &&
                       l.JournalEntry.Status == "Posted" &&
                       l.Account.AccountType == "Expense" &&
                       l.DebitAmount > 0)
            .GroupBy(l => l.Account.AccountName)
            .Select(g => new ExpenseCategorySummary
            {
                Category = g.Key,
                Amount = g.Sum(l => l.DebitAmount)
            })
            .ToListAsync();

        if (expensesByAccount.Any())
        {
            expensesByCategory = expensesByAccount.OrderByDescending(e => e.Amount).ToList();
        }
        else
        {
            // Fallback to direct expenses table
            expensesByCategory = await _context.Expenses
                .Where(e => e.ExpenseDate >= fromDate && e.ExpenseDate <= toDate && e.Status == "Approved")
                .GroupBy(e => e.Category ?? "Uncategorized")
                .Select(g => new ExpenseCategorySummary
                {
                    Category = g.Key,
                    Amount = g.Sum(e => e.Amount)
                })
                .OrderByDescending(e => e.Amount)
                .ToListAsync();
        }

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
        var expenseQuery = _context.Expenses.AsQueryable();

        if (fromDate.HasValue)
            expenseQuery = expenseQuery.Where(e => e.ExpenseDate >= fromDate.Value);

        if (toDate.HasValue)
            expenseQuery = expenseQuery.Where(e => e.ExpenseDate <= toDate.Value);

        var expenses = await expenseQuery.ToListAsync();

        // Get payroll transactions (treated as expenses)
        var payrollQuery = _context.PayrollTransactions.AsQueryable();
        
        if (fromDate.HasValue)
            payrollQuery = payrollQuery.Where(pt => (pt.PaymentDate ?? pt.CreatedAt) >= fromDate.Value);
        
        if (toDate.HasValue)
            payrollQuery = payrollQuery.Where(pt => (pt.PaymentDate ?? pt.CreatedAt) <= toDate.Value);

        var payrollTransactions = await payrollQuery
            .Where(pt => pt.Status == "Paid")
            .ToListAsync();

        // Combine expenses and payroll for category grouping
        var allExpenses = expenses
            .Where(e => e.Status == "Approved")
            .Select(e => new { Category = e.Category ?? "Uncategorized", Amount = e.Amount })
            .ToList();

        // Add payroll as "Payroll" category
        // Use GrossSalary + TotalDeductions to match Income Statement (total company expense)
        var payrollExpenses = payrollTransactions
            .Select(pt => new { Category = "Payroll", Amount = pt.GrossSalary + pt.TotalDeductions })
            .ToList();

        var combinedExpenses = allExpenses.Concat(payrollExpenses).ToList();

        // Group by category (including Payroll)
        var byCategory = combinedExpenses
            .GroupBy(e => e.Category)
            .Select(g => new ExpenseCategorySummary
            {
                Category = g.Key,
                Amount = g.Sum(e => e.Amount),
                Count = g.Count()
            })
            .OrderByDescending(e => e.Amount)
            .ToList();

        // Group by status (expenses only, payroll is always "Paid")
        var expenseStatusGroups = expenses
            .GroupBy(e => e.Status ?? "Pending")
            .Select(g => new ExpenseStatusSummary
            {
                Status = g.Key,
                Amount = g.Sum(e => e.Amount),
                Count = g.Count()
            })
            .ToList();

        // Add payroll status summary
        // Use GrossSalary + TotalDeductions to match Income Statement (total company expense)
        if (payrollTransactions.Any())
        {
            expenseStatusGroups.Add(new ExpenseStatusSummary
            {
                Status = "Payroll (Paid)",
                Amount = payrollTransactions.Sum(pt => pt.GrossSalary + pt.TotalDeductions),
                Count = payrollTransactions.Count
            });
        }

        var byStatus = expenseStatusGroups
            .OrderByDescending(e => e.Amount)
            .ToList();

        // Group by payment method (expenses only, payroll uses "Payroll" method)
        var expensePaymentMethods = expenses
            .Where(e => e.Status == "Approved")
            .GroupBy(e => e.PaymentMethod ?? "Cash")
            .Select(g => new ExpensePaymentMethodSummary
            {
                PaymentMethod = g.Key,
                Amount = g.Sum(e => e.Amount),
                Count = g.Count()
            })
            .ToList();

        // Add payroll payment method
        // Use GrossSalary + TotalDeductions to match Income Statement (total company expense)
        if (payrollTransactions.Any())
        {
            expensePaymentMethods.Add(new ExpensePaymentMethodSummary
            {
                PaymentMethod = "Payroll",
                Amount = payrollTransactions.Sum(pt => pt.GrossSalary + pt.TotalDeductions),
                Count = payrollTransactions.Count
            });
        }

        var byPaymentMethod = expensePaymentMethods
            .OrderByDescending(e => e.Amount)
            .ToList();

        // Monthly trends (combine expenses and payroll)
        var expenseMonthlyTrends = expenses
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
            .ToList();

        var payrollMonthlyTrends = payrollTransactions
            .GroupBy(pt => new { 
                Year = (pt.PaymentDate ?? pt.CreatedAt).Year, 
                Month = (pt.PaymentDate ?? pt.CreatedAt).Month 
            })
            .Select(g => new ExpenseMonthlyTrend
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                Amount = g.Sum(pt => pt.GrossSalary + pt.TotalDeductions), // Total company expense
                Count = g.Count()
            })
            .ToList();

        // Combine and merge monthly trends
        var monthlyTrendsDict = expenseMonthlyTrends
            .Concat(payrollMonthlyTrends)
            .GroupBy(t => new { t.Year, t.Month })
            .Select(g => new ExpenseMonthlyTrend
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                Amount = g.Sum(t => t.Amount),
                Count = g.Sum(t => t.Count)
            })
            .OrderBy(e => e.Year)
            .ThenBy(e => e.Month)
            .ToList();

        var totalExpenses = expenses.Where(e => e.Status == "Approved").Sum(e => e.Amount);
        // Use GrossSalary + TotalDeductions to match Income Statement (total company expense)
        var totalPayroll = payrollTransactions.Sum(pt => pt.GrossSalary + pt.TotalDeductions);

        return new ExpenseAnalysisReport
        {
            TotalExpenses = totalExpenses + totalPayroll, // Include payroll in total
            TotalCount = expenses.Count + payrollTransactions.Count,
            ByCategory = byCategory,
            ByStatus = byStatus,
            ByPaymentMethod = byPaymentMethod,
            MonthlyTrends = monthlyTrendsDict
        };
    }

    #endregion

    #region Cash Flow Report

    public async Task<CashFlowReport> GetCashFlowReportAsync(DateTime fromDate, DateTime toDate)
    {
        // Get Cash account (1000)
        var cashAccount = await _context.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.AccountCode == "1000" && a.IsActive);

        List<CashFlowItem> cashInflows;
        List<CashFlowItem> cashOutflows;

        // Try to get from General Ledger (Journal Entries) - ERP approach
        if (cashAccount != null)
        {
            var cashInflowsFromLedger = await _context.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == cashAccount.AccountId &&
                           l.JournalEntry.EntryDate >= fromDate &&
                           l.JournalEntry.EntryDate <= toDate &&
                           l.JournalEntry.Status == "Posted" &&
                           l.DebitAmount > 0)
                .GroupBy(l => l.JournalEntry.ReferenceType)
                .Select(g => new CashFlowItem
                {
                    Category = g.Key,
                    Amount = g.Sum(l => l.DebitAmount),
                    Type = "Inflow"
                })
                .ToListAsync();

            var cashOutflowsFromLedger = await _context.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == cashAccount.AccountId &&
                           l.JournalEntry.EntryDate >= fromDate &&
                           l.JournalEntry.EntryDate <= toDate &&
                           l.JournalEntry.Status == "Posted" &&
                           l.CreditAmount > 0)
                .GroupBy(l => l.JournalEntry.ReferenceType)
                .Select(g => new CashFlowItem
                {
                    Category = g.Key,
                    Amount = g.Sum(l => l.CreditAmount),
                    Type = "Outflow"
                })
                .ToListAsync();

            if (cashInflowsFromLedger.Any() || cashOutflowsFromLedger.Any())
            {
                cashInflows = cashInflowsFromLedger;
                cashOutflows = cashOutflowsFromLedger;
            }
            else
            {
                // Fallback to direct tables (backward compatibility)
                cashInflows = await _context.StudentPayments
                    .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
                    .GroupBy(p => p.PaymentMethod)
                    .Select(g => new CashFlowItem
                    {
                        Category = g.Key,
                        Amount = g.Sum(p => p.Amount),
                        Type = "Inflow"
                    })
                    .ToListAsync();

                cashOutflows = await _context.Expenses
                    .Where(e => e.ExpenseDate >= fromDate && e.ExpenseDate <= toDate && e.Status == "Approved")
                    .GroupBy(e => e.PaymentMethod ?? "Cash")
                    .Select(g => new CashFlowItem
                    {
                        Category = g.Key,
                        Amount = g.Sum(e => e.Amount),
                        Type = "Outflow"
                    })
                    .ToListAsync();
            }
        }
        else
        {
            // Fallback if Cash account doesn't exist yet
            cashInflows = await _context.StudentPayments
                .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new CashFlowItem
                {
                    Category = g.Key,
                    Amount = g.Sum(p => p.Amount),
                    Type = "Inflow"
                })
                .ToListAsync();

            cashOutflows = await _context.Expenses
                .Where(e => e.ExpenseDate >= fromDate && e.ExpenseDate <= toDate && e.Status == "Approved")
                .GroupBy(e => e.PaymentMethod ?? "Cash")
                .Select(g => new CashFlowItem
                {
                    Category = g.Key,
                    Amount = g.Sum(e => e.Amount),
                    Type = "Outflow"
                })
                .ToListAsync();
        }

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

            // Try to get from General Ledger first
            decimal monthInflow = 0;
            decimal monthOutflow = 0;

            if (cashAccount != null)
            {
                monthInflow = await _context.JournalEntryLines
                    .Include(l => l.JournalEntry)
                    .Where(l => l.AccountId == cashAccount.AccountId &&
                               l.JournalEntry.EntryDate >= monthStart &&
                               l.JournalEntry.EntryDate <= monthEnd &&
                               l.JournalEntry.Status == "Posted" &&
                               l.DebitAmount > 0)
                    .SumAsync(l => (decimal?)l.DebitAmount) ?? 0m;

                monthOutflow = await _context.JournalEntryLines
                    .Include(l => l.JournalEntry)
                    .Where(l => l.AccountId == cashAccount.AccountId &&
                               l.JournalEntry.EntryDate >= monthStart &&
                               l.JournalEntry.EntryDate <= monthEnd &&
                               l.JournalEntry.Status == "Posted" &&
                               l.CreditAmount > 0)
                    .SumAsync(l => (decimal?)l.CreditAmount) ?? 0m;

                // Fallback if no journal entries
                if (monthInflow == 0 && monthOutflow == 0)
                {
                    monthInflow = await _context.StudentPayments
                        .Where(p => p.CreatedAt >= monthStart && p.CreatedAt <= monthEnd)
                        .SumAsync(p => (decimal?)p.Amount) ?? 0m;

                    monthOutflow = await _context.Expenses
                        .Where(e => e.ExpenseDate >= monthStart && e.ExpenseDate <= monthEnd && e.Status == "Approved")
                        .SumAsync(e => (decimal?)e.Amount) ?? 0m;
                }
            }
            else
            {
                // Fallback if Cash account doesn't exist
                monthInflow = await _context.StudentPayments
                    .Where(p => p.CreatedAt >= monthStart && p.CreatedAt <= monthEnd)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0m;

                monthOutflow = await _context.Expenses
                    .Where(e => e.ExpenseDate >= monthStart && e.ExpenseDate <= monthEnd && e.Status == "Approved")
                    .SumAsync(e => (decimal?)e.Amount) ?? 0m;
            }

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

    #region Trial Balance Report

    public async Task<TrialBalanceReport> GetTrialBalanceAsync(DateTime asOfDate)
    {
        // Get all active accounts
        var accounts = await _context.ChartOfAccounts
            .Where(a => a.IsActive)
            .OrderBy(a => a.AccountCode)
            .ToListAsync();

        var trialBalanceItems = new List<TrialBalanceItem>();

        foreach (var account in accounts)
        {
            // Calculate account balance from journal entries up to asOfDate
            var debitTotal = await _context.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == account.AccountId &&
                           l.JournalEntry.EntryDate <= asOfDate &&
                           l.JournalEntry.Status == "Posted" &&
                           l.DebitAmount > 0)
                .SumAsync(l => (decimal?)l.DebitAmount) ?? 0m;

            var creditTotal = await _context.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == account.AccountId &&
                           l.JournalEntry.EntryDate <= asOfDate &&
                           l.JournalEntry.Status == "Posted" &&
                           l.CreditAmount > 0)
                .SumAsync(l => (decimal?)l.CreditAmount) ?? 0m;

            // Calculate balance based on normal balance
            decimal balance = 0;
            if (account.NormalBalance == "Debit")
            {
                // Assets, Expenses: Debit increases, Credit decreases
                balance = debitTotal - creditTotal;
            }
            else
            {
                // Liabilities, Equity, Revenue: Credit increases, Debit decreases
                balance = creditTotal - debitTotal;
            }

            // Only include accounts with activity or non-zero balance
            if (debitTotal > 0 || creditTotal > 0 || balance != 0)
            {
                trialBalanceItems.Add(new TrialBalanceItem
                {
                    AccountCode = account.AccountCode,
                    AccountName = account.AccountName,
                    AccountType = account.AccountType,
                    DebitTotal = debitTotal,
                    CreditTotal = creditTotal,
                    Balance = balance,
                    NormalBalance = account.NormalBalance
                });
            }
        }

        var totalDebits = trialBalanceItems.Sum(t => t.DebitTotal);
        var totalCredits = trialBalanceItems.Sum(t => t.CreditTotal);
        var isBalanced = Math.Abs(totalDebits - totalCredits) < 0.01m;

        return new TrialBalanceReport
        {
            AsOfDate = asOfDate,
            Items = trialBalanceItems,
            TotalDebits = totalDebits,
            TotalCredits = totalCredits,
            IsBalanced = isBalanced,
            Difference = totalDebits - totalCredits
        };
    }

    #endregion

    #region Balance Sheet Report

    public async Task<BalanceSheetReport> GetBalanceSheetAsync(DateTime asOfDate)
    {
        // Get all accounts grouped by type
        var assets = await GetAccountBalancesByTypeAsync("Asset", asOfDate);
        var liabilities = await GetAccountBalancesByTypeAsync("Liability", asOfDate);
        var equity = await GetAccountBalancesByTypeAsync("Equity", asOfDate);

        // Calculate totals
        var totalAssets = assets.Sum(a => a.Balance);
        var totalLiabilities = liabilities.Sum(l => l.Balance);
        var totalEquity = equity.Sum(e => e.Balance);

        // FIXED: Always ensure Retained Earnings includes Net Income from Income Statement
        // Calculate from beginning of year to asOfDate
        var yearStart = new DateTime(asOfDate.Year, 1, 1);
        var incomeStatement = await GetIncomeStatementAsync(yearStart, asOfDate);
        var netIncome = incomeStatement.NetIncome;

        // Find or update Retained Earnings account
        var retainedEarningsAccount = equity.FirstOrDefault(e => e.AccountCode == "3100");
        if (retainedEarningsAccount == null)
        {
            // Account doesn't exist, add it
            equity.Add(new BalanceSheetAccount
            {
                AccountCode = "3100",
                AccountName = "Retained Earnings",
                Balance = netIncome
            });
            totalEquity += netIncome;
        }
        else
        {
            // Account exists, update it with Net Income (replace existing balance)
            // Remove old balance and add new balance
            totalEquity -= retainedEarningsAccount.Balance;
            retainedEarningsAccount.Balance = netIncome;
            totalEquity += netIncome;
        }

        var isBalanced = Math.Abs(totalAssets - (totalLiabilities + totalEquity)) < 0.01m;

        return new BalanceSheetReport
        {
            AsOfDate = asOfDate,
            Assets = assets,
            Liabilities = liabilities,
            Equity = equity,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            TotalEquity = totalEquity,
            IsBalanced = isBalanced,
            Difference = totalAssets - (totalLiabilities + totalEquity)
        };
    }

    private async Task<List<BalanceSheetAccount>> GetAccountBalancesByTypeAsync(string accountType, DateTime asOfDate)
    {
        var accounts = await _context.ChartOfAccounts
            .Where(a => a.IsActive && a.AccountType == accountType)
            .OrderBy(a => a.AccountCode)
            .ToListAsync();

        var balances = new List<BalanceSheetAccount>();

        foreach (var account in accounts)
        {
            var debitTotal = await _context.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == account.AccountId &&
                           l.JournalEntry.EntryDate <= asOfDate &&
                           l.JournalEntry.Status == "Posted" &&
                           l.DebitAmount > 0)
                .SumAsync(l => (decimal?)l.DebitAmount) ?? 0m;

            var creditTotal = await _context.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == account.AccountId &&
                           l.JournalEntry.EntryDate <= asOfDate &&
                           l.JournalEntry.Status == "Posted" &&
                           l.CreditAmount > 0)
                .SumAsync(l => (decimal?)l.CreditAmount) ?? 0m;

            decimal balance = 0;
            if (account.NormalBalance == "Debit")
            {
                balance = debitTotal - creditTotal;
            }
            else
            {
                balance = creditTotal - debitTotal;
            }

            balances.Add(new BalanceSheetAccount
            {
                AccountCode = account.AccountCode,
                AccountName = account.AccountName,
                Balance = balance
            });
        }

        return balances;
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

public class TrialBalanceReport
{
    public DateTime AsOfDate { get; set; }
    public List<TrialBalanceItem> Items { get; set; } = new();
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public bool IsBalanced { get; set; }
    public decimal Difference { get; set; }
}

public class TrialBalanceItem
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal DebitTotal { get; set; }
    public decimal CreditTotal { get; set; }
    public decimal Balance { get; set; }
    public string NormalBalance { get; set; } = string.Empty;
}

public class BalanceSheetReport
{
    public DateTime AsOfDate { get; set; }
    public List<BalanceSheetAccount> Assets { get; set; } = new();
    public List<BalanceSheetAccount> Liabilities { get; set; } = new();
    public List<BalanceSheetAccount> Equity { get; set; } = new();
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public bool IsBalanced { get; set; }
    public decimal Difference { get; set; }
}

public class BalanceSheetAccount
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

#endregion

