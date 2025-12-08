using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

/// <summary>
/// Manages school years with open/closed status for academic period isolation.
/// Only one school year can be open at a time.
/// </summary>
[Table("tbl_SchoolYear")]
public class SchoolYear
{
    [Key]
    [Column("school_year_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SchoolYearId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("school_year")]
    public string SchoolYearName { get; set; } = string.Empty; // "2024-2025"

    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; } = false; // Currently active school year

    [Required]
    [Column("is_open")]
    public bool IsOpen { get; set; } = false; // Can enroll students

    [Column("start_date", TypeName = "date")]
    public DateTime? StartDate { get; set; }

    [Column("end_date", TypeName = "date")]
    public DateTime? EndDate { get; set; }

    [Required]
    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("opened_at", TypeName = "datetime")]
    public DateTime? OpenedAt { get; set; }

    [Column("closed_at", TypeName = "datetime")]
    public DateTime? ClosedAt { get; set; }
}

