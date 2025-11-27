namespace BrightEnroll_DES.Components.Pages.Admin.Enrollment.EnrollmentCS;

// Model for editing student information in enrollment
public class StudentEditModel
{
    public string StudentId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string MiddleName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Suffix { get; set; } = "";
    public DateTime BirthDate { get; set; }
    public int Age { get; set; }
    public string PlaceOfBirth { get; set; } = "";
    public string Sex { get; set; } = "";
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
    public string CurrentBarangay { get; set; } = "";
    public string CurrentCity { get; set; } = "";
    public string CurrentProvince { get; set; } = "";
    public string CurrentCountry { get; set; } = "";
    public string CurrentZipCode { get; set; } = "";
    public bool SameAsCurrentAddress { get; set; } = false;
    
    // Permanent Address
    public string PermanentHouseNo { get; set; } = "";
    public string PermanentStreetName { get; set; } = "";
    public string PermanentBarangay { get; set; } = "";
    public string PermanentCity { get; set; } = "";
    public string PermanentProvince { get; set; } = "";
    public string PermanentCountry { get; set; } = "";
    public string PermanentZipCode { get; set; } = "";
    
    // Guardian Information
    public string GuardianFirstName { get; set; } = "";
    public string GuardianMiddleName { get; set; } = "";
    public string GuardianLastName { get; set; } = "";
    public string GuardianSuffix { get; set; } = "";
    public string GuardianContactNumber { get; set; } = "";
    public string GuardianRelationship { get; set; } = "";
    public string GuardianRelationshipOther { get; set; } = "";
    
    // Enrollment Details
    public string StudentType { get; set; } = "";
    public string HasLRN { get; set; } = "";
    public string LearnerReferenceNo { get; set; } = "";
    public string SchoolYear { get; set; } = "";
    public string GradeToEnroll { get; set; } = "";
    public string Status { get; set; } = "";
    public string? RejectionReason { get; set; }
    
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
}

