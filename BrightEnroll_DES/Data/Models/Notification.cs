using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Notification model for approval requests and system notifications
[Table("tbl_Notifications")]
public class Notification
{
    [Key]
    [Column("notification_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int NotificationId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("notification_type")]
    public string NotificationType { get; set; } = string.Empty; 

    [Required]
    [MaxLength(100)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("message", TypeName = "nvarchar(500)")]
    public string? Message { get; set; }

    [Required]
    [Column("reference_type")]
    [MaxLength(50)]
    public string ReferenceType { get; set; } = string.Empty; 

    [Column("reference_id")]
    public int? ReferenceId { get; set; } 

    [Required]
    [Column("is_read")]
    public bool IsRead { get; set; } = false;

    [Column("read_at", TypeName = "datetime")]
    public DateTime? ReadAt { get; set; }

    [Required]
    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [MaxLength(100)]
    [Column("action_url")]
    public string? ActionUrl { get; set; } 

    [MaxLength(50)]
    [Column("priority")]
    public string Priority { get; set; } = "Normal"; 

    [Column("created_by")]
    public int? CreatedBy { get; set; } 

    // Navigation properties
    [ForeignKey("CreatedBy")]
    public virtual UserEntity? CreatedByUser { get; set; }
}

