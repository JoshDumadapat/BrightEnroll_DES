using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Audit trail for student status changes
[Table("tbl_student_status_logs")]
public class StudentStatusLog
{
    [Key]
    [Column("log_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LogId { get; set; }

    [Required]
    [MaxLength(6)]
    [Column("student_id")]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("old_status")]
    public string OldStatus { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("new_status")]
    public string NewStatus { get; set; } = string.Empty;

    [Column("changed_by")]
    public int? ChangedBy { get; set; } // UserEntity.UserId

    [Column("changed_by_name")]
    [MaxLength(100)]
    public string? ChangedByName { get; set; }

    [Required]
    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("ChangedBy")]
    public virtual UserEntity? ChangedByUser { get; set; }
}


