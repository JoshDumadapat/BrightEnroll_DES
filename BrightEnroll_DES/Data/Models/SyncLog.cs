using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Detailed sync operation logs
[Table("tbl_SyncLogs")]
public class SyncLog
{
    [Key]
    [Column("log_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LogId { get; set; }

    [Column("sync_id")]
    public int? SyncId { get; set; } 

    [Required]
    [MaxLength(50)]
    [Column("log_type")]
    public string LogType { get; set; } = string.Empty; 

    [Required]
    [Column("log_message", TypeName = "nvarchar(max)")]
    public string LogMessage { get; set; } = string.Empty;

    [Required]
    [Column("timestamp", TypeName = "datetime")]
    public DateTime Timestamp { get; set; } = DateTime.Now;

    [MaxLength(20)]
    [Column("severity")]
    public string? Severity { get; set; } 

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation property
    [ForeignKey("SyncId")]
    public virtual SyncHistory? SyncHistory { get; set; }
}

