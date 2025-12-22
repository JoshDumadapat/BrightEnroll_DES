using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_SalesLeads")]
public class SalesLead
{
    [Key]
    [Column("lead_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LeadId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("lead_code")]
    public string LeadCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("school_name")]
    public string SchoolName { get; set; } = string.Empty;

    [MaxLength(200)]
    [Column("contact_person")]
    public string? ContactPerson { get; set; }

    [MaxLength(150)]
    [Column("contact_email")]
    public string? ContactEmail { get; set; }

    [MaxLength(20)]
    [Column("contact_phone")]
    public string? ContactPhone { get; set; }

    [MaxLength(500)]
    [Column("address")]
    public string? Address { get; set; }

    [MaxLength(50)]
    [Column("stage")]
    public string Stage { get; set; } = "New Lead"; 

    [MaxLength(50)]
    [Column("interested_plan")]
    public string? InterestedPlan { get; set; }

    [Column("estimated_value", TypeName = "decimal(18,2)")]
    public decimal? EstimatedValue { get; set; }

    [Column("follow_up_date", TypeName = "date")]
    public DateTime? FollowUpDate { get; set; }

    [Column("conversion_date", TypeName = "date")]
    public DateTime? ConversionDate { get; set; }

    [Column("converted_amount", TypeName = "decimal(18,2)")]
    public decimal? ConvertedAmount { get; set; }

    [Column("assigned_to")]
    public int? AssignedTo { get; set; }

    [MaxLength(1000)]
    [Column("notes", TypeName = "nvarchar(max)")]
    public string? Notes { get; set; }

    [Column("created_at", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("AssignedTo")]
    public virtual UserEntity? AssignedToUser { get; set; }
}

