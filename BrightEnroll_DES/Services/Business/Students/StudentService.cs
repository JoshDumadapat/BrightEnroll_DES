using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace BrightEnroll_DES.Services.Business.Students;

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
    // This method performs comprehensive duplicate checking and error handling
    // Includes automatic sequence table synchronization to prevent primary key conflicts
    public async Task<Student> RegisterStudentAsync(StudentRegistrationData studentData)
    {
        return await RegisterStudentInternalAsync(studentData, isRetry: false);
    }

    // Internal method that handles the actual registration logic
    // isRetry flag prevents infinite recursion when retrying after sequence sync
    private async Task<Student> RegisterStudentInternalAsync(StudentRegistrationData studentData, bool isRetry)
    {
        // STEP 1: Perform local duplicate checks BEFORE starting transaction
        // This prevents false positives from cloud database sync and provides early validation
        // Validation errors don't need transaction rollback, so we check before transaction starts
        // Checks are performed on LOCAL database only to avoid conflicts with cloud data
        _logger?.LogInformation("Starting student registration for: {FirstName} {LastName} (Retry: {IsRetry})", 
            studentData.FirstName, studentData.LastName, isRetry);
        
        try
        {
            await CheckForDuplicateStudentAsync(studentData);
            _logger?.LogInformation("Local duplicate check passed for: {FirstName} {LastName}", studentData.FirstName, studentData.LastName);
        }
        catch (Exception duplicateEx)
        {
            // Local duplicate check failed - this is a VALIDATION ERROR, not a database error
            // No transaction to rollback since we haven't started one yet
            _logger?.LogWarning("Local duplicate check failed: {Message}", duplicateEx.Message);
            throw new Exception($"DUPLICATE_DETECTED: {duplicateEx.Message}", duplicateEx);
        }

        // STEP 2: Ensure sequence table is synchronized with existing student IDs
        // This prevents primary key conflicts when the sequence table is out of sync with cloud database
        // The sequence table must always be >= the highest existing student ID to avoid conflicts
        // Only sync on first attempt, not on retry (retry means we already tried to sync)
        if (!isRetry)
        {
            try
            {
                await SyncStudentIDSequenceAsync();
                _logger?.LogInformation("Student ID sequence table synchronized");
            }
            catch (Exception syncEx)
            {
                // Sequence sync failed - log but don't fail registration yet
                // The stored procedure will handle the conflict if it occurs
                _logger?.LogWarning(syncEx, "Failed to sync student ID sequence table: {Message}. Registration will continue, but may fail if sequence is out of sync.", syncEx.Message);
            }
        }

        // STEP 3: Begin database transaction for atomicity
        // All operations (guardian creation, student creation, requirements) must succeed or all rollback
        // Transaction starts AFTER validation passes to avoid rollback conflicts
        var transaction = await _context.Database.BeginTransactionAsync();
        var transactionRolledBack = false; // Track rollback state to prevent multiple rollbacks
        try
        {

            // STEP 4: Create guardian record first (required foreign key for student)
            // Guardian must be created before student due to foreign key constraint
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
            
            // STEP 5: Call stored procedure to create student with auto-generated ID
            // The stored procedure handles ID generation atomically using sequence table
            // If primary key conflict occurs, it means sequence table is still out of sync
            // The error handler will attempt to sync and retry
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
                
                // Associate the command with the EF Core transaction to ensure atomicity
                command.Transaction = transaction.GetDbTransaction();

                // STEP 5: Add all input parameters to stored procedure
                // All parameters are properly null-handled to avoid SQL errors
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
                
                // Add output parameter to receive generated student ID
                var studentIdParam = new SqlParameter("@student_id", System.Data.SqlDbType.VarChar, 6)
                {
                    Direction = System.Data.ParameterDirection.Output
                };
                command.Parameters.Add(studentIdParam);

                // STEP 6: Execute stored procedure
                // This may throw SqlException for various reasons:
                // - Primary key conflicts (student_id already exists)
                // - Unique constraint violations (LRN, name+birthdate)
                // - Sequence table conflicts
                // - Missing parameters or invalid data
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
                // STEP 7: Handle SQL exceptions from stored procedure with detailed logging
                // This is a STORED PROCEDURE FAILURE (not a local duplicate check failure)
                
                // Check if this is a primary key conflict due to sequence table mismatch
                // If so, attempt to sync sequence table and retry once
                if (spEx.Number == 2627 && spEx.Message.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.LogWarning("Primary key conflict detected. Attempting to sync sequence table and retry...");
                    
                    // Rollback current transaction before syncing
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
                    
                    // Dispose current transaction
                    try
                    {
                        transaction?.Dispose();
                    }
                    catch { }
                    
                    // Attempt to sync sequence table and retry
                    // Only retry once to prevent infinite loops
                    if (!isRetry)
                    {
                        try
                        {
                            await SyncStudentIDSequenceAsync();
                            _logger?.LogInformation("Sequence table synced successfully. Retrying student registration...");
                            
                            // Retry registration with a new transaction (set isRetry flag to prevent infinite recursion)
                            return await RegisterStudentInternalAsync(studentData, isRetry: true);
                        }
                        catch (Exception syncEx)
                        {
                            _logger?.LogError(syncEx, "Failed to sync sequence table during retry: {Message}", syncEx.Message);
                            // Fall through to normal error handling
                        }
                    }
                    else
                    {
                        _logger?.LogError("Primary key conflict occurred even after sequence sync. This may indicate a deeper synchronization issue.");
                    }
                }
                
                // Rollback transaction only once and mark as rolled back
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
                
                // Log comprehensive SQL error details
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
                
                // Handle specific SQL error codes with clear messages
                var errorMessage = HandleSqlException(spEx, studentData);
                throw new Exception($"STORED_PROCEDURE_ERROR: {errorMessage}", spEx);
            }
            finally
            {
                // Ensure connection is closed if we opened it
                if (!wasOpen)
                {
                    await connection.CloseAsync();
                }
            }

            // STEP 8: Load the created student from database to verify insertion
            // Verify that the student was actually inserted into the database
            var student = await _context.Students
                .Include(s => s.Guardian)
                .FirstOrDefaultAsync(s => s.StudentId == generatedStudentId);

            if (student == null)
            {
                // Student not found after insertion - rollback transaction
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
            
            // STEP 9: Create default requirements based on student type
            // Requirements are created after student is successfully inserted
            var requirements = GetDefaultRequirements(student.StudentId, student.StudentType);
            
            if (requirements.Any())
            {
                _context.StudentRequirements.AddRange(requirements);
                await _context.SaveChangesAsync();
                _logger?.LogInformation("Created {Count} requirements for student {StudentId}", requirements.Count, student.StudentId);
            }

            // STEP 10: Commit transaction - all operations succeeded
            // Only commit if transaction hasn't been rolled back (shouldn't happen in success path)
            // Only commit if transaction hasn't been rolled back
            if (!transactionRolledBack)
            {
                await transaction.CommitAsync();
                _logger?.LogInformation("Student registration completed successfully. Student ID: {StudentId}", generatedStudentId);
            }
            else
            {
                _logger?.LogWarning("Transaction was already rolled back, skipping commit");
            }
            
            // Load related data for return
            await _context.Entry(student).Reference(s => s.Guardian).LoadAsync();
            await _context.Entry(student).Collection(s => s.Requirements).LoadAsync();

            return student;
        }
        catch (Exception ex)
        {
            // STEP 11: Handle all other exceptions (non-SQL exceptions)
            // This includes other validation errors or unexpected errors
            // Note: SqlException from stored procedure is already handled and rolled back in inner catch
            // Note: Duplicate check errors are thrown before transaction starts, so no rollback needed
            
            // Only rollback if this is NOT a SqlException and transaction hasn't been rolled back
            // Duplicate errors are thrown before transaction, so they won't reach here
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
            
            // Check if this is a duplicate error from local check (shouldn't reach here, but handle just in case)
            if (ex.Message.StartsWith("DUPLICATE_DETECTED:", StringComparison.OrdinalIgnoreCase))
            {
                // Re-throw duplicate errors as-is (they already have clear messages)
                throw;
            }
            
            // Check if this is a stored procedure error (already logged with full details)
            if (ex.Message.StartsWith("STORED_PROCEDURE_ERROR:", StringComparison.OrdinalIgnoreCase))
            {
                // Re-throw stored procedure errors as-is (they already have detailed messages)
                throw;
            }
            
            // Log other exceptions
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

    // Handles SQL exceptions from stored procedure with specific error code handling
    // Returns user-friendly error messages based on SQL error number
    private string HandleSqlException(SqlException sqlEx, StudentRegistrationData studentData)
    {
        // SQL Error Number Reference:
        // https://docs.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors
        
        switch (sqlEx.Number)
        {
            case 2627: // Unique constraint violation (primary key or unique index)
                // Check if this is a primary key violation (student ID conflict)
                if (sqlEx.Message.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase) ||
                    sqlEx.Message.Contains("PK__tbl_Stud", StringComparison.OrdinalIgnoreCase))
                {
                    return $"Primary key conflict: The generated student ID already exists in the database. " +
                           $"This indicates the sequence table (tbl_StudentID_Sequence) is out of sync with existing student IDs. " +
                           $"The system will attempt to synchronize the sequence table and retry. " +
                           $"If this error persists, please contact your administrator to manually sync the sequence table. " +
                           $"SQL Error: {sqlEx.Message}";
                }
                return $"Unique constraint violation. " +
                       $"A student with this ID or unique identifier already exists in the database. " +
                       $"This may indicate a sequence table conflict or duplicate data. " +
                       $"SQL Error: {sqlEx.Message}";
            
            case 2601: // Cannot insert duplicate key row (unique index violation)
                return $"Unique constraint violation. " +
                       $"A student with duplicate data (possibly LRN '{studentData.LearnerReferenceNo}' or Name+Birthdate combination) already exists. " +
                       $"SQL Error: {sqlEx.Message}";
            
            case 515: // Cannot insert NULL value (NOT NULL constraint)
                return $"Required field is missing (NULL value not allowed). " +
                       $"Please ensure all required student fields are provided. " +
                       $"SQL Error: {sqlEx.Message}";
            
            case 547: // Foreign key constraint violation
                return $"Foreign key constraint violation. " +
                       $"A referenced record (e.g., guardian) does not exist or is invalid. " +
                       $"SQL Error: {sqlEx.Message}";
            
            case 2812: // Object (stored procedure) not found
                return $"The stored procedure 'sp_CreateStudent' was not found in the database. " +
                       $"Please ensure the database is properly initialized and the stored procedure exists. " +
                       $"SQL Error: {sqlEx.Message}";
            
            case 208: // Invalid object name (table/view not found)
                return $"A required database table is missing. " +
                       $"The table 'tbl_Students' or 'tbl_StudentID_Sequence' may not exist. " +
                       $"Please ensure the database is properly initialized. " +
                       $"SQL Error: {sqlEx.Message}";
            
            case 50000: // User-defined error (RAISERROR)
                // Check if it's a sequence table limit error
                if (sqlEx.Message.Contains("Student ID limit reached", StringComparison.OrdinalIgnoreCase))
                {
                    return $"Student ID generation limit reached (999999). " +
                           $"Cannot generate more student IDs. " +
                           $"Please contact system administrator. " +
                           $"SQL Error: {sqlEx.Message}";
                }
                // Check if it's a duplicate student error from stored procedure
                if (sqlEx.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                    sqlEx.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                {
                    return $"Duplicate student detected by stored procedure. " +
                           $"A student with the same information already exists in the database. " +
                           $"This check was performed by the stored procedure (not local check). " +
                           $"SQL Error: {sqlEx.Message}";
                }
                return $"Database constraint or business rule violation. " +
                       $"SQL Error: {sqlEx.Message}";
            
            default:
                // Generic SQL error - log all details
                var errorDetails = $"SQL Error Number: {sqlEx.Number}, " +
                                 $"Severity: {sqlEx.Class}, " +
                                 $"State: {sqlEx.State}, " +
                                 $"Procedure: {sqlEx.Procedure ?? "N/A"}, " +
                                 $"Line: {sqlEx.LineNumber}, " +
                                 $"Message: {sqlEx.Message}";
                
                // Check for common error patterns in message
                if (sqlEx.Message.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase))
                {
                    return $"Primary key conflict. The generated student ID already exists. " +
                           $"This may indicate a sequence table synchronization issue. " +
                           $"{errorDetails}";
                }
                
                if (sqlEx.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
                {
                    return $"Unique constraint violation. A duplicate record exists. " +
                           $"This may be due to LRN or name+birthdate uniqueness. " +
                           $"{errorDetails}";
                }
                
                if (sqlEx.Message.Contains("sequence", StringComparison.OrdinalIgnoreCase))
                {
                    return $"Sequence table error. There may be a conflict in the student ID sequence. " +
                           $"Please verify the tbl_StudentID_Sequence table. " +
                           $"{errorDetails}";
                }
                
                return $"Database error occurred during student registration. " +
                       $"{errorDetails}";
        }
    }

    // Checks for duplicate student in LOCAL database only
    // This prevents false positives when cloud database has students but local is empty
    // IMPORTANT: This method filters out placeholder LRN values ('N/A', 'Pending', null, empty)
    // to avoid false duplicate detection for students without real LRN values
    private async Task CheckForDuplicateStudentAsync(StudentRegistrationData studentData)
    {
        // Check by LRN if provided AND it's a valid (non-placeholder) value
        // Placeholder values like 'N/A' or 'Pending' should not be checked for duplicates
        // because multiple students can legitimately have these placeholder values
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
            // Log that LRN duplicate check was skipped due to placeholder value
            _logger?.LogInformation("LRN duplicate check skipped for placeholder value: '{LRN}'", 
                studentData.LearnerReferenceNo ?? "null");
        }

        // Check by name + birthdate combination (common duplicate detection)
        // Only check if we have the required fields
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

    // Synchronizes the student ID sequence table with the highest existing student ID
    // This prevents primary key conflicts when the sequence table is out of sync
    // CRITICAL: This must be called before generating new student IDs to avoid conflicts
    // The sequence table's LastStudentID must always be >= the highest existing student ID
    private async Task SyncStudentIDSequenceAsync()
    {
        try
        {
            // Get the highest existing student ID from the database
            // Student IDs are stored as VARCHAR(6) but represent numeric values
            // We need to convert them to INT for comparison, handling non-numeric values
            var highestStudentId = await _context.Students
                .Where(s => s.StudentId != null && s.StudentId.Length == 6)
                .Select(s => s.StudentId)
                .ToListAsync();
            
            int maxId = 158021; // Default starting value if no students exist
            
            foreach (var studentId in highestStudentId)
            {
                // Try to parse the student ID as an integer
                // Student IDs are formatted as 6-digit strings (e.g., "158022")
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
                    
                    -- Get current sequence value
                    SELECT @CurrentSequence = [LastStudentID]
                    FROM [dbo].[tbl_StudentID_Sequence] WITH (UPDLOCK);
                    
                    -- Update sequence if it's lower than the highest student ID
                    -- This ensures the sequence is always ahead of existing IDs
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

    // Helper method to determine if an LRN value is valid for duplicate checking
    // Returns true only if LRN is a real value (not a placeholder)
    // Placeholder values like 'N/A', 'Pending', null, or empty should not be checked
    // because multiple students can legitimately have these placeholder values
    private bool IsValidLRNForDuplicateCheck(string? lrn)
    {
        // Return false if LRN is null, empty, or whitespace
        if (string.IsNullOrWhiteSpace(lrn))
        {
            return false;
        }
        
        // Normalize the LRN value for comparison (trim and convert to uppercase)
        var normalized = lrn.Trim().ToUpperInvariant();
        
        // Return false for known placeholder values
        // These are not real LRN values and should not be checked for duplicates
        return normalized != "N/A" && 
               normalized != "PENDING" &&
               normalized != "NA" &&
               normalized != "NULL";
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

