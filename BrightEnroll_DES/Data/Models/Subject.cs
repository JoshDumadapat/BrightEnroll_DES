using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_Subjects")]
public class Subject
{
    [Key]
    [Column("SubjectID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SubjectId { get; set; }

    [Required]
    [Column("GradeLvlID")]
    public int GradeLevelId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("SubjectName")]
    public string SubjectName { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("Description")]
    public string? Description { get; set; }

    [Column("CreatedAt", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("UpdatedAt", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("GradeLevelId")]
    public virtual GradeLevel? GradeLevel { get; set; }

    public virtual ICollection<SubjectSection> SubjectSections { get; set; } = new List<SubjectSection>();
    public virtual ICollection<SubjectSchedule> SubjectSchedules { get; set; } = new List<SubjectSchedule>();
    public virtual ICollection<TeacherSectionAssignment> TeacherAssignments { get; set; } = new List<TeacherSectionAssignment>();
}

