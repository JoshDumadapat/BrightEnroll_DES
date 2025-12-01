using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models
{
    [Table("tbl_SchoolCustomers")]
    public class SchoolCustomerEntity
    {
        [Key]
        [Column("customer_id")]
        public int CustomerId { get; set; }

        [Required]
        [MaxLength(150)]
        [Column("school_name")]
        public string SchoolName { get; set; } = string.Empty;

        [MaxLength(50)]
        [Column("school_type")]
        public string? SchoolType { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("address")]
        public string Address { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("city")]
        public string City { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column("province")]
        public string? Province { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("contact_person")]
        public string ContactPerson { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column("contact_position")]
        public string? ContactPosition { get; set; }

        [Required]
        [MaxLength(150)]
        [Column("contact_email")]
        public string ContactEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        [Column("contact_phone")]
        public string ContactPhone { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [Column("plan")]
        public string Plan { get; set; } = string.Empty;

        [Column("monthly_fee")]
        public decimal MonthlyFee { get; set; }

        [Column("contract_start_date")]
        public DateTime ContractStartDate { get; set; }

        [Column("contract_end_date")]
        public DateTime ContractEndDate { get; set; }

        [Column("student_count")]
        public int? StudentCount { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "Active";

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}


