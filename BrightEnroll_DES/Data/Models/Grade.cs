using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

/// <summary>
/// Stores student grades for subjects by grading period.
/// Teachers can input component grades (Quiz, Exam, Project, Participation) and final grades.
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

    [Column("quiz", TypeName = "decimal(5,2)")]
    public decimal? Quiz { get; set; }

    [Column("exam", TypeName = "decimal(5,2)")]
    public decimal? Exam { get; set; }

    [Column("project", TypeName = "decimal(5,2)")]
    public decimal? Project { get; set; }

    [Column("participation", TypeName = "decimal(5,2)")]
    public decimal? Participation { get; set; }

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

