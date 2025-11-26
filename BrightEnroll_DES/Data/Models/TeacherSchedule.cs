using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Maps to tbl_TeacherSchedules table - detailed schedule entries with day and time slots
[Table("tbl_TeacherSchedules")]
public class TeacherSchedule
{
    [Key]
    [Column("schedule_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ScheduleId { get; set; }

    [Required]
    [Column("assignment_id")]
    public int AssignmentId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("day_of_week")]
    public string DayOfWeek { get; set; } = string.Empty; // Monday, Tuesday, etc.

    [Required]
    [MaxLength(20)]
    [Column("start_time")]
    public string StartTime { get; set; } = string.Empty; // e.g., "8:00 AM"

    [Required]
    [MaxLength(20)]
    [Column("end_time")]
    public string EndTime { get; set; } = string.Empty; // e.g., "9:00 AM"

    [Column("created_date", TypeName = "datetime")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Column("updated_date", TypeName = "datetime")]
    public DateTime? UpdatedDate { get; set; }

    [MaxLength(50)]
    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [MaxLength(50)]
    [Column("updated_by")]
    public string? UpdatedBy { get; set; }

    // Navigation properties
    [ForeignKey("AssignmentId")]
    public virtual ClassAssignment? ClassAssignment { get; set; }
}

