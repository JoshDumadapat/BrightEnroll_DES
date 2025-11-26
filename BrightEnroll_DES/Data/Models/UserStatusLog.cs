using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_user_status_logs")]
public class UserStatusLog
{
    [Key]
    [Column("log_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LogId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

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

    [Column("reason")]
    public string? Reason { get; set; }

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("UserId")]
    public UserEntity User { get; set; } = null!;

    [ForeignKey("ChangedBy")]
    public UserEntity ChangedByUser { get; set; } = null!;
}


