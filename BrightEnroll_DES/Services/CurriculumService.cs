using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services;

public class CurriculumService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CurriculumService>? _logger;

    public CurriculumService(AppDbContext context, ILogger<CurriculumService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    #region Classroom Operations

    public async Task<List<Classroom>> GetAllClassroomsAsync()
    {
        return await _context.Classrooms
            .OrderBy(c => c.BuildingName)
            .ThenBy(c => c.FloorNumber)
            .ThenBy(c => c.RoomName)
            .ToListAsync();
    }

    public async Task<Classroom?> GetClassroomByIdAsync(int roomId)
    {
        return await _context.Classrooms.FindAsync(roomId);
    }

    public async Task<Classroom> CreateClassroomAsync(Classroom classroom)
    {
        classroom.CreatedAt = DateTime.Now;
        _context.Classrooms.Add(classroom);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Classroom created: {RoomName}", classroom.RoomName);
        return classroom;
    }

    public async Task<Classroom> UpdateClassroomAsync(Classroom classroom)
    {
        classroom.UpdatedAt = DateTime.Now;
        _context.Classrooms.Update(classroom);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Classroom updated: {RoomName}", classroom.RoomName);
        return classroom;
    }

    public async Task<bool> DeleteClassroomAsync(int roomId)
    {
        var classroom = await _context.Classrooms.FindAsync(roomId);
        if (classroom == null) return false;

        // Check if classroom is used in sections or schedules
        var isUsedInSections = await _context.Sections.AnyAsync(s => s.ClassroomId == roomId);
        var isUsedInSchedules = await _context.ClassSchedules.AnyAsync(s => s.RoomId == roomId);

        if (isUsedInSections || isUsedInSchedules)
        {
            // Instead of deleting, mark as inactive
            classroom.Status = "Inactive";
            classroom.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        _context.Classrooms.Remove(classroom);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Classroom deleted: {RoomName}", classroom.RoomName);
        return true;
    }

    #endregion

    #region Section Operations

    public async Task<List<Section>> GetAllSectionsAsync()
    {
        return await _context.Sections
            .Include(s => s.GradeLevel)
            .Include(s => s.Classroom)
            .OrderBy(s => s.GradeLevelId)
            .ThenBy(s => s.SectionName)
            .ToListAsync();
    }

    public async Task<Section?> GetSectionByIdAsync(int sectionId)
    {
        return await _context.Sections
            .Include(s => s.GradeLevel)
            .Include(s => s.Classroom)
            .FirstOrDefaultAsync(s => s.SectionId == sectionId);
    }

    public async Task<Section> CreateSectionAsync(Section section)
    {
        section.CreatedAt = DateTime.Now;
        _context.Sections.Add(section);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Section created: {SectionName}", section.SectionName);
        return section;
    }

    public async Task<Section> UpdateSectionAsync(Section section)
    {
        section.UpdatedAt = DateTime.Now;
        _context.Sections.Update(section);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Section updated: {SectionName}", section.SectionName);
        return section;
    }

    public async Task<bool> DeleteSectionAsync(int sectionId)
    {
        var section = await _context.Sections.FindAsync(sectionId);
        if (section == null) return false;

        _context.Sections.Remove(section);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Section deleted: {SectionName}", section.SectionName);
        return true;
    }

    #endregion

    #region Subject Operations

    public async Task<List<Subject>> GetAllSubjectsAsync()
    {
        return await _context.Subjects
            .Include(s => s.GradeLevel)
            .OrderBy(s => s.GradeLevelId)
            .ThenBy(s => s.SubjectName)
            .ToListAsync();
    }

    public async Task<Subject?> GetSubjectByIdAsync(int subjectId)
    {
        return await _context.Subjects
            .Include(s => s.GradeLevel)
            .FirstOrDefaultAsync(s => s.SubjectId == subjectId);
    }

    public async Task<Subject> CreateSubjectAsync(Subject subject)
    {
        subject.CreatedAt = DateTime.Now;
        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Subject created: {SubjectName}", subject.SubjectName);
        return subject;
    }

    public async Task<Subject> UpdateSubjectAsync(Subject subject)
    {
        subject.UpdatedAt = DateTime.Now;
        _context.Subjects.Update(subject);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Subject updated: {SubjectName}", subject.SubjectName);
        return subject;
    }

    public async Task<bool> DeleteSubjectAsync(int subjectId)
    {
        var subject = await _context.Subjects.FindAsync(subjectId);
        if (subject == null) return false;

        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Subject deleted: {SubjectName}", subject.SubjectName);
        return true;
    }

    #endregion

    #region Subject-Section Linking Operations

    public async Task<List<SubjectSection>> GetSubjectSectionsBySectionIdAsync(int sectionId)
    {
        return await _context.SubjectSections
            .Include(ss => ss.Subject)
            .Where(ss => ss.SectionId == sectionId)
            .ToListAsync();
    }

    public async Task<List<SubjectSection>> GetSubjectSectionsBySubjectIdAsync(int subjectId)
    {
        return await _context.SubjectSections
            .Include(ss => ss.Section)
            .Where(ss => ss.SubjectId == subjectId)
            .ToListAsync();
    }

    public async Task<bool> LinkSubjectToSectionAsync(int subjectId, int sectionId)
    {
        // Check if link already exists
        var exists = await _context.SubjectSections
            .AnyAsync(ss => ss.SubjectId == subjectId && ss.SectionId == sectionId);

        if (exists) return false;

        var link = new SubjectSection
        {
            SubjectId = subjectId,
            SectionId = sectionId
        };

        _context.SubjectSections.Add(link);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Subject {SubjectId} linked to Section {SectionId}", subjectId, sectionId);
        return true;
    }

    public async Task<bool> UnlinkSubjectFromSectionAsync(int subjectId, int sectionId)
    {
        var link = await _context.SubjectSections
            .FirstOrDefaultAsync(ss => ss.SubjectId == subjectId && ss.SectionId == sectionId);

        if (link == null) return false;

        _context.SubjectSections.Remove(link);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Subject {SubjectId} unlinked from Section {SectionId}", subjectId, sectionId);
        return true;
    }

    public async Task UpdateSubjectSectionLinksAsync(int subjectId, List<int> sectionIds)
    {
        // Remove existing links
        var existingLinks = await _context.SubjectSections
            .Where(ss => ss.SubjectId == subjectId)
            .ToListAsync();

        _context.SubjectSections.RemoveRange(existingLinks);

        // Add new links
        foreach (var sectionId in sectionIds)
        {
            var link = new SubjectSection
            {
                SubjectId = subjectId,
                SectionId = sectionId
            };
            _context.SubjectSections.Add(link);
        }

        await _context.SaveChangesAsync();
        _logger?.LogInformation("Subject {SubjectId} links updated", subjectId);
    }

    #endregion

    #region Teacher Assignment Operations

    public async Task<List<TeacherSectionAssignment>> GetAllTeacherAssignmentsAsync()
    {
        return await _context.TeacherSectionAssignments
            .Include(a => a.Teacher)
            .Include(a => a.Section)
                .ThenInclude(s => s!.GradeLevel)
            .Include(a => a.Subject)
            .OrderBy(a => a.Section!.GradeLevelId)
            .ThenBy(a => a.Section!.SectionName)
            .ToListAsync();
    }

    public async Task<TeacherSectionAssignment?> GetTeacherAssignmentByIdAsync(int assignmentId)
    {
        return await _context.TeacherSectionAssignments
            .Include(a => a.Teacher)
            .Include(a => a.Section)
                .ThenInclude(s => s!.GradeLevel)
            .Include(a => a.Subject)
            .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);
    }

    public async Task<TeacherSectionAssignment> CreateTeacherAssignmentAsync(TeacherSectionAssignment assignment)
    {
        assignment.CreatedAt = DateTime.Now;
        _context.TeacherSectionAssignments.Add(assignment);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Teacher assignment created: {AssignmentId}", assignment.AssignmentId);
        return assignment;
    }

    public async Task<TeacherSectionAssignment> UpdateTeacherAssignmentAsync(TeacherSectionAssignment assignment)
    {
        assignment.UpdatedAt = DateTime.Now;
        _context.TeacherSectionAssignments.Update(assignment);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Teacher assignment updated: {AssignmentId}", assignment.AssignmentId);
        return assignment;
    }

    public async Task<bool> DeleteTeacherAssignmentAsync(int assignmentId)
    {
        var assignment = await _context.TeacherSectionAssignments.FindAsync(assignmentId);
        if (assignment == null) return false;

        _context.TeacherSectionAssignments.Remove(assignment);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Teacher assignment deleted: {AssignmentId}", assignmentId);
        return true;
    }

    #endregion

    #region Class Schedule Operations

    public async Task<List<ClassSchedule>> GetAllClassSchedulesAsync()
    {
        return await _context.ClassSchedules
            .Include(s => s.Assignment)
                .ThenInclude(a => a!.Teacher)
            .Include(s => s.Assignment)
                .ThenInclude(a => a!.Section)
            .Include(s => s.Assignment)
                .ThenInclude(a => a!.Subject)
            .Include(s => s.Room)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<ClassSchedule?> GetClassScheduleByIdAsync(int scheduleId)
    {
        return await _context.ClassSchedules
            .Include(s => s.Assignment)
                .ThenInclude(a => a!.Teacher)
            .Include(s => s.Assignment)
                .ThenInclude(a => a!.Section)
            .Include(s => s.Assignment)
                .ThenInclude(a => a!.Subject)
            .Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);
    }

    public async Task<ClassSchedule> CreateClassScheduleAsync(ClassSchedule schedule)
    {
        schedule.CreatedAt = DateTime.Now;
        _context.ClassSchedules.Add(schedule);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Class schedule created: {ScheduleId}", schedule.ScheduleId);
        return schedule;
    }

    public async Task<ClassSchedule> UpdateClassScheduleAsync(ClassSchedule schedule)
    {
        schedule.UpdatedAt = DateTime.Now;
        _context.ClassSchedules.Update(schedule);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Class schedule updated: {ScheduleId}", schedule.ScheduleId);
        return schedule;
    }

    public async Task<bool> DeleteClassScheduleAsync(int scheduleId)
    {
        var schedule = await _context.ClassSchedules.FindAsync(scheduleId);
        if (schedule == null) return false;

        _context.ClassSchedules.Remove(schedule);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Class schedule deleted: {ScheduleId}", scheduleId);
        return true;
    }

    public async Task<bool> CheckScheduleConflictAsync(int assignmentId, string dayOfWeek, TimeSpan startTime, TimeSpan endTime, int? excludeScheduleId = null)
    {
        var query = _context.ClassSchedules
            .Where(s => s.AssignmentId == assignmentId 
                && s.DayOfWeek == dayOfWeek
                && s.ScheduleId != excludeScheduleId);

        // Check for time overlap
        var conflicts = await query
            .Where(s => (s.StartTime < endTime && s.EndTime > startTime))
            .AnyAsync();

        return conflicts;
    }

    public async Task<bool> CheckRoomConflictAsync(int roomId, string dayOfWeek, TimeSpan startTime, TimeSpan endTime, int? excludeScheduleId = null)
    {
        var conflicts = await _context.ClassSchedules
            .Where(s => s.RoomId == roomId
                && s.DayOfWeek == dayOfWeek
                && s.ScheduleId != excludeScheduleId
                && (s.StartTime < endTime && s.EndTime > startTime))
            .AnyAsync();

        return conflicts;
    }

    #endregion

    #region Helper Methods

    public async Task<List<GradeLevel>> GetAllGradeLevelsAsync()
    {
        return await _context.GradeLevels
            .Where(g => g.IsActive)
            .OrderBy(g => g.GradeLevelId)
            .ToListAsync();
    }

    public async Task<List<UserEntity>> GetTeachersAsync()
    {
        return await _context.Users
            .Where(u => u.UserRole.ToLower() == "teacher" || u.UserRole.ToLower().Contains("teacher") || u.UserRole.ToLower().Contains("faculty"))
            .Where(u => u.Status.ToLower() == "active")
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();
    }

    #endregion
}

