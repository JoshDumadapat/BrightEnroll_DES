using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace BrightEnroll_DES.Services;

// Handles student registration - creates student, guardian, and requirements
public class StudentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<StudentService>? _logger;

    public StudentService(AppDbContext context, ILogger<StudentService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    // Creates a new student with guardian and requirements
    public async Task<Student> RegisterStudentAsync(StudentRegistrationData studentData)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
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
            
            // Call stored procedure to create student with auto-generated ID
            string generatedStudentId;
            var connection = _context.Database.GetDbConnection();
            var wasOpen = connection.State == System.Data.ConnectionState.Open;
            if (!wasOpen)
            {
                await connection.OpenAsync();
            }
            
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "[dbo].[sp_CreateStudent]";
                command.CommandType = System.Data.CommandType.StoredProcedure;
                
                // Associate the command with the EF Core transaction
                command.Transaction = transaction.GetDbTransaction();

                // Add all input parameters
                command.Parameters.Add(new SqlParameter("@first_name", studentData.FirstName));
                command.Parameters.Add(new SqlParameter("@middle_name", string.IsNullOrWhiteSpace(studentData.MiddleName) ? string.Empty : studentData.MiddleName));
                command.Parameters.Add(new SqlParameter("@last_name", studentData.LastName));
                command.Parameters.Add(new SqlParameter("@suffix", string.IsNullOrWhiteSpace(studentData.Suffix) ? (object)DBNull.Value : studentData.Suffix));
                command.Parameters.Add(new SqlParameter("@birthdate", studentData.BirthDate));
                command.Parameters.Add(new SqlParameter("@age", studentData.Age ?? 0));
                command.Parameters.Add(new SqlParameter("@place_of_birth", string.IsNullOrWhiteSpace(studentData.PlaceOfBirth) ? (object)DBNull.Value : studentData.PlaceOfBirth));
                command.Parameters.Add(new SqlParameter("@sex", studentData.Sex));
                command.Parameters.Add(new SqlParameter("@mother_tongue", string.IsNullOrWhiteSpace(studentData.MotherTongue) ? (object)DBNull.Value : studentData.MotherTongue));
                command.Parameters.Add(new SqlParameter("@ip_comm", studentData.IsIPCommunity == "Yes" ? 1 : 0));
                command.Parameters.Add(new SqlParameter("@ip_specify", string.IsNullOrWhiteSpace(studentData.IPCommunitySpecify) ? (object)DBNull.Value : studentData.IPCommunitySpecify));
                command.Parameters.Add(new SqlParameter("@four_ps", studentData.Is4PsBeneficiary == "Yes" ? 1 : 0));
                command.Parameters.Add(new SqlParameter("@four_ps_hseID", string.IsNullOrWhiteSpace(studentData.FourPsHouseholdId) ? (object)DBNull.Value : studentData.FourPsHouseholdId));
                command.Parameters.Add(new SqlParameter("@hse_no", string.IsNullOrWhiteSpace(studentData.CurrentHouseNo) ? (object)DBNull.Value : studentData.CurrentHouseNo));
                command.Parameters.Add(new SqlParameter("@street", string.IsNullOrWhiteSpace(studentData.CurrentStreetName) ? (object)DBNull.Value : studentData.CurrentStreetName));
                command.Parameters.Add(new SqlParameter("@brngy", string.IsNullOrWhiteSpace(studentData.CurrentBarangay) ? (object)DBNull.Value : studentData.CurrentBarangay));
                command.Parameters.Add(new SqlParameter("@province", string.IsNullOrWhiteSpace(studentData.CurrentProvince) ? (object)DBNull.Value : studentData.CurrentProvince));
                command.Parameters.Add(new SqlParameter("@city", string.IsNullOrWhiteSpace(studentData.CurrentCity) ? (object)DBNull.Value : studentData.CurrentCity));
                command.Parameters.Add(new SqlParameter("@country", string.IsNullOrWhiteSpace(studentData.CurrentCountry) ? (object)DBNull.Value : studentData.CurrentCountry));
                command.Parameters.Add(new SqlParameter("@zip_code", string.IsNullOrWhiteSpace(studentData.CurrentZipCode) ? (object)DBNull.Value : studentData.CurrentZipCode));
                command.Parameters.Add(new SqlParameter("@phse_no", string.IsNullOrWhiteSpace(studentData.PermanentHouseNo) ? (object)DBNull.Value : studentData.PermanentHouseNo));
                command.Parameters.Add(new SqlParameter("@pstreet", string.IsNullOrWhiteSpace(studentData.PermanentStreetName) ? (object)DBNull.Value : studentData.PermanentStreetName));
                command.Parameters.Add(new SqlParameter("@pbrngy", string.IsNullOrWhiteSpace(studentData.PermanentBarangay) ? (object)DBNull.Value : studentData.PermanentBarangay));
                command.Parameters.Add(new SqlParameter("@pprovince", string.IsNullOrWhiteSpace(studentData.PermanentProvince) ? (object)DBNull.Value : studentData.PermanentProvince));
                command.Parameters.Add(new SqlParameter("@pcity", string.IsNullOrWhiteSpace(studentData.PermanentCity) ? (object)DBNull.Value : studentData.PermanentCity));
                command.Parameters.Add(new SqlParameter("@pcountry", string.IsNullOrWhiteSpace(studentData.PermanentCountry) ? (object)DBNull.Value : studentData.PermanentCountry));
                command.Parameters.Add(new SqlParameter("@pzip_code", string.IsNullOrWhiteSpace(studentData.PermanentZipCode) ? (object)DBNull.Value : studentData.PermanentZipCode));
                command.Parameters.Add(new SqlParameter("@student_type", studentData.StudentType));
                command.Parameters.Add(new SqlParameter("@LRN", string.IsNullOrWhiteSpace(studentData.LearnerReferenceNo) || studentData.LearnerReferenceNo == "Pending" ? (object)DBNull.Value : studentData.LearnerReferenceNo));
                command.Parameters.Add(new SqlParameter("@school_yr", studentData.SchoolYear));
                command.Parameters.Add(new SqlParameter("@grade_level", studentData.GradeToEnroll));
                command.Parameters.Add(new SqlParameter("@guardian_id", guardian.GuardianId));
                
                // Add output parameter
                var studentIdParam = new SqlParameter("@student_id", System.Data.SqlDbType.VarChar, 6)
                {
                    Direction = System.Data.ParameterDirection.Output
                };
                command.Parameters.Add(studentIdParam);

                await command.ExecuteNonQueryAsync();
                generatedStudentId = studentIdParam.Value?.ToString() ?? string.Empty;
            }
            finally
            {
                if (!wasOpen)
                {
                    await connection.CloseAsync();
                }
            }

            _logger?.LogInformation("Student created with ID: {StudentId}", generatedStudentId);

            // Load the created student from database
            var student = await _context.Students
                .Include(s => s.Guardian)
                .FirstOrDefaultAsync(s => s.StudentId == generatedStudentId);

            if (student == null)
            {
                throw new Exception($"Failed to retrieve created student with ID: {generatedStudentId}");
            }
            var requirements = GetDefaultRequirements(student.StudentId, student.StudentType);
            
            if (requirements.Any())
            {
                _context.StudentRequirements.AddRange(requirements);
                await _context.SaveChangesAsync();
                _logger?.LogInformation("Created {Count} requirements for student {StudentId}", requirements.Count, student.StudentId);
            }

            await transaction.CommitAsync();
            await _context.Entry(student).Reference(s => s.Guardian).LoadAsync();
            await _context.Entry(student).Collection(s => s.Requirements).LoadAsync();

            return student;
        }
        catch (SqlException sqlEx)
        {
            await transaction.RollbackAsync();
            _logger?.LogError(sqlEx, "SQL error registering student: {Message}. Error Number: {Number}", sqlEx.Message, sqlEx.Number);
            
            // Provide more specific error messages for common SQL errors
            if (sqlEx.Number == 2812) // Object not found (stored procedure)
            {
                throw new Exception($"The stored procedure 'sp_CreateStudent' was not found. Please ensure the database is properly initialized.", sqlEx);
            }
            else if (sqlEx.Number == 208) // Invalid object name (table)
            {
                throw new Exception($"A required database table is missing. Please ensure the database is properly initialized.", sqlEx);
            }
            
            throw new Exception($"Database error: {sqlEx.Message}", sqlEx);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger?.LogError(ex, "Error registering student: {Message}. Stack trace: {StackTrace}", ex.Message, ex.StackTrace);
            throw new Exception($"Failed to register student: {ex.Message}", ex);
        }
    }

    // Returns default requirements list based on student type
    private List<StudentRequirement> GetDefaultRequirements(string studentId, string studentType)
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

    // Gets student by ID with guardian and requirements
    public async Task<Student?> GetStudentByIdAsync(string studentId)
    {
        return await _context.Students
            .Include(s => s.Guardian)
            .Include(s => s.Requirements)
            .FirstOrDefaultAsync(s => s.StudentId == studentId);
    }

    // Gets all students with guardian and requirements using EF Core ORM
    // Uses EF Core's Include for eager loading - fully ORM approach
    public async Task<List<Student>> GetAllStudentsAsync()
    {
        try
        {
            // Use EF Core's Include for eager loading - fully ORM, secure approach
            // No raw SQL - all queries are generated by EF Core with parameterization
            var students = await _context.Students
                .Include(s => s.Guardian)
                .Include(s => s.Requirements)
                .OrderBy(s => s.StudentId)
                .ToListAsync();

            return students;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching all students: {Message}", ex.Message);
            throw new Exception($"Failed to fetch students: {ex.Message}", ex);
        }
    }

    // Gets enrolled students from view for enrollment table display using EF Core ORM
    // Returns only the data needed for the Enrolled tab
    // Uses EF Core's FromSqlRaw with parameterized WHERE clause for security
    public async Task<List<EnrolledStudentDto>> GetEnrolledStudentsAsync()
    {
        try
        {
            // Use EF Core's FromSqlRaw with parameterized query - secure ORM approach
            // Status is hardcoded in the query (not user input), so it's safe
            // EF Core still provides connection management and query validation
            var enrolledStatus = "Enrolled";
            
            var enrolledStudents = await _context.StudentDataViews
                .FromSqlRaw(@"
                    SELECT 
                        StudentId,
                        FirstName,
                        MiddleName,
                        LastName,
                        Suffix,
                        FullName,
                        BirthDate,
                        Age,
                        LRN,
                        GradeLevel,
                        SchoolYear,
                        DateRegistered,
                        Status,
                        StudentType
                    FROM [dbo].[vw_StudentData] 
                    WHERE Status = {0}", enrolledStatus)
                .Where(s => s.Status == enrolledStatus)
                .OrderByDescending(s => s.DateRegistered)
                .Select(s => new EnrolledStudentDto
                {
                    Id = s.StudentId ?? "N/A",
                    Name = s.FullName ?? "N/A",
                    LRN = s.LRN ?? "N/A",
                    Date = s.DateRegistered.HasValue 
                        ? s.DateRegistered.Value.ToString("dd MMM yyyy") 
                        : "N/A",
                    GradeLevel = s.GradeLevel ?? "N/A",
                    Section = "N/A", // Section not in database yet
                    Documents = "Verified", // Default value
                    Status = s.Status ?? "Pending"
                })
                .ToListAsync();

            return enrolledStudents;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching enrolled students: {Message}", ex.Message);
            throw new Exception($"Failed to fetch enrolled students: {ex.Message}", ex);
        }
    }

    // Gets pending students (new applicants) from view for enrollment table display using EF Core ORM
    // Returns only the data needed for the New Applicants tab
    // Uses EF Core's FromSqlRaw with parameterized WHERE clause for security
    public async Task<List<NewApplicantDto>> GetNewApplicantsAsync()
    {
        try
        {
            // Use EF Core's FromSqlRaw with parameterized query - secure ORM approach
            // Status is hardcoded in the query (not user input), so it's safe
            var pendingStatus = "Pending";
            
            var newApplicants = await _context.StudentDataViews
                .FromSqlRaw(@"
                    SELECT 
                        StudentId,
                        FirstName,
                        MiddleName,
                        LastName,
                        Suffix,
                        FullName,
                        BirthDate,
                        Age,
                        LRN,
                        GradeLevel,
                        SchoolYear,
                        DateRegistered,
                        Status,
                        StudentType
                    FROM [dbo].[vw_StudentData] 
                    WHERE Status = {0}", pendingStatus)
                .Where(s => s.Status == pendingStatus)
                .OrderByDescending(s => s.DateRegistered)
                .Select(s => new NewApplicantDto
                {
                    Id = s.StudentId ?? "N/A",
                    Name = s.FullName ?? "N/A",
                    LRN = s.LRN ?? "N/A",
                    Date = s.DateRegistered.HasValue 
                        ? s.DateRegistered.Value.ToString("MMMM dd, yyyy") 
                        : "N/A",
                    Type = s.StudentType ?? "New",
                    GradeLevel = s.GradeLevel ?? "N/A",
                    Status = s.Status ?? "Pending"
                })
                .ToListAsync();

            return newApplicants;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching new applicants: {Message}", ex.Message);
            throw new Exception($"Failed to fetch new applicants: {ex.Message}", ex);
        }
    }
}

// DTO for enrolled student display in enrollment table
public class EnrolledStudentDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LRN { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Documents { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

// DTO for new applicant display in enrollment table
public class NewApplicantDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LRN { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

// Holds student registration form data
public class StudentRegistrationData
{
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

