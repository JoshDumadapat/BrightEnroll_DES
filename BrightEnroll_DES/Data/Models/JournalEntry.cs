using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Journal entry header - represents a complete accounting transaction
[Table("tbl_JournalEntries")]
public class JournalEntry
{
    [Key]
    [Column("journal_entry_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int JournalEntryId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("entry_number")]
    public string EntryNumber { get; set; } = string.Empty; // e.g., "JE-2025-001"

    [Required]
    [Column("entry_date", TypeName = "date")]
    public DateTime EntryDate { get; set; }

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("reference_type")]
    public string ReferenceType { get; set; } = string.Empty; // Payment, Expense, Payroll, Manual

    [Column("reference_id")]
    public int? ReferenceId { get; set; } // Links to source transaction (PaymentId, ExpenseId, etc.)

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Posted"; // Draft, Posted, Reversed

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("approved_by")]
    public int? ApprovedBy { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<JournalEntryLine> JournalEntryLines { get; set; } = new List<JournalEntryLine>();

    [ForeignKey("CreatedBy")]
    public virtual UserEntity? CreatedByUser { get; set; }

    [ForeignKey("ApprovedBy")]
    public virtual UserEntity? ApprovedByUser { get; set; }
}

