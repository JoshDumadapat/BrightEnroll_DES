using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_Sections")]
public class Section
{
    [Key]
    [Column("SectionID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SectionId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("SectionName")]
    public string SectionName { get; set; } = string.Empty;

    [Required]
    [Column("GradeLvlID")]
    public int GradeLevelId { get; set; }

    [Column("ClassroomID")]
    public int? ClassroomId { get; set; }

    [Required]
    [Column("Capacity")]
    public int Capacity { get; set; }

    [MaxLength(500)]
    [Column("Notes")]
    public string? Notes { get; set; }

    [Column("CreatedAt", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("UpdatedAt", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("GradeLevelId")]
    public virtual GradeLevel? GradeLevel { get; set; }

    [ForeignKey("ClassroomId")]
    public virtual Classroom? Classroom { get; set; }

    public virtual ICollection<SubjectSection> SubjectSections { get; set; } = new List<SubjectSection>();
    public virtual ICollection<TeacherSectionAssignment> TeacherAssignments { get; set; } = new List<TeacherSectionAssignment>();
}

