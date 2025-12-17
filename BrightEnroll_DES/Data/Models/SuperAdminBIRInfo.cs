using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_SuperAdminBIRInfo")]
public class SuperAdminBIRInfo
{
    [Key]
    [Column("bir_info_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int BirInfoId { get; set; }

    [MaxLength(50)]
    [Column("tin_number")]
    public string? TinNumber { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("business_name")]
    public string BusinessName { get; set; } = string.Empty;

    [Column("business_address", TypeName = "nvarchar(500)")]
    public string? BusinessAddress { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("registration_type")]
    public string RegistrationType { get; set; } = "VAT";

    [Column("vat_rate", TypeName = "decimal(5,2)")]
    public decimal VatRate { get; set; } = 0.12m;

    [Column("is_vat_registered")]
    public bool IsVatRegistered { get; set; } = true;

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }
}
