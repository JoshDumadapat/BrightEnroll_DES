using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_SystemUpdates")]
public class SystemUpdate
{
    [Key]
    [Column("update_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UpdateId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("version_number")]
    public string VersionNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description", TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    [MaxLength(50)]
    [Column("update_type")]
    public string UpdateType { get; set; } = "Feature"; // Feature, Bug Fix, Security, Enhancement

    [Column("release_date", TypeName = "date")]
    public DateTime ReleaseDate { get; set; }

    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Released"; // Planned, In Development, Testing, Released

    [Column("is_major_update")]
    public bool IsMajorUpdate { get; set; } = false;

    [Column("requires_action")]
    public bool RequiresAction { get; set; } = false;

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("CreatedBy")]
    public virtual UserEntity? CreatedByUser { get; set; }
}

