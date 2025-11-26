using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services;

// Handles class assignment operations for teachers
public class ClassAssignmentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ClassAssignmentService>? _logger;

    public ClassAssignmentService(AppDbContext context, ILogger<ClassAssignmentService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    // Gets all class assignments for a specific teacher
    public async Task<List<ClassAssignmentDto>> GetTeacherClassAssignmentsAsync(int teacherId, string? schoolYear = null)
    {
        try
        {
            var query = _context.ClassAssignments
                .Include(c => c.Teacher)
                .Where(c => c.TeacherId == teacherId);

            if (!string.IsNullOrWhiteSpace(schoolYear))
            {
                query = query.Where(c => c.SchoolYear == schoolYear);
            }

            var assignments = await query
                .OrderBy(c => c.SchoolYear)
                .ThenBy(c => c.GradeLevel)
                .ThenBy(c => c.Subject)
                .ToListAsync();

            var result = new List<ClassAssignmentDto>();

            foreach (var assignment in assignments)
            {
                // Get student count for this class
                var studentCount = await GetStudentCountForClassAsync(
                    assignment.SchoolYear,
                    assignment.GradeLevel,
                    assignment.Section);

                result.Add(new ClassAssignmentDto
                {
                    Id = assignment.AssignmentId.ToString(),
                    Subject = assignment.Subject,
                    GradeLevel = assignment.GradeLevel,
                    Section = assignment.Section ?? "N/A",
                    StudentCount = studentCount,
                    Schedule = assignment.Schedule ?? "TBA",
                    Room = assignment.Room ?? "TBA",
                    Status = assignment.Status,
                    SchoolYear = assignment.SchoolYear
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching teacher class assignments: {Message}", ex.Message);
            throw new Exception($"Failed to fetch class assignments: {ex.Message}", ex);
        }
    }

    // Gets student count for a specific class (school year, grade level, section)
    private async Task<int> GetStudentCountForClassAsync(string schoolYear, string gradeLevel, string? section)
    {
        try
        {
            var query = _context.Students
                .Where(s => s.SchoolYr == schoolYear && s.GradeLevel == gradeLevel);

            // Note: Section filtering would need to be added to Student model if sections are stored
            // For now, we'll count all students in the grade level for the school year
            // This can be enhanced later when section is properly linked to students

            return await query.CountAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error counting students for class: {Message}", ex.Message);
            return 0;
        }
    }

    // Gets all class assignments (for admin use)
    public async Task<List<ClassAssignmentDto>> GetAllClassAssignmentsAsync(string? schoolYear = null)
    {
        try
        {
            IQueryable<ClassAssignment> query = _context.ClassAssignments;

            if (!string.IsNullOrWhiteSpace(schoolYear))
            {
                query = query.Where(c => c.SchoolYear == schoolYear);
            }

            var assignments = await query
                .Include(c => c.Teacher)
                .OrderBy(c => c.SchoolYear)
                .ThenBy(c => c.GradeLevel)
                .ThenBy(c => c.Subject)
                .ToListAsync();

            var result = new List<ClassAssignmentDto>();

            foreach (var assignment in assignments)
            {
                var studentCount = await GetStudentCountForClassAsync(
                    assignment.SchoolYear,
                    assignment.GradeLevel,
                    assignment.Section);

                result.Add(new ClassAssignmentDto
                {
                    Id = assignment.AssignmentId.ToString(),
                    Subject = assignment.Subject,
                    GradeLevel = assignment.GradeLevel,
                    Section = assignment.Section ?? "N/A",
                    StudentCount = studentCount,
                    Schedule = assignment.Schedule ?? "TBA",
                    Room = assignment.Room ?? "TBA",
                    Status = assignment.Status,
                    SchoolYear = assignment.SchoolYear
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching all class assignments: {Message}", ex.Message);
            throw new Exception($"Failed to fetch class assignments: {ex.Message}", ex);
        }
    }

    // Creates a new class assignment
    public async Task<ClassAssignment> CreateClassAssignmentAsync(
        int teacherId,
        string schoolYear,
        string gradeLevel,
        string? section,
        string subject,
        string? schedule = null,
        string? room = null,
        string status = "Active",
        string? createdBy = null)
    {
        try
        {
            // Check if assignment already exists
            var existing = await _context.ClassAssignments
                .FirstOrDefaultAsync(c => c.TeacherId == teacherId
                    && c.SchoolYear == schoolYear
                    && c.GradeLevel == gradeLevel
                    && c.Section == section
                    && c.Subject == subject);

            if (existing != null)
            {
                throw new Exception("Class assignment already exists for this teacher, school year, grade level, section, and subject.");
            }

            var assignment = new ClassAssignment
            {
                TeacherId = teacherId,
                SchoolYear = schoolYear,
                GradeLevel = gradeLevel,
                Section = section,
                Subject = subject,
                Schedule = schedule,
                Room = room,
                Status = status,
                CreatedDate = DateTime.Now,
                CreatedBy = createdBy
            };

            _context.ClassAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Class assignment created: {AssignmentId}", assignment.AssignmentId);
            return assignment;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating class assignment: {Message}", ex.Message);
            throw new Exception($"Failed to create class assignment: {ex.Message}", ex);
        }
    }

    // Updates an existing class assignment
    public async Task<ClassAssignment> UpdateClassAssignmentAsync(
        int assignmentId,
        string? schedule = null,
        string? room = null,
        string? status = null,
        string? updatedBy = null)
    {
        try
        {
            var assignment = await _context.ClassAssignments
                .FirstOrDefaultAsync(c => c.AssignmentId == assignmentId);

            if (assignment == null)
            {
                throw new Exception($"Class assignment with ID {assignmentId} not found.");
            }

            if (!string.IsNullOrWhiteSpace(schedule))
                assignment.Schedule = schedule;
            if (!string.IsNullOrWhiteSpace(room))
                assignment.Room = room;
            if (!string.IsNullOrWhiteSpace(status))
                assignment.Status = status;
            
            assignment.UpdatedDate = DateTime.Now;
            assignment.UpdatedBy = updatedBy;

            await _context.SaveChangesAsync();

            _logger?.LogInformation("Class assignment updated: {AssignmentId}", assignmentId);
            return assignment;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating class assignment: {Message}", ex.Message);
            throw new Exception($"Failed to update class assignment: {ex.Message}", ex);
        }
    }

    // Deletes a class assignment
    public async Task<bool> DeleteClassAssignmentAsync(int assignmentId)
    {
        try
        {
            var assignment = await _context.ClassAssignments
                .FirstOrDefaultAsync(c => c.AssignmentId == assignmentId);

            if (assignment == null)
            {
                return false;
            }

            _context.ClassAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Class assignment deleted: {AssignmentId}", assignmentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting class assignment: {Message}", ex.Message);
            throw new Exception($"Failed to delete class assignment: {ex.Message}", ex);
        }
    }

    // Gets available school years from class assignments
    public async Task<List<string>> GetAvailableSchoolYearsAsync()
    {
        try
        {
            var schoolYears = await _context.ClassAssignments
                .Where(c => !string.IsNullOrWhiteSpace(c.SchoolYear))
                .Select(c => c.SchoolYear)
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

    // Gets teacher schedule items for display in schedule view
    public async Task<List<ScheduleItemDto>> GetTeacherScheduleItemsAsync(int teacherId)
    {
        try
        {
            // Get all active class assignments with their schedules in one query
            var assignments = await _context.ClassAssignments
                .Include(c => c.Schedules)
                .Where(c => c.TeacherId == teacherId && c.Status == "Active")
                .ToListAsync();

            var scheduleItems = new List<ScheduleItemDto>();

            // Build schedule items from assignments and their schedules
            foreach (var assignment in assignments)
            {
                foreach (var schedule in assignment.Schedules)
                {
                    scheduleItems.Add(new ScheduleItemDto
                    {
                        Day = schedule.DayOfWeek,
                        StartTime = schedule.StartTime,
                        EndTime = schedule.EndTime,
                        Subject = assignment.Subject,
                        Class = $"{assignment.GradeLevel}-{assignment.Section ?? "N/A"}",
                        Room = assignment.Room ?? "TBA"
                    });
                }
            }

            return scheduleItems.OrderBy(s => GetDayOrder(s.Day)).ThenBy(s => s.StartTime).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching teacher schedule items: {Message}", ex.Message);
            throw new Exception($"Failed to fetch schedule items: {ex.Message}", ex);
        }
    }

    // Helper method to get day order for sorting
    private int GetDayOrder(string day)
    {
        return day.ToLower() switch
        {
            "monday" => 1,
            "tuesday" => 2,
            "wednesday" => 3,
            "thursday" => 4,
            "friday" => 5,
            "saturday" => 6,
            "sunday" => 7,
            _ => 99
        };
    }

    // Creates schedule entries for a class assignment
    public async Task<List<TeacherSchedule>> CreateScheduleEntriesAsync(
        int assignmentId,
        List<ScheduleEntryData> scheduleEntries,
        string? createdBy = null)
    {
        try
        {
            // Delete existing schedules for this assignment
            var existingSchedules = await _context.TeacherSchedules
                .Where(s => s.AssignmentId == assignmentId)
                .ToListAsync();

            if (existingSchedules.Any())
            {
                _context.TeacherSchedules.RemoveRange(existingSchedules);
            }

            // Create new schedule entries
            var newSchedules = scheduleEntries.Select(e => new TeacherSchedule
            {
                AssignmentId = assignmentId,
                DayOfWeek = e.DayOfWeek,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                CreatedDate = DateTime.Now,
                CreatedBy = createdBy
            }).ToList();

            _context.TeacherSchedules.AddRange(newSchedules);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Created {Count} schedule entries for assignment {AssignmentId}", newSchedules.Count, assignmentId);
            return newSchedules;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating schedule entries: {Message}", ex.Message);
            throw new Exception($"Failed to create schedule entries: {ex.Message}", ex);
        }
    }

    // Parses schedule string (e.g., "MWF 8:00-9:00 AM") and creates schedule entries
    public async Task<List<TeacherSchedule>> ParseAndCreateScheduleAsync(
        int assignmentId,
        string scheduleString,
        string? createdBy = null)
    {
        try
        {
            var scheduleEntries = ParseScheduleString(scheduleString);
            return await CreateScheduleEntriesAsync(assignmentId, scheduleEntries, createdBy);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error parsing and creating schedule: {Message}", ex.Message);
            throw new Exception($"Failed to parse schedule: {ex.Message}", ex);
        }
    }

    // Helper method to parse schedule string like "MWF 8:00-9:00 AM" or "TTH 10:00-11:30 AM"
    private List<ScheduleEntryData> ParseScheduleString(string scheduleString)
    {
        var entries = new List<ScheduleEntryData>();

        if (string.IsNullOrWhiteSpace(scheduleString))
            return entries;

        // Common patterns: "MWF 8:00-9:00 AM", "TTH 10:00-11:30 AM", "Monday 8:00-9:00 AM"
        var parts = scheduleString.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length < 2)
            return entries;

        // Extract time range (last parts)
        var timePart = string.Join(" ", parts.Skip(1));
        var timeMatch = System.Text.RegularExpressions.Regex.Match(timePart, @"(\d{1,2}:\d{2})\s*-\s*(\d{1,2}:\d{2})\s*(AM|PM)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        if (!timeMatch.Success)
            return entries;

        var startTime = $"{timeMatch.Groups[1].Value} {timeMatch.Groups[3].Value}";
        var endTime = $"{timeMatch.Groups[2].Value} {timeMatch.Groups[3].Value}";

        // Parse days
        var daysPart = parts[0].ToUpper();
        
        // Map day abbreviations to full names
        var dayMap = new Dictionary<string, List<string>>
        {
            { "M", new List<string> { "Monday" } },
            { "T", new List<string> { "Tuesday" } },
            { "W", new List<string> { "Wednesday" } },
            { "TH", new List<string> { "Thursday" } },
            { "F", new List<string> { "Friday" } },
            { "S", new List<string> { "Saturday" } },
            { "SU", new List<string> { "Sunday" } },
            { "MWF", new List<string> { "Monday", "Wednesday", "Friday" } },
            { "TTH", new List<string> { "Tuesday", "Thursday" } },
            { "MTWTHF", new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" } }
        };

        // Check for full day names
        var fullDayNames = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        if (fullDayNames.Any(d => daysPart.Contains(d, StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var day in fullDayNames)
            {
                if (daysPart.Contains(day, StringComparison.OrdinalIgnoreCase))
                {
                    entries.Add(new ScheduleEntryData
                    {
                        DayOfWeek = day,
                        StartTime = startTime,
                        EndTime = endTime
                    });
                }
            }
        }
        else if (dayMap.ContainsKey(daysPart))
        {
            foreach (var day in dayMap[daysPart])
            {
                entries.Add(new ScheduleEntryData
                {
                    DayOfWeek = day,
                    StartTime = startTime,
                    EndTime = endTime
                });
            }
        }
        else
        {
            // Try to parse individual characters
            foreach (var ch in daysPart)
            {
                var dayChar = ch.ToString();
                if (dayMap.ContainsKey(dayChar))
                {
                    foreach (var day in dayMap[dayChar])
                    {
                        entries.Add(new ScheduleEntryData
                        {
                            DayOfWeek = day,
                            StartTime = startTime,
                            EndTime = endTime
                        });
                    }
                }
            }
        }

        return entries;
    }

    // Gets students assigned to a teacher's classes
    public async Task<List<TeacherStudentDto>> GetTeacherStudentsAsync(
        int teacherId,
        string? schoolYear = null,
        string? gradeLevel = null,
        string? section = null,
        string? subject = null)
    {
        try
        {
            // Get teacher's class assignments
            var assignmentsQuery = _context.ClassAssignments
                .Where(c => c.TeacherId == teacherId && c.Status == "Active");

            if (!string.IsNullOrWhiteSpace(schoolYear))
                assignmentsQuery = assignmentsQuery.Where(c => c.SchoolYear == schoolYear);
            if (!string.IsNullOrWhiteSpace(gradeLevel))
                assignmentsQuery = assignmentsQuery.Where(c => c.GradeLevel == gradeLevel);
            if (!string.IsNullOrWhiteSpace(section))
                assignmentsQuery = assignmentsQuery.Where(c => c.Section == section);
            if (!string.IsNullOrWhiteSpace(subject))
                assignmentsQuery = assignmentsQuery.Where(c => c.Subject == subject);

            var assignments = await assignmentsQuery.ToListAsync();

            if (!assignments.Any())
            {
                return new List<TeacherStudentDto>();
            }

            // Get unique grade levels and school years from assignments
            var gradeLevels = assignments.Select(a => a.GradeLevel).Distinct().ToList();
            var schoolYears = assignments.Select(a => a.SchoolYear).Distinct().ToList();

            // Get students matching the grade levels and school years
            var studentsQuery = _context.Students
                .Include(s => s.Guardian)
                .Where(s => gradeLevels.Contains(s.GradeLevel ?? "") 
                    && schoolYears.Contains(s.SchoolYr ?? ""));

            var students = await studentsQuery.ToListAsync();

            // Get student grades to calculate averages
            var studentIds = students.Select(s => s.StudentId).ToList();
            var studentGrades = await _context.StudentGrades
                .Where(g => studentIds.Contains(g.StudentId) 
                    && (!string.IsNullOrWhiteSpace(schoolYear) ? g.SchoolYear == schoolYear : true))
                .ToListAsync();

            // Build result with averages
            var result = new List<TeacherStudentDto>();

            foreach (var student in students)
            {
                // Find matching assignment to get section
                var matchingAssignment = assignments.FirstOrDefault(a => 
                    a.GradeLevel == student.GradeLevel && 
                    a.SchoolYear == student.SchoolYr);

                // Calculate average from all grades for this student
                var grades = studentGrades
                    .Where(g => g.StudentId == student.StudentId)
                    .Select(g => g.FinalGrade)
                    .Where(g => g.HasValue)
                    .Select(g => (double)g!.Value)
                    .ToList();

                var average = grades.Any() ? grades.Average() : 0.0;

                // Get guardian contact
                var contactNumber = student.Guardian?.ContactNum ?? "N/A";

                result.Add(new TeacherStudentDto
                {
                    StudentId = student.StudentId,
                    Name = $"{student.FirstName} {student.MiddleName} {student.LastName}".Trim(),
                    Email = "", // Email not stored in Student model - could be added later
                    GradeLevel = student.GradeLevel ?? "N/A",
                    Section = matchingAssignment?.Section ?? "N/A",
                    ContactNumber = contactNumber,
                    Average = average,
                    Status = student.Status ?? "Active"
                });
            }

            return result.OrderBy(s => s.GradeLevel).ThenBy(s => s.Name).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching teacher students: {Message}", ex.Message);
            throw new Exception($"Failed to fetch students: {ex.Message}", ex);
        }
    }

    // Gets available classes (grade level - section combinations) for a teacher
    public async Task<List<string>> GetTeacherClassesAsync(int teacherId, string? schoolYear = null)
    {
        try
        {
            var query = _context.ClassAssignments
                .Where(c => c.TeacherId == teacherId && c.Status == "Active");

            if (!string.IsNullOrWhiteSpace(schoolYear))
            {
                query = query.Where(c => c.SchoolYear == schoolYear);
            }

            var assignments = await query
                .Select(c => new { c.GradeLevel, c.Section })
                .Distinct()
                .ToListAsync();

            return assignments
                .Select(a => $"{a.GradeLevel} - {a.Section ?? "N/A"}")
                .OrderBy(c => c)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching teacher classes: {Message}", ex.Message);
            return new List<string>();
        }
    }

    // Gets available subjects for a teacher
    public async Task<List<string>> GetTeacherSubjectsAsync(int teacherId, string? schoolYear = null)
    {
        try
        {
            var query = _context.ClassAssignments
                .Where(c => c.TeacherId == teacherId && c.Status == "Active");

            if (!string.IsNullOrWhiteSpace(schoolYear))
            {
                query = query.Where(c => c.SchoolYear == schoolYear);
            }

            var subjects = await query
                .Select(c => c.Subject)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            return subjects;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching teacher subjects: {Message}", ex.Message);
            return new List<string>();
        }
    }
}

// DTO for schedule item display
public class ScheduleItemDto
{
    public string Day { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
}

// Data class for creating schedule entries
public class ScheduleEntryData
{
    public string DayOfWeek { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}

// DTO for teacher student display
public class TeacherStudentDto
{
    public string StudentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public double Average { get; set; }
    public string Status { get; set; } = string.Empty;
}

// DTO for class assignment display
public class ClassAssignmentDto
{
    public string Id { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public string Schedule { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SchoolYear { get; set; } = string.Empty;
}

