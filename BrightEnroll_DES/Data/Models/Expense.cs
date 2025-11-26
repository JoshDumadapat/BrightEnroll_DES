using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_Expenses")]
public class Expense
{
    [Key]
    [Column("expense_ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ExpenseId { get; set; }

    // Human‑readable ID shown in the UI, e.g. "EXP-20251126123456"
    [Required]
    [MaxLength(40)]
    [Column("expense_code")]
    public string ExpenseCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [Column("expense_date")]
    public DateTime ExpenseDate { get; set; }

    [MaxLength(150)]
    [Column("payee")]
    public string? Payee { get; set; }

    [MaxLength(50)]
    [Column("or_number")]
    public string? OrNumber { get; set; }

    [MaxLength(30)]
    [Column("payment_method")]
    public string PaymentMethod { get; set; } = "Cash";

    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Pending";

    [MaxLength(100)]
    [Column("recorded_by")]
    public string? RecordedBy { get; set; }

    [MaxLength(100)]
    [Column("approved_by")]
    public string? ApprovedBy { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual ICollection<ExpenseAttachment> Attachments { get; set; } = new List<ExpenseAttachment>();
}

[Table("tbl_ExpenseAttachments")]
public class ExpenseAttachment
{
    [Key]
    [Column("attachment_ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AttachmentId { get; set; }

    [Required]
    [Column("expense_ID")]
    public int ExpenseId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Column("file_path")]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; } = DateTime.Now;

    [ForeignKey(nameof(ExpenseId))]
    public virtual Expense? Expense { get; set; }
}


