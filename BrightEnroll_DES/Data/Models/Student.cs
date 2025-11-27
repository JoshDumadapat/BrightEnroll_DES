using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Maps to tbl_Students table
[Table("tbl_Students")]
public class Student
{
    [Key]
    [Column("student_id")]
    [MaxLength(6)]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("middle_name")]
    public string? MiddleName { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(10)]
    [Column("suffix")]
    public string? Suffix { get; set; }

    [Required]
    [Column("birthdate", TypeName = "date")]
    public DateTime Birthdate { get; set; }

    [Required]
    [Column("age")]
    public int Age { get; set; }

    [MaxLength(100)]
    [Column("place_of_birth")]
    public string? PlaceOfBirth { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("sex")]
    public string Sex { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("mother_tongue")]
    public string? MotherTongue { get; set; }

    [Required]
    [Column("ip_comm")]
    public bool IpComm { get; set; } = false;

    [MaxLength(50)]
    [Column("ip_specify")]
    public string? IpSpecify { get; set; }

    [Required]
    [Column("four_ps")]
    public bool FourPs { get; set; } = false;

    [MaxLength(50)]
    [Column("four_ps_hseID")]
    public string? FourPsHseId { get; set; }

    [MaxLength(20)]
    [Column("hse_no")]
    public string? HseNo { get; set; }

    [MaxLength(100)]
    [Column("street")]
    public string? Street { get; set; }

    [MaxLength(50)]
    [Column("brngy")]
    public string? Brngy { get; set; }

    [MaxLength(50)]
    [Column("province")]
    public string? Province { get; set; }

    [MaxLength(50)]
    [Column("city")]
    public string? City { get; set; }

    [MaxLength(50)]
    [Column("country")]
    public string? Country { get; set; }

    [MaxLength(10)]
    [Column("zip_code")]
    public string? ZipCode { get; set; }

    [MaxLength(20)]
    [Column("phse_no")]
    public string? PhseNo { get; set; }

    [MaxLength(100)]
    [Column("pstreet")]
    public string? Pstreet { get; set; }

    [MaxLength(50)]
    [Column("pbrngy")]
    public string? Pbrngy { get; set; }

    [MaxLength(50)]
    [Column("pprovince")]
    public string? Pprovince { get; set; }

    [MaxLength(50)]
    [Column("pcity")]
    public string? Pcity { get; set; }

    [MaxLength(50)]
    [Column("pcountry")]
    public string? Pcountry { get; set; }

    [MaxLength(10)]
    [Column("pzip_code")]
    public string? PzipCode { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("student_type")]
    public string StudentType { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("LRN")]
    public string? Lrn { get; set; }

    [MaxLength(20)]
    [Column("school_yr")]
    public string? SchoolYr { get; set; }

    [MaxLength(10)]
    [Column("grade_level")]
    public string? GradeLevel { get; set; }

    [Required]
    [Column("guardian_id")]
    public int GuardianId { get; set; }

    [Required]
    [Column("date_registered", TypeName = "datetime")]
    public DateTime DateRegistered { get; set; } = DateTime.Now;

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("archive_reason", TypeName = "text")]
    public string? ArchiveReason { get; set; }

    [ForeignKey("GuardianId")]
    public virtual Guardian Guardian { get; set; } = null!;

    public virtual ICollection<StudentRequirement> Requirements { get; set; } = new List<StudentRequirement>();
}

