using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_InventoryItems")]
public class InventoryItem
{
    [Key]
    [Column("item_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ItemId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("item_code")]
    public string ItemCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("item_name")]
    public string ItemName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("unit")]
    public string Unit { get; set; } = "Piece"; // Piece, Box, Ream, etc.

    [Column("quantity")]
    public int Quantity { get; set; } = 0;

    [Column("reorder_level")]
    public int ReorderLevel { get; set; } = 10;

    [Column("max_stock")]
    public int MaxStock { get; set; } = 1000;

    [Column("unit_price", TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; } = 0.00m;

    [MaxLength(200)]
    [Column("supplier")]
    public string? Supplier { get; set; }

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

