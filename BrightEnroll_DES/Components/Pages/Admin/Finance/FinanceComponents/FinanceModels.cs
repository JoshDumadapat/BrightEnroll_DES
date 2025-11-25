namespace BrightEnroll_DES.Components.Pages.Admin.FinanceComponents;

// Models for finance module
public class FeeModel
{
    public string GradeLevel { get; set; } = "";
    public decimal TuitionFee { get; set; }
    public decimal MiscFee { get; set; }
    public decimal OtherFee { get; set; }
    public decimal DiscountFee { get; set; }
    public decimal TotalFee => TuitionFee + MiscFee + OtherFee - DiscountFee;
    public string Remarks { get; set; } = "";
    public List<FeeBreakdownItem> TuitionBreakdown { get; set; } = new();
    public List<FeeBreakdownItem> MiscBreakdown { get; set; } = new();
    public List<FeeBreakdownItem> OtherBreakdown { get; set; } = new();
    public List<FeeBreakdownItem> DiscountBreakdown { get; set; } = new();
}

public class PaymentDataModel
{
    public string StudentId { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string GradeLevel { get; set; } = "";
    public decimal TotalFee { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public string PaymentStatus { get; set; } = "";
    public decimal NewPaymentAmount { get; set; } = 0;
    public string PaymentMethod { get; set; } = "Cash";
}

public class PaymentRecord
{
    public string StudentId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Grade { get; set; } = "";
    public decimal TotalFee { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public string PaymentStatus { get; set; } = "";
}

public class ExpenseFormModel
{
    public string ExpenseId { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public string Amount { get; set; } = "";
    public DateTime ExpenseDate { get; set; }
    public string Payee { get; set; } = "";
    public string OrNumber { get; set; } = "";
    public string PaymentMethod { get; set; } = "Cash";
    public string ApprovedBy { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string RecordedBy { get; set; } = "";
}

public class ExpenseRecord
{
    public string ExpenseId { get; set; } = "";
    public DateTime ExpenseDate { get; set; }
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public string Payee { get; set; } = "";
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string ReceiptNumber { get; set; } = "";
    public string RecordedBy { get; set; } = "";
    public string ApprovedBy { get; set; } = "";
    public string Status { get; set; } = "";
    public List<AttachmentModel>? Attachments { get; set; }
}

public class AttachmentModel
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
}

public class FeeBreakdownItem
{
    public string Name { get; set; } = "";
    public string AmountString { get; set; } = "";
    public decimal Amount => ParseCurrency(AmountString);
    
    private decimal ParseCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;
        var cleaned = value.Replace("Php", "").Replace(" ", "").Replace(",", "").Trim();
        if (decimal.TryParse(cleaned, out var result))
            return result;
        return 0;
    }
}

public class FeeFormData
{
    public string GradeLevel { get; set; } = "";
    public string TuitionFeeString { get; set; } = "";
    public string MiscFeeString { get; set; } = "";
    public string OtherFeeString { get; set; } = "";
    public string DiscountFeeString { get; set; } = "";
    
    public decimal TuitionFee => ParseCurrency(TuitionFeeString);
    public decimal MiscFee => ParseCurrency(MiscFeeString);
    public decimal OtherFee => ParseCurrency(OtherFeeString);
    public decimal DiscountFee => ParseCurrency(DiscountFeeString);
    
    private decimal ParseCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;
        var cleaned = value.Replace("Php", "").Replace(" ", "").Replace(",", "").Trim();
        if (decimal.TryParse(cleaned, out var result))
            return result;
        return 0;
    }
}

