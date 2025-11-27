using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_Fees")]
public class Fee
{
    [Key]
    [Column("fee_ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int FeeId { get; set; }

    [Required]
    [Column("gradelevel_ID")]
    public int GradeLevelId { get; set; }

    [Required]
    [Column("tuition_fee", TypeName = "decimal(18,2)")]
    public decimal TuitionFee { get; set; }

    [Required]
    [Column("misc_fee", TypeName = "decimal(18,2)")]
    public decimal MiscFee { get; set; }

    [Required]
    [Column("other_fee", TypeName = "decimal(18,2)")]
    public decimal OtherFee { get; set; }

    [Column("total_fee", TypeName = "decimal(18,2)")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal TotalFee { get; set; }

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

    [Required]
    [Column("is_synced")]
    public bool IsSynced { get; set; } = false;

    // Navigation properties
    [ForeignKey("GradeLevelId")]
    public virtual GradeLevel? GradeLevel { get; set; }

    public virtual ICollection<FeeBreakdown> Breakdowns { get; set; } = new List<FeeBreakdown>();
}

