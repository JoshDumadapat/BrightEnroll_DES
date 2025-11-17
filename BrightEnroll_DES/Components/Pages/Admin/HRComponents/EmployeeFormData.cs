namespace BrightEnroll_DES.Components.Pages.Admin.HRComponents;

// Data model for employee form containing personal info, address, role, and salary details
public class EmployeeFormData
{
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;
    public string BirthDate { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Age { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string DefaultPassword { get; set; } = string.Empty;
    public string SystemId { get; set; } = string.Empty;
    public string HouseNo { get; set; } = string.Empty;
    public string StreetName { get; set; } = string.Empty;
    public string Barangay { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    
    // Emergency Contact fields
    public string EmergencyContactFirstName { get; set; } = string.Empty;
    public string EmergencyContactMiddleName { get; set; } = string.Empty;
    public string EmergencyContactLastName { get; set; } = string.Empty;
    public string EmergencyContactSuffix { get; set; } = string.Empty;
    public string EmergencyContactRelationship { get; set; } = string.Empty;
    public string EmergencyContactNumber { get; set; } = string.Empty;
    public string EmergencyContactAddress { get; set; } = string.Empty;
    
    // Salary calculation fields
    public decimal BaseSalary { get; set; } = 0;
    public decimal Allowance { get; set; } = 0;
    public decimal Bonus { get; set; } = 0;
    public decimal Deductions { get; set; } = 0;
    public decimal TotalSalary { get; set; } = 0;
    
    // Deduction breakdown fields (for transparency)
    public decimal SSS { get; set; } = 0;
    public decimal PhilHealth { get; set; } = 0;
    public decimal PagIbig { get; set; } = 0;
    public decimal WithholdingTax { get; set; } = 0;
}

