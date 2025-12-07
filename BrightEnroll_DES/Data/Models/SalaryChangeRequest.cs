using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

/// <summary>
/// Tracks salary change requests from HR that require Payroll/Admin approval
/// </summary>
[Table("tbl_salary_change_requests")]
public class SalaryChangeRequest
{
    [Key]
    [Column("request_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int RequestId { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("current_base_salary", TypeName = "decimal(12,2)")]
    public decimal CurrentBaseSalary { get; set; }

    [Required]
    [Column("current_allowance", TypeName = "decimal(12,2)")]
    public decimal CurrentAllowance { get; set; }

    [Required]
    [Column("requested_base_salary", TypeName = "decimal(12,2)")]
    public decimal RequestedBaseSalary { get; set; }

    [Required]
    [Column("requested_allowance", TypeName = "decimal(12,2)")]
    public decimal RequestedAllowance { get; set; }

    [MaxLength(500)]
    [Column("reason", TypeName = "nvarchar(500)")]
    public string? Reason { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

    [MaxLength(500)]
    [Column("rejection_reason", TypeName = "nvarchar(500)")]
    public string? RejectionReason { get; set; }

    [Required]
    [Column("requested_by")]
    public int RequestedBy { get; set; } // HR user who created the request

    [Column("approved_by")]
    public int? ApprovedBy { get; set; } // Admin/Payroll user who approved/rejected

    [Required]
    [Column("requested_at", TypeName = "datetime")]
    public DateTime RequestedAt { get; set; } = DateTime.Now;

    [Column("approved_at", TypeName = "datetime")]
    public DateTime? ApprovedAt { get; set; }

    [Column("effective_date", TypeName = "date")]
    public DateTime? EffectiveDate { get; set; } // Date when the approved salary change takes effect

    [Required]
    [MaxLength(20)]
    [Column("school_year")]
    public string SchoolYear { get; set; } = string.Empty;

    [Column("is_initial_registration")]
    public bool IsInitialRegistration { get; set; } = false; // True if from Add Employee, False if from Edit

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual UserEntity? User { get; set; }

    [ForeignKey("RequestedBy")]
    public virtual UserEntity? RequestedByUser { get; set; }

    [ForeignKey("ApprovedBy")]
    public virtual UserEntity? ApprovedByUser { get; set; }
}

