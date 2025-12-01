using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models
{
    [Table("tbl_SystemVersions")]
    public class SystemVersionEntity
    {
        [Key]
        [Column("version_id")]
        public int VersionId { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("version_name")]
        public string VersionName { get; set; } = string.Empty;

        [Column("release_date")]
        public DateTime ReleaseDate { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }
    }
}


