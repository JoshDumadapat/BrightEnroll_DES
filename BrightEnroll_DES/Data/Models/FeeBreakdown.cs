using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_FeeBreakdown")]
public class FeeBreakdown
{
    [Key]
    [Column("breakdown_ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int BreakdownId { get; set; }

    [Required]
    [Column("fee_ID")]
    public int FeeId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("breakdown_type")]
    public string BreakdownType { get; set; } = string.Empty; // "Tuition", "Misc", "Other"

    [Required]
    [MaxLength(200)]
    [Column("item_name")]
    public string ItemName { get; set; } = string.Empty;

    [Required]
    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [Column("display_order")]
    public int DisplayOrder { get; set; } = 0;

    [Required]
    [Column("created_date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Column("updated_date")]
    public DateTime? UpdatedDate { get; set; }

    // Navigation property
    [ForeignKey("FeeId")]
    public virtual Fee? Fee { get; set; }
}

