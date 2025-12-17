using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_SchoolInformation")]
public class SchoolInformation
{
    [Key]
    [Column("school_info_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SchoolInfoId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("school_name")]
    public string SchoolName { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("school_code")]
    public string? SchoolCode { get; set; }

    [MaxLength(20)]
    [Column("contact_number")]
    public string? ContactNumber { get; set; }

    [MaxLength(150)]
    [Column("email")]
    public string? Email { get; set; }

    [MaxLength(255)]
    [Column("website")]
    public string? Website { get; set; }

    [MaxLength(50)]
    [Column("house_no")]
    public string? HouseNo { get; set; }

    [MaxLength(200)]
    [Column("street_name")]
    public string? StreetName { get; set; }

    [MaxLength(100)]
    [Column("barangay")]
    public string? Barangay { get; set; }

    [MaxLength(100)]
    [Column("city")]
    public string? City { get; set; }

    [MaxLength(100)]
    [Column("province")]
    public string? Province { get; set; }

    [MaxLength(100)]
    [Column("country")]
    public string Country { get; set; } = "Philippines";

    [MaxLength(20)]
    [Column("zip_code")]
    public string? ZipCode { get; set; }

    // BIR Information
    [MaxLength(50)]
    [Column("bir_tin")]
    public string? BirTin { get; set; }

    [MaxLength(200)]
    [Column("bir_business_name")]
    public string? BirBusinessName { get; set; }

    [Column("bir_address", TypeName = "nvarchar(500)")]
    public string? BirAddress { get; set; }

    [MaxLength(20)]
    [Column("bir_registration_type")]
    public string? BirRegistrationType { get; set; }

    [Column("vat_rate", TypeName = "decimal(5,2)")]
    public decimal? VatRate { get; set; }

    [Column("is_vat_registered")]
    public bool IsVatRegistered { get; set; }

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }
}
