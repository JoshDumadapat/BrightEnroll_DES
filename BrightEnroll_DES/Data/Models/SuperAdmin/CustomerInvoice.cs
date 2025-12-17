using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models.SuperAdmin;

[Table("tbl_CustomerInvoices")]
public class CustomerInvoice
{
    [Key]
    [Column("invoice_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int InvoiceId { get; set; }

    [Required]
    [Column("customer_id")]
    public int CustomerId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("invoice_number")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Column("invoice_date", TypeName = "date")]
    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    [Column("due_date", TypeName = "date")]
    public DateTime DueDate { get; set; }

    [Column("billing_period_start", TypeName = "date")]
    public DateTime? BillingPeriodStart { get; set; }

    [Column("billing_period_end", TypeName = "date")]
    public DateTime? BillingPeriodEnd { get; set; }

    [Column("subtotal", TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    [Column("vat_amount", TypeName = "decimal(18,2)")]
    public decimal VatAmount { get; set; }

    [Column("total_amount", TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Column("amount_paid", TypeName = "decimal(18,2)")]
    public decimal AmountPaid { get; set; } = 0;

    [Column("balance", TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; }

    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Pending"; // Pending, Partially Paid, Paid, Overdue, Cancelled

    [Column("payment_terms", TypeName = "nvarchar(200)")]
    public string? PaymentTerms { get; set; }

    [Column("notes", TypeName = "nvarchar(max)")]
    public string? Notes { get; set; }

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("paid_at", TypeName = "datetime")]
    public DateTime? PaidAt { get; set; }

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }
}

[Table("tbl_CustomerPayments")]
public class CustomerPayment
{
    [Key]
    [Column("payment_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PaymentId { get; set; }

    [Required]
    [Column("invoice_id")]
    public int InvoiceId { get; set; }

    [Required]
    [Column("customer_id")]
    public int CustomerId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("payment_reference")]
    public string PaymentReference { get; set; } = string.Empty;

    [Column("payment_date", TypeName = "date")]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(50)]
    [Column("payment_method")]
    public string PaymentMethod { get; set; } = "Bank Transfer"; // Bank Transfer, Cash, Check, Online Payment

    [Column("notes", TypeName = "nvarchar(max)")]
    public string? Notes { get; set; }

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    // Navigation properties
    [ForeignKey("InvoiceId")]
    public virtual CustomerInvoice? Invoice { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }
}
