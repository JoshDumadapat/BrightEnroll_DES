using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Services.Authentication;
using BrightEnroll_DES.Services.Business.Audit;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace BrightEnroll_DES.Services.Business.Students;

public class StudentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<StudentService>? _logger;
    private readonly AuditLogService? _auditLogService;
    private readonly IAuthService? _authService;

    public StudentService(
        AppDbContext context, 
        ILogger<StudentService>? logger = null,
        AuditLogService? auditLogService = null,
        IAuthService? authService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
        _auditLogService = auditLogService;
        _authService = authService;
    }

    public async Task<Student> RegisterStudentAsync(StudentRegistrationData studentData)
    {
        return await RegisterStudentInternalAsync(studentData, isRetry: false);
    }

    private async Task<Student> RegisterStudentInternalAsync(StudentRegistrationData studentData, bool isRetry)
    {
        _logger?.LogInformation("Starting student registration for: {FirstName} {LastName} (Retry: {IsRetry})", 
            studentData.FirstName, studentData.LastName, isRetry);
        
        try
        {
            await CheckForDuplicateStudentAsync(studentData);
            _logger?.LogInformation("Local duplicate check passed for: {FirstName} {LastName}", studentData.FirstName, studentData.LastName);
        }
        catch (Exception duplicateEx)
        {
            _logger?.LogWarning("Local duplicate check failed: {Message}", duplicateEx.Message);
            throw new Exception($"DUPLICATE_DETECTED: {duplicateEx.Message}", duplicateEx);
        }

        if (!isRetry)
        {
            try
            {
                await SyncStudentIDSequenceAsync();
                _logger?.LogInformation("Student ID sequence table synchronized");
            }
            catch (Exception syncEx)
            {
                _logger?.LogWarning(syncEx, "Failed to sync student ID sequence table: {Message}. Registration will continue, but may fail if sequence is out of sync.", syncEx.Message);
            }
        }

        var transaction = await _context.Database.BeginTransactionAsync();
        var transactionRolledBack = false;
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
            _logger?.LogInformation("Guardian created successfully with ID: {GuardianId}", guardian.GuardianId);
            
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
                command.Transaction = transaction.GetDbTransaction();

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
                
                var studentIdParam = new SqlParameter("@student_id", System.Data.SqlDbType.VarChar, 6)
                {
                    Direction = System.Data.ParameterDirection.Output
                };
                command.Parameters.Add(studentIdParam);

                _logger?.LogInformation("Executing stored procedure sp_CreateStudent for: {FirstName} {LastName}", studentData.FirstName, studentData.LastName);
                await command.ExecuteNonQueryAsync();
                generatedStudentId = studentIdParam.Value?.ToString() ?? string.Empty;
                
                if (string.IsNullOrEmpty(generatedStudentId))
                {
                    throw new Exception("Stored procedure executed but did not return a student ID. The output parameter @student_id is empty.");
                }
                
                _logger?.LogInformation("Stored procedure executed successfully. Generated Student ID: {StudentId}", generatedStudentId);
            }
            catch (SqlException spEx)
            {
                if (spEx.Number == 2627 && spEx.Message.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.LogWarning("Primary key conflict detected. Attempting to sync sequence table and retry...");
                    
                    if (!transactionRolledBack)
                    {
                        try
                        {
                            await transaction.RollbackAsync();
                            transactionRolledBack = true;
                            _logger?.LogInformation("Transaction rolled back due to primary key conflict");
                        }
                        catch (Exception rollbackEx)
                        {
                            _logger?.LogError(rollbackEx, "Error rolling back transaction: {Message}", rollbackEx.Message);
                        }
                    }
                    
                    try
                    {
                        transaction?.Dispose();
                    }
                    catch { }
                    
                    if (!isRetry)
                    {
                        try
                        {
                            await SyncStudentIDSequenceAsync();
                            _logger?.LogInformation("Sequence table synced successfully. Retrying student registration...");
                            return await RegisterStudentInternalAsync(studentData, isRetry: true);
                        }
                        catch (Exception syncEx)
                        {
                            _logger?.LogError(syncEx, "Failed to sync sequence table during retry: {Message}", syncEx.Message);
                        }
                    }
                    else
                    {
                        _logger?.LogError("Primary key conflict occurred even after sequence sync. This may indicate a deeper synchronization issue.");
                    }
                }
                
                if (!transactionRolledBack && transaction != null)
                {
                    try
                    {
                        await transaction.RollbackAsync();
                        transactionRolledBack = true;
                        _logger?.LogInformation("Transaction rolled back due to stored procedure error");
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger?.LogError(rollbackEx, "Error rolling back transaction after stored procedure failure: {Message}", rollbackEx.Message);
                    }
                }
                
                _logger?.LogError(spEx, 
                    "STORED PROCEDURE FAILURE - SQL Error registering student. " +
                    "Error Number: {Number}, " +
                    "Severity: {Class}, " +
                    "State: {State}, " +
                    "Procedure: {Procedure}, " +
                    "Line Number: {LineNumber}, " +
                    "Message: {Message}, " +
                    "Server: {Server}, " +
                    "Database: {Database}",
                    spEx.Number,
                    spEx.Class,
                    spEx.State,
                    spEx.Procedure ?? "N/A",
                    spEx.LineNumber,
                    spEx.Message,
                    spEx.Server ?? "N/A",
                    connection.Database ?? "N/A");
                
                var errorMessage = HandleSqlException(spEx, studentData);
                throw new Exception($"STORED_PROCEDURE_ERROR: {errorMessage}", spEx);
            }
            finally
            {
                if (!wasOpen)
                {
                    await connection.CloseAsync();
                }
            }

            var student = await _context.Students
                .Include(s => s.Guardian)
                .FirstOrDefaultAsync(s => s.StudentId == generatedStudentId);

            if (student == null)
            {
                if (!transactionRolledBack)
                {
                    try
                    {
                        await transaction.RollbackAsync();
                        transactionRolledBack = true;
                        _logger?.LogWarning("Transaction rolled back - student not found after insertion");
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger?.LogError(rollbackEx, "Error rolling back transaction: {Message}", rollbackEx.Message);
                    }
                }
                throw new Exception($"Failed to retrieve created student with ID: {generatedStudentId}. The stored procedure may have failed silently.");
            }
            
            var requirements = GetDefaultRequirements(student.StudentId, student.StudentType, studentData);
            
            if (requirements.Any())
            {
                try
                {
                    _context.StudentRequirements.AddRange(requirements);
                    await _context.SaveChangesAsync();
                    _logger?.LogInformation("Created {Count} requirements for student {StudentId}", requirements.Count, student.StudentId);
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
                {
                    // Extract the innermost SQL exception for detailed error information
                    var innerEx = dbEx.InnerException;
                    var sqlEx = innerEx as Microsoft.Data.SqlClient.SqlException;
                    
                    // Build detailed error message
                    var errorMsg = $"Failed to save student requirements: {dbEx.Message}";
                    if (sqlEx != null)
                    {
                        errorMsg += $"\nSQL Error Number: {sqlEx.Number}";
                        errorMsg += $"\nSQL Error Message: {sqlEx.Message}";
                        errorMsg += $"\nSQL Error Severity: {sqlEx.Class}";
                        errorMsg += $"\nSQL Error State: {sqlEx.State}";
                        if (sqlEx.LineNumber > 0)
                            errorMsg += $"\nSQL Error Line: {sqlEx.LineNumber}";
                    }
                    else if (innerEx != null)
                    {
                        errorMsg += $"\nInner Exception: {innerEx.GetType().Name} - {innerEx.Message}";
                    }
                    
                    _logger?.LogError(dbEx, "Error saving student requirements: {ErrorMessage}", errorMsg);
                    throw new Exception(errorMsg, dbEx);
                }
            }

            if (!transactionRolledBack)
            {
                await transaction.CommitAsync();
                _logger?.LogInformation("Student registration completed successfully. Student ID: {StudentId}", generatedStudentId);
            }
            else
            {
                _logger?.LogWarning("Transaction was already rolled back, skipping commit");
            }
            
            await _context.Entry(student).Reference(s => s.Guardian).LoadAsync();
            await _context.Entry(student).Collection(s => s.Requirements).LoadAsync();

            // Create enhanced audit log entry
            if (_auditLogService != null)
            {
                try
                {
                    var studentName = $"{student.FirstName} {student.MiddleName} {student.LastName}".Replace("  ", " ").Trim();
                    var registrarId = _authService?.CurrentUser?.user_ID;
                    var registrarName = _authService?.CurrentUser != null 
                        ? $"{_authService.CurrentUser.first_name} {_authService.CurrentUser.last_name}".Trim()
                        : null;

                    await _auditLogService.CreateStudentRegistrationLogAsync(
                        studentId: student.StudentId,
                        studentName: studentName,
                        grade: student.GradeLevel,
                        studentStatus: student.Status,
                        registrarId: registrarId,
                        registrarName: registrarName,
                        ipAddress: null // Can be passed from component if needed
                    );
                }
                catch (Exception auditEx)
                {
                    _logger?.LogWarning(auditEx, "Failed to create audit log entry for student registration. Registration was successful.");
                    // Don't throw - audit logging failure shouldn't break registration
                }
            }

            return student;
        }
        catch (Exception ex)
        {
            if (!(ex is SqlException) && !transactionRolledBack)
            {
                try
                {
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync();
                        transactionRolledBack = true;
                        _logger?.LogInformation("Transaction rolled back due to: {ExceptionType}", ex.GetType().Name);
                    }
                }
                catch (Exception rollbackEx)
                {
                    _logger?.LogError(rollbackEx, "Error rolling back transaction: {Message}", rollbackEx.Message);
                }
            }
            
            if (ex.Message.StartsWith("DUPLICATE_DETECTED:", StringComparison.OrdinalIgnoreCase))
            {
                throw;
            }
            
            if (ex.Message.StartsWith("STORED_PROCEDURE_ERROR:", StringComparison.OrdinalIgnoreCase))
            {
                throw;
            }
            
            _logger?.LogError(ex, 
                "GENERAL ERROR registering student: {Message}. " +
                "Type: {ExceptionType}, " +
                "Stack Trace: {StackTrace}",
                ex.Message,
                ex.GetType().Name,
                ex.StackTrace);
            
            throw new Exception($"Failed to register student: {ex.Message}", ex);
        }
        finally
        {
            // Dispose transaction in all cases
            try
            {
                transaction?.Dispose();
            }
            catch (Exception disposeEx)
            {
                _logger?.LogError(disposeEx, "Error disposing transaction: {Message}", disposeEx.Message);
            }
        }
    }

    private string HandleSqlException(SqlException sqlEx, StudentRegistrationData studentData)
    {
        
        switch (sqlEx.Number)
        {
            case 2627:
                if (sqlEx.Message.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase) ||
                    sqlEx.Message.Contains("PK__tbl_Stud", StringComparison.OrdinalIgnoreCase))
                {
                    return $"Primary key conflict: The generated student ID already exists in the database. " +
                           $"This indicates the sequence table (tbl_StudentID_Sequence) is out of sync with existing student IDs. " +
                           $"The system will attempt to synchronize the sequence table and retry. " +
                           $"If this error persists, please contact your administrator to manually sync the sequence table. " +
                           $"SQL Error: {sqlEx.Message}";
                }
                return $"Unique constraint violation. A student with this ID or unique identifier already exists. SQL Error: {sqlEx.Message}";
            
            case 2601:
                return $"Unique constraint violation. A student with duplicate data (possibly LRN '{studentData.LearnerReferenceNo}' or Name+Birthdate combination) already exists. SQL Error: {sqlEx.Message}";
            
            case 515:
                return $"Required field is missing (NULL value not allowed). Please ensure all required student fields are provided. SQL Error: {sqlEx.Message}";
            
            case 547:
                return $"Foreign key constraint violation. A referenced record (e.g., guardian) does not exist or is invalid. SQL Error: {sqlEx.Message}";
            
            case 2812:
                return $"The stored procedure 'sp_CreateStudent' was not found in the database. Please ensure the database is properly initialized. SQL Error: {sqlEx.Message}";
            
            case 208:
                return $"A required database table is missing. The table 'tbl_Students' or 'tbl_StudentID_Sequence' may not exist. Please ensure the database is properly initialized. SQL Error: {sqlEx.Message}";
            
            case 50000:
                if (sqlEx.Message.Contains("Student ID limit reached", StringComparison.OrdinalIgnoreCase))
                {
                    return $"Student ID generation limit reached (999999). Cannot generate more student IDs. Please contact system administrator. SQL Error: {sqlEx.Message}";
                }
                if (sqlEx.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                    sqlEx.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                {
                    return $"Duplicate student detected by stored procedure. A student with the same information already exists in the database. SQL Error: {sqlEx.Message}";
                }
                return $"Database constraint or business rule violation. SQL Error: {sqlEx.Message}";
            
            default:
                var errorDetails = $"SQL Error Number: {sqlEx.Number}, Severity: {sqlEx.Class}, State: {sqlEx.State}, Procedure: {sqlEx.Procedure ?? "N/A"}, Line: {sqlEx.LineNumber}, Message: {sqlEx.Message}";
                
                if (sqlEx.Message.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase))
                {
                    return $"Primary key conflict. The generated student ID already exists. This may indicate a sequence table synchronization issue. {errorDetails}";
                }
                
                if (sqlEx.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
                {
                    return $"Unique constraint violation. A duplicate record exists. This may be due to LRN or name+birthdate uniqueness. {errorDetails}";
                }
                
                if (sqlEx.Message.Contains("sequence", StringComparison.OrdinalIgnoreCase))
                {
                    return $"Sequence table error. There may be a conflict in the student ID sequence. Please verify the tbl_StudentID_Sequence table. {errorDetails}";
                }
                
                return $"Database error occurred during student registration. {errorDetails}";
        }
    }

    private async Task CheckForDuplicateStudentAsync(StudentRegistrationData studentData)
    {
        if (IsValidLRNForDuplicateCheck(studentData.LearnerReferenceNo))
        {
            var existingByLRN = await _context.Students
                .FirstOrDefaultAsync(s => s.Lrn == studentData.LearnerReferenceNo);
            
            if (existingByLRN != null)
            {
                throw new Exception($"A student with LRN '{studentData.LearnerReferenceNo}' already exists in the local database.");
            }
        }
        else
        {
            _logger?.LogInformation("LRN duplicate check skipped for placeholder value: '{LRN}'", 
                studentData.LearnerReferenceNo ?? "null");
        }

        if (!string.IsNullOrWhiteSpace(studentData.FirstName) && 
            !string.IsNullOrWhiteSpace(studentData.LastName) && 
            studentData.BirthDate != default)
        {
            var existingByNameAndDOB = await _context.Students
                .Where(s => s.FirstName.ToLower() == studentData.FirstName.ToLower() &&
                           s.LastName.ToLower() == studentData.LastName.ToLower() &&
                           s.Birthdate == studentData.BirthDate.Date)
                .FirstOrDefaultAsync();
            
            if (existingByNameAndDOB != null)
            {
                throw new Exception($"A student with the same name ({studentData.FirstName} {studentData.LastName}) and birthdate ({studentData.BirthDate:yyyy-MM-dd}) already exists in the local database.");
            }
        }
    }

    private async Task SyncStudentIDSequenceAsync()
    {
        try
        {
            var highestStudentId = await _context.Students
                .Where(s => s.StudentId != null && s.StudentId.Length == 6)
                .Select(s => s.StudentId)
                .ToListAsync();
            
            int maxId = 158021;
            
            foreach (var studentId in highestStudentId)
            {
                if (int.TryParse(studentId, out int idValue))
                {
                    if (idValue > maxId)
                    {
                        maxId = idValue;
                    }
                }
            }
            
            // Get current sequence value
            var connection = _context.Database.GetDbConnection();
            var wasOpen = connection.State == System.Data.ConnectionState.Open;
            
            if (!wasOpen)
            {
                await connection.OpenAsync();
            }
            
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    DECLARE @CurrentSequence INT;
                    DECLARE @MaxStudentID INT = @maxId;
                    
                    SELECT @CurrentSequence = [LastStudentID]
                    FROM [dbo].[tbl_StudentID_Sequence] WITH (UPDLOCK);
                    
                    IF @CurrentSequence < @MaxStudentID
                    BEGIN
                        UPDATE [dbo].[tbl_StudentID_Sequence]
                        SET [LastStudentID] = @MaxStudentID;
                        
                        SELECT 'Updated' AS Result, @MaxStudentID AS NewSequence, @CurrentSequence AS OldSequence;
                    END
                    ELSE
                    BEGIN
                        SELECT 'NoUpdate' AS Result, @CurrentSequence AS CurrentSequence, @MaxStudentID AS MaxStudentID;
                    END";
                
                command.Parameters.Add(new SqlParameter("@maxId", maxId));
                
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var result = reader["Result"]?.ToString();
                    if (result == "Updated")
                    {
                        var newSeq = reader["NewSequence"];
                        var oldSeq = reader["OldSequence"];
                        _logger?.LogInformation(
                            "Student ID sequence table synchronized. Updated from {OldSequence} to {NewSequence} (highest existing student ID: {MaxStudentID})",
                            oldSeq, newSeq, maxId);
                    }
                    else
                    {
                        _logger?.LogInformation(
                            "Student ID sequence table is already synchronized. Current: {CurrentSequence}, Highest Student ID: {MaxStudentID}",
                            reader["CurrentSequence"], maxId);
                    }
                }
            }
            finally
            {
                if (!wasOpen)
                {
                    await connection.CloseAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error synchronizing student ID sequence table: {Message}", ex.Message);
            throw;
        }
    }

    private bool IsValidLRNForDuplicateCheck(string? lrn)
    {
        if (string.IsNullOrWhiteSpace(lrn))
        {
            return false;
        }
        
        var normalized = lrn.Trim().ToUpperInvariant();
        return normalized != "N/A" && 
               normalized != "PENDING" &&
               normalized != "NA" &&
               normalized != "NULL";
    }

    private List<StudentRequirement> GetDefaultRequirements(string studentId, string studentType, StudentRegistrationData? studentData = null)
    {
        var requirements = new List<StudentRequirement>();

        switch (studentType.ToLower())
        {
            case "new student":
                requirements.AddRange(new[]
                {
                    new StudentRequirement { StudentId = studentId, RequirementName = "PSA Birth Certificate", RequirementType = "new", Status = studentData?.HasPSABirthCert == true ? "verified" : "not submitted", IsVerified = studentData?.HasPSABirthCert ?? false },
                    new StudentRequirement { StudentId = studentId, RequirementName = "Baptismal Certificate", RequirementType = "new", Status = studentData?.HasBaptismalCert == true ? "verified" : "not submitted", IsVerified = studentData?.HasBaptismalCert ?? false },
                    new StudentRequirement { StudentId = studentId, RequirementName = "Report Card", RequirementType = "new", Status = studentData?.HasReportCard == true ? "verified" : "not submitted", IsVerified = studentData?.HasReportCard ?? false }
                });
                break;

            case "transferee":
                requirements.AddRange(new[]
                {
                    new StudentRequirement { StudentId = studentId, RequirementName = "PSA Birth Certificate", RequirementType = "transferee", Status = studentData?.HasPSABirthCert == true ? "verified" : "not submitted", IsVerified = studentData?.HasPSABirthCert ?? false },
                    new StudentRequirement { StudentId = studentId, RequirementName = "Baptismal Certificate", RequirementType = "transferee", Status = studentData?.HasBaptismalCert == true ? "verified" : "not submitted", IsVerified = studentData?.HasBaptismalCert ?? false },
                    new StudentRequirement { StudentId = studentId, RequirementName = "Form 138 (Report Card)", RequirementType = "transferee", Status = studentData?.HasForm138 == true ? "verified" : "not submitted", IsVerified = studentData?.HasForm138 ?? false },
                    new StudentRequirement { StudentId = studentId, RequirementName = "Form 137 (Permanent Record)", RequirementType = "transferee", Status = studentData?.HasForm137 == true ? "verified" : "not submitted", IsVerified = studentData?.HasForm137 ?? false },
                    new StudentRequirement { StudentId = studentId, RequirementName = "Good Moral Certificate", RequirementType = "transferee", Status = studentData?.HasGoodMoralCert == true ? "verified" : "not submitted", IsVerified = studentData?.HasGoodMoralCert ?? false },
                    new StudentRequirement { StudentId = studentId, RequirementName = "Transfer Certificate", RequirementType = "transferee", Status = studentData?.HasTransferCert == true ? "verified" : "not submitted", IsVerified = studentData?.HasTransferCert ?? false }
                });
                break;

            case "returnee":
                requirements.AddRange(new[]
                {
                    new StudentRequirement { StudentId = studentId, RequirementName = "Form 138 (Report Card)", RequirementType = "returnee", Status = studentData?.HasForm138 == true ? "verified" : "not submitted", IsVerified = studentData?.HasForm138 ?? false },
                    new StudentRequirement { StudentId = studentId, RequirementName = "Updated Enrollment Form", RequirementType = "returnee", Status = studentData?.HasUpdatedEnrollmentForm == true ? "verified" : "not submitted", IsVerified = studentData?.HasUpdatedEnrollmentForm ?? false },
                    new StudentRequirement { StudentId = studentId, RequirementName = "Clearance", RequirementType = "returnee", Status = studentData?.HasClearance == true ? "verified" : "not submitted", IsVerified = studentData?.HasClearance ?? false }
                });
                break;

            default:
                _logger?.LogWarning("Unknown student type: {StudentType}. No requirements created.", studentType);
                break;
        }

        return requirements;
    }

    public async Task<Student?> GetStudentByIdAsync(string studentId)
    {
        return await _context.Students
            .Include(s => s.Guardian)
            .Include(s => s.Requirements)
            .Include(s => s.SectionEnrollments)
                .ThenInclude(e => e.Section)
                    .ThenInclude(sec => sec.GradeLevel)
            .FirstOrDefaultAsync(s => s.StudentId == studentId);
    }

    public async Task<List<Student>> GetAllStudentsAsync()
    {
        try
        {
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

    public async Task<List<EnrolledStudentDto>> GetEnrolledStudentsAsync(string? schoolYear = null)
    {
        try
        {
            var enrolledStatus = "Enrolled";
            
            // Get active school year if not provided
            if (string.IsNullOrEmpty(schoolYear))
            {
                var activeSY = await _context.SchoolYears
                    .Where(sy => sy.IsActive && sy.IsOpen)
                    .Select(sy => sy.SchoolYearName)
                    .FirstOrDefaultAsync();
                schoolYear = activeSY;
            }
            
            // Base query from section enrollments so we can get section + grade information
            var query = _context.StudentSectionEnrollments
                .Include(e => e.Student)
                    .ThenInclude(s => s.Requirements)
                .Include(e => e.Section)
                    .ThenInclude(sec => sec.GradeLevel)
                .Where(e => e.Status == enrolledStatus);

            // Filter by school year if provided
            if (!string.IsNullOrEmpty(schoolYear))
            {
                query = query.Where(e => e.SchoolYear == schoolYear);
            }

            var enrollments = await query
                .OrderByDescending(e => e.Student.DateRegistered)
                .ToListAsync();

            var result = new List<EnrolledStudentDto>();

            foreach (var enrollment in enrollments)
            {
                var s = enrollment.Student;

                var fullName = $"{s.FirstName} {s.MiddleName} {s.LastName}"
                    .Replace("  ", " ")
                    .Trim();

                var docsVerified = DocumentsVerifiedHelper.CalculateDocumentsVerified(
                    s.Requirements, s.StudentType);

                // Use grade level from enrollment's section (at enrollment time), NOT from student's main record
                // This ensures historical accuracy - grade level at enrollment time is preserved
                var gradeLevelAtEnrollment = enrollment.Section?.GradeLevel?.GradeLevelName ?? "N/A";
                
                result.Add(new EnrolledStudentDto
                {
                    Id = s.StudentId,
                    Name = string.IsNullOrWhiteSpace(fullName) ? "N/A" : fullName,
                    LRN = string.IsNullOrWhiteSpace(s.Lrn) ? "N/A" : s.Lrn!,
                    Date = s.DateRegistered.ToString("dd MMM yyyy"),
                    GradeLevel = gradeLevelAtEnrollment, // Use enrollment's grade level, not student's current grade
                    Section = enrollment.Section?.SectionName ?? "N/A",
                    Documents = docsVerified ? "Verified" : "Pending",
                    Status = enrollment.Status ?? s.Status ?? enrolledStatus
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching enrolled students: {Message}", ex.Message);
            throw new Exception($"Failed to fetch enrolled students: {ex.Message}", ex);
        }
    }

    public async Task<List<NewApplicantDto>> GetNewApplicantsAsync()
    {
        try
        {
            var pendingStatus = "Pending";
            
            // Get active school year
            var activeSchoolYear = await _context.SchoolYears
                .Where(sy => sy.IsActive && sy.IsOpen)
                .Select(sy => sy.SchoolYearName)
                .FirstOrDefaultAsync();
            
            // Filter by active school year - only show students registered for the current school year
            // Students with "Pending" status who don't have an enrollment record yet for the active school year
            var query = _context.Students
                .Include(s => s.Requirements)
                .Where(s => s.Status == pendingStatus);
            
            // Only include students registered for the active school year
            if (!string.IsNullOrEmpty(activeSchoolYear))
            {
                query = query.Where(s => s.SchoolYr == activeSchoolYear);
            }
            
            var students = await query
                .OrderByDescending(s => s.DateRegistered)
                .ToListAsync();

            var newApplicants = students.Select(s => new NewApplicantDto
            {
                Id = s.StudentId,
                Name = $"{s.FirstName} {s.MiddleName} {s.LastName}".Replace("  ", " ").Trim(),
                LRN = s.Lrn ?? "N/A",
                Date = s.DateRegistered.ToString("MMMM dd, yyyy"),
                Type = s.StudentType,
                GradeLevel = s.GradeLevel ?? "N/A",
                Status = s.Status,
                DocumentsVerified = DocumentsVerifiedHelper.CalculateDocumentsVerified(s.Requirements, s.StudentType)
            }).ToList();

            return newApplicants;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching new applicants: {Message}", ex.Message);
            throw new Exception($"Failed to fetch new applicants: {ex.Message}", ex);
        }
    }

    public async Task UpdateStudentAsync(Components.Pages.Admin.Enrollment.EnrollmentCS.StudentEditModel model)
    {
        try
        {
            var student = await _context.Students
                .Include(s => s.Guardian)
                .Include(s => s.Requirements)
                .Include(s => s.SectionEnrollments)
                .FirstOrDefaultAsync(s => s.StudentId == model.StudentId);

            if (student == null)
            {
                _logger?.LogWarning("Student {StudentId} not found for update", model.StudentId);
                throw new Exception($"Student {model.StudentId} not found");
            }

            // Update student basic information
            student.FirstName = model.FirstName;
            // middle_name is NOT NULL in database, so use empty string instead of null
            student.MiddleName = string.IsNullOrWhiteSpace(model.MiddleName) ? string.Empty : model.MiddleName;
            student.LastName = model.LastName;
            student.Suffix = string.IsNullOrWhiteSpace(model.Suffix) ? null : model.Suffix;
            student.Birthdate = model.BirthDate;
            student.Age = model.Age;
            student.PlaceOfBirth = string.IsNullOrWhiteSpace(model.PlaceOfBirth) ? null : model.PlaceOfBirth;
            student.Sex = model.Sex;
            student.MotherTongue = string.IsNullOrWhiteSpace(model.MotherTongue) ? null : model.MotherTongue;
            student.IpComm = model.IsIPCommunity == "Yes";
            student.IpSpecify = string.IsNullOrWhiteSpace(model.IPCommunitySpecify) ? null : model.IPCommunitySpecify;
            student.FourPs = model.Is4PsBeneficiary == "Yes";
            student.FourPsHseId = string.IsNullOrWhiteSpace(model.FourPsHouseholdId) ? null : model.FourPsHouseholdId;

            // Update address
            student.HseNo = string.IsNullOrWhiteSpace(model.CurrentHouseNo) ? null : model.CurrentHouseNo;
            student.Street = string.IsNullOrWhiteSpace(model.CurrentStreetName) ? null : model.CurrentStreetName;
            student.Brngy = string.IsNullOrWhiteSpace(model.CurrentBarangay) ? null : model.CurrentBarangay;
            student.City = string.IsNullOrWhiteSpace(model.CurrentCity) ? null : model.CurrentCity;
            student.Province = string.IsNullOrWhiteSpace(model.CurrentProvince) ? null : model.CurrentProvince;
            student.Country = string.IsNullOrWhiteSpace(model.CurrentCountry) ? null : model.CurrentCountry;
            student.ZipCode = string.IsNullOrWhiteSpace(model.CurrentZipCode) ? null : model.CurrentZipCode;

            // Update permanent address
            student.PhseNo = string.IsNullOrWhiteSpace(model.PermanentHouseNo) ? null : model.PermanentHouseNo;
            student.Pstreet = string.IsNullOrWhiteSpace(model.PermanentStreetName) ? null : model.PermanentStreetName;
            student.Pbrngy = string.IsNullOrWhiteSpace(model.PermanentBarangay) ? null : model.PermanentBarangay;
            student.Pcity = string.IsNullOrWhiteSpace(model.PermanentCity) ? null : model.PermanentCity;
            student.Pprovince = string.IsNullOrWhiteSpace(model.PermanentProvince) ? null : model.PermanentProvince;
            student.Pcountry = string.IsNullOrWhiteSpace(model.PermanentCountry) ? null : model.PermanentCountry;
            student.PzipCode = string.IsNullOrWhiteSpace(model.PermanentZipCode) ? null : model.PermanentZipCode;

            // Update enrollment details
            student.StudentType = model.StudentType;
            student.Lrn = model.HasLRN == "Yes" && !string.IsNullOrWhiteSpace(model.LearnerReferenceNo) && model.LearnerReferenceNo != "N/A"
                ? model.LearnerReferenceNo
                : null;
            
            // Only update student.SchoolYr and student.GradeLevel if this is a new student (no existing enrollments)
            // For re-enrollments or students with existing enrollments, these should remain as historical data
            var hasExistingEnrollments = student.SectionEnrollments != null && student.SectionEnrollments.Any();
            
            if (!hasExistingEnrollments)
            {
                // New student - safe to set SchoolYr and GradeLevel
                student.SchoolYr = string.IsNullOrWhiteSpace(model.SchoolYear) ? null : model.SchoolYear;
                student.GradeLevel = string.IsNullOrWhiteSpace(model.GradeToEnroll) ? null : model.GradeToEnroll;
            }
            // For existing students, do NOT update SchoolYr or GradeLevel - they should remain as historical data

            // Update section enrollment (assignment to a section for a specific school year)
            if (model.SectionId.HasValue && !string.IsNullOrWhiteSpace(model.SchoolYear))
            {
                // Check if the school year is closed - prevent updates to closed school years
                var schoolYearEntity = await _context.SchoolYears
                    .FirstOrDefaultAsync(sy => sy.SchoolYearName == model.SchoolYear);
                
                if (schoolYearEntity != null && !schoolYearEntity.IsOpen)
                {
                    throw new Exception($"Cannot update enrollment for closed school year {model.SchoolYear}. " +
                                        $"This school year was closed on {schoolYearEntity.ClosedAt:MMM dd, yyyy}. " +
                                        $"Please use the active school year for new enrollments.");
                }

                var targetSection = await _context.Sections.FirstOrDefaultAsync(s => s.SectionId == model.SectionId.Value);
                if (targetSection != null)
                {
                    // Check capacity for the target section in the given school year
                    var currentEnrolledCount = await _context.StudentSectionEnrollments
                        .CountAsync(e => e.SectionId == model.SectionId.Value
                                         && e.SchoolYear == model.SchoolYear
                                         && e.Status == "Enrolled");

                    if (currentEnrolledCount >= targetSection.Capacity)
                    {
                        throw new Exception($"Section '{targetSection.SectionName}' is already full for school year {model.SchoolYear}. " +
                                            $"Capacity: {targetSection.Capacity}, Enrolled: {currentEnrolledCount}.");
                    }

                    // Get existing enrollment for this student and school year (if any)
                    var existingEnrollment = await _context.StudentSectionEnrollments
                        .FirstOrDefaultAsync(e => e.StudentId == student.StudentId
                                                  && e.SchoolYear == model.SchoolYear);

                    if (existingEnrollment == null)
                    {
                        // Check if school year is closed before creating new enrollment
                        if (schoolYearEntity != null && !schoolYearEntity.IsOpen)
                        {
                            throw new Exception($"Cannot create enrollment for closed school year {model.SchoolYear}. " +
                                                $"This school year was closed on {schoolYearEntity.ClosedAt:MMM dd, yyyy}. " +
                                                $"Please use the active school year for new enrollments.");
                        }

                        var newEnrollment = new StudentSectionEnrollment
                        {
                            StudentId = student.StudentId,
                            SectionId = model.SectionId.Value,
                            SchoolYear = model.SchoolYear,
                            Status = "Enrolled",
                            CreatedAt = DateTime.Now
                        };
                        _context.StudentSectionEnrollments.Add(newEnrollment);
                    }
                    else
                    {
                        // Check if school year is closed before updating enrollment
                        if (schoolYearEntity != null && !schoolYearEntity.IsOpen)
                        {
                            throw new Exception($"Cannot update enrollment for closed school year {model.SchoolYear}. " +
                                                $"This school year was closed on {schoolYearEntity.ClosedAt:MMM dd, yyyy}. " +
                                                $"Enrollment records for closed school years are read-only.");
                        }

                        existingEnrollment.SectionId = model.SectionId.Value;
                        existingEnrollment.Status = "Enrolled";
                        existingEnrollment.UpdatedAt = DateTime.Now;
                    }
                }
            }
            
            // If GradeToEnroll is changed but SectionId is not provided, update the enrollment section to match the new grade level
            if (!model.SectionId.HasValue && !string.IsNullOrWhiteSpace(model.GradeToEnroll) && !string.IsNullOrWhiteSpace(model.SchoolYear))
            {
                // Get existing enrollment for this student and school year
                var existingEnrollment = await _context.StudentSectionEnrollments
                    .Include(e => e.Section)
                        .ThenInclude(s => s!.GradeLevel)
                    .FirstOrDefaultAsync(e => e.StudentId == student.StudentId
                                              && e.SchoolYear == model.SchoolYear);
                
                if (existingEnrollment != null)
                {
                    // Check if the current section's grade level matches the new grade level
                    var currentGradeLevel = existingEnrollment.Section?.GradeLevel?.GradeLevelName;
                    if (currentGradeLevel != model.GradeToEnroll)
                    {
                        // Find a section that matches the new grade level
                        var gradeLevel = await _context.GradeLevels
                            .FirstOrDefaultAsync(g => g.GradeLevelName == model.GradeToEnroll);
                        
                        if (gradeLevel != null)
                        {
                            // Find a section for this grade level (prefer one with available capacity)
                            var sectionsForGrade = await _context.Sections
                                .Where(s => s.GradeLevelId == gradeLevel.GradeLevelId)
                                .ToListAsync();
                            
                            if (sectionsForGrade.Any())
                            {
                                // Check if school year is closed before updating enrollment
                                var schoolYearEntity = await _context.SchoolYears
                                    .FirstOrDefaultAsync(sy => sy.SchoolYearName == model.SchoolYear);
                                
                                if (schoolYearEntity != null && !schoolYearEntity.IsOpen)
                                {
                                    throw new Exception($"Cannot update enrollment for closed school year {model.SchoolYear}. " +
                                                        $"This school year was closed on {schoolYearEntity.ClosedAt:MMM dd, yyyy}. " +
                                                        $"Enrollment records for closed school years are read-only.");
                                }
                                
                                // Find a section with available capacity
                                Section? targetSection = null;
                                foreach (var section in sectionsForGrade)
                                {
                                    var currentEnrolledCount = await _context.StudentSectionEnrollments
                                        .CountAsync(e => e.SectionId == section.SectionId
                                                         && e.SchoolYear == model.SchoolYear
                                                         && e.Status == "Enrolled");
                                    
                                    if (currentEnrolledCount < section.Capacity)
                                    {
                                        targetSection = section;
                                        break;
                                    }
                                }
                                
                                // If no section with capacity, use the first one (will show capacity warning elsewhere)
                                targetSection ??= sectionsForGrade.First();
                                
                                // Update the enrollment section
                                existingEnrollment.SectionId = targetSection.SectionId;
                                existingEnrollment.UpdatedAt = DateTime.Now;
                            }
                        }
                    }
                }
            }
            
            if (!string.IsNullOrWhiteSpace(model.Status) && model.Status.Length > 20)
            {
                student.Status = model.Status.Substring(0, 20);
                _logger?.LogWarning("Status truncated from '{OriginalStatus}' to '{TruncatedStatus}' for student {StudentId}. Please update database column size.", 
                    model.Status, student.Status, model.StudentId);
            }
            else
            {
                student.Status = model.Status;
            }

            // Update guardian if exists
            if (student.Guardian != null)
            {
                student.Guardian.FirstName = model.GuardianFirstName;
                student.Guardian.MiddleName = string.IsNullOrWhiteSpace(model.GuardianMiddleName) ? null : model.GuardianMiddleName;
                student.Guardian.LastName = model.GuardianLastName;
                student.Guardian.Suffix = string.IsNullOrWhiteSpace(model.GuardianSuffix) ? null : model.GuardianSuffix;
                student.Guardian.ContactNum = string.IsNullOrWhiteSpace(model.GuardianContactNumber) ? null : model.GuardianContactNumber;
                student.Guardian.Relationship = string.IsNullOrWhiteSpace(model.GuardianRelationship) ? null : model.GuardianRelationship;
            }

            // Ensure Requirements collection is initialized
            if (student.Requirements == null)
            {
                student.Requirements = new List<StudentRequirement>();
            }
            
            // Update or create ALL requirements - this ensures all requirements exist for proper mapping
            // UpdateRequirement will create missing requirements if they don't exist
            // Pass student ID and type to help create new requirements
            UpdateRequirement(student.Requirements, student.StudentId, "PSA Birth Certificate", model.HasPSABirthCert, student.StudentType);
            UpdateRequirement(student.Requirements, student.StudentId, "Baptismal Certificate", model.HasBaptismalCert, student.StudentType);
            UpdateRequirement(student.Requirements, student.StudentId, "Report Card", model.HasReportCard, student.StudentType);
            UpdateRequirement(student.Requirements, student.StudentId, "Form 138 (Report Card)", model.HasForm138, student.StudentType);
            UpdateRequirement(student.Requirements, student.StudentId, "Form 137 (Permanent Record)", model.HasForm137, student.StudentType);
            UpdateRequirement(student.Requirements, student.StudentId, "Good Moral Certificate", model.HasGoodMoralCert, student.StudentType);
            UpdateRequirement(student.Requirements, student.StudentId, "Transfer Certificate", model.HasTransferCert, student.StudentType);
            UpdateRequirement(student.Requirements, student.StudentId, "Updated Enrollment Form", model.HasUpdatedEnrollmentForm, student.StudentType);
            UpdateRequirement(student.Requirements, student.StudentId, "Clearance", model.HasClearance, student.StudentType);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                // Extract the innermost SQL exception for detailed error information
                var innerEx = dbEx.InnerException;
                var sqlEx = innerEx as Microsoft.Data.SqlClient.SqlException;
                
                if (sqlEx != null && sqlEx.Number == 207 && sqlEx.Message.Contains("is_verified", StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.LogWarning("is_verified column not found. Updating only Status field for requirements.");
                    
                    if (student.Requirements != null && student.Requirements.Any())
                    {
                        UpdateRequirementStatusOnly(student.Requirements, "PSA Birth Certificate", model.HasPSABirthCert);
                        UpdateRequirementStatusOnly(student.Requirements, "Baptismal Certificate", model.HasBaptismalCert);
                        UpdateRequirementStatusOnly(student.Requirements, "Report Card", model.HasReportCard);
                        UpdateRequirementStatusOnly(student.Requirements, "Form 138 (Report Card)", model.HasForm138);
                        UpdateRequirementStatusOnly(student.Requirements, "Form 137 (Permanent Record)", model.HasForm137);
                        UpdateRequirementStatusOnly(student.Requirements, "Good Moral Certificate", model.HasGoodMoralCert);
                        UpdateRequirementStatusOnly(student.Requirements, "Transfer Certificate", model.HasTransferCert);
                        UpdateRequirementStatusOnly(student.Requirements, "Updated Enrollment Form", model.HasUpdatedEnrollmentForm);
                        UpdateRequirementStatusOnly(student.Requirements, "Clearance", model.HasClearance);
                    }
                    
                    await _context.SaveChangesAsync();
                    _logger?.LogInformation("Student {StudentId} updated successfully (using Status field only)", model.StudentId);
                }
                else if (sqlEx != null && sqlEx.Number == 2628 && sqlEx.Message.Contains("status", StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.LogWarning("Status column truncation detected. Truncating status value to fit column size.");
                    
                    if (student.Status != null && student.Status.Length > 20)
                    {
                        var originalStatus = student.Status;
                        student.Status = student.Status.Substring(0, 20);
                        _logger?.LogWarning("Status truncated from '{OriginalStatus}' to '{TruncatedStatus}'", originalStatus, student.Status);
                        
                        await _context.SaveChangesAsync();
                        _logger?.LogInformation("Student {StudentId} updated successfully (status truncated to fit column)", model.StudentId);
                        
                        throw new Exception(
                            $"Status value was truncated due to database column size limit. " +
                            $"Please run the SQL script 'Database_Scripts/Update_Status_Column_Size.sql' to fix this. " +
                            $"Current status saved as: '{student.Status}' (truncated from '{originalStatus}')", dbEx);
                    }
                    else
                    {
                        throw;
                    }
                }
                else
                {
                    var errorMsg = $"Failed to update student: {dbEx.Message}";
                    if (sqlEx != null)
                    {
                        errorMsg += $"\nSQL Error Number: {sqlEx.Number}";
                        errorMsg += $"\nSQL Error Message: {sqlEx.Message}";
                        errorMsg += $"\nSQL Error Severity: {sqlEx.Class}";
                        errorMsg += $"\nSQL Error State: {sqlEx.State}";
                        if (sqlEx.LineNumber > 0)
                            errorMsg += $"\nSQL Error Line: {sqlEx.LineNumber}";
                    }
                    else if (innerEx != null)
                    {
                        errorMsg += $"\nInner Exception: {innerEx.GetType().Name} - {innerEx.Message}";
                    }
                    
                    _logger?.LogError(dbEx, "Error updating student requirements: {ErrorMessage}", errorMsg);
                    throw new Exception(errorMsg, dbEx);
                }
            }
            _logger?.LogInformation("Student {StudentId} updated successfully", model.StudentId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating student {StudentId}: {Message}", model.StudentId, ex.Message);
            throw;
        }
    }

    private void UpdateRequirement(ICollection<StudentRequirement> requirements, string studentId, string requirementName, bool isVerified, string? studentType = null)
    {
        var requirement = requirements.FirstOrDefault(r => r.RequirementName == requirementName);
        if (requirement != null)
        {
            // Since IsVerified is ignored in AppDbContext, only Status field is saved to database
            // Set IsVerified for code logic, but Status is what persists
            requirement.IsVerified = isVerified;
            requirement.Status = isVerified ? "verified" : "not verified";
            
            _logger?.LogDebug("Updated requirement {RequirementName}: IsVerified={IsVerified}, Status={Status}", 
                requirementName, isVerified, requirement.Status);
        }
        else
        {
            // Requirement doesn't exist - create it
            // Determine requirement type based on student type first, then fallback to requirement name
            string requirementType = "new"; // default
            
            if (!string.IsNullOrWhiteSpace(studentType))
            {
                // Use student type to determine requirement type
                var studentTypeLower = studentType.ToLower();
                if (studentTypeLower == "transferee")
                {
                    requirementType = "transferee";
                }
                else if (studentTypeLower == "returnee")
                {
                    requirementType = "returnee";
                }
                else if (studentTypeLower == "new student")
                {
                    requirementType = "new";
                }
            }
            
            // Fallback: Determine requirement type based on requirement name if student type not available
            if (requirementType == "new" && !string.IsNullOrWhiteSpace(requirementName))
            {
                if (requirementName.Contains("Form 138") || requirementName.Contains("Form 137") || 
                    requirementName.Contains("Good Moral") || requirementName.Contains("Transfer Certificate"))
                {
                    requirementType = "transferee";
                }
                else if (requirementName.Contains("Updated Enrollment") || requirementName.Contains("Clearance"))
                {
                    requirementType = "returnee";
                }
            }
            
            if (!string.IsNullOrEmpty(studentId))
            {
                var newRequirement = new StudentRequirement
                {
                    StudentId = studentId,
                    RequirementName = requirementName,
                    RequirementType = requirementType,
                    Status = isVerified ? "verified" : "not verified",
                    IsVerified = isVerified
                };
                
                requirements.Add(newRequirement);
                _context.StudentRequirements.Add(newRequirement);
                
                _logger?.LogInformation("Created new requirement {RequirementName} for student {StudentId} (type: {RequirementType}): IsVerified={IsVerified}, Status={Status}", 
                    requirementName, studentId, requirementType, isVerified, newRequirement.Status);
            }
            else
            {
                _logger?.LogWarning("Cannot create requirement {RequirementName} - student ID not provided", requirementName);
            }
        }
    }

    private void UpdateRequirementStatusOnly(ICollection<StudentRequirement> requirements, string requirementName, bool isVerified)
    {
        var requirement = requirements.FirstOrDefault(r => r.RequirementName == requirementName);
        if (requirement != null)
        {
            requirement.Status = isVerified ? "verified" : "not verified";
        }
    }
}

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

public class NewApplicantDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LRN { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool DocumentsVerified { get; set; } = false;
}

// Helper methods for calculating documents verified status
public static class DocumentsVerifiedHelper
{
    // Helper method to calculate if all required documents for a student type are verified
    public static bool CalculateDocumentsVerified(ICollection<StudentRequirement>? requirements, string? studentType)
    {
        if (requirements == null || !requirements.Any())
        {
            return false;
        }
        
        // Get the list of required document names for this student type
        var requiredNames = GetRequiredDocumentNamesForStudentType(studentType);
        
        if (!requiredNames.Any())
        {
            return false; // No requirements defined for this student type
        }
        
        // Check if all required documents are verified
        foreach (var requiredName in requiredNames)
        {
            var requirement = requirements.FirstOrDefault(r => r.RequirementName == requiredName);
            if (requirement == null)
            {
                // Required document doesn't exist - not verified
                return false;
            }
            
            // Check if this requirement is verified (using Status field since IsVerified is ignored)
            bool isVerified = !string.IsNullOrWhiteSpace(requirement.Status) && 
                             requirement.Status.Equals("verified", StringComparison.OrdinalIgnoreCase);
            
            if (!isVerified)
            {
                // At least one required document is not verified
                return false;
            }
        }
        
        // All required documents are verified
        return true;
    }
    
    // Helper method to get required document names for a student type
    private static HashSet<string> GetRequiredDocumentNamesForStudentType(string? studentType)
    {
        var requiredNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        if (string.IsNullOrWhiteSpace(studentType))
        {
            return requiredNames;
        }
        
        var studentTypeLower = studentType.ToLower();
        
        switch (studentTypeLower)
        {
            case "new student":
                requiredNames.Add("PSA Birth Certificate");
                requiredNames.Add("Baptismal Certificate");
                requiredNames.Add("Report Card");
                break;
                
            case "transferee":
                requiredNames.Add("PSA Birth Certificate");
                requiredNames.Add("Baptismal Certificate");
                requiredNames.Add("Form 138 (Report Card)");
                requiredNames.Add("Form 137 (Permanent Record)");
                requiredNames.Add("Good Moral Certificate");
                requiredNames.Add("Transfer Certificate");
                break;
                
            case "returnee":
                requiredNames.Add("Form 138 (Report Card)");
                requiredNames.Add("Updated Enrollment Form");
                requiredNames.Add("Clearance");
                break;
        }
        
        return requiredNames;
    }
}

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

