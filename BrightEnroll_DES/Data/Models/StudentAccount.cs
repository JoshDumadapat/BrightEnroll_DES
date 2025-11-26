using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_StudentAccounts")]
public class StudentAccount
{
    [Key]
    [Column("account_ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AccountId { get; set; }

    [Required]
    [MaxLength(6)]
    [Column("student_id")]
    public string StudentId { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("school_year")]
    public string? SchoolYear { get; set; }

    [MaxLength(10)]
    [Column("grade_level")]
    public string? GradeLevel { get; set; }

    [Required]
    [Column("assessment_amount", TypeName = "decimal(18,2)")]
    public decimal AssessmentAmount { get; set; } = 0.00m;

    [Required]
    [Column("amount_paid", TypeName = "decimal(18,2)")]
    public decimal AmountPaid { get; set; } = 0.00m;

    [Column("balance", TypeName = "decimal(18,2)")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal Balance { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("payment_status")]
    public string PaymentStatus { get; set; } = "Unpaid";

    [Required]
    [Column("created_date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Column("updated_date")]
    public DateTime? UpdatedDate { get; set; }

    [MaxLength(50)]
    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [MaxLength(50)]
    [Column("updated_by")]
    public string? UpdatedBy { get; set; }

    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    [ForeignKey("StudentId")]
    public virtual Student? Student { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

