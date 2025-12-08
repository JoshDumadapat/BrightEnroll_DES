namespace BrightEnroll_DES.Components.Pages.Admin.Enrollment.EnrollmentCS;

public class ReEnrollmentStudent
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LRN { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public string Documents { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    
    // Payment information
    public decimal TotalFee { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public string PaymentStatus { get; set; } = "Unpaid";
    public bool IsFullyPaid => Balance <= 0 && (PaymentStatus == "Fully Paid" || AmountPaid >= TotalFee);
    
    // Re-enrollment specific fields
    public string? PreviousSchoolYear { get; set; } // The school year the student was last enrolled in
    public bool IsEligible { get; set; } = false; // Whether the student is eligible for re-enrollment
}

