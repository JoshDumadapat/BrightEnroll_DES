using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_Contracts")]
public class Contract
{
    [Key]
    [Column("contract_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ContractId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("contract_number")]
    public string ContractNumber { get; set; } = string.Empty;

    [Column("customer_id")]
    public int CustomerId { get; set; }

    [MaxLength(200)]
    [Column("contract_type")]
    public string? ContractType { get; set; }

    [Column("start_date", TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column("end_date", TypeName = "date")]
    public DateTime EndDate { get; set; }

    [Column("renewal_date", TypeName = "date")]
    public DateTime? RenewalDate { get; set; }

    [Column("monthly_fee", TypeName = "decimal(18,2)")]
    public decimal MonthlyFee { get; set; }

    [MaxLength(50)]
    [Column("status")]
    public string Status { get; set; } = "Active"; // Active, Expired, Renewed, Cancelled

    [Column("auto_renew")]
    public bool AutoRenew { get; set; } = false;

    [Column("contract_file_path", TypeName = "nvarchar(max)")]
    public string? ContractFilePath { get; set; }

    [Column("notes", TypeName = "nvarchar(max)")]
    public string? Notes { get; set; }

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual UserEntity? CreatedByUser { get; set; }
}

