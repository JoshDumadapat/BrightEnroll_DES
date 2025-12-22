using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Activity log for teachers - tracks actions and activities
[Table("tbl_TeacherActivityLogs")]
public class TeacherActivityLog
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("teacher_id")]
    public int TeacherId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("action")]
    public string Action { get; set; } = string.Empty; 

    [Column("details", TypeName = "nvarchar(max)")]
    public string? Details { get; set; } 

    [Required]
    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation property
    [ForeignKey("TeacherId")]
    public virtual UserEntity? Teacher { get; set; }
}

