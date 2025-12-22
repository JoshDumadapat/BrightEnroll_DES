using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_ClassSchedule")]
public class ClassSchedule
{
    [Key]
    [Column("ScheduleID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ScheduleId { get; set; }

    [Required]
    [Column("AssignmentID")]
    public int AssignmentId { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("DayOfWeek")]
    public string DayOfWeek { get; set; } = string.Empty;

    [Required]
    [Column("StartTime", TypeName = "time")]
    public TimeSpan StartTime { get; set; }

    [Required]
    [Column("EndTime", TypeName = "time")]
    public TimeSpan EndTime { get; set; }

    [Required]
    [Column("RoomID")]
    public int RoomId { get; set; }

    [Column("CreatedAt", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("UpdatedAt", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("AssignmentId")]
    public virtual TeacherSectionAssignment? Assignment { get; set; }

    [ForeignKey("RoomId")]
    public virtual Classroom? Room { get; set; }
}

