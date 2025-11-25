using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services;

// Handles class management for teachers
public class ClassService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ClassService>? _logger;

    public ClassService(AppDbContext context, ILogger<ClassService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    // Get all classes for a specific teacher
    public async Task<List<Class>> GetClassesByTeacherIdAsync(int teacherId)
    {
        try
        {
            return await _context.Classes
                .Where(c => c.TeacherId == teacherId)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting classes for teacher {TeacherId}", teacherId);
            throw;
        }
    }

    // Get classes filtered by school year and search term
    public async Task<List<Class>> GetClassesByTeacherIdFilteredAsync(int teacherId, string? schoolYear = null, string? searchTerm = null)
    {
        try
        {
            var query = _context.Classes
                .Where(c => c.TeacherId == teacherId);

            if (!string.IsNullOrWhiteSpace(schoolYear))
            {
                query = query.Where(c => c.SchoolYear == schoolYear);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c =>
                    c.Subject.Contains(searchTerm) ||
                    c.GradeLevel.Contains(searchTerm) ||
                    c.Section.Contains(searchTerm));
            }

            return await query
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting filtered classes for teacher {TeacherId}", teacherId);
            throw;
        }
    }

    // Get a single class by ID
    public async Task<Class?> GetClassByIdAsync(int classId)
    {
        try
        {
            return await _context.Classes
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.ClassId == classId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting class {ClassId}", classId);
            throw;
        }
    }

    // Create a new class
    public async Task<Class> CreateClassAsync(Class classEntity)
    {
        try
        {
            classEntity.CreatedDate = DateTime.Now;
            _context.Classes.Add(classEntity);
            await _context.SaveChangesAsync();
            
            _logger?.LogInformation("Class created with ID: {ClassId}", classEntity.ClassId);
            return classEntity;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating class");
            throw;
        }
    }

    // Update an existing class
    public async Task<Class> UpdateClassAsync(Class classEntity)
    {
        try
        {
            classEntity.UpdatedDate = DateTime.Now;
            _context.Classes.Update(classEntity);
            await _context.SaveChangesAsync();
            
            _logger?.LogInformation("Class updated with ID: {ClassId}", classEntity.ClassId);
            return classEntity;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating class {ClassId}", classEntity.ClassId);
            throw;
        }
    }

    // Delete a class
    public async Task<bool> DeleteClassAsync(int classId)
    {
        try
        {
            var classEntity = await _context.Classes.FindAsync(classId);
            if (classEntity == null)
            {
                return false;
            }

            _context.Classes.Remove(classEntity);
            await _context.SaveChangesAsync();
            
            _logger?.LogInformation("Class deleted with ID: {ClassId}", classId);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting class {ClassId}", classId);
            throw;
        }
    }

    // Update student count for a class
    public async Task UpdateStudentCountAsync(int classId, int studentCount)
    {
        try
        {
            var classEntity = await _context.Classes.FindAsync(classId);
            if (classEntity != null)
            {
                classEntity.StudentCount = studentCount;
                classEntity.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating student count for class {ClassId}", classId);
            throw;
        }
    }

    // Get all students enrolled in a teacher's classes
    public async Task<List<StudentWithClassInfo>> GetStudentsByTeacherIdAsync(int teacherId)
    {
        try
        {
            return await _context.ClassEnrollments
                .Include(e => e.Class)
                .Include(e => e.Student)
                    .ThenInclude(s => s.Guardian)
                .Where(e => e.Class != null && 
                           e.Class.TeacherId == teacherId && 
                           e.IsActive && 
                           e.Student != null)
                .Select(e => new StudentWithClassInfo
                {
                    StudentId = e.Student!.StudentId,
                    FirstName = e.Student.FirstName,
                    MiddleName = e.Student.MiddleName,
                    LastName = e.Student.LastName,
                    Suffix = e.Student.Suffix ?? string.Empty,
                    GradeLevel = e.Class!.GradeLevel,
                    Section = e.Class.Section,
                    Subject = e.Class.Subject,
                    ClassId = e.Class.ClassId,
                    ContactNumber = e.Student.Guardian != null ? e.Student.Guardian.ContactNum ?? string.Empty : string.Empty,
                    Status = e.Student.Status,
                    EnrollmentStatus = e.Status
                })
                .Distinct()
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting students for teacher {TeacherId}", teacherId);
            throw;
        }
    }

    // Get students filtered by class and subject
    public async Task<List<StudentWithClassInfo>> GetStudentsByTeacherIdFilteredAsync(
        int teacherId, 
        int? classId = null, 
        string? subject = null, 
        string? searchTerm = null)
    {
        try
        {
            var query = _context.ClassEnrollments
                .Include(e => e.Class)
                .Include(e => e.Student)
                    .ThenInclude(s => s.Guardian)
                .Where(e => e.Class != null && 
                           e.Class.TeacherId == teacherId && 
                           e.IsActive && 
                           e.Student != null);

            if (classId.HasValue)
            {
                query = query.Where(e => e.ClassId == classId.Value);
            }

            if (!string.IsNullOrWhiteSpace(subject))
            {
                query = query.Where(e => e.Class!.Subject == subject);
            }

            var students = await query
                .Select(e => new StudentWithClassInfo
                {
                    StudentId = e.Student!.StudentId,
                    FirstName = e.Student.FirstName,
                    MiddleName = e.Student.MiddleName,
                    LastName = e.Student.LastName,
                    Suffix = e.Student.Suffix ?? string.Empty,
                    GradeLevel = e.Class!.GradeLevel,
                    Section = e.Class.Section,
                    Subject = e.Class.Subject,
                    ClassId = e.Class.ClassId,
                    ContactNumber = e.Student.Guardian != null ? e.Student.Guardian.ContactNum ?? string.Empty : string.Empty,
                    Status = e.Student.Status,
                    EnrollmentStatus = e.Status
                })
                .GroupBy(s => s.StudentId)
                .Select(g => g.First())
                .ToListAsync();

            // Apply search filter in memory (after getting distinct students)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                students = students.Where(s =>
                    s.StudentId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    $"{s.FirstName} {s.MiddleName} {s.LastName}".Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    s.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    s.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            return students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting filtered students for teacher {TeacherId}", teacherId);
            throw;
        }
    }

    // Get total number of classes for a teacher
    public async Task<int> GetTotalClassesCountAsync(int teacherId)
    {
        try
        {
            return await _context.Classes
                .Where(c => c.TeacherId == teacherId && c.Status == "Active")
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting total classes count for teacher {TeacherId}", teacherId);
            throw;
        }
    }

    // Get total number of unique students enrolled in teacher's classes
    public async Task<int> GetTotalStudentsCountAsync(int teacherId)
    {
        try
        {
            return await _context.ClassEnrollments
                .Include(e => e.Class)
                .Where(e => e.Class != null && 
                           e.Class.TeacherId == teacherId && 
                           e.IsActive)
                .Select(e => e.StudentId)
                .Distinct()
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting total students count for teacher {TeacherId}", teacherId);
            throw;
        }
    }
}

// DTO for student with class information
public class StudentWithClassInfo
{
    public string StudentId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ContactNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string EnrollmentStatus { get; set; } = string.Empty;
    
    public string FullName => 
        string.IsNullOrWhiteSpace(Suffix) 
            ? $"{FirstName} {MiddleName} {LastName}".Trim()
            : $"{FirstName} {MiddleName} {LastName} {Suffix}".Trim();
}

