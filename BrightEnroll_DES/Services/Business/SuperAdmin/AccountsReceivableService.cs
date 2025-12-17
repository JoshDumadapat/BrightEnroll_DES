using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Data.Models.SuperAdmin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.SuperAdmin;

public class AccountsReceivableService
{
    private readonly SuperAdminDbContext _context;
    private readonly ILogger<AccountsReceivableService>? _logger;

    public AccountsReceivableService(SuperAdminDbContext context, ILogger<AccountsReceivableService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task<List<AccountsReceivableSummary>> GetAccountsReceivableSummaryAsync(DateTime? asOfDate = null)
    {
        var cutoffDate = asOfDate ?? DateTime.Now;
        
        var customers = await _context.Customers
            .Where(c => c.Status == "Active")
            .ToListAsync();

        var invoices = await _context.CustomerInvoices
            .Where(i => i.Status != "Cancelled")
            .ToListAsync();

        var payments = await _context.CustomerPayments
            .Where(p => p.PaymentDate <= cutoffDate)
            .GroupBy(p => p.InvoiceId)
            .Select(g => new { InvoiceId = g.Key, TotalPaid = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => x.TotalPaid);

        var summaries = new List<AccountsReceivableSummary>();

        foreach (var customer in customers)
        {
            var customerInvoices = invoices.Where(i => i.CustomerId == customer.CustomerId).ToList();
            
            decimal totalInvoiced = 0;
            decimal totalPaid = 0;
            decimal totalBalance = 0;
            DateTime? oldestInvoiceDate = null;
            DateTime? oldestDueDate = null;

            foreach (var invoice in customerInvoices)
            {
                // Calculate from actual payments (source of truth) - not stored invoice.Balance
                // This ensures accuracy even if invoice.Balance is out of sync
                var paid = payments.GetValueOrDefault(invoice.InvoiceId, 0);
                var balance = invoice.TotalAmount - paid;

                // Verify stored values match calculated values (for data integrity)
                if (Math.Abs(invoice.AmountPaid - paid) > 0.01m || Math.Abs(invoice.Balance - balance) > 0.01m)
                {
                    _logger?.LogWarning(
                        "Invoice {InvoiceId} balance mismatch. Stored: AmountPaid={StoredPaid}, Balance={StoredBalance}. Calculated: AmountPaid={CalcPaid}, Balance={CalcBalance}",
                        invoice.InvoiceId, invoice.AmountPaid, invoice.Balance, paid, balance);
                }

                totalInvoiced += invoice.TotalAmount;
                totalPaid += paid;
                totalBalance += balance;

                if (balance > 0)
                {
                    if (!oldestInvoiceDate.HasValue || invoice.InvoiceDate < oldestInvoiceDate)
                        oldestInvoiceDate = invoice.InvoiceDate;
                    if (!oldestDueDate.HasValue || invoice.DueDate < oldestDueDate)
                        oldestDueDate = invoice.DueDate;
                }
            }

            if (totalBalance > 0)
            {
                int daysOverdue = 0;
                if (oldestDueDate.HasValue && oldestDueDate.Value < cutoffDate)
                {
                    daysOverdue = (int)(cutoffDate - oldestDueDate.Value).TotalDays;
                }

                string agingBucket = GetAgingBucket(daysOverdue);

                summaries.Add(new AccountsReceivableSummary
                {
                    CustomerId = customer.CustomerId,
                    CustomerCode = customer.CustomerCode,
                    CustomerName = customer.SchoolName,
                    TotalInvoiced = totalInvoiced,
                    TotalPaid = totalPaid,
                    Balance = totalBalance,
                    DaysOverdue = daysOverdue,
                    AgingBucket = agingBucket,
                    OldestInvoiceDate = oldestInvoiceDate,
                    OldestDueDate = oldestDueDate,
                    Status = totalBalance > 0 ? (daysOverdue > 0 ? "Overdue" : "Current") : "Paid"
                });
            }
        }

        return summaries.OrderByDescending(s => s.Balance).ToList();
    }

    public async Task<List<CustomerInvoiceDetail>> GetCustomerInvoiceDetailsAsync(int customerId)
    {
        var invoices = await _context.CustomerInvoices
            .Where(i => i.CustomerId == customerId)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        var payments = await _context.CustomerPayments
            .Where(p => invoices.Select(i => i.InvoiceId).Contains(p.InvoiceId))
            .GroupBy(p => p.InvoiceId)
            .Select(g => new { InvoiceId = g.Key, TotalPaid = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => x.TotalPaid);

        var details = new List<CustomerInvoiceDetail>();

        foreach (var invoice in invoices)
        {
            // Calculate from actual payments (source of truth) - ensures accuracy
            var paid = payments.GetValueOrDefault(invoice.InvoiceId, 0);
            var balance = invoice.TotalAmount - paid;
            var daysOverdue = 0;

            if (balance > 0 && invoice.DueDate < DateTime.Now)
            {
                daysOverdue = (int)(DateTime.Now - invoice.DueDate).TotalDays;
            }

            // Determine status from calculated balance (not stored status)
            string calculatedStatus = invoice.Status;
            if (balance <= 0)
            {
                calculatedStatus = "Paid";
            }
            else if (paid > 0)
            {
                calculatedStatus = "Partially Paid";
            }
            else
            {
                calculatedStatus = "Pending";
            }

            if (balance > 0 && invoice.DueDate < DateTime.Now && calculatedStatus != "Overdue")
            {
                calculatedStatus = "Overdue";
            }

            details.Add(new CustomerInvoiceDetail
            {
                InvoiceId = invoice.InvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                Subtotal = invoice.Subtotal,
                VatAmount = invoice.VatAmount,
                TotalAmount = invoice.TotalAmount,
                AmountPaid = paid, // Use calculated value from payments
                Balance = balance, // Use calculated value from payments
                DaysOverdue = daysOverdue,
                Status = calculatedStatus, // Use calculated status
                AgingBucket = GetAgingBucket(daysOverdue)
            });
        }

        return details;
    }

    public async Task<CustomerInvoice> CreateInvoiceAsync(CustomerInvoice invoice, int? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
        {
            invoice.InvoiceNumber = await GenerateInvoiceNumberAsync();
        }

        invoice.Balance = invoice.TotalAmount;
        invoice.Status = "Pending";
        invoice.CreatedBy = createdBy;
        invoice.CreatedAt = DateTime.Now;

        _context.CustomerInvoices.Add(invoice);
        await _context.SaveChangesAsync();

        _logger?.LogInformation("Invoice created: {InvoiceNumber} for customer {CustomerId}", invoice.InvoiceNumber, invoice.CustomerId);
        return invoice;
    }

    public async Task<CustomerPayment> RecordPaymentAsync(CustomerPayment payment, int? createdBy = null)
    {
        payment.CreatedBy = createdBy;
        payment.CreatedAt = DateTime.Now;

        _context.CustomerPayments.Add(payment);

        // Update invoice balance
        var invoice = await _context.CustomerInvoices
            .FirstOrDefaultAsync(i => i.InvoiceId == payment.InvoiceId);

        if (invoice != null)
        {
            invoice.AmountPaid += payment.Amount;
            invoice.Balance = invoice.TotalAmount - invoice.AmountPaid;

            if (invoice.Balance <= 0)
            {
                invoice.Status = "Paid";
                invoice.PaidAt = DateTime.Now;
            }
            else if (invoice.AmountPaid > 0)
            {
                invoice.Status = "Partially Paid";
            }

            // Check if overdue
            if (invoice.Balance > 0 && invoice.DueDate < DateTime.Now && invoice.Status != "Overdue")
            {
                invoice.Status = "Overdue";
            }
        }

        await _context.SaveChangesAsync();

        _logger?.LogInformation("Payment recorded: {PaymentReference} for invoice {InvoiceId}", payment.PaymentReference, payment.InvoiceId);
        return payment;
    }

    /// <summary>
    /// Recalculates invoice balances from actual payments to ensure data consistency
    /// This should be called periodically or after bulk payment operations
    /// </summary>
    public async Task RecalculateInvoiceBalancesAsync()
    {
        try
        {
            var invoices = await _context.CustomerInvoices
                .Where(i => i.Status != "Cancelled")
                .ToListAsync();

            var payments = await _context.CustomerPayments
                .GroupBy(p => p.InvoiceId)
                .Select(g => new { InvoiceId = g.Key, TotalPaid = g.Sum(p => p.Amount) })
                .ToDictionaryAsync(x => x.InvoiceId, x => x.TotalPaid);

            bool hasChanges = false;
            foreach (var invoice in invoices)
            {
                var paid = payments.GetValueOrDefault(invoice.InvoiceId, 0);
                var calculatedBalance = invoice.TotalAmount - paid;
                var calculatedAmountPaid = paid;

                // Update if values don't match (with small tolerance for rounding)
                if (Math.Abs(invoice.AmountPaid - calculatedAmountPaid) > 0.01m ||
                    Math.Abs(invoice.Balance - calculatedBalance) > 0.01m)
                {
                    invoice.AmountPaid = calculatedAmountPaid;
                    invoice.Balance = calculatedBalance;

                    // Update status based on calculated balance
                    if (invoice.Balance <= 0)
                    {
                        invoice.Status = "Paid";
                        invoice.PaidAt = invoice.PaidAt ?? DateTime.Now;
                    }
                    else if (invoice.AmountPaid > 0)
                    {
                        invoice.Status = "Partially Paid";
                    }
                    else
                    {
                        invoice.Status = "Pending";
                    }

                    // Check if overdue
                    if (invoice.Balance > 0 && invoice.DueDate < DateTime.Now && invoice.Status != "Overdue")
                    {
                        invoice.Status = "Overdue";
                    }

                    // Note: updated_at column does not exist in tbl_CustomerInvoices table
                    // invoice.UpdatedAt = DateTime.Now;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
                _logger?.LogInformation("Recalculated and updated invoice balances. {Count} invoices updated.", invoices.Count);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error recalculating invoice balances: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<decimal> GetTotalAccountsReceivableAsync(DateTime? asOfDate = null)
    {
        var cutoffDate = asOfDate ?? DateTime.Now;
        var summaries = await GetAccountsReceivableSummaryAsync(cutoffDate);
        return summaries.Sum(s => s.Balance);
    }

    public async Task<AgingReport> GetAgingReportAsync(DateTime? asOfDate = null)
    {
        var cutoffDate = asOfDate ?? DateTime.Now;
        var summaries = await GetAccountsReceivableSummaryAsync(cutoffDate);

        return new AgingReport
        {
            Current = summaries.Where(s => s.AgingBucket == "Current").Sum(s => s.Balance),
            Days1_30 = summaries.Where(s => s.AgingBucket == "1-30").Sum(s => s.Balance),
            Days31_60 = summaries.Where(s => s.AgingBucket == "31-60").Sum(s => s.Balance),
            Days61_90 = summaries.Where(s => s.AgingBucket == "61-90").Sum(s => s.Balance),
            DaysOver90 = summaries.Where(s => s.AgingBucket == "Over 90").Sum(s => s.Balance),
            Total = summaries.Sum(s => s.Balance)
        };
    }

    private string GetAgingBucket(int daysOverdue)
    {
        if (daysOverdue <= 0) return "Current";
        if (daysOverdue <= 30) return "1-30";
        if (daysOverdue <= 60) return "31-60";
        if (daysOverdue <= 90) return "61-90";
        return "Over 90";
    }

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var year = DateTime.Now.Year;
        var month = DateTime.Now.Month.ToString("00");
        var prefix = $"INV-{year}{month}-";

        var lastInvoice = await _context.CustomerInvoices
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .FirstOrDefaultAsync();

        int sequence = 1;
        if (lastInvoice != null)
        {
            var parts = lastInvoice.InvoiceNumber.Split('-');
            if (parts.Length > 2 && int.TryParse(parts[2], out int lastSeq))
            {
                sequence = lastSeq + 1;
            }
        }

        return $"{prefix}{sequence:0000}";
    }
}

public class AccountsReceivableSummary
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalInvoiced { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }
    public int DaysOverdue { get; set; }
    public string AgingBucket { get; set; } = string.Empty;
    public DateTime? OldestInvoiceDate { get; set; }
    public DateTime? OldestDueDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CustomerInvoiceDetail
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public int DaysOverdue { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AgingBucket { get; set; } = string.Empty;
}

public class AgingReport
{
    public decimal Current { get; set; }
    public decimal Days1_30 { get; set; }
    public decimal Days31_60 { get; set; }
    public decimal Days61_90 { get; set; }
    public decimal DaysOver90 { get; set; }
    public decimal Total { get; set; }
}
