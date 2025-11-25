using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services;

// Handles grade management for teachers
public class GradeService
{
    private readonly AppDbContext _context;
    private readonly ILogger<GradeService>? _logger;

    public GradeService(AppDbContext context, ILogger<GradeService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    // Get grades for students in a specific class and school year
    public async Task<List<GradeWithStudentInfo>> GetGradesByClassAndSchoolYearAsync(int classId, string schoolYear)
    {
        try
        {
            return await _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Class)
                .Where(g => g.ClassId == classId && g.SchoolYear == schoolYear)
                .Select(g => new GradeWithStudentInfo
                {
                    GradeId = g.GradeId,
                    StudentId = g.Student!.StudentId,
                    StudentName = $"{g.Student.FirstName} {g.Student.MiddleName} {g.Student.LastName}".Trim() + 
                                 (string.IsNullOrEmpty(g.Student.Suffix) ? "" : $" {g.Student.Suffix}"),
                    FirstQuarter = g.FirstQuarter.HasValue ? (double?)g.FirstQuarter.Value : null,
                    SecondQuarter = g.SecondQuarter.HasValue ? (double?)g.SecondQuarter.Value : null,
                    ThirdQuarter = g.ThirdQuarter.HasValue ? (double?)g.ThirdQuarter.Value : null,
                    FourthQuarter = g.FourthQuarter.HasValue ? (double?)g.FourthQuarter.Value : null,
                    FinalGrade = g.FinalGrade.HasValue ? (double?)g.FinalGrade.Value : null,
                    Remarks = g.Remarks ?? string.Empty
                })
                .OrderBy(g => g.StudentName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting grades for class {ClassId} and school year {SchoolYear}", classId, schoolYear);
            throw;
        }
    }

    // Get grades for a specific student in a class
    public async Task<Grade?> GetGradeByClassStudentAndYearAsync(int classId, string studentId, string schoolYear)
    {
        try
        {
            return await _context.Grades
                .Include(g => g.Student)
                .Include(g => g.Class)
                .FirstOrDefaultAsync(g => g.ClassId == classId && 
                                         g.StudentId == studentId && 
                                         g.SchoolYear == schoolYear);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting grade for class {ClassId}, student {StudentId}, year {SchoolYear}", 
                classId, studentId, schoolYear);
            throw;
        }
    }

    // Create or update a grade
    public async Task<Grade> SaveGradeAsync(Grade grade, string? createdBy = null, string? updatedBy = null)
    {
        try
        {
            // Calculate final grade as average of quarters
            var quarters = new List<decimal?>
            {
                grade.FirstQuarter,
                grade.SecondQuarter,
                grade.ThirdQuarter,
                grade.FourthQuarter
            }.Where(q => q.HasValue).ToList();

            if (quarters.Any())
            {
                grade.FinalGrade = (decimal)quarters.Average(q => q!.Value);
                
                // Determine remarks based on final grade
                if (quarters.Count < 4)
                {
                    grade.Remarks = "INCOMPLETE";
                }
                else if (grade.FinalGrade >= 75)
                {
                    grade.Remarks = "PASSED";
                }
                else
                {
                    grade.Remarks = "FAILED";
                }
            }
            else
            {
                grade.FinalGrade = null;
                grade.Remarks = "INCOMPLETE";
            }

            var existingGrade = await _context.Grades
                .FirstOrDefaultAsync(g => g.ClassId == grade.ClassId && 
                                         g.StudentId == grade.StudentId && 
                                         g.SchoolYear == grade.SchoolYear);

            if (existingGrade == null)
            {
                // Create new grade
                grade.CreatedDate = DateTime.Now;
                grade.CreatedBy = createdBy;
                _context.Grades.Add(grade);
                _logger?.LogInformation("Grade created for student {StudentId} in class {ClassId}", 
                    grade.StudentId, grade.ClassId);
            }
            else
            {
                // Update existing grade
                existingGrade.FirstQuarter = grade.FirstQuarter;
                existingGrade.SecondQuarter = grade.SecondQuarter;
                existingGrade.ThirdQuarter = grade.ThirdQuarter;
                existingGrade.FourthQuarter = grade.FourthQuarter;
                existingGrade.FinalGrade = grade.FinalGrade;
                existingGrade.Remarks = grade.Remarks;
                existingGrade.UpdatedDate = DateTime.Now;
                existingGrade.UpdatedBy = updatedBy;
                _logger?.LogInformation("Grade updated for student {StudentId} in class {ClassId}", 
                    grade.StudentId, grade.ClassId);
            }

            await _context.SaveChangesAsync();
            return existingGrade ?? grade;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving grade for student {StudentId} in class {ClassId}", 
                grade.StudentId, grade.ClassId);
            throw;
        }
    }

    // Save multiple grades at once
    public async Task<List<Grade>> SaveGradesAsync(List<Grade> grades, string? createdBy = null, string? updatedBy = null)
    {
        try
        {
            var savedGrades = new List<Grade>();

            foreach (var grade in grades)
            {
                // Calculate final grade for each
                var quarters = new List<decimal?>
                {
                    grade.FirstQuarter,
                    grade.SecondQuarter,
                    grade.ThirdQuarter,
                    grade.FourthQuarter
                }.Where(q => q.HasValue).ToList();

                if (quarters.Any())
                {
                    grade.FinalGrade = (decimal)quarters.Average(q => q!.Value);
                    
                    if (quarters.Count < 4)
                    {
                        grade.Remarks = "INCOMPLETE";
                    }
                    else if (grade.FinalGrade >= 75)
                    {
                        grade.Remarks = "PASSED";
                    }
                    else
                    {
                        grade.Remarks = "FAILED";
                    }
                }
                else
                {
                    grade.FinalGrade = null;
                    grade.Remarks = "INCOMPLETE";
                }

                var existingGrade = await _context.Grades
                    .FirstOrDefaultAsync(g => g.ClassId == grade.ClassId && 
                                             g.StudentId == grade.StudentId && 
                                             g.SchoolYear == grade.SchoolYear);

                if (existingGrade == null)
                {
                    grade.CreatedDate = DateTime.Now;
                    grade.CreatedBy = createdBy;
                    _context.Grades.Add(grade);
                    savedGrades.Add(grade);
                }
                else
                {
                    existingGrade.FirstQuarter = grade.FirstQuarter;
                    existingGrade.SecondQuarter = grade.SecondQuarter;
                    existingGrade.ThirdQuarter = grade.ThirdQuarter;
                    existingGrade.FourthQuarter = grade.FourthQuarter;
                    existingGrade.FinalGrade = grade.FinalGrade;
                    existingGrade.Remarks = grade.Remarks;
                    existingGrade.UpdatedDate = DateTime.Now;
                    existingGrade.UpdatedBy = updatedBy;
                    savedGrades.Add(existingGrade);
                }
            }

            await _context.SaveChangesAsync();
            _logger?.LogInformation("Saved {Count} grades", savedGrades.Count);
            return savedGrades;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving multiple grades");
            throw;
        }
    }

    // Get students enrolled in a class who don't have grades yet
    public async Task<List<StudentForGradeEntry>> GetStudentsWithoutGradesAsync(int classId, string schoolYear)
    {
        try
        {
            var enrolledStudents = await _context.ClassEnrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                .Where(e => e.ClassId == classId && 
                           e.IsActive && 
                           e.Student != null)
                .Select(e => e.Student!.StudentId)
                .ToListAsync();

            var studentsWithGrades = await _context.Grades
                .Where(g => g.ClassId == classId && g.SchoolYear == schoolYear)
                .Select(g => g.StudentId)
                .ToListAsync();

            var studentsWithoutGrades = enrolledStudents
                .Except(studentsWithGrades)
                .ToList();

            return await _context.Students
                .Where(s => studentsWithoutGrades.Contains(s.StudentId))
                .Select(s => new StudentForGradeEntry
                {
                    StudentId = s.StudentId,
                    StudentName = $"{s.FirstName} {s.MiddleName} {s.LastName}".Trim() + 
                                 (string.IsNullOrEmpty(s.Suffix) ? "" : $" {s.Suffix}")
                })
                .OrderBy(s => s.StudentName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting students without grades for class {ClassId}", classId);
            throw;
        }
    }

    // Get count of pending grades (students without complete grades) for a teacher
    public async Task<int> GetPendingGradesCountAsync(int teacherId, string? schoolYear = null)
    {
        try
        {
            // Get all classes for the teacher
            var teacherClassIds = await _context.Classes
                .Where(c => c.TeacherId == teacherId && c.Status == "Active")
                .Select(c => c.ClassId)
                .ToListAsync();

            if (!teacherClassIds.Any())
            {
                return 0;
            }

            // Get current school year if not provided
            if (string.IsNullOrWhiteSpace(schoolYear))
            {
                var currentYear = DateTime.Now.Year;
                var month = DateTime.Now.Month;
                // Assume school year starts in June/July
                if (month >= 6)
                {
                    schoolYear = $"{currentYear}-{currentYear + 1}";
                }
                else
                {
                    schoolYear = $"{currentYear - 1}-{currentYear}";
                }
            }

            // Count students enrolled in teacher's classes
            var enrolledStudents = await _context.ClassEnrollments
                .Where(e => teacherClassIds.Contains(e.ClassId) && 
                           e.IsActive)
                .Select(e => new { e.ClassId, e.StudentId })
                .ToListAsync();

            // Count students with incomplete grades (missing any quarter or no grade record)
            var studentsWithCompleteGrades = await _context.Grades
                .Where(g => teacherClassIds.Contains(g.ClassId) && 
                           g.SchoolYear == schoolYear &&
                           g.FirstQuarter.HasValue &&
                           g.SecondQuarter.HasValue &&
                           g.ThirdQuarter.HasValue &&
                           g.FourthQuarter.HasValue)
                .Select(g => new { g.ClassId, g.StudentId })
                .Distinct()
                .ToListAsync();

            // Count pending: enrolled students without complete grades
            var pendingCount = enrolledStudents
                .Except(studentsWithCompleteGrades)
                .Count();

            return pendingCount;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting pending grades count for teacher {TeacherId}", teacherId);
            throw;
        }
    }
}

// DTO for grade with student information
public class GradeWithStudentInfo
{
    public int GradeId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public double? FirstQuarter { get; set; }
    public double? SecondQuarter { get; set; }
    public double? ThirdQuarter { get; set; }
    public double? FourthQuarter { get; set; }
    public double? FinalGrade { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

// DTO for student without grade entry
public class StudentForGradeEntry
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
}

