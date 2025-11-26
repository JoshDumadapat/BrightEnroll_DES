using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

[Table("tbl_employee_address")]
public class EmployeeAddress
{
    [Key]
    [Column("address_ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AddressId { get; set; }

    [Column("user_ID")]
    public int UserId { get; set; }

    [MaxLength(50)]
    [Column("house_no")]
    public string? HouseNo { get; set; }

    [MaxLength(150)]
    [Column("street_name")]
    public string? StreetName { get; set; }

    [MaxLength(100)]
    [Column("province")]
    public string? Province { get; set; }

    [MaxLength(100)]
    [Column("city")]
    public string? City { get; set; }

    [MaxLength(150)]
    [Column("barangay")]
    public string? Barangay { get; set; }

    [MaxLength(100)]
    [Column("country")]
    public string? Country { get; set; }

    [MaxLength(10)]
    [Column("zip_code")]
    public string? ZipCode { get; set; }

    [ForeignKey("UserId")]
    public UserEntity User { get; set; } = null!;
}


