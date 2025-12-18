using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models.SuperAdmin;

/// <summary>
/// Maps a subscription plan to its included module packages
/// </summary>
[Table("tbl_PlanModules")]
public class PlanModule
{
    [Key]
    [Column("plan_module_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PlanModuleId { get; set; }

    [Required]
    [Column("plan_id")]
    public int PlanId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("module_package_id")]
    public string ModulePackageId { get; set; } = string.Empty; // 'core', 'enrollment', 'finance', 'hr_payroll'

    [Column("granted_date", TypeName = "datetime")]
    public DateTime GrantedDate { get; set; } = DateTime.Now;

    // Navigation properties
    [ForeignKey("PlanId")]
    public virtual SubscriptionPlan? Plan { get; set; }
}
