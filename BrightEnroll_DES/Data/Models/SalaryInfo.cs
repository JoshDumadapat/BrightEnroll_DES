using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Maps to tbl_salary_info table
[Table("tbl_salary_info")]
public class SalaryInfo
{
    [Key]
    [Column("salary_ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SalaryId { get; set; }

    [Required]
    [Column("user_ID")]
    public int UserId { get; set; }

    [Required]
    [Column("base_salary", TypeName = "decimal(12,2)")]
    public decimal BaseSalary { get; set; }

    [Column("allowance", TypeName = "decimal(12,2)")]
    public decimal Allowance { get; set; } = 0.00m;

    [Column("date_effective", TypeName = "date")]
    public DateTime DateEffective { get; set; } = DateTime.Today;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}

