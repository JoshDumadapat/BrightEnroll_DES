using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_SuperAdminBIRFilings")]
public class SuperAdminBIRFiling
{
    [Key]
    [Column("filing_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int FilingId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("filing_type")]
    public string FilingType { get; set; } = string.Empty; // "Quarterly VAT", "Annual ITR", "Monthly VAT", etc.

    [Required]
    [MaxLength(20)]
    [Column("period")]
    public string Period { get; set; } = string.Empty; // "Q1 2024", "2024", "Jan 2024", etc.

    [Required]
    [Column("filing_date", TypeName = "datetime")]
    public DateTime FilingDate { get; set; }

    [Required]
    [Column("due_date", TypeName = "datetime")]
    public DateTime DueDate { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Pending"; // "Pending", "Filed", "Overdue", "Late Filed"

    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal? Amount { get; set; }

    [MaxLength(100)]
    [Column("reference_number")]
    public string? ReferenceNumber { get; set; } // BIR confirmation number, receipt number, etc.

    [Column("notes", TypeName = "nvarchar(1000)")]
    public string? Notes { get; set; }

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }
}
