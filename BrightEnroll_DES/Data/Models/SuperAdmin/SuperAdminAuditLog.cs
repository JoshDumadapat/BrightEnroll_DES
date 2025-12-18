using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models.SuperAdmin;

// Separate audit log table specifically for Super Admin actions
[Table("tbl_SuperAdminAuditLogs")]
public class SuperAdminAuditLog
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

    // Transaction tracking fields
    [MaxLength(100)]
    [Column("entity_type")]
    public string? EntityType { get; set; } // Customer, Invoice, Payment, etc.

    [MaxLength(50)]
    [Column("entity_id")]
    public string? EntityId { get; set; } // ID of the entity being tracked

    [Column("old_values", TypeName = "nvarchar(max)")]
    public string? OldValues { get; set; } // Previous values before change

    [Column("new_values", TypeName = "nvarchar(max)")]
    public string? NewValues { get; set; } // New values after change

    // Additional fields for Super Admin specific tracking
    [MaxLength(50)]
    [Column("customer_code")]
    public string? CustomerCode { get; set; } // For customer-related actions

    [MaxLength(200)]
    [Column("customer_name")]
    public string? CustomerName { get; set; } // For customer-related actions
}
