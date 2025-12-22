using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Discount configuration - stores discount types and rules for students
[Table("tbl_discounts")]
public class Discount
{
    [Key]
    [Column("discount_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int DiscountId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("discount_type")]
    public string DiscountType { get; set; } = string.Empty; 

    [Required]
    [MaxLength(100)]
    [Column("discount_name")]
    public string DiscountName { get; set; } = string.Empty;

    [Required]
    [Column("rate_or_value", TypeName = "decimal(12,4)")]
    public decimal RateOrValue { get; set; }

    [Column("is_percentage")]
    public bool IsPercentage { get; set; } = true;

    [Column("max_amount", TypeName = "decimal(12,2)")]
    public decimal? MaxAmount { get; set; }

    [Column("min_amount", TypeName = "decimal(12,2)")]
    public decimal? MinAmount { get; set; }

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_date", TypeName = "datetime")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Column("updated_date", TypeName = "datetime")]
    public DateTime? UpdatedDate { get; set; }

    // Navigation property for ledger charges that use this discount
    public virtual ICollection<LedgerCharge> LedgerCharges { get; set; } = new List<LedgerCharge>();
}
