using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models.SuperAdmin;

/// <summary>
/// Represents a predefined subscription plan (Basic, Standard, Premium, etc.)
/// </summary>
[Table("tbl_SubscriptionPlans")]
public class SubscriptionPlan
{
    [Key]
    [Column("plan_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PlanId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("plan_code")]
    public string PlanCode { get; set; } = string.Empty; // 'basic', 'standard', 'premium'

    [Required]
    [MaxLength(100)]
    [Column("plan_name")]
    public string PlanName { get; set; } = string.Empty; // 'Basic Plan', 'Standard Plan', 'Premium Plan'

    [Column("description", TypeName = "nvarchar(500)")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<PlanModule> PlanModules { get; set; } = new List<PlanModule>();
    public virtual ICollection<CustomerSubscription> CustomerSubscriptions { get; set; } = new List<CustomerSubscription>();
}
