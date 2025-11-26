using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services;

// Handles student grade operations
public class GradeService
{
    private readonly AppDbContext _context;
    private readonly ILogger<GradeService>? _logger;

    public GradeService(AppDbContext context, ILogger<GradeService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    // Gets grades for students in a specific class and subject
    public async Task<List<StudentGradeDto>> GetStudentGradesAsync(string schoolYear, string gradeLevel, string? section, string subject)
    {
        try
        {
            var query = _context.StudentGrades
                .Include(g => g.Student)
                .Where(g => g.SchoolYear == schoolYear 
                    && g.GradeLevel == gradeLevel 
                    && g.Subject == subject);

            if (!string.IsNullOrWhiteSpace(section))
            {
                query = query.Where(g => g.Section == section);
            }

            var grades = await query
                .OrderBy(g => g.Student != null ? g.Student.LastName : "")
                .ToListAsync();

            return grades.Select(g => new StudentGradeDto
            {
                GradeId = g.GradeId,
                StudentId = g.StudentId,
                Name = g.Student != null 
                    ? $"{g.Student.FirstName} {g.Student.MiddleName} {g.Student.LastName}".Trim()
                    : "Unknown",
                FirstQuarter = g.FirstQuarter,
                SecondQuarter = g.SecondQuarter,
                ThirdQuarter = g.ThirdQuarter,
                FourthQuarter = g.FourthQuarter,
                FinalGrade = g.FinalGrade,
                Remarks = g.Remarks
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching student grades: {Message}", ex.Message);
            throw new Exception($"Failed to fetch student grades: {ex.Message}", ex);
        }
    }

    // Gets students for a specific class (for grade entry)
    public async Task<List<StudentForGradeDto>> GetStudentsForClassAsync(string schoolYear, string gradeLevel, string? section)
    {
        try
        {
            var query = _context.Students
                .Where(s => s.SchoolYr == schoolYear && s.GradeLevel == gradeLevel);

            if (!string.IsNullOrWhiteSpace(section))
            {
                // Note: Section filtering would need to be added to Student model if sections are stored
                // For now, we'll return all students for the grade level
            }

            var students = await query
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            return students.Select(s => new StudentForGradeDto
            {
                StudentId = s.StudentId,
                Name = $"{s.FirstName} {s.MiddleName} {s.LastName}".Trim()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching students for class: {Message}", ex.Message);
            throw new Exception($"Failed to fetch students: {ex.Message}", ex);
        }
    }

    // Saves or updates a single student grade
    public async Task<StudentGrade> SaveStudentGradeAsync(StudentGradeDto gradeDto, int? teacherId, string? createdBy = null)
    {
        try
        {
            StudentGrade? grade;

            if (gradeDto.GradeId > 0)
            {
                // Update existing grade
                grade = await _context.StudentGrades
                    .FirstOrDefaultAsync(g => g.GradeId == gradeDto.GradeId);

                if (grade == null)
                {
                    throw new Exception($"Grade with ID {gradeDto.GradeId} not found");
                }

                grade.FirstQuarter = gradeDto.FirstQuarter;
                grade.SecondQuarter = gradeDto.SecondQuarter;
                grade.ThirdQuarter = gradeDto.ThirdQuarter;
                grade.FourthQuarter = gradeDto.FourthQuarter;
                grade.Remarks = CalculateRemarks(gradeDto.FirstQuarter, gradeDto.SecondQuarter, gradeDto.ThirdQuarter, gradeDto.FourthQuarter);
                grade.UpdatedDate = DateTime.Now;
                grade.UpdatedBy = createdBy;
            }
            else
            {
                // Create new grade - need school year, grade level, section, and subject from context
                // This should be called with full context
                throw new Exception("Cannot create grade without full context. Use SaveStudentGradeWithContextAsync instead.");
            }

            await _context.SaveChangesAsync();
            _logger?.LogInformation("Grade saved for student {StudentId}", gradeDto.StudentId);

            return grade;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving student grade: {Message}", ex.Message);
            throw new Exception($"Failed to save grade: {ex.Message}", ex);
        }
    }

    // Saves or updates a student grade with full context
    public async Task<StudentGrade> SaveStudentGradeWithContextAsync(
        string studentId, 
        string schoolYear, 
        string gradeLevel, 
        string? section, 
        string subject,
        decimal? firstQuarter,
        decimal? secondQuarter,
        decimal? thirdQuarter,
        decimal? fourthQuarter,
        int? teacherId,
        string? createdBy = null)
    {
        try
        {
            var grade = await _context.StudentGrades
                .FirstOrDefaultAsync(g => g.StudentId == studentId 
                    && g.SchoolYear == schoolYear 
                    && g.GradeLevel == gradeLevel 
                    && g.Subject == subject);

            if (grade == null)
            {
                // Create new grade
                grade = new StudentGrade
                {
                    StudentId = studentId,
                    SchoolYear = schoolYear,
                    GradeLevel = gradeLevel,
                    Section = section,
                    Subject = subject,
                    FirstQuarter = firstQuarter,
                    SecondQuarter = secondQuarter,
                    ThirdQuarter = thirdQuarter,
                    FourthQuarter = fourthQuarter,
                    Remarks = CalculateRemarks(firstQuarter, secondQuarter, thirdQuarter, fourthQuarter),
                    TeacherId = teacherId,
                    CreatedDate = DateTime.Now,
                    CreatedBy = createdBy
                };

                _context.StudentGrades.Add(grade);
            }
            else
            {
                // Update existing grade
                grade.FirstQuarter = firstQuarter;
                grade.SecondQuarter = secondQuarter;
                grade.ThirdQuarter = thirdQuarter;
                grade.FourthQuarter = fourthQuarter;
                grade.Remarks = CalculateRemarks(firstQuarter, secondQuarter, thirdQuarter, fourthQuarter);
                grade.UpdatedDate = DateTime.Now;
                grade.UpdatedBy = createdBy;
                if (teacherId.HasValue)
                {
                    grade.TeacherId = teacherId;
                }
            }

            await _context.SaveChangesAsync();
            _logger?.LogInformation("Grade saved for student {StudentId}, subject {Subject}", studentId, subject);

            return grade;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving student grade: {Message}", ex.Message);
            throw new Exception($"Failed to save grade: {ex.Message}", ex);
        }
    }

    // Saves multiple grades in a batch
    public async Task SaveMultipleGradesAsync(
        string schoolYear,
        string gradeLevel,
        string? section,
        string subject,
        List<StudentGradeDto> grades,
        int? teacherId,
        string? createdBy = null)
    {
        try
        {
            foreach (var gradeDto in grades)
            {
                await SaveStudentGradeWithContextAsync(
                    gradeDto.StudentId,
                    schoolYear,
                    gradeLevel,
                    section,
                    subject,
                    gradeDto.FirstQuarter,
                    gradeDto.SecondQuarter,
                    gradeDto.ThirdQuarter,
                    gradeDto.FourthQuarter,
                    teacherId,
                    createdBy);
            }

            _logger?.LogInformation("Saved {Count} grades for {Subject}", grades.Count, subject);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving multiple grades: {Message}", ex.Message);
            throw new Exception($"Failed to save grades: {ex.Message}", ex);
        }
    }

    // Calculates remarks based on final grade
    private string? CalculateRemarks(decimal? firstQuarter, decimal? secondQuarter, decimal? thirdQuarter, decimal? fourthQuarter)
    {
        var quarters = new[] { firstQuarter, secondQuarter, thirdQuarter, fourthQuarter }
            .Where(q => q.HasValue)
            .Select(q => q!.Value)
            .ToList();

        if (!quarters.Any())
        {
            return "Incomplete";
        }

        var average = quarters.Average();
        return average >= 75 ? "PASSED" : "FAILED";
    }

    // Gets available school years from student records
    public async Task<List<string>> GetAvailableSchoolYearsAsync()
    {
        try
        {
            var schoolYears = await _context.Students
                .Where(s => !string.IsNullOrWhiteSpace(s.SchoolYr))
                .Select(s => s.SchoolYr!)
                .Distinct()
                .OrderByDescending(sy => sy)
                .ToListAsync();

            return schoolYears;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching school years: {Message}", ex.Message);
            return new List<string>();
        }
    }

    // Gets available grade levels for a school year
    public async Task<List<string>> GetAvailableGradeLevelsAsync(string schoolYear)
    {
        try
        {
            var gradeLevels = await _context.Students
                .Where(s => s.SchoolYr == schoolYear && !string.IsNullOrWhiteSpace(s.GradeLevel))
                .Select(s => s.GradeLevel!)
                .Distinct()
                .OrderBy(gl => gl)
                .ToListAsync();

            return gradeLevels;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching grade levels: {Message}", ex.Message);
            return new List<string>();
        }
    }

    // Gets available subjects (could be from a separate table or hardcoded)
    public List<string> GetAvailableSubjects()
    {
        return new List<string>
        {
            "Mathematics",
            "Science",
            "English",
            "Filipino",
            "History",
            "MAPEH",
            "TLE",
            "Values Education",
            "Computer",
            "Physical Education"
        };
    }
}

// DTO for student grade display
public class StudentGradeDto
{
    public int GradeId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? FirstQuarter { get; set; }
    public decimal? SecondQuarter { get; set; }
    public decimal? ThirdQuarter { get; set; }
    public decimal? FourthQuarter { get; set; }
    public decimal? FinalGrade { get; set; }
    public string? Remarks { get; set; }
}

// DTO for student list in grade entry
public class StudentForGradeDto
{
    public string StudentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

