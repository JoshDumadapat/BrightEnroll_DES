using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Logs student payments with OR numbers
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
    public string PaymentMethod { get; set; } = string.Empty; 
    [Required]
    [MaxLength(50)]
    [Column("or_number")]
    public string OrNumber { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("processed_by")]
    public string? ProcessedBy { get; set; }

    [MaxLength(20)]
    [Column("school_year")]
    public string? SchoolYear { get; set; } 

    [Required]
    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey("StudentId")]
    public virtual Student? Student { get; set; }
}


