using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

/// <summary>
/// Links a student to a section for a specific school year and tracks enrollment status.
/// Used by the enrollment module when assigning students to sections.
/// </summary>
[Table("tbl_StudentSectionEnrollment")]
public class StudentSectionEnrollment
{
    [Key]
    [Column("enrollment_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EnrollmentId { get; set; }

    [Required]
    [MaxLength(6)]
    [Column("student_id")]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    [Column("SectionID")]
    public int SectionId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("school_yr")]
    public string SchoolYear { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Enrolled";

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("StudentId")]
    public virtual Student Student { get; set; } = null!;

    [ForeignKey("SectionId")]
    public virtual Section Section { get; set; } = null!;
}


