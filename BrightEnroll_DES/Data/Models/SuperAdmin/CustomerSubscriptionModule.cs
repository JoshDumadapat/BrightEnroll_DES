using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models.SuperAdmin;

/// <summary>
/// Stores explicitly granted modules for custom subscriptions
/// Only used when subscription_type = 'custom'
/// </summary>
[Table("tbl_CustomerSubscriptionModules")]
public class CustomerSubscriptionModule
{
    [Key]
    [Column("customer_module_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CustomerModuleId { get; set; }

    [Required]
    [Column("subscription_id")]
    public int SubscriptionId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("module_package_id")]
    public string ModulePackageId { get; set; } = string.Empty; // 'core', 'enrollment', 'finance', 'hr_payroll'

    [Column("granted_date", TypeName = "datetime")]
    public DateTime GrantedDate { get; set; } = DateTime.Now;

    [Column("granted_by")]
    public int? GrantedBy { get; set; }

    [Column("revoked_date", TypeName = "datetime")]
    public DateTime? RevokedDate { get; set; } // Soft delete for audit trail

    [Column("revoked_by")]
    public int? RevokedBy { get; set; }

    // Navigation properties
    [ForeignKey("SubscriptionId")]
    public virtual CustomerSubscription? Subscription { get; set; }
}
