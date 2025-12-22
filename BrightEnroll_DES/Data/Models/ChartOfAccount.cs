using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Chart of accounts for double-entry bookkeeping
[Table("tbl_ChartOfAccounts")]
public class ChartOfAccount
{
    [Key]
    [Column("account_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AccountId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("account_code")]
    public string AccountCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("account_name")]
    public string AccountName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("account_type")]
    public string AccountType { get; set; } = string.Empty; // Asset, Liability, Equity, Revenue, Expense

    [Column("parent_account_id")]
    public int? ParentAccountId { get; set; } // For hierarchical structure

    [Required]
    [MaxLength(10)]
    [Column("normal_balance")]
    public string NormalBalance { get; set; } = "Debit"; // Debit or Credit

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("ParentAccountId")]
    public virtual ChartOfAccount? ParentAccount { get; set; }

    public virtual ICollection<ChartOfAccount> ChildAccounts { get; set; } = new List<ChartOfAccount>();
    public virtual ICollection<JournalEntryLine> JournalEntryLines { get; set; } = new List<JournalEntryLine>();
}

