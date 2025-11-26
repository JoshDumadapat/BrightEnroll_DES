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

    [Column("gradelevel_ID")]
    public int GradeLevelId { get; set; }

    [Column("tuition_fee", TypeName = "decimal(18,2)")]
    public decimal TuitionFee { get; set; }

    [Column("misc_fee", TypeName = "decimal(18,2)")]
    public decimal MiscFee { get; set; }

    [Column("other_fee", TypeName = "decimal(18,2)")]
    public decimal OtherFee { get; set; }

    // total_fee is a computed column in SQL; keep it nullable here
    [Column("total_fee", TypeName = "decimal(18,2)")]
    public decimal? TotalFee { get; set; }

    [Column("created_date", TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    [Column("updated_date", TypeName = "datetime")]
    public DateTime? UpdatedDate { get; set; }

    [MaxLength(50)]
    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [MaxLength(50)]
    [Column("updated_by")]
    public string? UpdatedBy { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [ForeignKey("GradeLevelId")]
    public GradeLevel? GradeLevel { get; set; }

    public ICollection<FeeBreakdown> Breakdowns { get; set; } = new List<FeeBreakdown>();
}


