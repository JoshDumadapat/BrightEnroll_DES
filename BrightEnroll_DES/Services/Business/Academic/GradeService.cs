using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Academic;

public class GradeService
{
    private readonly AppDbContext _context;
    private readonly ILogger<GradeService>? _logger;

    public GradeService(AppDbContext context, ILogger<GradeService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
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
            var grades = await _context.Grades
                .Where(g => g.SectionId == sectionId 
                         && g.SubjectId == subjectId 
                         && g.SchoolYear == schoolYear 
                         && g.GradingPeriod == gradingPeriod)
                .ToListAsync();

            return grades.ToDictionary(g => g.StudentId, g => g);
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
            return await _context.Grades
                .Where(g => g.StudentId == studentId && g.SchoolYear == schoolYear)
                .Include(g => g.Subject)
                .Include(g => g.Section)
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

