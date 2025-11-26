using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_Payments")]
public class Payment
{
    [Key]
    [Column("payment_ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PaymentId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("or_number")]
    public string ORNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(6)]
    [Column("student_id")]
    public string StudentId { get; set; } = string.Empty;

    [Column("account_id")]
    public int? AccountId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("payment_type")]
    public string PaymentType { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("payment_method")]
    public string PaymentMethod { get; set; } = string.Empty;

    [Required]
    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(100)]
    [Column("reference_number")]
    public string? ReferenceNumber { get; set; }

    [MaxLength(500)]
    [Column("remarks")]
    public string? Remarks { get; set; }

    [Required]
    [Column("transaction_date")]
    public DateTime TransactionDate { get; set; } = DateTime.Now;

    [Column("processed_by")]
    public int? ProcessedBy { get; set; }

    [Required]
    [Column("created_date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [MaxLength(50)]
    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [Required]
    [Column("is_void")]
    public bool IsVoid { get; set; } = false;

    // Navigation properties
    [ForeignKey("StudentId")]
    public virtual Student? Student { get; set; }

    [ForeignKey("AccountId")]
    public virtual StudentAccount? Account { get; set; }

    [ForeignKey("ProcessedBy")]
    public virtual UserEntity? ProcessedByUser { get; set; }
}

