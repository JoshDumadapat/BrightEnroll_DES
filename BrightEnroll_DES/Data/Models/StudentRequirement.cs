using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

/// <summary>
/// EF Core model for student_requirements_tbl table
/// </summary>
[Table("student_requirements_tbl")]
public class StudentRequirement
{
    [Key]
    [Column("requirement_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int RequirementId { get; set; }

    [Required]
    [Column("student_id")]
    public int StudentId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("requirement_name")]
    public string RequirementName { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "not submitted";

    [Required]
    [MaxLength(20)]
    [Column("requirement_type")]
    public string RequirementType { get; set; } = string.Empty;

    // Navigation property
    [ForeignKey("StudentId")]
    public virtual Student Student { get; set; } = null!;
}

