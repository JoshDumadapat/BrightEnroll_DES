using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_Schedules")]
public class Schedule
{
    [Key]
    [Column("schedule_ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ScheduleId { get; set; }

    [Required]
    [Column("class_ID")]
    public int ClassId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("day_of_week")]
    public string DayOfWeek { get; set; } = string.Empty; // Monday, Tuesday, Wednesday, Thursday, Friday

    [Required]
    [MaxLength(20)]
    [Column("start_time")]
    public string StartTime { get; set; } = string.Empty; // e.g., "8:00 AM"

    [Required]
    [MaxLength(20)]
    [Column("end_time")]
    public string EndTime { get; set; } = string.Empty; // e.g., "9:00 AM"

    [Column("duration_minutes")]
    public int? DurationMinutes { get; set; } // Optional: calculated duration in minutes

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_date", TypeName = "datetime")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Column("updated_date", TypeName = "datetime")]
    public DateTime? UpdatedDate { get; set; }

    // Navigation property
    [ForeignKey("ClassId")]
    public virtual Class? Class { get; set; }
}

