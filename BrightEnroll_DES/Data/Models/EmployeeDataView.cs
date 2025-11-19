using System.ComponentModel.DataAnnotations.Schema;

namespace BrightEnroll_DES.Data.Models;

// Keyless entity for vw_EmployeeData view - read-only
[Table("vw_EmployeeData")]
public class EmployeeDataView
{
    [Column("UserId")]
    public int UserId { get; set; }

    [Column("SystemId")]
    public string? SystemId { get; set; }

    [Column("FirstName")]
    public string? FirstName { get; set; }

    [Column("MiddleName")]
    public string? MiddleName { get; set; }

    [Column("LastName")]
    public string? LastName { get; set; }

    [Column("Suffix")]
    public string? Suffix { get; set; }

    [Column("FullName")]
    public string? FullName { get; set; }

    [Column("BirthDate")]
    public DateTime? BirthDate { get; set; }

    [Column("Age")]
    public byte? Age { get; set; }

    [Column("Gender")]
    public string? Gender { get; set; }

    [Column("ContactNumber")]
    public string? ContactNumber { get; set; }

    [Column("Role")]
    public string? Role { get; set; }

    [Column("Email")]
    public string? Email { get; set; }

    [Column("DateHired")]
    public DateTime? DateHired { get; set; }

    [Column("Status")]
    public string? Status { get; set; }

    [Column("FormattedAddress")]
    public string? FormattedAddress { get; set; }

    // Address fields
    [Column("HouseNo")]
    public string? HouseNo { get; set; }

    [Column("StreetName")]
    public string? StreetName { get; set; }

    [Column("Province")]
    public string? Province { get; set; }

    [Column("City")]
    public string? City { get; set; }

    [Column("Barangay")]
    public string? Barangay { get; set; }

    [Column("Country")]
    public string? Country { get; set; }

    [Column("ZipCode")]
    public string? ZipCode { get; set; }

    // Emergency contact fields
    [Column("EmergencyContactFirstName")]
    public string? EmergencyContactFirstName { get; set; }

    [Column("EmergencyContactMiddleName")]
    public string? EmergencyContactMiddleName { get; set; }

    [Column("EmergencyContactLastName")]
    public string? EmergencyContactLastName { get; set; }

    [Column("EmergencyContactSuffix")]
    public string? EmergencyContactSuffix { get; set; }

    [Column("EmergencyContactRelationship")]
    public string? EmergencyContactRelationship { get; set; }

    [Column("EmergencyContactNumber")]
    public string? EmergencyContactNumber { get; set; }

    [Column("EmergencyContactAddress")]
    public string? EmergencyContactAddress { get; set; }

    // Salary fields
    [Column("BaseSalary")]
    public decimal? BaseSalary { get; set; }

    [Column("Allowance")]
    public decimal? Allowance { get; set; }

    [Column("TotalSalary")]
    public decimal? TotalSalary { get; set; }

    [Column("SalaryDateEffective")]
    public DateTime? SalaryDateEffective { get; set; }

    [Column("SalaryIsActive")]
    public bool? SalaryIsActive { get; set; }
}

