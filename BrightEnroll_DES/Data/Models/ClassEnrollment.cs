using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_ClassEnrollments")]
public class ClassEnrollment
{
    [Key]
    [Column("enrollment_ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EnrollmentId { get; set; }

    [Required]
    [Column("class_ID")]
    public int ClassId { get; set; }

    [Required]
    [MaxLength(6)]
    [Column("student_id")]
    public string StudentId { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("school_year")]
    public string? SchoolYear { get; set; }

    [Column("enrollment_date", TypeName = "datetime")]
    public DateTime EnrollmentDate { get; set; } = DateTime.Now;

    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Active"; // Active, Dropped, Transferred

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_date", TypeName = "datetime")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Column("updated_date", TypeName = "datetime")]
    public DateTime? UpdatedDate { get; set; }

    // Navigation properties
    [ForeignKey("ClassId")]
    public virtual Class? Class { get; set; }

    [ForeignKey("StudentId")]
    public virtual Student? Student { get; set; }
}

