using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_Customers")]
public class Customer
{
    [Key]
    [Column("customer_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CustomerId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("customer_code")]
    public string CustomerCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("school_name")]
    public string SchoolName { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("school_type")]
    public string? SchoolType { get; set; }

    [MaxLength(500)]
    [Column("address")]
    public string? Address { get; set; }

    [MaxLength(200)]
    [Column("contact_person")]
    public string? ContactPerson { get; set; }

    [MaxLength(100)]
    [Column("contact_position")]
    public string? ContactPosition { get; set; }

    [MaxLength(150)]
    [Column("contact_email")]
    public string? ContactEmail { get; set; }

    [MaxLength(20)]
    [Column("contact_phone")]
    public string? ContactPhone { get; set; }

    [MaxLength(50)]
    [Column("subscription_plan")]
    public string? SubscriptionPlan { get; set; }

    [Column("monthly_fee", TypeName = "decimal(18,2)")]
    public decimal MonthlyFee { get; set; }

    [Column("contract_start_date", TypeName = "date")]
    public DateTime? ContractStartDate { get; set; }

    [Column("contract_end_date", TypeName = "date")]
    public DateTime? ContractEndDate { get; set; }

    [Column("contract_duration_months")]
    public int? ContractDurationMonths { get; set; }

    [Column("student_count")]
    public int? StudentCount { get; set; }

    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Active";

    [Column("notes", TypeName = "nvarchar(max)")]
    public string? Notes { get; set; }

    [Column("date_registered", TypeName = "datetime")]
    public DateTime DateRegistered { get; set; } = DateTime.Now;

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [Column("database_name", TypeName = "nvarchar(200)")]
    public string? DatabaseName { get; set; }

    [Column("database_connection_string", TypeName = "nvarchar(max)")]
    public string? DatabaseConnectionString { get; set; }

    [Column("cloud_connection_string", TypeName = "nvarchar(max)")]
    public string? CloudConnectionString { get; set; }

    [Column("admin_username", TypeName = "nvarchar(100)")]
    public string? AdminUsername { get; set; }

    [Column("admin_password", TypeName = "nvarchar(255)")]
    public string? AdminPassword { get; set; }

    // BIR Compliance Information (for the school/customer)
    [MaxLength(50)]
    [Column("bir_tin")]
    public string? BirTin { get; set; }

    [MaxLength(200)]
    [Column("bir_business_name")]
    public string? BirBusinessName { get; set; }

    [Column("bir_address", TypeName = "nvarchar(500)")]
    public string? BirAddress { get; set; }

    [MaxLength(20)]
    [Column("bir_registration_type")]
    public string? BirRegistrationType { get; set; }

    [Column("is_vat_registered")]
    public bool IsVatRegistered { get; set; }

    [Column("vat_rate", TypeName = "decimal(5,2)")]
    public decimal? VatRate { get; set; }

    // Contract Terms & Services
    [Column("warranty_period", TypeName = "nvarchar(100)")]
    public string? WarrantyPeriod { get; set; }

    [Column("maintenance_schedule", TypeName = "nvarchar(200)")]
    public string? MaintenanceSchedule { get; set; }

    [Column("free_services", TypeName = "nvarchar(500)")]
    public string? FreeServices { get; set; }

    [Column("payment_terms", TypeName = "nvarchar(200)")]
    public string? PaymentTerms { get; set; }

    [Column("termination_clause", TypeName = "nvarchar(500)")]
    public string? TerminationClause { get; set; }

    [Column("sla_details", TypeName = "nvarchar(500)")]
    public string? SlaDetails { get; set; }

    [Column("contract_terms_text", TypeName = "nvarchar(max)")]
    public string? ContractTermsText { get; set; }

    [Column("auto_renewal")]
    public bool AutoRenewal { get; set; }

    // Navigation properties
    [ForeignKey("CreatedBy")]
    public virtual UserEntity? CreatedByUser { get; set; }
}

