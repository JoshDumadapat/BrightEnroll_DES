namespace BrightEnroll_DES.Components.Pages.Admin.Enrollment.EnrollmentCS;

public class EnrollmentHistoryRecord
{
    public string SchoolYear { get; set; } = "";
    public string GradeLevel { get; set; } = "";
    public string Section { get; set; } = "";
    public string Status { get; set; } = "";
    public string StudentType { get; set; } = "";
    public DateTime? EnrollmentDate { get; set; }
    public string PaymentStatus { get; set; } = "";
    public decimal TotalFee { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public string? UpdatedAt { get; set; }
}

