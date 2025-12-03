using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_SubjectSchedule")]
public class SubjectSchedule
{
    [Key]
    [Column("ScheduleID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ScheduleId { get; set; }

    [Required]
    [Column("SubjectID", TypeName = "int")]
    public int SubjectId { get; set; }

    [Required]
    [Column("GradeLvlID", TypeName = "int")]
    public int GradeLevelId { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("DayOfWeek")]
    public string DayOfWeek { get; set; } = string.Empty; // M, T, W, TH, F, Sat, Sun

    [Required]
    [Column("StartTime", TypeName = "time")]
    public TimeSpan StartTime { get; set; }

    [Required]
    [Column("EndTime", TypeName = "time")]
    public TimeSpan EndTime { get; set; }

    [Required]
    [Column("IsDefault")]
    public bool IsDefault { get; set; } = true; // Indicates this is the grade-level default schedule

    [Column("CreatedAt", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("UpdatedAt", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Subject? Subject { get; set; }

    public virtual GradeLevel? GradeLevel { get; set; }
}

