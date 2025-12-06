using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

/// <summary>
/// Stores configurable grade weights per subject for computing final grades.
/// </summary>
[Table("tbl_GradeWeights")]
public class GradeWeight
{
    [Key]
    [Column("weight_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int WeightId { get; set; }

    [Required]
    [Column("subject_id")]
    public int SubjectId { get; set; }

    [Column("quiz_weight", TypeName = "decimal(5,2)")]
    public decimal QuizWeight { get; set; } = 0.30m; // Default 30%

    [Column("exam_weight", TypeName = "decimal(5,2)")]
    public decimal ExamWeight { get; set; } = 0.40m; // Default 40%

    [Column("project_weight", TypeName = "decimal(5,2)")]
    public decimal ProjectWeight { get; set; } = 0.20m; // Default 20%

    [Column("participation_weight", TypeName = "decimal(5,2)")]
    public decimal ParticipationWeight { get; set; } = 0.10m; // Default 10%

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    [ForeignKey("SubjectId")]
    public virtual Subject? Subject { get; set; }
}

