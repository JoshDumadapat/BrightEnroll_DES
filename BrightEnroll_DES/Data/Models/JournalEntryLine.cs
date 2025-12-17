using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Journal entry line - individual debit/credit entries
[Table("tbl_JournalEntryLines")]
public class JournalEntryLine
{
    [Key]
    [Column("line_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LineId { get; set; }

    [Required]
    [Column("journal_entry_id")]
    public int JournalEntryId { get; set; }

    [Required]
    [Column("account_id")]
    public int AccountId { get; set; }

    [Required]
    [Column("line_number")]
    public int LineNumber { get; set; }

    [Column("debit_amount", TypeName = "decimal(18,2)")]
    public decimal DebitAmount { get; set; } = 0.00m;

    [Column("credit_amount", TypeName = "decimal(18,2)")]
    public decimal CreditAmount { get; set; } = 0.00m;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    // Navigation properties
    [ForeignKey("JournalEntryId")]
    public virtual JournalEntry JournalEntry { get; set; } = null!;

    [ForeignKey("AccountId")]
    public virtual ChartOfAccount Account { get; set; } = null!;
}

