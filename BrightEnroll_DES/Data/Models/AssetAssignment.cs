using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_AssetAssignments")]
public class AssetAssignment
{
    [Key]
    [Column("assignment_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AssignmentId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("asset_id")]
    public string AssetId { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("assigned_to_type")]
    public string AssignedToType { get; set; } = string.Empty; // Employee, Section, Classroom

    [MaxLength(50)]
    [Column("assigned_to_id")]
    public string? AssignedToId { get; set; }

    [MaxLength(200)]
    [Column("assigned_to_name")]
    public string? AssignedToName { get; set; }

    [Column("assigned_date", TypeName = "datetime")]
    public DateTime AssignedDate { get; set; } = DateTime.Now;

    [Column("return_date", TypeName = "datetime")]
    public DateTime? ReturnDate { get; set; }

    [MaxLength(500)]
    [Column("notes")]
    public string? Notes { get; set; }

    [MaxLength(50)]
    [Column("status")]
    public string Status { get; set; } = "Active"; // Active, Returned

    [Column("created_date", TypeName = "datetime")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Navigation property
    [ForeignKey("AssetId")]
    public Asset? Asset { get; set; }
}

