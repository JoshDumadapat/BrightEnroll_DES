using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Student attendance records tracked by teachers
[Table("tbl_Attendance")]
public class Attendance
{
    [Key]
    [Column("AttendanceID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AttendanceId { get; set; }

    [Required]
    [MaxLength(6)]
    [Column("StudentID")]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    [Column("SectionID")]
    public int SectionId { get; set; }

    [Column("SubjectID")]
    public int? SubjectId { get; set; } 

    [Required]
    [Column("AttendanceDate", TypeName = "date")]
    public DateTime AttendanceDate { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("Status")]
    public string Status { get; set; } = string.Empty;

    [Column("TimeIn", TypeName = "time")]
    public TimeSpan? TimeIn { get; set; } 

    [Column("TimeOut", TypeName = "time")]
    public TimeSpan? TimeOut { get; set; }

    [MaxLength(500)]
    [Column("Remarks")]
    public string? Remarks { get; set; }

    [Required]
    [Column("TeacherID")]
    public int TeacherId { get; set; } // Who recorded it

    [Required]
    [MaxLength(20)]
    [Column("SchoolYear")]
    public string SchoolYear { get; set; } = string.Empty;

    [Required]
    [Column("CreatedAt", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("UpdatedAt", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("StudentId")]
    public virtual Student? Student { get; set; }

    [ForeignKey("SectionId")]
    public virtual Section? Section { get; set; }

    [ForeignKey("SubjectId")]
    public virtual Subject? Subject { get; set; }

    [ForeignKey("TeacherId")]
    public virtual UserEntity? Teacher { get; set; }
}

