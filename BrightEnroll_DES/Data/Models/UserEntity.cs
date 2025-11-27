using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// EF Core entity for tbl_Users table
[Table("tbl_Users")]
public class UserEntity
{
    [Key]
    [Column("user_ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UserId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("system_ID")]
    public string SystemId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("mid_name")]
    public string? MidName { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(10)]
    [Column("suffix")]
    public string? Suffix { get; set; }

    [Required]
    [Column("birthdate", TypeName = "date")]
    public DateTime Birthdate { get; set; }

    [Required]
    [Column("age")]
    public byte Age { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("gender")]
    public string Gender { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("contact_num")]
    public string ContactNum { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("user_role")]
    public string UserRole { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Column("date_hired", TypeName = "datetime")]
    public DateTime DateHired { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "active";

    [Required]
    [Column("is_synced")]
    public bool IsSynced { get; set; } = false;
}

