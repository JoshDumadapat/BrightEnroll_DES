using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

/// <summary>
/// EF Core model for guardians_tbl table
/// </summary>
[Table("guardians_tbl")]
public class Guardian
{
    [Key]
    [Column("guardian_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int GuardianId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("middle_name")]
    public string? MiddleName { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(10)]
    [Column("suffix")]
    public string? Suffix { get; set; }

    [MaxLength(20)]
    [Column("contact_num")]
    public string? ContactNum { get; set; }

    [MaxLength(50)]
    [Column("relationship")]
    public string? Relationship { get; set; }

    // Navigation property
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}

