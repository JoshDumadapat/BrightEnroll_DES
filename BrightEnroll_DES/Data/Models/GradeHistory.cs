using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

/// <summary>
/// Tracks all changes to grades for audit trail purposes.
/// </summary>
[Table("tbl_GradeHistory")]
public class GradeHistory
{
    [Key]
    [Column("history_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int HistoryId { get; set; }

    [Required]
    [Column("grade_id")]
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

    [Column("quiz_old", TypeName = "decimal(5,2)")]
    public decimal? QuizOld { get; set; }

    [Column("quiz_new", TypeName = "decimal(5,2)")]
    public decimal? QuizNew { get; set; }

    [Column("exam_old", TypeName = "decimal(5,2)")]
    public decimal? ExamOld { get; set; }

    [Column("exam_new", TypeName = "decimal(5,2)")]
    public decimal? ExamNew { get; set; }

    [Column("project_old", TypeName = "decimal(5,2)")]
    public decimal? ProjectOld { get; set; }

    [Column("project_new", TypeName = "decimal(5,2)")]
    public decimal? ProjectNew { get; set; }

    [Column("participation_old", TypeName = "decimal(5,2)")]
    public decimal? ParticipationOld { get; set; }

    [Column("participation_new", TypeName = "decimal(5,2)")]
    public decimal? ParticipationNew { get; set; }

    [Column("final_grade_old", TypeName = "decimal(5,2)")]
    public decimal? FinalGradeOld { get; set; }

    [Column("final_grade_new", TypeName = "decimal(5,2)")]
    public decimal? FinalGradeNew { get; set; }

    [Required]
    [Column("changed_by")]
    public int ChangedBy { get; set; } // User ID who made the change

    [MaxLength(500)]
    [Column("change_reason")]
    public string? ChangeReason { get; set; }

    [Required]
    [Column("changed_at", TypeName = "datetime")]
    public DateTime ChangedAt { get; set; } = DateTime.Now;

    // Navigation properties
    [ForeignKey("GradeId")]
    public virtual Grade? Grade { get; set; }

    [ForeignKey("ChangedBy")]
    public virtual UserEntity? ChangedByUser { get; set; }
}

