using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

/// <summary>
/// Audit log for tracking system actions and events.
/// Enhanced to support detailed student registration logging.
/// </summary>
[Table("tbl_audit_logs")]
public class AuditLog
{
    [Key]
    [Column("log_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LogId { get; set; }

    [Required]
    [Column("timestamp", TypeName = "datetime")]
    public DateTime Timestamp { get; set; } = DateTime.Now;

    [MaxLength(100)]
    [Column("user_name")]
    public string? UserName { get; set; }

    [MaxLength(50)]
    [Column("user_role")]
    public string? UserRole { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("action")]
    public string Action { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("module")]
    public string? Module { get; set; }

    [Column("description", TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    [MaxLength(45)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [MaxLength(20)]
    [Column("status")]
    public string? Status { get; set; } // Success, Failed, Warning

    [MaxLength(20)]
    [Column("severity")]
    public string? Severity { get; set; } // Low, Medium, High, Critical

    // Enhanced fields for student registration
    [MaxLength(6)]
    [Column("student_id")]
    public string? StudentId { get; set; }

    [MaxLength(200)]
    [Column("student_name")]
    public string? StudentName { get; set; }

    [MaxLength(10)]
    [Column("grade")]
    public string? Grade { get; set; }

    [MaxLength(20)]
    [Column("student_status")]
    public string? StudentStatus { get; set; }

    [Column("registrar_id")]
    public int? RegistrarId { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual UserEntity? User { get; set; }

    [ForeignKey("RegistrarId")]
    public virtual UserEntity? Registrar { get; set; }

    [ForeignKey("StudentId")]
    public virtual Student? Student { get; set; }
}

