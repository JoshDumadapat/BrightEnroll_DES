using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using BrightEnroll_DES.Services.Database.Initialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BrightEnroll_DES.Services.Business.Audit;
using BrightEnroll_DES.Services.Authentication;

namespace BrightEnroll_DES.Services.Business.Academic;

public class GradeService
{
    private readonly AppDbContext _context;
    private readonly ILogger<GradeService>? _logger;
    private readonly IConfiguration? _configuration;
    private readonly IServiceScopeFactory? _serviceScopeFactory;
    private static bool _tableChecked = false;
    private static readonly object _lockObject = new object();

    public GradeService(
        AppDbContext context, 
        ILogger<GradeService>? logger = null, 
        IConfiguration? configuration = null,
        IServiceScopeFactory? serviceScopeFactory = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
        _configuration = configuration;
        _serviceScopeFactory = serviceScopeFactory;
    }

    // Check if school year is open for editing
    private async Task<bool> IsSchoolYearOpenAsync(string schoolYear)
    {
        try
        {
            var schoolYearEntity = await _context.SchoolYears
                .FirstOrDefaultAsync(sy => sy.SchoolYearName == schoolYear);
            
            // If school year doesn't exist in database, allow editing (backward compatibility)
            if (schoolYearEntity == null)
            {
                return true;
            }
            
            // School year must be active and open
            return schoolYearEntity.IsActive && schoolYearEntity.IsOpen;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking if school year {SchoolYear} is open", schoolYear);
            // On error, allow editing (fail open for backward compatibility)
            return true;
        }
    }

    // Ensure grades table exists before querying
    private async Task EnsureGradesTableExistsAsync()
    {
        // Double-check locking pattern to avoid multiple initializations
        if (_tableChecked)
        {
            // Still verify table exists even if we checked before
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT TOP 1 grade_id FROM [dbo].[tbl_Grades]");
                return; // Table exists
            }
            catch (SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
            {
                // Table was checked before but doesn't exist now - reset flag and try again
                lock (_lockObject)
                {
                    _tableChecked = false;
                }
            }
        }

        lock (_lockObject)
        {
            if (_tableChecked)
                return;
            _tableChecked = true; 
        }

        try
        {
            // Quick check if table exists by trying a simple query
            await _context.Database.ExecuteSqlRawAsync("SELECT TOP 1 grade_id FROM [dbo].[tbl_Grades]");
            _logger?.LogInformation("tbl_Grades table exists.");
            return; // Table exists
        }
        catch (SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
        {
            // Table doesn't exist, need to create it
            _logger?.LogWarning("tbl_Grades table not found. Attempting to create it...");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking for tbl_Grades table: {Message}", ex.Message);
            lock (_lockObject)
            {
                _tableChecked = false;
            }
            return;
        }

        // Table doesn't exist, create it
        try
        {
            var connectionString = _context.Database.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString) && _configuration != null)
            {
                connectionString = _configuration.GetConnectionString("DefaultConnection");
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger?.LogError("Cannot create tbl_Grades table: Connection string is null or empty.");
                lock (_lockObject)
                {
                    _tableChecked = false;
                }
                return;
            }

            _logger?.LogInformation("Creating tbl_Grades table using DatabaseInitializer...");
            var initializer = new DatabaseInitializer(connectionString);
            var created = await initializer.EnsureGradesTableExistsAsync();
            
            // Always verify table exists after creation attempt
            bool tableExists = false;
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT TOP 1 grade_id FROM [dbo].[tbl_Grades]");
                tableExists = true;
                _logger?.LogInformation("Verified tbl_Grades table exists after creation attempt.");
            }
            catch (SqlException verifyEx) when (verifyEx.Number == 208)
            {
                tableExists = false;
                _logger?.LogWarning("tbl_Grades table still does not exist after DatabaseInitializer attempt. Trying direct SQL creation...");
                
                // Fallback: Try direct SQL creation
                try
                {
                    var createTableSql = @"
                        CREATE TABLE [dbo].[tbl_Grades](
                            [grade_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [student_id] VARCHAR(6) NOT NULL,
                            [subject_id] INT NOT NULL,
                            [section_id] INT NOT NULL,
                            [school_year] VARCHAR(20) NOT NULL,
                            [grading_period] VARCHAR(10) NOT NULL,
                            [written_work] DECIMAL(5,2) NULL,
                            [performance_tasks] DECIMAL(5,2) NULL,
                            [quarterly_assessment] DECIMAL(5,2) NULL,
                            [final_grade] DECIMAL(5,2) NULL,
                            [teacher_id] INT NOT NULL,
                            [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                            [updated_at] DATETIME NULL,
                            CONSTRAINT FK_tbl_Grades_tbl_Students FOREIGN KEY ([student_id]) REFERENCES [dbo].[tbl_Students]([student_id]) ON DELETE CASCADE,
                            CONSTRAINT FK_tbl_Grades_tbl_Subjects FOREIGN KEY ([subject_id]) REFERENCES [dbo].[tbl_Subjects]([SubjectID]) ON DELETE CASCADE,
                            CONSTRAINT FK_tbl_Grades_tbl_Sections FOREIGN KEY ([section_id]) REFERENCES [dbo].[tbl_Sections]([SectionID]) ON DELETE CASCADE,
                            CONSTRAINT FK_tbl_Grades_tbl_Users FOREIGN KEY ([teacher_id]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION
                        )";
                    
                    await _context.Database.ExecuteSqlRawAsync(createTableSql);
                    _logger?.LogInformation("Successfully created tbl_Grades table using direct SQL.");
                    
                    // Create indexes
                    var indexScripts = new[]
                    {
                        "CREATE INDEX IX_tbl_Grades_StudentId ON [dbo].[tbl_Grades]([student_id])",
                        "CREATE INDEX IX_tbl_Grades_SubjectId ON [dbo].[tbl_Grades]([subject_id])",
                        "CREATE INDEX IX_tbl_Grades_SectionId ON [dbo].[tbl_Grades]([section_id])",
                        "CREATE INDEX IX_tbl_Grades_TeacherId ON [dbo].[tbl_Grades]([teacher_id])",
                        "CREATE INDEX IX_tbl_Grades_Student_Subject_Section_Period ON [dbo].[tbl_Grades]([student_id], [subject_id], [section_id], [grading_period], [school_year])",
                        "CREATE UNIQUE INDEX UX_tbl_Grades_Student_Subject_Section_Period_Year ON [dbo].[tbl_Grades]([student_id], [subject_id], [section_id], [grading_period], [school_year])"
                    };
                    
                    foreach (var indexScript in indexScripts)
                    {
                        try
                        {
                            await _context.Database.ExecuteSqlRawAsync(indexScript);
                        }
                        catch (Exception idxEx)
                        {
                            _logger?.LogWarning($"Could not create index: {idxEx.Message}");
                        }
                    }
                    
                    // Verify again
                    await _context.Database.ExecuteSqlRawAsync("SELECT TOP 1 grade_id FROM [dbo].[tbl_Grades]");
                    tableExists = true;
                    _logger?.LogInformation("Verified tbl_Grades table exists after direct SQL creation.");
                }
                catch (Exception directSqlEx)
                {
                    _logger?.LogError(directSqlEx, "Failed to create tbl_Grades table using direct SQL: {Message}", directSqlEx.Message);
                    tableExists = false;
                }
            }
            
            if (!tableExists)
            {
                _logger?.LogError("ERROR: tbl_Grades table could not be created. All creation methods failed.");
                lock (_lockObject)
                {
                    _tableChecked = false;
                }
                throw new InvalidOperationException("Failed to create tbl_Grades table. Please check database permissions and ensure all required tables (tbl_Students, tbl_Subjects, tbl_Sections, tbl_Users) exist. Check the application logs for detailed error messages.");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating tbl_Grades table: {Message}", ex.Message);
            if (ex is InvalidOperationException)
            {
                throw; 
            }
            // Reset flag so we can try again next time
            lock (_lockObject)
            {
                _tableChecked = false;
            }
            throw new InvalidOperationException($"Failed to create tbl_Grades table: {ex.Message}", ex);
        }
    }

    // Get all students enrolled in section for school year
    public async Task<List<StudentGradeDto>> GetStudentsForGradeEntryAsync(int sectionId, string schoolYear)
    {
        try
        {
            var enrollments = await _context.StudentSectionEnrollments
                .Where(e => e.SectionId == sectionId && e.SchoolYear == schoolYear && e.Status == "Enrolled")
                .Include(e => e.Student)
                .OrderBy(e => e.Student!.LastName)
                .ThenBy(e => e.Student!.FirstName)
                .ToListAsync();

            return enrollments.Select(e => new StudentGradeDto
            {
                StudentId = e.StudentId,
                Name = $"{e.Student!.FirstName} {e.Student.LastName}".Trim(),
                SectionId = sectionId,
                SchoolYear = schoolYear
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting students for grade entry: {Message}", ex.Message);
            throw;
        }
    }
    public async Task<Dictionary<string, Grade>> GetExistingGradesAsync(int sectionId, int subjectId, string schoolYear, string gradingPeriod)
    {
        try
        {
            await EnsureGradesTableExistsAsync();

            var grades = await _context.Grades
                .Where(g => g.SectionId == sectionId 
                         && g.SubjectId == subjectId 
                         && g.SchoolYear == schoolYear 
                         && g.GradingPeriod == gradingPeriod)
                .ToListAsync();

            return grades.ToDictionary(g => g.StudentId, g => g);
        }
        catch (SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
        {
            _logger?.LogError(sqlEx, "tbl_Grades table still does not exist after creation attempt");
            // Return empty dictionary instead of throwing
            return new Dictionary<string, Grade>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting existing grades: {Message}", ex.Message);
            throw;
        }
    }

    // Save or update grades for multiple students
    public async Task<bool> SaveGradesAsync(List<GradeInputDto> gradeInputs, int teacherId)
    {
        try
        {
            // Validate teacher assignment for all grade inputs
            if (gradeInputs == null || !gradeInputs.Any())
            {
                _logger?.LogWarning("Attempted to save empty grade list");
                return false;
            }

            // Check if school year is open for editing
            var schoolYear = gradeInputs.First().SchoolYear;
            var isOpen = await IsSchoolYearOpenAsync(schoolYear);
            if (!isOpen)
            {
                _logger?.LogWarning("Attempted to save grades for closed school year {SchoolYear}", schoolYear);
                throw new InvalidOperationException($"Cannot save grades. The school year {schoolYear} is closed. Please contact an administrator to reopen it if you need to make changes.");
            }

            // Get unique section-subject combinations
            var sectionSubjectPairs = gradeInputs
                .Select(g => new { g.SectionId, g.SubjectId })
                .Distinct()
                .ToList();

            // Validate teacher is assigned to all section-subject combinations
            foreach (var pair in sectionSubjectPairs)
            {
                var isAssigned = await _context.TeacherSectionAssignments
                    .AnyAsync(a => a.TeacherId == teacherId
                                && a.SectionId == pair.SectionId
                                && a.SubjectId == pair.SubjectId
                                && !a.IsArchived);

                if (!isAssigned)
                {
                    _logger?.LogWarning("Teacher {TeacherId} attempted to save grades for unauthorized section {SectionId}, subject {SubjectId}", 
                        teacherId, pair.SectionId, pair.SubjectId);
                    throw new UnauthorizedAccessException($"You are not assigned to teach this subject in this section.");
                }
            }

            // Validate grade ranges and required components (DepEd requirement: all three components must be entered)
            foreach (var input in gradeInputs)
            {
                // Validate all three components are present (DepEd requirement)
                if (!input.WrittenWork.HasValue || !input.PerformanceTasks.HasValue || !input.QuarterlyAssessment.HasValue)
                {
                    throw new ArgumentException($"All grade components (Written Work, Performance Tasks, Quarterly Assessment) must be entered. Student: {input.StudentId}");
                }

                if (input.WrittenWork.HasValue && (input.WrittenWork < 0 || input.WrittenWork > 100))
                {
                    throw new ArgumentException($"Written Work grade must be between 0 and 100. Student: {input.StudentId}");
                }
                if (input.PerformanceTasks.HasValue && (input.PerformanceTasks < 0 || input.PerformanceTasks > 100))
                {
                    throw new ArgumentException($"Performance Tasks grade must be between 0 and 100. Student: {input.StudentId}");
                }
                if (input.QuarterlyAssessment.HasValue && (input.QuarterlyAssessment < 0 || input.QuarterlyAssessment > 100))
                {
                    throw new ArgumentException($"Quarterly Assessment grade must be between 0 and 100. Student: {input.StudentId}");
                }
                if (input.FinalGrade.HasValue && (input.FinalGrade < 0 || input.FinalGrade > 100))
                {
                    throw new ArgumentException($"Final grade must be between 0 and 100. Student: {input.StudentId}");
                }
            }

            // Ensure table exists before proceeding - this will throw if table cannot be created
            try
            {
                await EnsureGradesTableExistsAsync();
            }
            catch (InvalidOperationException ex)
            {
                _logger?.LogError(ex, "Cannot save grades: tbl_Grades table does not exist and could not be created.");
                throw new InvalidOperationException("Cannot save grades. The grades table is missing and could not be created automatically. Please contact your system administrator.", ex);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var input in gradeInputs)
                {
                    // Calculate quarterly grade using DepEd formula if not provided
                    decimal? computedFinalGrade = input.FinalGrade;
                    if (!computedFinalGrade.HasValue && input.WrittenWork.HasValue && 
                        input.PerformanceTasks.HasValue && input.QuarterlyAssessment.HasValue)
                    {
                        computedFinalGrade = CalculateQuarterlyGrade(
                            input.WrittenWork, 
                            input.PerformanceTasks, 
                            input.QuarterlyAssessment);
                    }

                    // Check if grade already exists
                    var existingGrade = await _context.Grades
                        .FirstOrDefaultAsync(g => g.StudentId == input.StudentId
                                                && g.SubjectId == input.SubjectId
                                                && g.SectionId == input.SectionId
                                                && g.SchoolYear == input.SchoolYear
                                                && g.GradingPeriod == input.GradingPeriod);

                    if (existingGrade != null)
                    {
                        // Create history record before updating
                        await CreateGradeHistoryAsync(existingGrade, input, teacherId, "Grade updated");

                        // Update existing grade
                        existingGrade.WrittenWork = input.WrittenWork;
                        existingGrade.PerformanceTasks = input.PerformanceTasks;
                        existingGrade.QuarterlyAssessment = input.QuarterlyAssessment;
                        existingGrade.FinalGrade = computedFinalGrade;
                        existingGrade.TeacherId = teacherId;
                        existingGrade.UpdatedAt = DateTime.Now;

                        _context.Grades.Update(existingGrade);
                    }
                    else
                    {
                        // Create new grade
                        var grade = new Grade
                        {
                            StudentId = input.StudentId,
                            SubjectId = input.SubjectId,
                            SectionId = input.SectionId,
                            SchoolYear = input.SchoolYear,
                            GradingPeriod = input.GradingPeriod,
                            WrittenWork = input.WrittenWork,
                            PerformanceTasks = input.PerformanceTasks,
                            QuarterlyAssessment = input.QuarterlyAssessment,
                            FinalGrade = computedFinalGrade,
                            TeacherId = teacherId,
                            CreatedAt = DateTime.Now
                        };

                        _context.Grades.Add(grade);
                    }
                }

                await _context.SaveChangesAsync();

                // Create history records after saving (to get GradeIds)
                foreach (var input in gradeInputs)
                {
                    var grade = await _context.Grades
                        .FirstOrDefaultAsync(g => g.StudentId == input.StudentId
                                              && g.SubjectId == input.SubjectId
                                              && g.SectionId == input.SectionId
                                              && g.SchoolYear == input.SchoolYear
                                              && g.GradingPeriod == input.GradingPeriod);

                    if (grade != null)
                    {
                        await CreateGradeHistoryAsync(grade, input, teacherId, 
                            grade.CreatedAt == grade.UpdatedAt ? "Grade created" : "Grade updated");
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger?.LogInformation("Successfully saved {Count} grades", gradeInputs.Count);
                
                // Audit logging (non-blocking, background task)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (_serviceScopeFactory != null)
                        {
                            using var scope = _serviceScopeFactory.CreateScope();
                            var auditLogService = scope.ServiceProvider.GetRequiredService<AuditLogService>();
                            var authService = scope.ServiceProvider.GetService<IAuthService>();
                            
                            var currentUser = authService?.CurrentUser;
                            var userName = currentUser != null ? $"{currentUser.first_name} {currentUser.last_name}".Trim() : "System";
                            var userRole = currentUser?.user_role ?? "Teacher";
                            var userId = teacherId;
                            
                            // Get unique section-subject combinations for description
                            var uniqueCombinations = gradeInputs
                                .Select(g => new { g.SectionId, g.SubjectId })
                                .Distinct()
                                .Count();
                            
                            await auditLogService.CreateTransactionLogAsync(
                                action: "Submit Grades",
                                module: "Academic",
                                description: $"Submitted {gradeInputs.Count} grades by Teacher ID {teacherId} for {uniqueCombinations} section-subject combination(s)",
                                userName: userName,
                                userRole: userRole,
                                userId: userId,
                                entityType: "Grade",
                                entityId: $"Teacher_{teacherId}",
                                oldValues: null,
                                newValues: $"Grades submitted for {gradeInputs.Count} student-subject combinations",
                                status: "Success",
                                severity: "High"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to create audit log for grade submission: {Message}", ex.Message);
                    }
                });
                
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger?.LogError(ex, "Error saving grades: {Message}", ex.Message);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in SaveGradesAsync: {Message}", ex.Message);
            return false;
        }
    }
    public async Task<bool> SaveQuarterlyGradesAsync(List<GradeInputDto> gradeInputs, int teacherId)
    {
        try
        {
            if (gradeInputs == null || !gradeInputs.Any())
            {
                throw new ArgumentException("Grade inputs cannot be empty.");
            }

            // Check if school year is open for editing
            var schoolYear = gradeInputs.First().SchoolYear;
            var isOpen = await IsSchoolYearOpenAsync(schoolYear);
            if (!isOpen)
            {
                _logger?.LogWarning("Attempted to save quarterly grades for closed school year {SchoolYear}", schoolYear);
                throw new InvalidOperationException($"Cannot save grades. The school year {schoolYear} is closed. Please contact an administrator to reopen it if you need to make changes.");
            }

            // Validate teacher is assigned to all section-subject combinations
            var sectionSubjectPairs = gradeInputs
                .Select(g => new { g.SectionId, g.SubjectId })
                .Distinct()
                .ToList();

            foreach (var pair in sectionSubjectPairs)
            {
                var isAssigned = await _context.TeacherSectionAssignments
                    .AnyAsync(a => a.TeacherId == teacherId
                                && a.SectionId == pair.SectionId
                                && a.SubjectId == pair.SubjectId
                                && !a.IsArchived);

                if (!isAssigned)
                {
                    _logger?.LogWarning("Teacher {TeacherId} attempted to save grades for unauthorized section {SectionId}, subject {SubjectId}", 
                        teacherId, pair.SectionId, pair.SubjectId);
                    throw new UnauthorizedAccessException($"You are not assigned to teach this subject in this section.");
                }
            }

            // Validate grade ranges
            foreach (var input in gradeInputs)
            {
                if (input.FinalGrade.HasValue && (input.FinalGrade < 0 || input.FinalGrade > 100))
                {
                    throw new ArgumentException($"Grade must be between 0 and 100. Student: {input.StudentId}, Quarter: {input.GradingPeriod}");
                }
            }

            // Ensure table exists
            try
            {
                await EnsureGradesTableExistsAsync();
            }
            catch (InvalidOperationException ex)
            {
                _logger?.LogError(ex, "Cannot save grades: tbl_Grades table does not exist and could not be created.");
                throw new InvalidOperationException("Cannot save grades. The grades table is missing and could not be created automatically. Please contact your system administrator.", ex);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var input in gradeInputs)
                {
                    if (!input.FinalGrade.HasValue)
                        continue; // Skip if no grade provided

                    // Check if grade already exists
                    var existingGrade = await _context.Grades
                        .FirstOrDefaultAsync(g => g.StudentId == input.StudentId
                                                && g.SubjectId == input.SubjectId
                                                && g.SectionId == input.SectionId
                                                && g.SchoolYear == input.SchoolYear
                                                && g.GradingPeriod == input.GradingPeriod);

                    if (existingGrade != null)
                    {
                        // Create history record before updating
                        await CreateGradeHistoryAsync(existingGrade, input, teacherId, "Grade updated");

                        // Update existing grade - save FinalGrade directly, clear components
                        existingGrade.FinalGrade = input.FinalGrade;
                        existingGrade.WrittenWork = null;
                        existingGrade.PerformanceTasks = null;
                        existingGrade.QuarterlyAssessment = null;
                        existingGrade.TeacherId = teacherId;
                        existingGrade.UpdatedAt = DateTime.Now;

                        _context.Grades.Update(existingGrade);
                    }
                    else
                    {
                        // Create new grade - save FinalGrade directly
                        var grade = new Grade
                        {
                            StudentId = input.StudentId,
                            SubjectId = input.SubjectId,
                            SectionId = input.SectionId,
                            SchoolYear = input.SchoolYear,
                            GradingPeriod = input.GradingPeriod,
                            FinalGrade = input.FinalGrade,
                            WrittenWork = null,
                            PerformanceTasks = null,
                            QuarterlyAssessment = null,
                            TeacherId = teacherId,
                            CreatedAt = DateTime.Now
                        };

                        _context.Grades.Add(grade);
                    }
                }

                await _context.SaveChangesAsync();

                // Create history records after saving (to get GradeIds)
                foreach (var input in gradeInputs)
                {
                    if (!input.FinalGrade.HasValue)
                        continue;

                    var grade = await _context.Grades
                        .FirstOrDefaultAsync(g => g.StudentId == input.StudentId
                                              && g.SubjectId == input.SubjectId
                                              && g.SectionId == input.SectionId
                                              && g.SchoolYear == input.SchoolYear
                                              && g.GradingPeriod == input.GradingPeriod);

                    if (grade != null)
                    {
                        await CreateGradeHistoryAsync(grade, input, teacherId, 
                            grade.CreatedAt == grade.UpdatedAt ? "Grade created" : "Grade updated");
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger?.LogInformation("Successfully saved {Count} quarterly grades", gradeInputs.Count);
                
                // Audit logging (non-blocking, background task)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (_serviceScopeFactory != null)
                        {
                            using var scope = _serviceScopeFactory.CreateScope();
                            var auditLogService = scope.ServiceProvider.GetRequiredService<AuditLogService>();
                            var authService = scope.ServiceProvider.GetService<IAuthService>();
                            
                            var currentUser = authService?.CurrentUser;
                            var userName = currentUser != null ? $"{currentUser.first_name} {currentUser.last_name}".Trim() : "System";
                            var userRole = currentUser?.user_role ?? "Teacher";
                            var userId = teacherId;
                            
                            // Get unique section-subject combinations for description
                            var uniqueCombinations = gradeInputs
                                .Select(g => new { g.SectionId, g.SubjectId })
                                .Distinct()
                                .Count();
                            
                            await auditLogService.CreateTransactionLogAsync(
                                action: "Submit Quarterly Grades",
                                module: "Academic",
                                description: $"Submitted {gradeInputs.Count} quarterly grades by Teacher ID {teacherId} for {uniqueCombinations} section-subject combination(s)",
                                userName: userName,
                                userRole: userRole,
                                userId: userId,
                                entityType: "Grade",
                                entityId: $"Teacher_{teacherId}",
                                oldValues: null,
                                newValues: $"Quarterly grades submitted for {gradeInputs.Count} student-subject combinations",
                                status: "Success",
                                severity: "High"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to create audit log for quarterly grade submission: {Message}", ex.Message);
                    }
                });
                
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger?.LogError(ex, "Error saving quarterly grades: {Message}", ex.Message);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in SaveQuarterlyGradesAsync: {Message}", ex.Message);
            return false;
        }
    }
    public async Task<List<Grade>> GetStudentGradesAsync(string studentId, string schoolYear)
    {
        try
        {
            await EnsureGradesTableExistsAsync();

            return await _context.Grades
                .Where(g => g.StudentId == studentId && g.SchoolYear == schoolYear)
                .Include(g => g.Subject)
                .Include(g => g.Section)
                    .ThenInclude(s => s!.GradeLevel)
                .OrderBy(g => g.Subject!.SubjectName)
                .ThenBy(g => g.GradingPeriod)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting student grades: {Message}", ex.Message);
            throw;
        }
    }
    public async Task<Grade?> GetGradeByIdAsync(int gradeId)
    {
        try
        {
            await EnsureGradesTableExistsAsync();

            return await _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Subject)
                .Include(g => g.Section)
                .Include(g => g.Teacher)
                .FirstOrDefaultAsync(g => g.GradeId == gradeId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting grade by ID: {Message}", ex.Message);
            throw;
        }
    }
    public async Task<List<Grade>> GetAllGradesForSectionAsync(int sectionId, string schoolYear)
    {
        try
        {
            await EnsureGradesTableExistsAsync();

            return await _context.Grades
                .Where(g => g.SectionId == sectionId && g.SchoolYear == schoolYear)
                .Include(g => g.Subject)
                .Include(g => g.Student)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting all grades for section: {Message}", ex.Message);
            throw;
        }
    }
    public async Task<List<GradeRecordDto>> GetGradeRecordsAsync(int teacherId, string? schoolYear = null, int? sectionId = null, int? subjectId = null, string? searchTerm = null)
    {
        try
        {
            var currentSchoolYear = schoolYear ?? await GetCurrentSchoolYearAsync();

            // Get assigned section-subject pairs for this teacher
            var assignments = await _context.TeacherSectionAssignments
                .Where(a => a.TeacherId == teacherId && !a.IsArchived)
                .Select(a => new { a.SectionId, a.SubjectId })
                .Distinct()
                .ToListAsync();

            var sectionIds = assignments.Select(a => a.SectionId).Distinct().ToList();
            var subjectIds = assignments.Select(a => a.SubjectId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();

            // Apply filters
            if (sectionId.HasValue)
                sectionIds = sectionIds.Where(id => id == sectionId.Value).ToList();
            if (subjectId.HasValue)
                subjectIds = subjectIds.Where(id => id == subjectId.Value).ToList();

            await EnsureGradesTableExistsAsync();

            // Get grades for assigned sections and subjects
            var gradesQuery = _context.Grades
                .Where(g => sectionIds.Contains(g.SectionId) 
                         && subjectIds.Contains(g.SubjectId)
                         && g.SchoolYear == currentSchoolYear)
                .Include(g => g.Student)
                .Include(g => g.Subject)
                .Include(g => g.Section)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                gradesQuery = gradesQuery.Where(g => 
                    g.StudentId.Contains(searchTerm) ||
                    (g.Student != null && (g.Student.FirstName.Contains(searchTerm) || g.Student.LastName.Contains(searchTerm))));
            }

            var grades = await gradesQuery.ToListAsync();

            // Group by student and subject to calculate quarterly and final grades
            var groupedGrades = grades
                .GroupBy(g => new { g.StudentId, g.SubjectId, g.SectionId })
                .Select(g => 
                {
                    var firstGrade = g.First();
                    var q1Grade = g.FirstOrDefault(x => x.GradingPeriod == "Q1");
                    var q2Grade = g.FirstOrDefault(x => x.GradingPeriod == "Q2");
                    var q3Grade = g.FirstOrDefault(x => x.GradingPeriod == "Q3");
                    var q4Grade = g.FirstOrDefault(x => x.GradingPeriod == "Q4");

                    var q1 = q1Grade?.FinalGrade ?? 0;
                    var q2 = q2Grade?.FinalGrade ?? 0;
                    var q3 = q3Grade?.FinalGrade ?? 0;
                    var q4 = q4Grade?.FinalGrade ?? 0;
                    var final = CalculateFinalGrade(
                        q1Grade?.FinalGrade,
                        q2Grade?.FinalGrade,
                        q3Grade?.FinalGrade,
                        q4Grade?.FinalGrade);

                    // Determine remarks based on completeness and grade
                    string remarks;
                    var quartersWithGrades = new List<decimal>();
                    if (q1 > 0) quartersWithGrades.Add(q1);
                    if (q2 > 0) quartersWithGrades.Add(q2);
                    if (q3 > 0) quartersWithGrades.Add(q3);
                    if (q4 > 0) quartersWithGrades.Add(q4);

                    if (!quartersWithGrades.Any())
                    {
                        remarks = "No Grade";
                    }
                    else if (quartersWithGrades.Count < 4)
                    {
                        remarks = "Incomplete";
                    }
                    else
                    {
                        var transmutedGrade = GetTransmutedGrade(final);
                        remarks = GetDescriptiveRating(transmutedGrade);
                    }

                    return new GradeRecordDto
                    {
                        StudentId = g.Key.StudentId,
                        Name = firstGrade.Student != null 
                            ? $"{firstGrade.Student.FirstName} {firstGrade.Student.LastName}".Trim()
                            : g.Key.StudentId,
                        Section = firstGrade.Section?.SectionName ?? "",
                        Subject = firstGrade.Subject?.SubjectName ?? "",
                        Q1 = q1,
                        Q2 = q2,
                        Q3 = q3,
                        Q4 = q4,
                        Final = final,
                        Remarks = remarks
                    };
                })
                .ToList();

            return groupedGrades
                .OrderBy(g => g.Subject)
                .ThenBy(g => g.Name)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting grade records: {Message}", ex.Message);
            throw;
        }
    }
    public async Task<GradeWeightDto> GetGradeWeightsAsync(int subjectId)
    {
        try
        {
            var weight = await _context.GradeWeights
                .FirstOrDefaultAsync(w => w.SubjectId == subjectId);

            if (weight != null)
            {
                return new GradeWeightDto
                {
                    SubjectId = weight.SubjectId,
                    WrittenWorkWeight = weight.WrittenWorkWeight,
                    PerformanceTasksWeight = weight.PerformanceTasksWeight,
                    QuarterlyAssessmentWeight = weight.QuarterlyAssessmentWeight
                };
            }

            // Return DepEd standard defaults
            return new GradeWeightDto
            {
                SubjectId = subjectId,
                WrittenWorkWeight = 0.20m,
                PerformanceTasksWeight = 0.60m,
                QuarterlyAssessmentWeight = 0.20m
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting grade weights: {Message}", ex.Message);
            // Return DepEd standard defaults on error
            return new GradeWeightDto
            {
                SubjectId = subjectId,
                WrittenWorkWeight = 0.20m,
                PerformanceTasksWeight = 0.60m,
                QuarterlyAssessmentWeight = 0.20m
            };
        }
    }
    public async Task<List<GradeHistory>> GetGradeHistoryAsync(int gradeId)
    {
        try
        {
            return await _context.GradeHistories
                .Where(h => h.GradeId == gradeId)
                .Include(h => h.ChangedByUser)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting grade history: {Message}", ex.Message);
            throw;
        }
    }
    public decimal GetTransmutedGrade(decimal rawGrade)
    {
        // DepEd Transmutation Table (K-12)
        if (rawGrade >= 96.00m && rawGrade <= 100.00m) return 100.00m;
        if (rawGrade >= 91.00m && rawGrade <= 95.99m) return 95.00m;
        if (rawGrade >= 86.00m && rawGrade <= 90.99m) return 90.00m;
        if (rawGrade >= 81.00m && rawGrade <= 85.99m) return 85.00m;
        if (rawGrade >= 76.00m && rawGrade <= 80.99m) return 80.00m;
        if (rawGrade >= 71.00m && rawGrade <= 75.99m) return 75.00m;
        if (rawGrade >= 66.00m && rawGrade <= 70.99m) return 70.00m;
        if (rawGrade >= 61.00m && rawGrade <= 65.99m) return 65.00m;
        if (rawGrade >= 60.00m && rawGrade <= 60.99m) return 60.00m;
        // Minimum passing grade: 60 (transmuted to 75 on report card)
        if (rawGrade >= 0.00m && rawGrade < 60.00m) return 75.00m;
        return 75.00m;
    }
    public string GetDescriptiveRating(decimal transmutedGrade)
    {
        if (transmutedGrade >= 90.00m && transmutedGrade <= 100.00m) return "Outstanding";
        if (transmutedGrade >= 85.00m && transmutedGrade <= 89.99m) return "Very Satisfactory";
        if (transmutedGrade >= 80.00m && transmutedGrade <= 84.99m) return "Satisfactory";
        if (transmutedGrade >= 75.00m && transmutedGrade <= 79.99m) return "Fairly Satisfactory";
        if (transmutedGrade >= 0.00m && transmutedGrade <= 74.99m) return "Did Not Meet Expectations";
        return "Did Not Meet Expectations";
    }
    public async Task<decimal> CalculateGeneralAverageAsync(string studentId, int sectionId, string schoolYear)
    {
        try
        {
            await EnsureGradesTableExistsAsync();

            var grades = await _context.Grades
                .Where(g => g.StudentId == studentId 
                         && g.SectionId == sectionId 
                         && g.SchoolYear == schoolYear)
                .ToListAsync();

            // Group by subject and calculate final grade per subject
            // Each subject's FinalGrade field contains the quarterly grade (computed from WW×20% + PT×60% + QA×20%)
            var subjectAverages = grades
                .GroupBy(g => g.SubjectId)
                .Select(g => 
                {
                    var q1 = g.FirstOrDefault(x => x.GradingPeriod == "Q1")?.FinalGrade;
                    var q2 = g.FirstOrDefault(x => x.GradingPeriod == "Q2")?.FinalGrade;
                    var q3 = g.FirstOrDefault(x => x.GradingPeriod == "Q3")?.FinalGrade;
                    var q4 = g.FirstOrDefault(x => x.GradingPeriod == "Q4")?.FinalGrade;
                    
                    // Calculate subject final grade (average of Q1-Q4, requires all 4 quarters)
                    return CalculateFinalGrade(q1, q2, q3, q4);
                })
                .Where(avg => avg > 0) // Only include subjects with all 4 quarters complete
                .ToList();

            if (!subjectAverages.Any())
                return 0;

            // General Average = Average of all subject final grades
            return subjectAverages.Average();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error calculating general average: {Message}", ex.Message);
            return 0;
        }
    }
    public async Task<List<ReportCardDto>> GetReportCardsAsync(int sectionId, string schoolYear)
    {
        try
        {
            await EnsureGradesTableExistsAsync();

            var enrollments = await _context.StudentSectionEnrollments
                .Where(e => e.SectionId == sectionId 
                         && e.SchoolYear == schoolYear 
                         && e.Status == "Enrolled")
                .Include(e => e.Student)
                .ToListAsync();

            var reportCards = new List<ReportCardDto>();

            foreach (var enrollment in enrollments)
            {
                var studentGrades = await _context.Grades
                    .Where(g => g.StudentId == enrollment.StudentId 
                             && g.SectionId == sectionId 
                             && g.SchoolYear == schoolYear)
                    .ToListAsync();

                var studentSubjectIds = studentGrades
                    .Select(g => g.SubjectId)
                    .Distinct()
                    .ToList();

                if (!studentSubjectIds.Any())
                {
                    reportCards.Add(new ReportCardDto
                    {
                        StudentId = enrollment.StudentId,
                        Name = enrollment.Student != null 
                            ? $"{enrollment.Student.FirstName} {enrollment.Student.LastName}".Trim()
                            : enrollment.StudentId,
                        Section = enrollment.Section?.SectionName ?? "",
                        SchoolYear = schoolYear,
                        GeneralAverage = 0,
                        TransmutedGrade = 0,
                        DescriptiveRating = "No Grade"
                    });
                    continue;
                }

                bool allSubjectsComplete = true;

                foreach (var subjectId in studentSubjectIds)
                {
                    var subjectGrades = studentGrades.Where(g => g.SubjectId == subjectId).ToList();
                    var hasQ1 = subjectGrades.Any(g => g.GradingPeriod == "Q1" && g.FinalGrade.HasValue && g.FinalGrade.Value > 0);
                    var hasQ2 = subjectGrades.Any(g => g.GradingPeriod == "Q2" && g.FinalGrade.HasValue && g.FinalGrade.Value > 0);
                    var hasQ3 = subjectGrades.Any(g => g.GradingPeriod == "Q3" && g.FinalGrade.HasValue && g.FinalGrade.Value > 0);
                    var hasQ4 = subjectGrades.Any(g => g.GradingPeriod == "Q4" && g.FinalGrade.HasValue && g.FinalGrade.Value > 0);
                    
                    bool isComplete = hasQ1 && hasQ2 && hasQ3 && hasQ4;
                    
                    if (!isComplete)
                    {
                        allSubjectsComplete = false;
                        break;
                    }
                }

                decimal generalAverage = 0;
                decimal transmutedGrade = 0;
                string descriptiveRating = "Incomplete";

                if (allSubjectsComplete)
                {
                    generalAverage = await CalculateGeneralAverageAsync(enrollment.StudentId, sectionId, schoolYear);
                    
                    if (generalAverage > 0)
                    {
                        transmutedGrade = GetTransmutedGrade(generalAverage);
                        descriptiveRating = GetDescriptiveRating(transmutedGrade);
                    }
                    else
                    {
                        descriptiveRating = "No Grade";
                    }
                }

                reportCards.Add(new ReportCardDto
                {
                    StudentId = enrollment.StudentId,
                    Name = enrollment.Student != null 
                        ? $"{enrollment.Student.FirstName} {enrollment.Student.LastName}".Trim()
                        : enrollment.StudentId,
                    Section = enrollment.Section?.SectionName ?? "",
                    SchoolYear = schoolYear,
                    GeneralAverage = generalAverage,
                    TransmutedGrade = transmutedGrade,
                    DescriptiveRating = descriptiveRating
                });
            }

            return reportCards.OrderBy(r => r.Name).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting report cards: {Message}", ex.Message);
            throw;
        }
    }

    #region Helper Methods

    private async Task CreateGradeHistoryAsync(Grade grade, GradeInputDto newInput, int changedBy, string reason)
    {
        try
        {
            // Only create history if there are actual changes
            bool hasChanges = grade.WrittenWork != newInput.WrittenWork ||
                             grade.PerformanceTasks != newInput.PerformanceTasks ||
                             grade.QuarterlyAssessment != newInput.QuarterlyAssessment ||
                             grade.FinalGrade != newInput.FinalGrade;

            if (!hasChanges && reason != "Grade created")
                return;

            var history = new GradeHistory
            {
                GradeId = grade.GradeId,
                StudentId = grade.StudentId,
                SubjectId = grade.SubjectId,
                SectionId = grade.SectionId,
                WrittenWorkOld = reason == "Grade created" ? null : grade.WrittenWork,
                WrittenWorkNew = newInput.WrittenWork,
                PerformanceTasksOld = reason == "Grade created" ? null : grade.PerformanceTasks,
                PerformanceTasksNew = newInput.PerformanceTasks,
                QuarterlyAssessmentOld = reason == "Grade created" ? null : grade.QuarterlyAssessment,
                QuarterlyAssessmentNew = newInput.QuarterlyAssessment,
                FinalGradeOld = reason == "Grade created" ? null : grade.FinalGrade,
                FinalGradeNew = newInput.FinalGrade,
                ChangedBy = changedBy,
                ChangeReason = reason,
                ChangedAt = DateTime.Now
            };

            _context.GradeHistories.Add(history);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating grade history: {Message}", ex.Message);
            // Don't throw - history is not critical
        }
    }
    private decimal CalculateQuarterlyGrade(decimal? writtenWork, decimal? performanceTasks, decimal? quarterlyAssessment)
    {
        if (!writtenWork.HasValue || !performanceTasks.HasValue || !quarterlyAssessment.HasValue)
            return 0;

        // DepEd Formula: Quarterly Grade = (WW × 20%) + (PT × 60%) + (QA × 20%)
        return (writtenWork.Value * 0.20m) + 
               (performanceTasks.Value * 0.60m) + 
               (quarterlyAssessment.Value * 0.20m);
    }

    private decimal CalculateFinalGrade(decimal? q1, decimal? q2, decimal? q3, decimal? q4)
    {
        // DepEd Requirement: All 4 quarters must have grades before calculating final grade
        if (!q1.HasValue || q1.Value <= 0) return 0;
        if (!q2.HasValue || q2.Value <= 0) return 0;
        if (!q3.HasValue || q3.Value <= 0) return 0;
        if (!q4.HasValue || q4.Value <= 0) return 0;

        // All quarters are complete, calculate average
        return (q1.Value + q2.Value + q3.Value + q4.Value) / 4.0m;
    }

    private Task<string> GetCurrentSchoolYearAsync()
    {
        var currentYear = DateTime.Now.Year;
        var nextYear = currentYear + 1;
        return Task.FromResult($"{currentYear}-{nextYear}");
    }

    #endregion
}

// DTO for student information in grade entry
public class StudentGradeDto
{
    public string StudentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SectionId { get; set; }
    public string SchoolYear { get; set; } = string.Empty;
}

// DTO for grade input from teacher
public class GradeInputDto
{
    public string StudentId { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public int SectionId { get; set; }
    public string SchoolYear { get; set; } = string.Empty;
    public string GradingPeriod { get; set; } = string.Empty;
    public decimal? WrittenWork { get; set; }
    public decimal? PerformanceTasks { get; set; }
    public decimal? QuarterlyAssessment { get; set; }
    public decimal? FinalGrade { get; set; }
}

// DTO for grade records display
public class GradeRecordDto
{
    public string StudentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public decimal Q1 { get; set; }
    public decimal Q2 { get; set; }
    public decimal Q3 { get; set; }
    public decimal Q4 { get; set; }
    public decimal Final { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

// DTO for grade weights
public class GradeWeightDto
{
    public int SubjectId { get; set; }
    public decimal WrittenWorkWeight { get; set; }
    public decimal PerformanceTasksWeight { get; set; }
    public decimal QuarterlyAssessmentWeight { get; set; }
}

// DTO for report card
public class ReportCardDto
{
    public string StudentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string SchoolYear { get; set; } = string.Empty;
    public decimal GeneralAverage { get; set; }
    public decimal TransmutedGrade { get; set; }
    public string DescriptiveRating { get; set; } = string.Empty;
}

