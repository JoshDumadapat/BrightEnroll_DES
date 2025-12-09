using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

/// <summary>
/// Represents an accounting period (month/year) with closing status
/// </summary>
[Table("tbl_AccountingPeriods")]
public class AccountingPeriod
{
    [Key]
    [Column("period_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PeriodId { get; set; }

    [Required]
    [Column("period_year")]
    public int PeriodYear { get; set; }

    [Required]
    [Column("period_month")]
    public int PeriodMonth { get; set; }

    [Required]
    [Column("period_name")]
    [MaxLength(50)]
    public string PeriodName { get; set; } = string.Empty; // e.g., "January 2025"

    [Required]
    [Column("start_date", TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Required]
    [Column("end_date", TypeName = "date")]
    public DateTime EndDate { get; set; }

    [Required]
    [Column("is_closed")]
    public bool IsClosed { get; set; } = false;

    [Column("closed_by")]
    public int? ClosedBy { get; set; }

    [Column("closed_at")]
    public DateTime? ClosedAt { get; set; }

    [MaxLength(500)]
    [Column("closing_notes")]
    public string? ClosingNotes { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ClosedBy))]
    public virtual UserEntity? ClosedByUser { get; set; }
}

