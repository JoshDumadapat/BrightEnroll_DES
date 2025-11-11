using System.ComponentModel.DataAnnotations;

namespace BrightEnroll_DES.Models;

public class StudentRegistrationModel
{
    // Personal Information
    [Required(ErrorMessage = "First name is required")]
    public string FirstName { get; set; } = "";
    public string MiddleName { get; set; } = "";
    [Required(ErrorMessage = "Last name is required")]
    public string LastName { get; set; } = "";
    public string Suffix { get; set; } = "";
    [Required(ErrorMessage = "Birth date is required")]
    public DateTime? BirthDate { get; set; }
    [Required(ErrorMessage = "Age is required")]
    [Range(1, 100, ErrorMessage = "Age must be between 1 and 100")]
    public int? Age { get; set; }
    [Required(ErrorMessage = "Place of birth is required")]
    public string PlaceOfBirth { get; set; } = "";
    [Required(ErrorMessage = "Sex is required")]
    public string Sex { get; set; } = "";
    [Required(ErrorMessage = "Mother tongue is required")]
    public string MotherTongue { get; set; } = "";
    public string MotherTongueOther { get; set; } = "";
    public string IsIPCommunity { get; set; } = "";
    public string IPCommunitySpecify { get; set; } = "";
    public string IPCommunitySpecifyOther { get; set; } = "";
    public string Is4PsBeneficiary { get; set; } = "";
    public string FourPsHouseholdId { get; set; } = "";
    
    // Current Address
    public string CurrentHouseNo { get; set; } = "";
    public string CurrentStreetName { get; set; } = "";
    [Required(ErrorMessage = "Barangay is required")]
    public string CurrentBarangay { get; set; } = "";
    [Required(ErrorMessage = "City is required")]
    public string CurrentCity { get; set; } = "";
    [Required(ErrorMessage = "Province is required")]
    public string CurrentProvince { get; set; } = "";
    [Required(ErrorMessage = "Country is required")]
    public string CurrentCountry { get; set; } = "";
    [Required(ErrorMessage = "ZIP code is required")]
    public string CurrentZipCode { get; set; } = "";
    
    // Permanent Address
    public bool SameAsCurrentAddress { get; set; } = false;
    public string PermanentHouseNo { get; set; } = "";
    public string PermanentStreetName { get; set; } = "";
    public string PermanentBarangay { get; set; } = "";
    public string PermanentCity { get; set; } = "";
    public string PermanentProvince { get; set; } = "";
    public string PermanentCountry { get; set; } = "";
    public string PermanentZipCode { get; set; } = "";
    
    // Guardian Information
    [Required(ErrorMessage = "Guardian first name is required")]
    public string GuardianFirstName { get; set; } = "";
    public string GuardianMiddleName { get; set; } = "";
    [Required(ErrorMessage = "Guardian last name is required")]
    public string GuardianLastName { get; set; } = "";
    public string GuardianSuffix { get; set; } = "";
    [Required(ErrorMessage = "Contact number is required")]
    [RegularExpression(@"^\d{1,11}$", ErrorMessage = "Contact number must contain only numbers and be maximum 11 digits")]
    [MaxLength(11, ErrorMessage = "Contact number must be maximum 11 digits")]
    public string GuardianContactNumber { get; set; } = "";
    [Required(ErrorMessage = "Relationship is required")]
    public string GuardianRelationship { get; set; } = "";
    public string GuardianRelationshipOther { get; set; } = "";
    
    // Enrollment Details
    [Required(ErrorMessage = "Student type is required")]
    public string StudentType { get; set; } = "";
    public string HasLRN { get; set; } = "";
    [Required(ErrorMessage = "Learner reference number is required")]
    public string LearnerReferenceNo { get; set; } = "";
    [Required(ErrorMessage = "School year is required")]
    public string SchoolYear { get; set; } = "";
    [Required(ErrorMessage = "Grade to enroll is required")]
    public string GradeToEnroll { get; set; } = "";
    
    // Requirements - New Student
    public bool HasPSABirthCert { get; set; } = false;
    public bool HasBaptismalCert { get; set; } = false;
    public bool HasReportCard { get; set; } = false;
    
    // Requirements - Transferee
    public bool HasForm138 { get; set; } = false;
    public bool HasForm137 { get; set; } = false;
    public bool HasGoodMoralCert { get; set; } = false;
    public bool HasTransferCert { get; set; } = false;
    
    // Requirements - Returnee
    public bool HasUpdatedEnrollmentForm { get; set; } = false;
    public bool HasClearance { get; set; } = false;
    
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms")]
    public bool AgreeToTerms { get; set; } = false;
}

