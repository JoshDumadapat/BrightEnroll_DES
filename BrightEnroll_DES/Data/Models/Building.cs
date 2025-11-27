using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_Buildings")]
public class Building
{
    [Key]
    [Column("BuildingID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int BuildingId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("BuildingName")]
    public string BuildingName { get; set; } = string.Empty;

    [Column("FloorCount")]
    public int? FloorCount { get; set; }

    [MaxLength(500)]
    [Column("Description")]
    public string? Description { get; set; }

    [Required]
    [Column("is_synced")]
    public bool IsSynced { get; set; } = false;
}

