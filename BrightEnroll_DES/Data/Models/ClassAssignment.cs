using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Maps to tbl_ClassAssignments table
[Table("tbl_ClassAssignments")]
public class ClassAssignment
{
    [Key]
    [Column("assignment_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AssignmentId { get; set; }

    [Required]
    [Column("teacher_id")]
    public int TeacherId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("school_year")]
    public string SchoolYear { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    [Column("grade_level")]
    public string GradeLevel { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("section")]
    public string? Section { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("subject")]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("schedule")]
    public string? Schedule { get; set; }

    [MaxLength(50)]
    [Column("room")]
    public string? Room { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Active";

    [Column("created_date", TypeName = "datetime")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Column("updated_date", TypeName = "datetime")]
    public DateTime? UpdatedDate { get; set; }

    [MaxLength(50)]
    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [MaxLength(50)]
    [Column("updated_by")]
    public string? UpdatedBy { get; set; }

    // Navigation properties
    [ForeignKey("TeacherId")]
    public virtual UserEntity? Teacher { get; set; }

    public virtual ICollection<TeacherSchedule> Schedules { get; set; } = new List<TeacherSchedule>();
}

