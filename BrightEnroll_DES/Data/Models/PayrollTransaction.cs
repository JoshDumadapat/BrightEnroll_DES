using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Tracks payroll payment transactions
[Table("tbl_payroll_transactions")]
public class PayrollTransaction
{
    [Key]
    [Column("transaction_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TransactionId { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("school_year")]
    public string SchoolYear { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("pay_period")]
    public string PayPeriod { get; set; } = string.Empty; 

    [Required]
    [Column("base_salary", TypeName = "decimal(12,2)")]
    public decimal BaseSalary { get; set; }

    [Required]
    [Column("allowance", TypeName = "decimal(12,2)")]
    public decimal Allowance { get; set; }

    [Required]
    [Column("gross_salary", TypeName = "decimal(12,2)")]
    public decimal GrossSalary { get; set; }

    // Deductions
    [Column("sss_deduction", TypeName = "decimal(12,2)")]
    public decimal SssDeduction { get; set; } = 0.00m;

    [Column("philhealth_deduction", TypeName = "decimal(12,2)")]
    public decimal PhilHealthDeduction { get; set; } = 0.00m;

    [Column("pagibig_deduction", TypeName = "decimal(12,2)")]
    public decimal PagIbigDeduction { get; set; } = 0.00m;

    [Column("tax_deduction", TypeName = "decimal(12,2)")]
    public decimal TaxDeduction { get; set; } = 0.00m;

    [Column("other_deductions", TypeName = "decimal(12,2)")]
    public decimal OtherDeductions { get; set; } = 0.00m;

    [Required]
    [Column("total_deductions", TypeName = "decimal(12,2)")]
    public decimal TotalDeductions { get; set; } = 0.00m;

    [Required]
    [Column("net_salary", TypeName = "decimal(12,2)")]
    public decimal NetSalary { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Pending"; 

    [Column("payment_date", TypeName = "date")]
    public DateTime? PaymentDate { get; set; }

    [MaxLength(50)]
    [Column("payment_method")]
    public string? PaymentMethod { get; set; } 

    [MaxLength(100)]
    [Column("reference_number")]
    public string? ReferenceNumber { get; set; } 

    [Required]
    [Column("processed_by")]
    public int ProcessedBy { get; set; } 

    [Required]
    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [Column("batch_timestamp", TypeName = "datetime")]
    public DateTime? BatchTimestamp { get; set; }

    [MaxLength(500)]
    [Column("notes", TypeName = "nvarchar(500)")]
    public string? Notes { get; set; }

    // Company Contributions
    [Column("company_sss_contribution", TypeName = "decimal(12,2)")]
    public decimal CompanySssContribution { get; set; } = 0.00m;

    [Column("company_philhealth_contribution", TypeName = "decimal(12,2)")]
    public decimal CompanyPhilHealthContribution { get; set; } = 0.00m;

    [Column("company_pagibig_contribution", TypeName = "decimal(12,2)")]
    public decimal CompanyPagIbigContribution { get; set; } = 0.00m;

    [Column("total_company_contribution", TypeName = "decimal(12,2)")]
    public decimal TotalCompanyContribution { get; set; } = 0.00m;

    // Audit Trail
    [Column("created_by")]
    public int CreatedBy { get; set; } 

    [Column("approved_by")]
    public int? ApprovedBy { get; set; }

    [Column("approved_at", TypeName = "datetime")]
    public DateTime? ApprovedAt { get; set; }

    [Column("cancelled_by")]
    public int? CancelledBy { get; set; } 

    [Column("cancelled_at", TypeName = "datetime")]
    public DateTime? CancelledAt { get; set; }

    [MaxLength(500)]
    [Column("cancellation_reason", TypeName = "nvarchar(500)")]
    public string? CancellationReason { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual UserEntity? User { get; set; }

    [ForeignKey("ProcessedBy")]
    public virtual UserEntity? ProcessedByUser { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual UserEntity? CreatedByUser { get; set; }

    [ForeignKey("ApprovedBy")]
    public virtual UserEntity? ApprovedByUser { get; set; }

    [ForeignKey("CancelledBy")]
    public virtual UserEntity? CancelledByUser { get; set; }
}

