using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using BrightEnroll_DES.Services.Database.Initialization;
using Microsoft.Extensions.Configuration;

namespace BrightEnroll_DES.Services.Business.Academic;

public class GradeService
{
    private readonly AppDbContext _context;
    private readonly ILogger<GradeService>? _logger;
    private readonly IConfiguration? _configuration;
    private static bool _tableChecked = false;
    private static readonly object _lockObject = new object();

    public GradeService(AppDbContext context, ILogger<GradeService>? logger = null, IConfiguration? configuration = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Ensures tbl_Grades table exists before querying
    /// </summary>
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
            _tableChecked = true; // Set early to prevent concurrent calls
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
                            [quiz] DECIMAL(5,2) NULL,
                            [exam] DECIMAL(5,2) NULL,
                            [project] DECIMAL(5,2) NULL,
                            [participation] DECIMAL(5,2) NULL,
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
                throw; // Re-throw InvalidOperationException
            }
            // Reset flag so we can try again next time
            lock (_lockObject)
            {
                _tableChecked = false;
            }
            throw new InvalidOperationException($"Failed to create tbl_Grades table: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets all students enrolled in a section for a specific school year
    /// </summary>
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

    /// <summary>
    /// Gets existing grades for students in a section/subject/period
    /// </summary>
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

    /// <summary>
    /// Saves or updates grades for multiple students
    /// </summary>
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

            // Validate grade ranges
            foreach (var input in gradeInputs)
            {
                if (input.Quiz.HasValue && (input.Quiz < 0 || input.Quiz > 100))
                {
                    throw new ArgumentException($"Quiz grade must be between 0 and 100. Student: {input.StudentId}");
                }
                if (input.Exam.HasValue && (input.Exam < 0 || input.Exam > 100))
                {
                    throw new ArgumentException($"Exam grade must be between 0 and 100. Student: {input.StudentId}");
                }
                if (input.Project.HasValue && (input.Project < 0 || input.Project > 100))
                {
                    throw new ArgumentException($"Project grade must be between 0 and 100. Student: {input.StudentId}");
                }
                if (input.Participation.HasValue && (input.Participation < 0 || input.Participation > 100))
                {
                    throw new ArgumentException($"Participation grade must be between 0 and 100. Student: {input.StudentId}");
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
                        existingGrade.Quiz = input.Quiz;
                        existingGrade.Exam = input.Exam;
                        existingGrade.Project = input.Project;
                        existingGrade.Participation = input.Participation;
                        existingGrade.FinalGrade = input.FinalGrade;
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
                            Quiz = input.Quiz,
                            Exam = input.Exam,
                            Project = input.Project,
                            Participation = input.Participation,
                            FinalGrade = input.FinalGrade,
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

    /// <summary>
    /// Gets all grades for a specific student
    /// </summary>
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

    /// <summary>
    /// Gets a specific grade by ID
    /// </summary>
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

    /// <summary>
    /// Gets grade records for a teacher, filtered by assignments
    /// </summary>
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

                    return new GradeRecordDto
                    {
                        StudentId = g.Key.StudentId,
                        Name = firstGrade.Student != null 
                            ? $"{firstGrade.Student.FirstName} {firstGrade.Student.LastName}".Trim()
                            : g.Key.StudentId,
                        Section = firstGrade.Section?.SectionName ?? "",
                        Subject = firstGrade.Subject?.SubjectName ?? "",
                        Q1 = q1Grade?.FinalGrade ?? 0,
                        Q2 = q2Grade?.FinalGrade ?? 0,
                        Q3 = q3Grade?.FinalGrade ?? 0,
                        Q4 = q4Grade?.FinalGrade ?? 0,
                        Final = CalculateFinalGrade(
                            q1Grade?.FinalGrade,
                            q2Grade?.FinalGrade,
                            q3Grade?.FinalGrade,
                            q4Grade?.FinalGrade)
                    };
                })
                .ToList();

            return groupedGrades.OrderBy(g => g.Name).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting grade records: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets grade weights for a subject, or returns defaults if not configured
    /// </summary>
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
                    QuizWeight = weight.QuizWeight,
                    ExamWeight = weight.ExamWeight,
                    ProjectWeight = weight.ProjectWeight,
                    ParticipationWeight = weight.ParticipationWeight
                };
            }

            // Return defaults
            return new GradeWeightDto
            {
                SubjectId = subjectId,
                QuizWeight = 0.30m,
                ExamWeight = 0.40m,
                ProjectWeight = 0.20m,
                ParticipationWeight = 0.10m
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting grade weights: {Message}", ex.Message);
            // Return defaults on error
            return new GradeWeightDto
            {
                SubjectId = subjectId,
                QuizWeight = 0.30m,
                ExamWeight = 0.40m,
                ProjectWeight = 0.20m,
                ParticipationWeight = 0.10m
            };
        }
    }

    /// <summary>
    /// Gets grade history for a grade
    /// </summary>
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

    /// <summary>
    /// Gets DepEd transmuted grade from raw grade
    /// </summary>
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
        if (rawGrade >= 56.00m && rawGrade <= 60.99m) return 60.00m;
        if (rawGrade >= 51.00m && rawGrade <= 55.99m) return 55.00m;
        if (rawGrade >= 46.00m && rawGrade <= 50.99m) return 50.00m;
        if (rawGrade >= 0.00m && rawGrade <= 45.99m) return 50.00m;
        return 50.00m; // Default minimum
    }

    /// <summary>
    /// Gets DepEd descriptive rating from transmuted grade
    /// </summary>
    public string GetDescriptiveRating(decimal transmutedGrade)
    {
        if (transmutedGrade >= 90.00m && transmutedGrade <= 100.00m) return "Outstanding";
        if (transmutedGrade >= 85.00m && transmutedGrade <= 89.99m) return "Very Satisfactory";
        if (transmutedGrade >= 80.00m && transmutedGrade <= 84.99m) return "Satisfactory";
        if (transmutedGrade >= 75.00m && transmutedGrade <= 79.99m) return "Fairly Satisfactory";
        if (transmutedGrade >= 0.00m && transmutedGrade <= 74.99m) return "Did Not Meet Expectations";
        return "Did Not Meet Expectations";
    }

    /// <summary>
    /// Calculates general average for a student across all subjects
    /// </summary>
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
            var subjectAverages = grades
                .GroupBy(g => g.SubjectId)
                .Select(g => CalculateFinalGrade(
                    g.FirstOrDefault(x => x.GradingPeriod == "Q1")?.FinalGrade,
                    g.FirstOrDefault(x => x.GradingPeriod == "Q2")?.FinalGrade,
                    g.FirstOrDefault(x => x.GradingPeriod == "Q3")?.FinalGrade,
                    g.FirstOrDefault(x => x.GradingPeriod == "Q4")?.FinalGrade))
                .Where(avg => avg > 0)
                .ToList();

            if (!subjectAverages.Any())
                return 0;

            return subjectAverages.Average();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error calculating general average: {Message}", ex.Message);
            return 0;
        }
    }

    /// <summary>
    /// Gets report card data for a section
    /// </summary>
    public async Task<List<ReportCardDto>> GetReportCardsAsync(int sectionId, string schoolYear)
    {
        try
        {
            var enrollments = await _context.StudentSectionEnrollments
                .Where(e => e.SectionId == sectionId 
                         && e.SchoolYear == schoolYear 
                         && e.Status == "Enrolled")
                .Include(e => e.Student)
                .ToListAsync();

            var reportCards = new List<ReportCardDto>();

            foreach (var enrollment in enrollments)
            {
                var generalAverage = await CalculateGeneralAverageAsync(enrollment.StudentId, sectionId, schoolYear);
                var transmutedGrade = GetTransmutedGrade(generalAverage);
                var descriptiveRating = GetDescriptiveRating(transmutedGrade);

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
            bool hasChanges = grade.Quiz != newInput.Quiz ||
                             grade.Exam != newInput.Exam ||
                             grade.Project != newInput.Project ||
                             grade.Participation != newInput.Participation ||
                             grade.FinalGrade != newInput.FinalGrade;

            if (!hasChanges && reason != "Grade created")
                return;

            var history = new GradeHistory
            {
                GradeId = grade.GradeId,
                StudentId = grade.StudentId,
                SubjectId = grade.SubjectId,
                SectionId = grade.SectionId,
                QuizOld = reason == "Grade created" ? null : grade.Quiz,
                QuizNew = newInput.Quiz,
                ExamOld = reason == "Grade created" ? null : grade.Exam,
                ExamNew = newInput.Exam,
                ProjectOld = reason == "Grade created" ? null : grade.Project,
                ProjectNew = newInput.Project,
                ParticipationOld = reason == "Grade created" ? null : grade.Participation,
                ParticipationNew = newInput.Participation,
                FinalGradeOld = reason == "Grade created" ? null : grade.FinalGrade,
                FinalGradeNew = newInput.FinalGrade,
                ChangedBy = changedBy,
                ChangeReason = reason,
                ChangedAt = DateTime.Now
            };

            _context.GradeHistories.Add(history);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating grade history: {Message}", ex.Message);
            // Don't throw - history is not critical
        }
    }

    private decimal CalculateFinalGrade(decimal? q1, decimal? q2, decimal? q3, decimal? q4)
    {
        var quarters = new List<decimal>();
        if (q1.HasValue && q1.Value > 0) quarters.Add(q1.Value);
        if (q2.HasValue && q2.Value > 0) quarters.Add(q2.Value);
        if (q3.HasValue && q3.Value > 0) quarters.Add(q3.Value);
        if (q4.HasValue && q4.Value > 0) quarters.Add(q4.Value);

        if (!quarters.Any())
            return 0;

        return quarters.Average();
    }

    private async Task<string> GetCurrentSchoolYearAsync()
    {
        var currentYear = DateTime.Now.Year;
        var nextYear = currentYear + 1;
        return $"{currentYear}-{nextYear}";
    }

    #endregion
}

/// <summary>
/// DTO for student information in grade entry
/// </summary>
public class StudentGradeDto
{
    public string StudentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SectionId { get; set; }
    public string SchoolYear { get; set; } = string.Empty;
}

/// <summary>
/// DTO for grade input from teacher
/// </summary>
public class GradeInputDto
{
    public string StudentId { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public int SectionId { get; set; }
    public string SchoolYear { get; set; } = string.Empty;
    public string GradingPeriod { get; set; } = string.Empty;
    public decimal? Quiz { get; set; }
    public decimal? Exam { get; set; }
    public decimal? Project { get; set; }
    public decimal? Participation { get; set; }
    public decimal? FinalGrade { get; set; }
}

/// <summary>
/// DTO for grade records display
/// </summary>
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
}

/// <summary>
/// DTO for grade weights
/// </summary>
public class GradeWeightDto
{
    public int SubjectId { get; set; }
    public decimal QuizWeight { get; set; }
    public decimal ExamWeight { get; set; }
    public decimal ProjectWeight { get; set; }
    public decimal ParticipationWeight { get; set; }
}

/// <summary>
/// DTO for report card
/// </summary>
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

