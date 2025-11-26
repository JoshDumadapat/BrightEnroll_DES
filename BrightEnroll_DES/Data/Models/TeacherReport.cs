using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Maps to tbl_TeacherReports table - stores generated report history
[Table("tbl_TeacherReports")]
public class TeacherReport
{
    [Key]
    [Column("report_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReportId { get; set; }

    [Required]
    [Column("teacher_id")]
    public int TeacherId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("report_type")]
    public string ReportType { get; set; } = string.Empty; // ClassPerformance, StudentGrades, SubjectSummary

    [Required]
    [MaxLength(20)]
    [Column("school_year")]
    public string SchoolYear { get; set; } = string.Empty;

    [MaxLength(10)]
    [Column("grade_level")]
    public string? GradeLevel { get; set; }

    [MaxLength(50)]
    [Column("section")]
    public string? Section { get; set; }

    [MaxLength(100)]
    [Column("subject")]
    public string? Subject { get; set; }

    [MaxLength(20)]
    [Column("grading_period")]
    public string? GradingPeriod { get; set; } // All, 1st, 2nd, 3rd, 4th, Final

    [MaxLength(6)]
    [Column("student_id")]
    public string? StudentId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("report_title")]
    public string ReportTitle { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("file_path")]
    public string? FilePath { get; set; } // Path to generated report file if saved

    [Column("generated_date", TypeName = "datetime")]
    public DateTime GeneratedDate { get; set; } = DateTime.Now;

    [MaxLength(50)]
    [Column("generated_by")]
    public string? GeneratedBy { get; set; }

    // Navigation properties
    [ForeignKey("TeacherId")]
    public virtual UserEntity? Teacher { get; set; }

    [ForeignKey("StudentId")]
    public virtual Student? Student { get; set; }
}

