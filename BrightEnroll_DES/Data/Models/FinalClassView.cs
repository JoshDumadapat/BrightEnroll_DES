using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_FinalClasses")]
[Keyless]
public class FinalClassView
{
    [Column("AssignmentId")]
    public int AssignmentId { get; set; }

    [Column("Role")]
    public string? Role { get; set; }

    [Column("TeacherName")]
    public string? TeacherName { get; set; }

    [Column("TeacherId")]
    public int? TeacherId { get; set; }

    [Column("SectionName")]
    public string? SectionName { get; set; }

    [Column("GradeLevel")]
    public string? GradeLevel { get; set; }

    [Column("SubjectName")]
    public string? SubjectName { get; set; }

    [Column("SubjectId")]
    public int? SubjectId { get; set; }

    [Column("DayOfWeek")]
    public string? DayOfWeek { get; set; }

    [Column("StartTime", TypeName = "time")]
    public TimeSpan? StartTime { get; set; }

    [Column("EndTime", TypeName = "time")]
    public TimeSpan? EndTime { get; set; }

    [Column("Classroom")]
    public string? Classroom { get; set; }

    [Column("BuildingName")]
    public string? BuildingName { get; set; }

    [Column("AssignmentCreatedAt", TypeName = "datetime")]
    public DateTime AssignmentCreatedAt { get; set; }

    [Column("AssignmentUpdatedAt", TypeName = "datetime")]
    public DateTime? AssignmentUpdatedAt { get; set; }
}

