using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_Students")]
public class Student
{
    [Key]
    [MaxLength(6)]
    [Column("student_id")]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("middle_name")]
    public string MiddleName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(10)]
    [Column("suffix")]
    public string? Suffix { get; set; }

    [Column("birthdate", TypeName = "date")]
    public DateTime Birthdate { get; set; }

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

    [Column("ip_comm")]
    public bool IsIndigenous { get; set; }

    [MaxLength(50)]
    [Column("ip_specify")]
    public string? IndigenousGroup { get; set; }

    [Column("four_ps")]
    public bool IsFourPs { get; set; }

    [MaxLength(50)]
    [Column("four_ps_hseID")]
    public string? FourPsHouseholdId { get; set; }

    [MaxLength(20)]
    [Column("hse_no")]
    public string? HouseNumber { get; set; }

    [MaxLength(100)]
    [Column("street")]
    public string? Street { get; set; }

    [MaxLength(50)]
    [Column("brngy")]
    public string? Barangay { get; set; }

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
    public string? PermanentHouseNumber { get; set; }

    [MaxLength(100)]
    [Column("pstreet")]
    public string? PermanentStreet { get; set; }

    [MaxLength(50)]
    [Column("pbrngy")]
    public string? PermanentBarangay { get; set; }

    [MaxLength(50)]
    [Column("pprovince")]
    public string? PermanentProvince { get; set; }

    [MaxLength(50)]
    [Column("pcity")]
    public string? PermanentCity { get; set; }

    [MaxLength(50)]
    [Column("pcountry")]
    public string? PermanentCountry { get; set; }

    [MaxLength(10)]
    [Column("pzip_code")]
    public string? PermanentZipCode { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("student_type")]
    public string StudentType { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("LRN")]
    public string? Lrn { get; set; }

    [MaxLength(20)]
    [Column("school_yr")]
    public string? SchoolYear { get; set; }

    [MaxLength(10)]
    [Column("grade_level")]
    public string? GradeLevel { get; set; }

    [Column("guardian_id")]
    public int GuardianId { get; set; }

    [Column("date_registered", TypeName = "datetime")]
    public DateTime DateRegistered { get; set; }

    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "Pending";

    // Navigation
    [ForeignKey("GuardianId")]
    public Guardian Guardian { get; set; } = null!;

    public ICollection<StudentRequirement> Requirements { get; set; } = new List<StudentRequirement>();
}


