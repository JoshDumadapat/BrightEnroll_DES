using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Maps to tbl_roles table
[Table("tbl_roles")]
public class Role
{
    [Key]
    [Column("role_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int RoleId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("role_name")]
    public string RoleName { get; set; } = string.Empty;

    [Required]
    [Column("base_salary", TypeName = "decimal(12,2)")]
    public decimal BaseSalary { get; set; }

    [Column("allowance", TypeName = "decimal(12,2)")]
    public decimal Allowance { get; set; } = 0.00m;

    [Column("threshold_percentage", TypeName = "decimal(5,2)")]
    public decimal ThresholdPercentage { get; set; } = 0.00m; // Default 0%

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_date", TypeName = "datetime")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Column("updated_date", TypeName = "datetime")]
    public DateTime? UpdatedDate { get; set; }
}

