using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services;

/// <summary>
/// Service for student registration and management operations
/// </summary>
public class StudentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<StudentService>? _logger;

    public StudentService(AppDbContext context, ILogger<StudentService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    /// <summary>
    /// Registers a new student with guardian and requirements
    /// </summary>
    /// <param name="studentData">Student data from registration form</param>
    /// <returns>Created Student entity with generated IDs</returns>
    /// <exception cref="Exception">Thrown if registration fails</exception>
    public async Task<Student> RegisterStudentAsync(StudentRegistrationData studentData)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Step 1: Insert Guardian first
            var guardian = new Guardian
            {
                FirstName = studentData.GuardianFirstName,
                MiddleName = string.IsNullOrWhiteSpace(studentData.GuardianMiddleName) ? null : studentData.GuardianMiddleName,
                LastName = studentData.GuardianLastName,
                Suffix = string.IsNullOrWhiteSpace(studentData.GuardianSuffix) ? null : studentData.GuardianSuffix,
                ContactNum = studentData.GuardianContactNumber,
                Relationship = studentData.GuardianRelationship
            };

            _context.Guardians.Add(guardian);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Guardian created with ID: {GuardianId}", guardian.GuardianId);

            // Step 2: Insert Student linked to Guardian
            var student = new Student
            {
                FirstName = studentData.FirstName,
                MiddleName = studentData.MiddleName,
                LastName = studentData.LastName,
                Suffix = string.IsNullOrWhiteSpace(studentData.Suffix) ? null : studentData.Suffix,
                Birthdate = studentData.BirthDate,
                Age = studentData.Age ?? 0,
                PlaceOfBirth = string.IsNullOrWhiteSpace(studentData.PlaceOfBirth) ? null : studentData.PlaceOfBirth,
                Sex = studentData.Sex,
                MotherTongue = string.IsNullOrWhiteSpace(studentData.MotherTongue) ? null : studentData.MotherTongue,
                IpComm = studentData.IsIPCommunity == "Yes",
                IpSpecify = string.IsNullOrWhiteSpace(studentData.IPCommunitySpecify) ? null : studentData.IPCommunitySpecify,
                FourPs = studentData.Is4PsBeneficiary == "Yes",
                FourPsHseId = string.IsNullOrWhiteSpace(studentData.FourPsHouseholdId) ? null : studentData.FourPsHouseholdId,
                HseNo = string.IsNullOrWhiteSpace(studentData.CurrentHouseNo) ? null : studentData.CurrentHouseNo,
                Street = string.IsNullOrWhiteSpace(studentData.CurrentStreetName) ? null : studentData.CurrentStreetName,
                Brngy = string.IsNullOrWhiteSpace(studentData.CurrentBarangay) ? null : studentData.CurrentBarangay,
                Province = string.IsNullOrWhiteSpace(studentData.CurrentProvince) ? null : studentData.CurrentProvince,
                City = string.IsNullOrWhiteSpace(studentData.CurrentCity) ? null : studentData.CurrentCity,
                Country = string.IsNullOrWhiteSpace(studentData.CurrentCountry) ? null : studentData.CurrentCountry,
                ZipCode = string.IsNullOrWhiteSpace(studentData.CurrentZipCode) ? null : studentData.CurrentZipCode,
                PhseNo = string.IsNullOrWhiteSpace(studentData.PermanentHouseNo) ? null : studentData.PermanentHouseNo,
                Pstreet = string.IsNullOrWhiteSpace(studentData.PermanentStreetName) ? null : studentData.PermanentStreetName,
                Pbrngy = string.IsNullOrWhiteSpace(studentData.PermanentBarangay) ? null : studentData.PermanentBarangay,
                Pprovince = string.IsNullOrWhiteSpace(studentData.PermanentProvince) ? null : studentData.PermanentProvince,
                Pcity = string.IsNullOrWhiteSpace(studentData.PermanentCity) ? null : studentData.PermanentCity,
                Pcountry = string.IsNullOrWhiteSpace(studentData.PermanentCountry) ? null : studentData.PermanentCountry,
                PzipCode = string.IsNullOrWhiteSpace(studentData.PermanentZipCode) ? null : studentData.PermanentZipCode,
                StudentType = studentData.StudentType,
                Lrn = string.IsNullOrWhiteSpace(studentData.LearnerReferenceNo) || studentData.LearnerReferenceNo == "Pending" 
                    ? null 
                    : studentData.LearnerReferenceNo,
                SchoolYr = studentData.SchoolYear,
                GradeLevel = studentData.GradeToEnroll,
                GuardianId = guardian.GuardianId
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Student created with ID: {StudentId}", student.StudentId);

            // Step 3: Insert default requirements based on student_type
            var requirements = GetDefaultRequirements(student.StudentId, student.StudentType);
            
            if (requirements.Any())
            {
                _context.StudentRequirements.AddRange(requirements);
                await _context.SaveChangesAsync();
                _logger?.LogInformation("Created {Count} requirements for student {StudentId}", requirements.Count, student.StudentId);
            }

            // Commit transaction
            await transaction.CommitAsync();

            // Reload student with related data
            await _context.Entry(student).Reference(s => s.Guardian).LoadAsync();
            await _context.Entry(student).Collection(s => s.Requirements).LoadAsync();

            return student;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger?.LogError(ex, "Error registering student: {Message}", ex.Message);
            throw new Exception($"Failed to register student: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets default requirements based on student type
    /// </summary>
    private List<StudentRequirement> GetDefaultRequirements(int studentId, string studentType)
    {
        var requirements = new List<StudentRequirement>();

        switch (studentType.ToLower())
        {
            case "new student":
                requirements.AddRange(new[]
                {
                    new StudentRequirement { StudentId = studentId, RequirementName = "PSA Birth Certificate", RequirementType = "new", Status = "not submitted" },
                    new StudentRequirement { StudentId = studentId, RequirementName = "Baptismal Certificate", RequirementType = "new", Status = "not submitted" },
                    new StudentRequirement { StudentId = studentId, RequirementName = "Report Card", RequirementType = "new", Status = "not submitted" }
                });
                break;

            case "transferee":
                requirements.AddRange(new[]
                {
                    new StudentRequirement { StudentId = studentId, RequirementName = "Form 138 (Report Card)", RequirementType = "transferee", Status = "not submitted" },
                    new StudentRequirement { StudentId = studentId, RequirementName = "Form 137 (Permanent Record)", RequirementType = "transferee", Status = "not submitted" },
                    new StudentRequirement { StudentId = studentId, RequirementName = "Good Moral Certificate", RequirementType = "transferee", Status = "not submitted" },
                    new StudentRequirement { StudentId = studentId, RequirementName = "Transfer Certificate", RequirementType = "transferee", Status = "not submitted" }
                });
                break;

            case "returnee":
                requirements.AddRange(new[]
                {
                    new StudentRequirement { StudentId = studentId, RequirementName = "Updated Enrollment Form", RequirementType = "returnee", Status = "not submitted" },
                    new StudentRequirement { StudentId = studentId, RequirementName = "Clearance", RequirementType = "returnee", Status = "not submitted" }
                });
                break;

            default:
                _logger?.LogWarning("Unknown student type: {StudentType}. No requirements created.", studentType);
                break;
        }

        return requirements;
    }

    /// <summary>
    /// Gets a student by ID with related data
    /// </summary>
    public async Task<Student?> GetStudentByIdAsync(int studentId)
    {
        return await _context.Students
            .Include(s => s.Guardian)
            .Include(s => s.Requirements)
            .FirstOrDefaultAsync(s => s.StudentId == studentId);
    }

    /// <summary>
    /// Gets all students with related data
    /// </summary>
    public async Task<List<Student>> GetAllStudentsAsync()
    {
        return await _context.Students
            .Include(s => s.Guardian)
            .Include(s => s.Requirements)
            .ToListAsync();
    }
}

/// <summary>
/// Data transfer object for student registration
/// Maps from StudentRegistrationModel to Student entity
/// </summary>
public class StudentRegistrationData
{
    // Student Information
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public int? Age { get; set; }
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
    public string GuardianSuffix { get; set; } = string.Empty;
    public string GuardianContactNumber { get; set; } = string.Empty;
    public string GuardianRelationship { get; set; } = string.Empty;

    // Enrollment Details
    public string StudentType { get; set; } = string.Empty;
    public string LearnerReferenceNo { get; set; } = string.Empty;
    public string SchoolYear { get; set; } = string.Empty;
    public string GradeToEnroll { get; set; } = string.Empty;
}

