using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

/// <summary>
/// Logs all student payments with OR numbers for audit and receipt history.
/// </summary>
[Table("tbl_StudentPayments")]
public class StudentPayment
{
    [Key]
    [Column("payment_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PaymentId { get; set; }

    [Required]
    [MaxLength(6)]
    [Column("student_id")]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("payment_method")]
    public string PaymentMethod { get; set; } = string.Empty; // cash, bank, gcash, etc.

    [Required]
    [MaxLength(50)]
    [Column("or_number")]
    public string OrNumber { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("processed_by")]
    public string? ProcessedBy { get; set; }

    [Required]
    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey("StudentId")]
    public virtual Student? Student { get; set; }
}


