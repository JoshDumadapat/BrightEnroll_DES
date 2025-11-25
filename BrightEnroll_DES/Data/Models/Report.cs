using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_Reports")]
public class Report
{
    [Key]
    [Column("report_ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReportId { get; set; }

    [Required]
    [Column("teacher_ID")]
    public int TeacherId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("report_type")]
    public string ReportType { get; set; } = string.Empty; // ClassPerformance, StudentGrades, SubjectSummary

    [Required]
    [MaxLength(200)]
    [Column("report_title")]
    public string ReportTitle { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("school_year")]
    public string? SchoolYear { get; set; }

    [MaxLength(50)]
    [Column("class_filter")]
    public string? ClassFilter { get; set; } // Grade Level - Section

    [MaxLength(100)]
    [Column("subject_filter")]
    public string? SubjectFilter { get; set; }

    [MaxLength(50)]
    [Column("student_filter")]
    public string? StudentFilter { get; set; }

    [MaxLength(20)]
    [Column("period_filter")]
    public string? PeriodFilter { get; set; } // All, 1st, 2nd, 3rd, 4th, Final

    [MaxLength(500)]
    [Column("file_path")]
    public string? FilePath { get; set; } // Path to generated report file if saved

    [Column("generated_date", TypeName = "datetime")]
    public DateTime GeneratedDate { get; set; } = DateTime.Now;

    [MaxLength(50)]
    [Column("generated_by")]
    public string? GeneratedBy { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation property
    [ForeignKey("TeacherId")]
    public virtual UserEntity? Teacher { get; set; }
}

