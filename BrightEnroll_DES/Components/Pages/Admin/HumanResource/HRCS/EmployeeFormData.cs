using System.ComponentModel.DataAnnotations;

namespace BrightEnroll_DES.Components.Pages.Admin.HumanResource.HRCS;

// Data model for employee form containing personal info, address, role, and salary details
public class EmployeeFormData
{
    [Required(ErrorMessage = "First name is required")]
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Last name is required")]
    public string LastName { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;
    [Required(ErrorMessage = "Birth date is required")]
    public DateTime? BirthDate { get; set; }
    [Required(ErrorMessage = "Sex is required")]
    public string Sex { get; set; } = string.Empty;
    [Required(ErrorMessage = "Age is required")]
    [Range(18, 65, ErrorMessage = "Age must be between 18 and 65 years old")]
    public int? Age { get; set; }
    [Required(ErrorMessage = "Contact number is required")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "Contact number must start with 09 and be 11 digits")]
    [MaxLength(11, ErrorMessage = "Contact number must be 11 digits")]
    public string ContactNumber { get; set; } = string.Empty;
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Role is required")]
    public string Role { get; set; } = string.Empty;
    public string RoleOther { get; set; } = string.Empty;
    public string DefaultPassword { get; set; } = string.Empty;
    public string SystemId { get; set; } = string.Empty;
    public string HouseNo { get; set; } = string.Empty;
    public string StreetName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Barangay is required")]
    public string Barangay { get; set; } = string.Empty;
    [Required(ErrorMessage = "City is required")]
    public string City { get; set; } = string.Empty;
    [Required(ErrorMessage = "Province is required")]
    public string Province { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    
    // Emergency Contact fields
    [Required(ErrorMessage = "Emergency contact first name is required")]
    public string EmergencyContactFirstName { get; set; } = string.Empty;
    public string EmergencyContactMiddleName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Emergency contact last name is required")]
    public string EmergencyContactLastName { get; set; } = string.Empty;
    public string EmergencyContactSuffix { get; set; } = string.Empty;
    [Required(ErrorMessage = "Emergency contact relationship is required")]
    public string EmergencyContactRelationship { get; set; } = string.Empty;
    public string EmergencyContactRelationshipOther { get; set; } = string.Empty;
    [Required(ErrorMessage = "Emergency contact number is required")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "Emergency contact number must start with 09 and be 11 digits")]
    [MaxLength(11, ErrorMessage = "Emergency contact number must be 11 digits")]
    public string EmergencyContactNumber { get; set; } = string.Empty;
    public string EmergencyContactAddress { get; set; } = string.Empty;
    
    // Salary fields
    [Required(ErrorMessage = "Monthly salary is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Monthly salary must be greater than or equal to 0")]
    public decimal BaseSalary { get; set; } = 0;
    [Range(0, double.MaxValue, ErrorMessage = "Allowance must be greater than or equal to 0")]
    public decimal Allowance { get; set; } = 0;
    public decimal TotalSalary { get; set; } = 0;
}

