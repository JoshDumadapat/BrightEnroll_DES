using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Maps to tbl_deductions table
[Table("tbl_deductions")]
public class Deduction
{
    [Key]
    [Column("deduction_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int DeductionId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("deduction_type")]
    public string DeductionType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("deduction_name")]
    public string DeductionName { get; set; } = string.Empty;

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

    [Required]
    [Column("is_synced")]
    public bool IsSynced { get; set; } = false;
}

