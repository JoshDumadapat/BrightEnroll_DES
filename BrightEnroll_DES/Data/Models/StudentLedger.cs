using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

/// <summary>
/// Student ledger per school year - tracks all charges and payments for a student in a specific school year
/// </summary>
[Table("tbl_StudentLedgers")]
public class StudentLedger
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(6)]
    [Column("student_id")]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("school_year")]
    public string SchoolYear { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("grade_level")]
    public string? GradeLevel { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Unpaid"; // "Unpaid", "Partially Paid", "Fully Paid"

    [Required]
    [Column("total_charges", TypeName = "decimal(18,2)")]
    public decimal TotalCharges { get; set; } = 0;

    [Required]
    [Column("total_payments", TypeName = "decimal(18,2)")]
    public decimal TotalPayments { get; set; } = 0;

    [Required]
    [Column("balance", TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; } = 0;

    [Required]
    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("StudentId")]
    public virtual Student? Student { get; set; }

    public virtual ICollection<LedgerCharge> Charges { get; set; } = new List<LedgerCharge>();
    public virtual ICollection<LedgerPayment> Payments { get; set; } = new List<LedgerPayment>();
}

