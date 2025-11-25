using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_Grades")]
public class Grade
{
    [Key]
    [Column("grade_ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int GradeId { get; set; }

    [Required]
    [Column("class_ID")]
    public int ClassId { get; set; }

    [Required]
    [MaxLength(6)]
    [Column("student_id")]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("school_year")]
    public string SchoolYear { get; set; } = string.Empty;

    [Column("first_quarter", TypeName = "decimal(5,2)")]
    public decimal? FirstQuarter { get; set; }

    [Column("second_quarter", TypeName = "decimal(5,2)")]
    public decimal? SecondQuarter { get; set; }

    [Column("third_quarter", TypeName = "decimal(5,2)")]
    public decimal? ThirdQuarter { get; set; }

    [Column("fourth_quarter", TypeName = "decimal(5,2)")]
    public decimal? FourthQuarter { get; set; }

    [Column("final_grade", TypeName = "decimal(5,2)")]
    public decimal? FinalGrade { get; set; } // Calculated as average of quarters

    [MaxLength(20)]
    [Column("remarks")]
    public string? Remarks { get; set; } // PASSED, FAILED, INCOMPLETE

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
    [ForeignKey("ClassId")]
    public virtual Class? Class { get; set; }

    [ForeignKey("StudentId")]
    public virtual Student? Student { get; set; }
}

