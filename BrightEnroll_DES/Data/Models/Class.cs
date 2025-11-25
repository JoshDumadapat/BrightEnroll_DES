using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_Classes")]
public class Class
{
    [Key]
    [Column("class_ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ClassId { get; set; }

    [Required]
    [Column("teacher_ID")]
    public int TeacherId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("subject")]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("grade_level")]
    public string GradeLevel { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("section")]
    public string Section { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("schedule")]
    public string Schedule { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("room")]
    public string Room { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("school_year")]
    public string SchoolYear { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Active";

    [Column("student_count")]
    public int StudentCount { get; set; } = 0;

    [Column("created_date", TypeName = "datetime")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Column("updated_date", TypeName = "datetime")]
    public DateTime? UpdatedDate { get; set; }

    // Navigation property
    [ForeignKey("TeacherId")]
    public virtual UserEntity? Teacher { get; set; }
}

