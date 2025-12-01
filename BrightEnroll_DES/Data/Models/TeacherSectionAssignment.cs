using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_TeacherSectionAssignment")]
public class TeacherSectionAssignment
{
    [Key]
    [Column("AssignmentID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AssignmentId { get; set; }

    [Required]
    [Column("TeacherID")]
    public int TeacherId { get; set; }

    [Required]
    [Column("SectionID")]
    public int SectionId { get; set; }

    [Column("SubjectID")]
    public int? SubjectId { get; set; } // NULL if adviser

    [Required]
    [MaxLength(50)]
    [Column("Role")]
    public string Role { get; set; } = string.Empty; // "adviser" or "subject_teacher"

    [Column("IsArchived")]
    public bool IsArchived { get; set; } = false;

    [Column("CreatedAt", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("UpdatedAt", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("TeacherId")]
    public virtual UserEntity? Teacher { get; set; }

    [ForeignKey("SectionId")]
    public virtual Section? Section { get; set; }

    [ForeignKey("SubjectId")]
    public virtual Subject? Subject { get; set; }

    public virtual ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();
}

