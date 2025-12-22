using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Payment records linked to student ledger
[Table("tbl_LedgerPayments")]
public class LedgerPayment
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("ledger_id")]
    public int LedgerId { get; set; }

    [Required]
    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("or_number")]
    public string OrNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("payment_method")]
    public string PaymentMethod { get; set; } = "Cash"; 

    [MaxLength(100)]
    [Column("processed_by")]
    public string? ProcessedBy { get; set; }

    [Required]
    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation property
    [ForeignKey("LedgerId")]
    public virtual StudentLedger? Ledger { get; set; }
}

