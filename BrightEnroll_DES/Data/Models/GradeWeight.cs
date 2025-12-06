using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

/// <summary>
/// Stores configurable grade weights per subject for computing final grades.
/// DepEd Standard: Written Work (20%), Performance Tasks (60%), Quarterly Assessment (20%).
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

    [Column("written_work_weight", TypeName = "decimal(5,2)")]
    public decimal WrittenWorkWeight { get; set; } = 0.20m; // DepEd Standard: 20%

    [Column("performance_tasks_weight", TypeName = "decimal(5,2)")]
    public decimal PerformanceTasksWeight { get; set; } = 0.60m; // DepEd Standard: 60%

    [Column("quarterly_assessment_weight", TypeName = "decimal(5,2)")]
    public decimal QuarterlyAssessmentWeight { get; set; } = 0.20m; // DepEd Standard: 20%

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    [ForeignKey("SubjectId")]
    public virtual Subject? Subject { get; set; }
}

