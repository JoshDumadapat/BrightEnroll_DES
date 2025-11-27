using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// EF Core entity for tbl_user_status_logs table
[Table("tbl_user_status_logs")]
public class UserStatusLog
{
    [Key]
    [Column("log_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LogId { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("changed_by")]
    public int ChangedBy { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("old_status")]
    public string OldStatus { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("new_status")]
    public string NewStatus { get; set; } = string.Empty;

    [Column("reason", TypeName = "text")]
    public string? Reason { get; set; }

    [Required]
    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Required]
    [Column("is_synced")]
    public bool IsSynced { get; set; } = false;

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual UserEntity? User { get; set; }

    [ForeignKey("ChangedBy")]
    public virtual UserEntity? ChangedByUser { get; set; }
}

