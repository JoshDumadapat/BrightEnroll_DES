using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_Classrooms")]
public class Classroom
{
    [Key]
    [Column("RoomID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int RoomId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("RoomName")]
    public string RoomName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("BuildingName")]
    public string? BuildingName { get; set; }

    [Column("FloorNumber")]
    public int? FloorNumber { get; set; }

    [MaxLength(50)]
    [Column("RoomType")]
    public string? RoomType { get; set; }

    [Required]
    [Column("Capacity")]
    public int Capacity { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("Status")]
    public string Status { get; set; } = "Active"; // Active / Inactive

    [MaxLength(500)]
    [Column("Notes")]
    public string? Notes { get; set; }

    [Column("CreatedAt", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("UpdatedAt", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [Required]
    [Column("is_synced")]
    public bool IsSynced { get; set; } = false;

    // Navigation properties
    public virtual ICollection<Section> Sections { get; set; } = new List<Section>();
    public virtual ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();
}

