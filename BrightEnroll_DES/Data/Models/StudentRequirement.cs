using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_StudentRequirements")]
public class StudentRequirement
{
    [Key]
    [Column("requirement_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int RequirementId { get; set; }

    [Required]
    [MaxLength(6)]
    [Column("student_id")]
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

    // Navigation
    [ForeignKey("StudentId")]
    public Student Student { get; set; } = null!;
}


