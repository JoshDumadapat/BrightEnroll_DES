using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Individual charges or discounts in student ledger
[Table("tbl_LedgerCharges")]
public class LedgerCharge
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("ledger_id")]
    public int LedgerId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("charge_type")]
    public string ChargeType { get; set; } = string.Empty; 

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; } = 0; 

    [Required]
    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [Column("discount_id")]
    public int? DiscountId { get; set; } 

    // Navigation properties
    [ForeignKey("LedgerId")]
    public virtual StudentLedger? Ledger { get; set; }

    [ForeignKey("DiscountId")]
    public virtual Discount? Discount { get; set; }
}

