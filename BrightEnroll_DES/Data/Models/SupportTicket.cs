using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_SupportTickets")]
public class SupportTicket
{
    [Key]
    [Column("ticket_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TicketId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("ticket_number")]
    public string TicketNumber { get; set; } = string.Empty;

    [Column("customer_id")]
    public int? CustomerId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("subject")]
    public string Subject { get; set; } = string.Empty;

    [Column("description", TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    [MaxLength(50)]
    [Column("priority")]
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical

    [MaxLength(50)]
    [Column("status")]
    public string Status { get; set; } = "Open"; // Open, In Progress, Resolved, Closed

    [MaxLength(50)]
    [Column("category")]
    public string? Category { get; set; }

    [Column("assigned_to")]
    public int? AssignedTo { get; set; }

    [Column("resolved_at", TypeName = "datetime")]
    public DateTime? ResolvedAt { get; set; }

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [ForeignKey("AssignedTo")]
    public virtual UserEntity? AssignedToUser { get; set; }
}

