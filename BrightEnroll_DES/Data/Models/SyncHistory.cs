using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

/// <summary>
/// Database entity for tracking cloud sync history and logs.
/// Persists sync operations, status, and results to database.
/// </summary>
[Table("tbl_SyncHistory")]
public class SyncHistory
{
    [Key]
    [Column("sync_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SyncId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("sync_type")]
    public string SyncType { get; set; } = string.Empty; // "Full", "Push", "Pull", "Reference"

    [Required]
    [Column("sync_time", TypeName = "datetime")]
    public DateTime SyncTime { get; set; } = DateTime.Now;

    [Required]
    [Column("status", TypeName = "varchar(20)")]
    public string Status { get; set; } = "Success"; // "Success", "Error", "Warning"

    [Column("records_pushed")]
    public int RecordsPushed { get; set; } = 0;

    [Column("records_pulled")]
    public int RecordsPulled { get; set; } = 0;

    [Column("message", TypeName = "nvarchar(max)")]
    public string? Message { get; set; }

    [Column("error_details", TypeName = "nvarchar(max)")]
    public string? ErrorDetails { get; set; }

    [Column("duration_seconds")]
    public int? DurationSeconds { get; set; }

    [MaxLength(100)]
    [Column("initiated_by")]
    public string? InitiatedBy { get; set; } // User who initiated the sync

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

