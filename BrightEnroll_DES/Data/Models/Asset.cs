using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_Assets")]
public class Asset
{
    [Key]
    [Column("asset_id")]
    [MaxLength(50)]
    public string AssetId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("asset_name")]
    public string AssetName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("brand")]
    public string? Brand { get; set; }

    [MaxLength(100)]
    [Column("model")]
    public string? Model { get; set; }

    [MaxLength(100)]
    [Column("serial_number")]
    public string? SerialNumber { get; set; }

    [MaxLength(100)]
    [Column("location")]
    public string Location { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("status")]
    public string Status { get; set; } = "Available"; // Available, In Use, Maintenance, Damaged, Disposed

    [Column("purchase_date", TypeName = "date")]
    public DateTime? PurchaseDate { get; set; }

    [Column("purchase_cost", TypeName = "decimal(18,2)")]
    public decimal PurchaseCost { get; set; } = 0.00m;

    [Column("current_value", TypeName = "decimal(18,2)")]
    public decimal CurrentValue { get; set; } = 0.00m;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("created_date", TypeName = "datetime")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Column("updated_date", TypeName = "datetime")]
    public DateTime? UpdatedDate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}

