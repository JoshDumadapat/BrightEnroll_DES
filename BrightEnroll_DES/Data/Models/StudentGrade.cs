using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Maps to tbl_StudentGrades table
[Table("tbl_StudentGrades")]
public class StudentGrade
{
    [Key]
    [Column("grade_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int GradeId { get; set; }

    [Required]
    [MaxLength(6)]
    [Column("student_id")]
    public string StudentId { get; set; } = string.Empty;

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

    [Column("first_quarter", TypeName = "decimal(5,2)")]
    public decimal? FirstQuarter { get; set; }

    [Column("second_quarter", TypeName = "decimal(5,2)")]
    public decimal? SecondQuarter { get; set; }

    [Column("third_quarter", TypeName = "decimal(5,2)")]
    public decimal? ThirdQuarter { get; set; }

    [Column("fourth_quarter", TypeName = "decimal(5,2)")]
    public decimal? FourthQuarter { get; set; }

    [Column("final_grade", TypeName = "decimal(5,2)")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal? FinalGrade { get; set; }

    [MaxLength(20)]
    [Column("remarks")]
    public string? Remarks { get; set; }

    [Column("teacher_id")]
    public int? TeacherId { get; set; }

    [Required]
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
    [ForeignKey("StudentId")]
    public virtual Student? Student { get; set; }

    [ForeignKey("TeacherId")]
    public virtual UserEntity? Teacher { get; set; }
}

