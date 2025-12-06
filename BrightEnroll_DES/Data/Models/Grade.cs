using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

/// <summary>
/// Stores student grades for subjects by grading period.
/// Teachers can input component grades (Written Work, Performance Tasks, Quarterly Assessment) per DepEd standards.
/// </summary>
[Table("tbl_Grades")]
public class Grade
{
    [Key]
    [Column("grade_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int GradeId { get; set; }

    [Required]
    [MaxLength(6)]
    [Column("student_id")]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    [Column("subject_id")]
    public int SubjectId { get; set; }

    [Required]
    [Column("section_id")]
    public int SectionId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("school_year")]
    public string SchoolYear { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    [Column("grading_period")]
    public string GradingPeriod { get; set; } = string.Empty; // Q1, Q2, Q3, Q4

    [Column("written_work", TypeName = "decimal(5,2)")]
    public decimal? WrittenWork { get; set; }

    [Column("performance_tasks", TypeName = "decimal(5,2)")]
    public decimal? PerformanceTasks { get; set; }

    [Column("quarterly_assessment", TypeName = "decimal(5,2)")]
    public decimal? QuarterlyAssessment { get; set; }

    [Column("final_grade", TypeName = "decimal(5,2)")]
    public decimal? FinalGrade { get; set; }

    [Required]
    [Column("teacher_id")]
    public int TeacherId { get; set; }

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("StudentId")]
    public virtual Student? Student { get; set; }

    [ForeignKey("SubjectId")]
    public virtual Subject? Subject { get; set; }

    [ForeignKey("SectionId")]
    public virtual Section? Section { get; set; }

    [ForeignKey("TeacherId")]
    public virtual UserEntity? Teacher { get; set; }
}

