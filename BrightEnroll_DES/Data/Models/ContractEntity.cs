using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models
{
    [Table("tbl_Contracts")]
    public class ContractEntity
    {
        [Key]
        [Column("contract_id")]
        public int ContractId { get; set; }

        [Required]
        [MaxLength(150)]
        [Column("school_name")]
        public string SchoolName { get; set; } = string.Empty;

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime EndDate { get; set; }

        [Column("max_users")]
        public int MaxUsers { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("enabled_modules")]
        public string EnabledModules { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "Active";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}


