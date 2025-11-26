using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_GradeLevel")]
public class GradeLevel
{
    [Key]
    [Column("gradelevel_ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int GradeLevelId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("grade_level_name")]
    public string GradeLevelName { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}


