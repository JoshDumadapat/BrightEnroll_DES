using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models.SuperAdmin;

// Notification model for SuperAdmin - separate from school admin notifications
[Table("tbl_SuperAdminNotifications")]
public class SuperAdminNotification
{
    [Key]
    [Column("notification_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int NotificationId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("notification_type")]
    public string NotificationType { get; set; } = string.Empty; // SupportTicket, Customer, Subscription, etc.

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
    public string ReferenceType { get; set; } = string.Empty; // SupportTicket, Customer, Subscription, etc.

    [Column("reference_id")]
    public int? ReferenceId { get; set; } // ID of the related entity

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
    public string? ActionUrl { get; set; } // URL to navigate when notification is clicked

    [MaxLength(50)]
    [Column("priority")]
    public string Priority { get; set; } = "Normal"; // Low, Normal, High, Urgent

    [Column("created_by")]
    public int? CreatedBy { get; set; } // User who created the notification

    // Navigation properties
    [ForeignKey("CreatedBy")]
    public virtual UserEntity? CreatedByUser { get; set; }
}
