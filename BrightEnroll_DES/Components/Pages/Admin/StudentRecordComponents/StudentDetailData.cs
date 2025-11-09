namespace BrightEnroll_DES.Components.Pages.Admin.StudentRecordComponents;

public class StudentDetailData
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string SchoolYear { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string LRN { get; set; } = string.Empty;
    
    // Personal Information
    public DateTime? BirthDate { get; set; }
    public string PlaceOfBirth { get; set; } = string.Empty;
    public string Sex { get; set; } = string.Empty;
    public string MotherTongue { get; set; } = string.Empty;
    public string IsIPCommunity { get; set; } = string.Empty;
    public string IPCommunitySpecify { get; set; } = string.Empty;
    public string Is4PsBeneficiary { get; set; } = string.Empty;
    public string FourPsHouseholdId { get; set; } = string.Empty;
    
    // Current Address
    public string CurrentHouseNo { get; set; } = string.Empty;
    public string CurrentStreetName { get; set; } = string.Empty;
    public string CurrentBarangay { get; set; } = string.Empty;
    public string CurrentCity { get; set; } = string.Empty;
    public string CurrentProvince { get; set; } = string.Empty;
    public string CurrentCountry { get; set; } = string.Empty;
    public string CurrentZipCode { get; set; } = string.Empty;
    
    // Permanent Address
    public bool SameAsCurrentAddress { get; set; } = false;
    public string PermanentHouseNo { get; set; } = string.Empty;
    public string PermanentStreetName { get; set; } = string.Empty;
    public string PermanentBarangay { get; set; } = string.Empty;
    public string PermanentCity { get; set; } = string.Empty;
    public string PermanentProvince { get; set; } = string.Empty;
    public string PermanentCountry { get; set; } = string.Empty;
    public string PermanentZipCode { get; set; } = string.Empty;
    
    // Guardian Information
    public string GuardianFirstName { get; set; } = string.Empty;
    public string GuardianMiddleName { get; set; } = string.Empty;
    public string GuardianLastName { get; set; } = string.Empty;
    public string GuardianContactNumber { get; set; } = string.Empty;
    public string GuardianRelationship { get; set; } = string.Empty;
    public string GuardianRelationshipOther { get; set; } = string.Empty;
    
    // Enrollment Details
    public string StudentType { get; set; } = string.Empty;
    public string HasLRN { get; set; } = string.Empty;
    public string LearnerReferenceNo { get; set; } = string.Empty;
    public string GradeToEnroll { get; set; } = string.Empty;
    public bool HasBirthCertificate { get; set; } = false;
}

