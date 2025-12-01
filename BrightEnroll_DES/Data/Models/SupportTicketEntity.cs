using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models
{
    [Table("tbl_SupportTickets")]
    public class SupportTicketEntity
    {
        [Key]
        [Column("ticket_id")]
        public int TicketId { get; set; }

        [Required]
        [MaxLength(150)]
        [Column("school_name")]
        public string SchoolName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("issue")]
        public string Issue { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [Column("priority")]
        public string Priority { get; set; } = "Medium";

        [Required]
        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "Open";

        [MaxLength(100)]
        [Column("assigned_to")]
        public string? AssignedTo { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}


