using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

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
    public string SchoolYearName { get; set; } = string.Empty; 

    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; } = false; 
    [Required]
    [Column("is_open")]
    public bool IsOpen { get; set; } = false; 

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

