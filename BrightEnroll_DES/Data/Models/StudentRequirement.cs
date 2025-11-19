using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Maps to tbl_StudentRequirements table
[Table("tbl_StudentRequirements")]
public class StudentRequirement
{
    [Key]
    [Column("requirement_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int RequirementId { get; set; }

    [Required]
    [Column("student_id")]
    [MaxLength(6)]
    public string StudentId { get; set; } = string.Empty;

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

    [ForeignKey("StudentId")]
    public virtual Student Student { get; set; } = null!;
}

