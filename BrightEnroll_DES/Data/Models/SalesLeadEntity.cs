using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models
{
    [Table("tbl_SalesLeads")]
    public class SalesLeadEntity
    {
        [Key]
        [Column("lead_id")]
        public int LeadId { get; set; }

        [Required]
        [MaxLength(150)]
        [Column("school_name")]
        public string SchoolName { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [Column("location")]
        public string Location { get; set; } = string.Empty;

        [MaxLength(50)]
        [Column("school_type")]
        public string? SchoolType { get; set; }

        [Column("estimated_students")]
        public int? EstimatedStudents { get; set; }

        [MaxLength(200)]
        [Column("website")]
        public string? Website { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("contact_name")]
        public string ContactName { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column("contact_position")]
        public string? ContactPosition { get; set; }

        [Required]
        [MaxLength(150)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        [Column("phone")]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(30)]
        [Column("alternative_phone")]
        public string? AlternativePhone { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("lead_source")]
        public string LeadSource { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [Column("interest_level")]
        public string InterestLevel { get; set; } = string.Empty;

        [MaxLength(20)]
        [Column("interested_plan")]
        public string? InterestedPlan { get; set; }

        [Column("expected_close_date")]
        public DateTime? ExpectedCloseDate { get; set; }

        [MaxLength(100)]
        [Column("assigned_agent")]
        public string? AssignedAgent { get; set; }

        [MaxLength(50)]
        [Column("budget_range")]
        public string? BudgetRange { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "New";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}


