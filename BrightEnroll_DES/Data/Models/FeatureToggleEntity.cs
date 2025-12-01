using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models
{
    [Table("tbl_FeatureToggles")]
    public class FeatureToggleEntity
    {
        [Key]
        [Column("feature_id")]
        public int FeatureId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("feature_name")]
        public string FeatureName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [Column("plan")]
        public string Plan { get; set; } = string.Empty;

        [Column("is_enabled")]
        public bool IsEnabled { get; set; }
    }
}


