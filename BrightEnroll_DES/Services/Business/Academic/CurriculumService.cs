using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Services.Business.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Academic;

public class CurriculumService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CurriculumService>? _logger;
    private readonly AuditLogService? _auditLogService;

    public CurriculumService(AppDbContext context, ILogger<CurriculumService>? logger = null, AuditLogService? auditLogService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
        _auditLogService = auditLogService;
    }

    #region Classroom Operations

    public async Task<List<Classroom>> GetAllClassroomsAsync()
    {
        return await _context.Classrooms
            .AsNoTracking()
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
        
        // Log classroom creation to audit trail (non-blocking)
        if (_auditLogService != null)
        {
            try
            {
                await _auditLogService.CreateTransactionLogAsync(
                    action: "Create Classroom",
                    module: "Curriculum",
                    description: $"Created classroom: {classroom.RoomName} ({classroom.BuildingName}, Floor {classroom.FloorNumber})",
                    userName: null,
                    userRole: null,
                    userId: null,
                    entityType: "Classroom",
                    entityId: classroom.RoomId.ToString(),
                    status: "Success",
                    severity: "Low"
                );
            }
            catch
            {
                // Don't break classroom creation if audit logging fails
            }
        }
        
        return classroom;
    }

    public async Task<Classroom> UpdateClassroomAsync(Classroom classroom)
    {
        classroom.UpdatedAt = DateTime.Now;
        _context.Classrooms.Update(classroom);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Classroom updated: {RoomName}", classroom.RoomName);
        
        // Log classroom update to audit trail (non-blocking)
        if (_auditLogService != null)
        {
            try
            {
                await _auditLogService.CreateTransactionLogAsync(
                    action: "Update Classroom",
                    module: "Curriculum",
                    description: $"Updated classroom: {classroom.RoomName} ({classroom.BuildingName}, Floor {classroom.FloorNumber})",
                    userName: null,
                    userRole: null,
                    userId: null,
                    entityType: "Classroom",
                    entityId: classroom.RoomId.ToString(),
                    status: "Success",
                    severity: "Low"
                );
            }
            catch
            {
                // Don't break classroom update if audit logging fails
            }
        }
        
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
            .AsNoTracking()
            .Include(s => s.GradeLevel)
            .Include(s => s.Classroom)
            .Include(s => s.Adviser)
            .OrderBy(s => s.GradeLevelId)
            .ThenBy(s => s.SectionName)
            .ToListAsync();
    }

    public async Task<Section?> GetSectionByIdAsync(int sectionId)
    {
        return await _context.Sections
            .Include(s => s.GradeLevel)
            .Include(s => s.Classroom)
            .Include(s => s.Adviser)
            .FirstOrDefaultAsync(s => s.SectionId == sectionId);
    }

    public async Task<Section> CreateSectionAsync(Section section)
    {
        section.CreatedAt = DateTime.Now;
        _context.Sections.Add(section);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Section created: {SectionName}", section.SectionName);
        
        // Log section creation to audit trail (non-blocking)
        if (_auditLogService != null)
        {
            try
            {
                await _auditLogService.CreateTransactionLogAsync(
                    action: "Create Section",
                    module: "Curriculum",
                    description: $"Created section: {section.SectionName}",
                    userName: null,
                    userRole: null,
                    userId: null,
                    entityType: "Section",
                    entityId: section.SectionId.ToString(),
                    status: "Success",
                    severity: "Medium"
                );
            }
            catch
            {
                // Don't break section creation if audit logging fails
            }
        }
        
        return section;
    }

    public async Task<Section> UpdateSectionAsync(Section section)
    {
        section.UpdatedAt = DateTime.Now;
        _context.Sections.Update(section);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Section updated: {SectionName}", section.SectionName);
        
        // Log section update to audit trail (non-blocking)
        if (_auditLogService != null)
        {
            try
            {
                await _auditLogService.CreateTransactionLogAsync(
                    action: "Update Section",
                    module: "Curriculum",
                    description: $"Updated section: {section.SectionName}",
                    userName: null,
                    userRole: null,
                    userId: null,
                    entityType: "Section",
                    entityId: section.SectionId.ToString(),
                    status: "Success",
                    severity: "Medium"
                );
            }
            catch
            {
                // Don't break section update if audit logging fails
            }
        }
        
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

    /// <summary>
    /// Automatically assigns all active subjects for the section's grade level
    /// to the given section using the SubjectSection linking table.
    /// </summary>
    public async Task AssignDefaultSubjectsToSectionAsync(int sectionId)
    {
        var section = await _context.Sections.AsNoTracking().FirstOrDefaultAsync(s => s.SectionId == sectionId);
        if (section == null) return;

        // Get all active subjects for the grade level
        var gradeLevelSubjects = await _context.Subjects
            .Where(s => s.GradeLevelId == section.GradeLevelId && s.IsActive)
            .Select(s => s.SubjectId)
            .ToListAsync();

        if (!gradeLevelSubjects.Any()) return;

        // Get existing links to avoid duplicates
        var existingLinks = await _context.SubjectSections
            .Where(ss => ss.SectionId == sectionId)
            .Select(ss => ss.SubjectId)
            .ToListAsync();

        var newSubjectIds = gradeLevelSubjects
            .Where(id => !existingLinks.Contains(id))
            .ToList();

        foreach (var subjectId in newSubjectIds)
        {
            _context.SubjectSections.Add(new SubjectSection
            {
                SubjectId = subjectId,
                SectionId = sectionId
            });
        }

        if (newSubjectIds.Any())
        {
            await _context.SaveChangesAsync();
            _logger?.LogInformation("Assigned {Count} default subjects to section {SectionId}", newSubjectIds.Count, sectionId);
        }
    }

    #region Subject Operations

    public async Task<List<Subject>> GetAllSubjectsAsync()
    {
        try
        {
            var subjects = await _context.Subjects
                .AsNoTracking()
                .Where(s => s.IsActive)
                .Include(s => s.GradeLevel)
                .OrderBy(s => s.GradeLevelId)
                .ThenBy(s => s.SubjectName)
                .ToListAsync();
            
            _logger?.LogInformation("Loaded {Count} subjects from database", subjects.Count);
            return subjects;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading subjects from database: {ErrorMessage}", ex.Message);
            throw;
        }
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
        if (subject.IsActive == default)
        {
            subject.IsActive = true;
        }
        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Subject created: {SubjectName}", subject.SubjectName);
        
        // Log subject creation to audit trail (non-blocking)
        if (_auditLogService != null)
        {
            try
            {
                await _auditLogService.CreateTransactionLogAsync(
                    action: "Create Subject",
                    module: "Curriculum",
                    description: $"Created subject: {subject.SubjectName}",
                    userName: null,
                    userRole: null,
                    userId: null,
                    entityType: "Subject",
                    entityId: subject.SubjectId.ToString(),
                    status: "Success",
                    severity: "Medium"
                );
            }
            catch
            {
                // Don't break subject creation if audit logging fails
            }
        }
        
        return subject;
    }

    public async Task<Subject> UpdateSubjectAsync(Subject subject)
    {
        subject.UpdatedAt = DateTime.Now;
        _context.Subjects.Update(subject);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Subject updated: {SubjectName}", subject.SubjectName);
        
        // Log subject update to audit trail (non-blocking)
        if (_auditLogService != null)
        {
            try
            {
                await _auditLogService.CreateTransactionLogAsync(
                    action: "Update Subject",
                    module: "Curriculum",
                    description: $"Updated subject: {subject.SubjectName}",
                    userName: null,
                    userRole: null,
                    userId: null,
                    entityType: "Subject",
                    entityId: subject.SubjectId.ToString(),
                    status: "Success",
                    severity: "Medium"
                );
            }
            catch
            {
                // Don't break subject update if audit logging fails
            }
        }
        
        return subject;
    }

    public async Task<bool> DeleteSubjectAsync(int subjectId)
    {
        var subject = await _context.Subjects.FindAsync(subjectId);
        if (subject == null) return false;

        // Soft delete: mark subject as inactive instead of removing
        subject.IsActive = false;
        subject.UpdatedAt = DateTime.Now;
        _context.Subjects.Update(subject);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Subject soft-deleted (set inactive): {SubjectName}", subject.SubjectName);
        
        // Log subject deletion to audit trail (non-blocking)
        if (_auditLogService != null)
        {
            try
            {
                await _auditLogService.CreateTransactionLogAsync(
                    action: "Delete Subject",
                    module: "Curriculum",
                    description: $"Deleted (deactivated) subject: {subject.SubjectName}",
                    userName: null,
                    userRole: null,
                    userId: null,
                    entityType: "Subject",
                    entityId: subject.SubjectId.ToString(),
                    status: "Success",
                    severity: "High"
                );
            }
            catch
            {
                // Don't break subject deletion if audit logging fails
            }
        }
        
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

    #region Subject Schedule Operations

    public async Task<List<SubjectSchedule>> GetSubjectSchedulesByGradeLevelAsync(int gradeLevelId)
    {
        return await _context.SubjectSchedules
            .Include(ss => ss.Subject)
            .Include(ss => ss.GradeLevel)
            .Where(ss => ss.GradeLevelId == gradeLevelId && ss.IsDefault)
            .OrderBy(ss => ss.Subject!.SubjectName)
            .ThenBy(ss => ss.DayOfWeek)
            .ThenBy(ss => ss.StartTime)
            .ToListAsync();
    }

    public async Task<List<SubjectSchedule>> GetSubjectSchedulesBySubjectIdAsync(int subjectId)
    {
        try
        {
            var schedules = await _context.SubjectSchedules
                .AsNoTracking()
                .Include(ss => ss.GradeLevel)
                .Where(ss => ss.SubjectId == subjectId && ss.IsDefault)
                .OrderBy(ss => ss.DayOfWeek)
                .ThenBy(ss => ss.StartTime)
                .ToListAsync();
            
            _logger?.LogInformation("Loaded {Count} schedules for subject ID {SubjectId}", schedules.Count, subjectId);
            return schedules;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading schedules for subject ID {SubjectId}: {ErrorMessage}", subjectId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets all subject schedules for multiple subjects at once to avoid concurrent DbContext operations
    /// </summary>
    public async Task<Dictionary<int, List<SubjectSchedule>>> GetSubjectSchedulesBySubjectIdsAsync(IEnumerable<int> subjectIds)
    {
        try
        {
            var subjectIdList = subjectIds.ToList();
            if (!subjectIdList.Any())
            {
                return new Dictionary<int, List<SubjectSchedule>>();
            }

            var allSchedules = await _context.SubjectSchedules
                .AsNoTracking()
                .Include(ss => ss.GradeLevel)
                .Where(ss => subjectIdList.Contains(ss.SubjectId) && ss.IsDefault)
                .OrderBy(ss => ss.SubjectId)
                .ThenBy(ss => ss.DayOfWeek)
                .ThenBy(ss => ss.StartTime)
                .ToListAsync();

            var result = allSchedules
                .GroupBy(ss => ss.SubjectId)
                .ToDictionary(g => g.Key, g => g.ToList());

            _logger?.LogInformation("Loaded schedules for {SubjectCount} subjects", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading schedules for multiple subjects: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    public async Task<SubjectSchedule> CreateSubjectScheduleAsync(SubjectSchedule schedule)
    {
        try
        {
            // Ensure CreatedAt is set
            if (schedule.CreatedAt == default(DateTime))
            {
                schedule.CreatedAt = DateTime.Now;
            }
            
            // Ensure IsDefault is set (default is true)
            if (!schedule.IsDefault)
            {
                schedule.IsDefault = true;
            }
            
            // Verify required fields are set
            if (schedule.SubjectId <= 0)
            {
                throw new ArgumentException("SubjectId must be greater than 0", nameof(schedule));
            }
            
            if (schedule.GradeLevelId <= 0)
            {
                throw new ArgumentException("GradeLevelId must be greater than 0", nameof(schedule));
            }
            
            if (string.IsNullOrWhiteSpace(schedule.DayOfWeek))
            {
                throw new ArgumentException("DayOfWeek cannot be null or empty", nameof(schedule));
            }
            
            _logger?.LogInformation("Attempting to create schedule: SubjectId={SubjectId}, GradeLevelId={GradeLevelId}, Day={Day}, Time={StartTime}-{EndTime}", 
                schedule.SubjectId, schedule.GradeLevelId, schedule.DayOfWeek, schedule.StartTime, schedule.EndTime);
            
            // Clear navigation properties to ensure EF Core uses only the foreign key columns
            var scheduleToAdd = new SubjectSchedule
            {
                SubjectId = schedule.SubjectId,
                GradeLevelId = schedule.GradeLevelId,
                DayOfWeek = schedule.DayOfWeek,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                IsDefault = schedule.IsDefault,
                CreatedAt = schedule.CreatedAt,
                UpdatedAt = schedule.UpdatedAt
            };
            
            _context.SubjectSchedules.Add(scheduleToAdd);
            await _context.SaveChangesAsync();
            
            // Update the original schedule with the generated ID
            schedule.ScheduleId = scheduleToAdd.ScheduleId;
            
            _logger?.LogInformation("Subject schedule created successfully: ScheduleId={ScheduleId}, SubjectId={SubjectId}, GradeLevelId={GradeLevelId}, Day={Day}, Time={StartTime}-{EndTime}, IsDefault={IsDefault}", 
                schedule.ScheduleId, schedule.SubjectId, schedule.GradeLevelId, schedule.DayOfWeek, schedule.StartTime, schedule.EndTime, schedule.IsDefault);
            
            return schedule;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating subject schedule: SubjectId={SubjectId}, GradeLevelId={GradeLevelId}, Day={Day}, Error={ErrorMessage}", 
                schedule.SubjectId, schedule.GradeLevelId, schedule.DayOfWeek, ex.Message);
            throw;
        }
    }

    public async Task<bool> DeleteSubjectScheduleAsync(int scheduleId)
    {
        var schedule = await _context.SubjectSchedules.FindAsync(scheduleId);
        if (schedule == null) return false;

        _context.SubjectSchedules.Remove(schedule);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Subject schedule deleted: {ScheduleId}", scheduleId);
        return true;
    }

    public async Task<bool> DeleteSubjectSchedulesBySubjectIdAsync(int subjectId)
    {
        var schedules = await _context.SubjectSchedules
            .Where(ss => ss.SubjectId == subjectId)
            .ToListAsync();

        if (!schedules.Any()) return false;

        _context.SubjectSchedules.RemoveRange(schedules);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Subject schedules deleted for Subject: {SubjectId}", subjectId);
        return true;
    }

    public async Task<List<SubjectSchedule>> GetSubjectSchedulesBySubjectAndTimeAsync(int subjectId, int gradeLevelId, TimeSpan startTime, TimeSpan endTime)
    {
        return await _context.SubjectSchedules
            .AsNoTracking()
            .Where(ss => ss.SubjectId == subjectId &&
                       ss.GradeLevelId == gradeLevelId &&
                       ss.StartTime == startTime &&
                       ss.EndTime == endTime &&
                       ss.IsDefault)
            .ToListAsync();
    }

    public async Task<SubjectSchedule?> GetSubjectScheduleBySubjectDayAndTimeAsync(int subjectId, int gradeLevelId, string dayOfWeek, TimeSpan startTime, TimeSpan endTime)
    {
        return await _context.SubjectSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(ss => ss.SubjectId == subjectId &&
                                      ss.GradeLevelId == gradeLevelId &&
                                      ss.DayOfWeek == dayOfWeek &&
                                      ss.StartTime == startTime &&
                                      ss.EndTime == endTime &&
                                      ss.IsDefault);
    }

    #endregion

    #region Teacher Assignment Operations

    public async Task<List<TeacherSectionAssignment>> GetAllTeacherAssignmentsAsync()
    {
        return await _context.TeacherSectionAssignments
            .AsNoTracking()
            .Where(a => !a.IsArchived)
            .Include(a => a.Teacher)
            .Include(a => a.Section)
                .ThenInclude(s => s!.GradeLevel)
            .Include(a => a.Section)
                .ThenInclude(s => s!.Classroom)
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
            .Include(a => a.Section)
                .ThenInclude(s => s!.Classroom)
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

        assignment.IsArchived = true;
        assignment.UpdatedAt = DateTime.Now;
        _context.TeacherSectionAssignments.Update(assignment);
        await _context.SaveChangesAsync();
        _logger?.LogInformation("Teacher assignment archived (soft delete): {AssignmentId}", assignmentId);
        return true;
    }

    public async Task<List<TeacherSectionAssignment>> CreateBatchTeacherAssignmentsAsync(
        List<TeacherSectionAssignment> assignments, 
        int sectionId, 
        int? classroomId)
    {
        var createdAssignments = new List<TeacherSectionAssignment>();
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var assignment in assignments)
            {
                assignment.CreatedAt = DateTime.Now;
                _context.TeacherSectionAssignments.Add(assignment);
                await _context.SaveChangesAsync();
                
                createdAssignments.Add(assignment);
                
                // If assignment has a subject, create ClassSchedule entries from SubjectSchedule
                if (assignment.SubjectId.HasValue && classroomId.HasValue)
                {
                    var subjectSchedules = await GetSubjectSchedulesBySubjectIdAsync(assignment.SubjectId.Value);
                    
                    foreach (var subjectSchedule in subjectSchedules)
                    {
                        var classSchedule = new ClassSchedule
                        {
                            AssignmentId = assignment.AssignmentId,
                            DayOfWeek = subjectSchedule.DayOfWeek,
                            StartTime = subjectSchedule.StartTime,
                            EndTime = subjectSchedule.EndTime,
                            RoomId = classroomId.Value,
                            CreatedAt = DateTime.Now
                        };
                        _context.ClassSchedules.Add(classSchedule);
                    }
                }
            }
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            _logger?.LogInformation("Batch teacher assignments created: {Count} assignments", createdAssignments.Count);
            return createdAssignments;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    #endregion

    #region Class Schedule Operations

    public async Task<List<ClassSchedule>> GetAllClassSchedulesAsync()
    {
        return await _context.ClassSchedules
            .AsNoTracking()
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
            .AsNoTracking()
            .Where(g => g.IsActive)
            .OrderBy(g => g.GradeLevelId)
            .ToListAsync();
    }

    /// <summary>
    /// Returns the number of students currently enrolled in the given section for a specific school year.
    /// Used by the enrollment module to compute available slots per section.
    /// </summary>
    public async Task<int> GetSectionEnrollmentCountAsync(int sectionId, string schoolYear)
    {
        if (string.IsNullOrWhiteSpace(schoolYear))
        {
            return 0;
        }

        return await _context.StudentSectionEnrollments
            .Where(e => e.SectionId == sectionId
                        && e.SchoolYear == schoolYear
                        && e.Status == "Enrolled")
            .CountAsync();
    }

    public async Task<List<UserEntity>> GetTeachersAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.UserRole.ToLower() == "teacher" || u.UserRole.ToLower().Contains("teacher") || u.UserRole.ToLower().Contains("faculty"))
            .Where(u => u.Status.ToLower() == "active")
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();
    }

    /// <summary>
    /// Gets section names that have enrollments in the specified school year
    /// </summary>
    public async Task<List<string>> GetSectionNamesWithEnrollmentsAsync(string schoolYear)
    {
        try
        {
            var sectionNames = await _context.StudentSectionEnrollments
                .AsNoTracking()
                .Where(e => e.Status == "Enrolled" && e.SchoolYear == schoolYear)
                .Select(e => e.SectionId)
                .Distinct()
                .Join(_context.Sections,
                    enrollmentId => enrollmentId,
                    section => section.SectionId,
                    (enrollmentId, section) => section.SectionName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToListAsync();

            return sectionNames;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting section names with enrollments for school year {SchoolYear}", schoolYear);
            return new List<string>();
        }
    }

    public async Task<List<FinalClassView>> GetFinalClassesAsync()
    {
        try
        {
            var finalClasses = await _context.FinalClassViews
                .AsNoTracking()
                .OrderBy(fc => fc.GradeLevel ?? "")
                .ThenBy(fc => fc.SectionName ?? "")
                .ThenBy(fc => fc.DayOfWeek ?? "")
                .ThenBy(fc => fc.StartTime ?? TimeSpan.Zero)
                .ToListAsync();
            
            _logger?.LogInformation("Loaded {Count} final classes from view", finalClasses.Count);
            return finalClasses;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error loading final classes from view (view may not exist yet): {ErrorMessage}", ex.Message);
            // Return empty list instead of throwing to prevent breaking the entire page
            // The view will be created when the database script is run
            return new List<FinalClassView>();
        }
    }

    #endregion
}

