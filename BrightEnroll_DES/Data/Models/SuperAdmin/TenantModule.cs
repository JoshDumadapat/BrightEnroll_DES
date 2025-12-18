using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BrightEnroll_DES.Data.Models;

namespace BrightEnroll_DES.Data.Models.SuperAdmin;

// Materialized cache table for tenant module entitlements
[Table("tbl_TenantModules")]
public class TenantModule
{
    [Key]
    [Column("tenant_module_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TenantModuleId { get; set; }

    [Required]
    [Column("customer_id")]
    public int CustomerId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("module_package_id")]
    public string ModulePackageId { get; set; } = string.Empty;

    [Required]
    [Column("subscription_id")]
    public int SubscriptionId { get; set; }

    [Column("granted_date", TypeName = "datetime")]
    public DateTime GrantedDate { get; set; } = DateTime.Now;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("last_updated", TypeName = "datetime")]
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    // Navigation properties
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [ForeignKey("SubscriptionId")]
    public virtual CustomerSubscription? Subscription { get; set; }
}
