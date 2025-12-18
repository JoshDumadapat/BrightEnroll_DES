using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BrightEnroll_DES.Data.Models;

namespace BrightEnroll_DES.Data.Models.SuperAdmin;

/// <summary>
/// Represents a customer's subscription (active or historical)
/// Supports both predefined plans and custom module selections
/// </summary>
[Table("tbl_CustomerSubscriptions")]
public class CustomerSubscription
{
    [Key]
    [Column("subscription_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SubscriptionId { get; set; }

    [Required]
    [Column("customer_id")]
    public int CustomerId { get; set; }

    [Column("plan_id")]
    public int? PlanId { get; set; } // NULL for custom subscriptions

    [Required]
    [MaxLength(20)]
    [Column("subscription_type")]
    public string SubscriptionType { get; set; } = "predefined"; // 'predefined' or 'custom'

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Active"; // 'Active', 'Suspended', 'Expired', 'Cancelled'

    [Required]
    [Column("start_date", TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column("end_date", TypeName = "date")]
    public DateTime? EndDate { get; set; } // NULL = no expiration

    [Required]
    [Column("monthly_fee", TypeName = "decimal(18,2)")]
    public decimal MonthlyFee { get; set; } = 0;

    [Column("auto_renewal")]
    public bool AutoRenewal { get; set; } = false;

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    // Navigation properties
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [ForeignKey("PlanId")]
    public virtual SubscriptionPlan? Plan { get; set; }

    public virtual ICollection<CustomerSubscriptionModule> CustomerSubscriptionModules { get; set; } = new List<CustomerSubscriptionModule>();
}
