using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

/// <summary>
/// Standalone table for cloud management operations and logs
/// </summary>
[Table("tbl_CloudManagementLogs")]
public class CloudManagementLog
{
    [Key]
    [Column("log_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LogId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("operation_type")]
    public string OperationType { get; set; } = string.Empty; // "Connectivity Check", "Sync", "Upload", "Download", "Error", etc.

    [Required]
    [Column("log_message", TypeName = "nvarchar(max)")]
    public string LogMessage { get; set; } = string.Empty;

    [Required]
    [Column("timestamp", TypeName = "datetime")]
    public DateTime Timestamp { get; set; } = DateTime.Now;

    [MaxLength(20)]
    [Column("severity")]
    public string? Severity { get; set; } // "Info", "Warning", "Error", "Success"

    [MaxLength(50)]
    [Column("status")]
    public string? Status { get; set; } // "Success", "Failed", "In Progress", "Completed"

    [MaxLength(100)]
    [Column("initiated_by")]
    public string? InitiatedBy { get; set; } // User who initiated the operation

    [MaxLength(100)]
    [Column("cloud_server")]
    public string? CloudServer { get; set; } // Cloud server URL or identifier

    [Column("duration_seconds")]
    public int? DurationSeconds { get; set; } // Operation duration in seconds

    [Column("records_affected")]
    public int? RecordsAffected { get; set; } // Number of records affected by the operation

    [Column("error_details", TypeName = "nvarchar(max)")]
    public string? ErrorDetails { get; set; } // Detailed error information if operation failed

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
