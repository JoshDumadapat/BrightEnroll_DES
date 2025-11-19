using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Maps to tbl_employee_emergency_contact table
[Table("tbl_employee_emergency_contact")]
public class EmployeeEmergencyContact
{
    [Key]
    [Column("emergency_ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EmergencyId { get; set; }

    [Required]
    [Column("user_ID")]
    public int UserId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("mid_name")]
    public string? MiddleName { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(10)]
    [Column("suffix")]
    public string? Suffix { get; set; }

    [MaxLength(50)]
    [Column("relationship")]
    public string? Relationship { get; set; }

    [MaxLength(20)]
    [Column("contact_number")]
    public string? ContactNumber { get; set; }

    [MaxLength(255)]
    [Column("address")]
    public string? Address { get; set; }
}

