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

    [Column("fee_ID")]
    public int FeeId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("breakdown_type")]
    public string BreakdownType { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("item_name")]
    public string ItemName { get; set; } = string.Empty;

    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("created_date", TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    [Column("updated_date", TypeName = "datetime")]
    public DateTime? UpdatedDate { get; set; }

    [ForeignKey("FeeId")]
    public Fee Fee { get; set; } = null!;
}


