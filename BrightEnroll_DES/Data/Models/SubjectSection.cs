using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_SubjectSection")]
public class SubjectSection
{
    [Key]
    [Column("ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("SectionID")]
    public int SectionId { get; set; }

    [Required]
    [Column("SubjectID")]
    public int SubjectId { get; set; }

    // Navigation properties
    [ForeignKey("SectionId")]
    public virtual Section? Section { get; set; }

    [ForeignKey("SubjectId")]
    public virtual Subject? Subject { get; set; }
}

