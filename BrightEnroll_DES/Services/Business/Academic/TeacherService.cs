using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Academic;

/// <summary>
/// Service for teacher-specific operations including dashboard data retrieval.
/// </summary>
public class TeacherService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TeacherService>? _logger;

    public TeacherService(AppDbContext context, ILogger<TeacherService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    #region Dashboard Data

    /// <summary>
    /// Gets the count of assigned classes for a teacher.
    /// </summary>
    public async Task<int> GetAssignedClassesCountAsync(int teacherId)
    {
        try
        {
            var currentSchoolYear = await GetCurrentSchoolYearAsync();
            
            // Get sections assigned to teacher that have enrollments in the active school year
            var assignedSections = await _context.TeacherSectionAssignments
                .Where(a => a.TeacherId == teacherId && !a.IsArchived)
                .Select(a => a.SectionId)
                .Distinct()
                .ToListAsync();
            
            // Filter to only sections with enrollments in the active school year
            if (!string.IsNullOrEmpty(currentSchoolYear))
            {
                var sectionsWithEnrollments = await _context.StudentSectionEnrollments
                    .Where(e => assignedSections.Contains(e.SectionId) &&
                               e.Status == "Enrolled" &&
                               e.SchoolYear == currentSchoolYear)
                    .Select(e => e.SectionId)
                    .Distinct()
                    .CountAsync();
                
                return sectionsWithEnrollments;
            }
            
            // If no active school year, return 0
            return 0;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting assigned classes count for teacher {TeacherId}", teacherId);
            return 0;
        }
    }

    /// <summary>
    /// Gets today's schedule for a teacher based on ClassSchedule table.
    /// </summary>
    public async Task<List<TodayScheduleItem>> GetTodayScheduleAsync(int teacherId)
    {
        try
        {
            var today = DateTime.Today;
            var dayOfWeek = GetDayOfWeekAbbreviation(today.DayOfWeek);
            var currentSchoolYear = await GetCurrentSchoolYearAsync();

            var schedules = await _context.ClassSchedules
                .Include(cs => cs.Assignment!)
                    .ThenInclude(a => a.Section!)
                        .ThenInclude(s => s.GradeLevel)
                .Include(cs => cs.Assignment!)
                    .ThenInclude(a => a.Subject)
                .Include(cs => cs.Room)
                .Where(cs => cs.Assignment != null &&
                            cs.Assignment.TeacherId == teacherId &&
                            !cs.Assignment.IsArchived &&
                            cs.DayOfWeek == dayOfWeek)
                .OrderBy(cs => cs.StartTime)
                .ToListAsync();

            // Filter to only sections with enrollments in the active school year
            var filteredSchedules = new List<TodayScheduleItem>();
            
            if (!string.IsNullOrEmpty(currentSchoolYear))
            {
                foreach (var cs in schedules)
                {
                    if (cs.Assignment?.SectionId == null) continue;
                    
                    // Check if this section has enrollments in the active school year
                    var hasEnrollments = await _context.StudentSectionEnrollments
                        .AnyAsync(e => e.SectionId == cs.Assignment.SectionId &&
                                      e.Status == "Enrolled" &&
                                      e.SchoolYear == currentSchoolYear);
                    
                    if (hasEnrollments)
                    {
                        filteredSchedules.Add(new TodayScheduleItem
                        {
                            ScheduleId = cs.ScheduleId,
                            SectionName = cs.Assignment?.Section?.SectionName ?? "Unknown",
                            GradeLevel = cs.Assignment?.Section?.GradeLevel?.GradeLevelName ?? "",
                            SubjectName = cs.Assignment?.Subject?.SubjectName ?? 
                                         (cs.Assignment?.Role == "adviser" ? "Homeroom" : "Unknown"),
                            RoomName = cs.Room?.RoomName ?? "TBA",
                            BuildingName = cs.Room?.BuildingName ?? "",
                            StartTime = cs.StartTime,
                            EndTime = cs.EndTime,
                            Role = cs.Assignment?.Role ?? ""
                        });
                    }
                }
            }

            return filteredSchedules;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting today's schedule for teacher {TeacherId}", teacherId);
            return new List<TodayScheduleItem>();
        }
    }

    /// <summary>
    /// Gets sections with pending attendance entries (no attendance recorded today).
    /// </summary>
    public async Task<List<PendingAttendanceItem>> GetPendingAttendanceAsync(int teacherId)
    {
        try
        {
            var today = DateTime.Today;
            var currentSchoolYear = await GetCurrentSchoolYearAsync();

            // Get all sections assigned to this teacher
            var assignedSections = await _context.TeacherSectionAssignments
                .Where(a => a.TeacherId == teacherId && !a.IsArchived)
                .Select(a => new { a.SectionId, a.Section })
                .Distinct()
                .ToListAsync();

            var pendingItems = new List<PendingAttendanceItem>();

            foreach (var assignment in assignedSections)
            {
                if (assignment.Section == null) continue;

                // Check if attendance was recorded today for this section
                var hasAttendanceToday = await _context.Attendances
                    .AnyAsync(a => a.SectionId == assignment.SectionId &&
                                  a.TeacherId == teacherId &&
                                  a.AttendanceDate == today &&
                                  a.SchoolYear == currentSchoolYear);

                if (!hasAttendanceToday)
                {
                    // Get student count for this section
                    var studentCount = await _context.StudentSectionEnrollments
                        .CountAsync(e => e.SectionId == assignment.SectionId &&
                                        e.Status == "Enrolled" &&
                                        e.SchoolYear == currentSchoolYear);

                    pendingItems.Add(new PendingAttendanceItem
                    {
                        SectionId = assignment.SectionId,
                        SectionName = assignment.Section.SectionName ?? "Unknown",
                        GradeLevel = assignment.Section.GradeLevel?.GradeLevelName ?? "",
                        StudentCount = studentCount
                    });
                }
            }

            return pendingItems;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting pending attendance for teacher {TeacherId}", teacherId);
            return new List<PendingAttendanceItem>();
        }
    }

    /// <summary>
    /// Gets grading periods where the teacher has not submitted grades yet.
    /// </summary>
    public async Task<List<PendingGradeItem>> GetPendingGradesAsync(int teacherId)
    {
        try
        {
            var currentSchoolYear = await GetCurrentSchoolYearAsync();
            var currentGradingPeriod = GetCurrentGradingPeriod();

            // Get all subjects assigned to this teacher
            var assignedSubjects = await _context.TeacherSectionAssignments
                .Where(a => a.TeacherId == teacherId &&
                           !a.IsArchived &&
                           a.SubjectId != null)
                .Select(a => new { a.SubjectId, a.SectionId, a.Subject, a.Section })
                .Distinct()
                .ToListAsync();

            var pendingItems = new List<PendingGradeItem>();

            foreach (var assignment in assignedSubjects)
            {
                if (assignment.Subject == null || assignment.Section == null) continue;

                // Get enrolled students for this section
                var enrolledStudents = await _context.StudentSectionEnrollments
                    .Where(e => e.SectionId == assignment.SectionId &&
                               e.Status == "Enrolled" &&
                               e.SchoolYear == currentSchoolYear)
                    .Select(e => e.StudentId)
                    .ToListAsync();

                // Check if grades exist for current grading period
                var hasGrades = await _context.Grades
                    .AnyAsync(g => g.SubjectId == assignment.SubjectId &&
                                  g.SectionId == assignment.SectionId &&
                                  g.TeacherId == teacherId &&
                                  g.SchoolYear == currentSchoolYear &&
                                  g.GradingPeriod == currentGradingPeriod);

                if (!hasGrades && enrolledStudents.Any())
                {
                    pendingItems.Add(new PendingGradeItem
                    {
                        SubjectId = assignment.SubjectId ?? 0,
                        SubjectName = assignment.Subject.SubjectName ?? "Unknown",
                        SectionId = assignment.SectionId,
                        SectionName = assignment.Section.SectionName ?? "Unknown",
                        GradeLevel = assignment.Section.GradeLevel?.GradeLevelName ?? "",
                        GradingPeriod = currentGradingPeriod,
                        StudentCount = enrolledStudents.Count
                    });
                }
            }

            return pendingItems;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting pending grades for teacher {TeacherId}", teacherId);
            return new List<PendingGradeItem>();
        }
    }

    /// <summary>
    /// Gets recent activity logs for a teacher.
    /// </summary>
    public async Task<List<TeacherActivityLog>> GetRecentActivityAsync(int teacherId, int limit = 10)
    {
        try
        {
            return await _context.TeacherActivityLogs
                .Where(log => log.TeacherId == teacherId)
                .OrderByDescending(log => log.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting recent activity for teacher {TeacherId}", teacherId);
            return new List<TeacherActivityLog>();
        }
    }

    #endregion

    #region Activity Logging

    /// <summary>
    /// Logs a teacher activity.
    /// </summary>
    public async Task LogActivityAsync(int teacherId, string action, string? details = null)
    {
        try
        {
            var log = new TeacherActivityLog
            {
                TeacherId = teacherId,
                Action = action,
                Details = details,
                CreatedAt = DateTime.Now
            };

            _context.TeacherActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error logging activity for teacher {TeacherId}", teacherId);
            // Don't throw - activity logging should not break the main flow
        }
    }

    /// <summary>
    /// Gets all assigned sections for a teacher with details.
    /// </summary>
    public async Task<List<AssignedSectionItem>> GetAssignedSectionsAsync(int teacherId, string? schoolYear = null, string? gradeLevel = null)
    {
        try
        {
            var currentSchoolYear = schoolYear ?? await GetCurrentSchoolYearAsync();

            var assignments = await _context.TeacherSectionAssignments
                .Where(a => a.TeacherId == teacherId && !a.IsArchived)
                .Include(a => a.Section!)
                    .ThenInclude(s => s.GradeLevel)
                .Include(a => a.Section!)
                    .ThenInclude(s => s.Classroom)
                .Include(a => a.Subject)
                .Include(a => a.ClassSchedules)
                    .ThenInclude(cs => cs.Room)
                .ToListAsync();

            // Group by section to get all subjects/roles for each section
            var sectionGroups = assignments
                .Where(a => a.Section != null)
                .GroupBy(a => a.SectionId)
                .ToList();

            var result = new List<AssignedSectionItem>();

            foreach (var group in sectionGroups)
            {
                var firstAssignment = group.First();
                var section = firstAssignment.Section!;
                var gradeLevelName = section.GradeLevel?.GradeLevelName ?? "";

                // Apply filters
                if (!string.IsNullOrEmpty(gradeLevel) && gradeLevelName != gradeLevel)
                {
                    continue;
                }

                // Get all subjects taught in this section
                var subjects = group
                    .Where(a => a.Subject != null)
                    .Select(a => a.Subject!.SubjectName)
                    .Distinct()
                    .ToList();

                // Get student count
                var studentCount = await _context.StudentSectionEnrollments
                    .CountAsync(e => e.SectionId == section.SectionId &&
                                    e.Status == "Enrolled" &&
                                    e.SchoolYear == currentSchoolYear);

                // Get schedules for this section with their associated assignments
                var scheduleAssignments = group
                    .SelectMany(a => a.ClassSchedules.Select(cs => new { Schedule = cs, Assignment = a }))
                    .OrderBy(x => GetDayOrder(x.Schedule.DayOfWeek))
                    .ThenBy(x => x.Schedule.StartTime)
                    .ToList();

                // Check if teacher is adviser
                var isAdviser = group.Any(a => a.Role == "adviser" || a.Role == "homeroom_all_subjects");

                result.Add(new AssignedSectionItem
                {
                    SectionId = section.SectionId,
                    SectionName = section.SectionName,
                    GradeLevel = gradeLevelName,
                    GradeLevelId = section.GradeLevelId,
                    ClassroomName = section.Classroom?.RoomName ?? "TBA",
                    BuildingName = section.Classroom?.BuildingName ?? "",
                    Capacity = section.Capacity,
                    StudentCount = studentCount,
                    Subjects = subjects,
                    IsAdviser = isAdviser,
                    Schedules = scheduleAssignments.Select(sa => new SectionScheduleItem
                    {
                        ScheduleId = sa.Schedule.ScheduleId,
                        DayOfWeek = sa.Schedule.DayOfWeek,
                        StartTime = sa.Schedule.StartTime,
                        EndTime = sa.Schedule.EndTime,
                        RoomName = sa.Schedule.Room?.RoomName ?? "TBA",
                        SubjectName = sa.Assignment.Subject?.SubjectName ?? 
                                     (sa.Assignment.Role == "adviser" || sa.Assignment.Role == "homeroom_all_subjects" ? "Homeroom" : "Unknown")
                    }).ToList()
                });
            }

            return result.OrderBy(r => r.GradeLevelId).ThenBy(r => r.SectionName).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting assigned sections for teacher {TeacherId}", teacherId);
            return new List<AssignedSectionItem>();
        }
    }

    /// <summary>
    /// Gets sections assigned to a teacher for grade entry (filtered by school year).
    /// </summary>
    public async Task<List<Section>> GetAssignedSectionsForGradeEntryAsync(int teacherId, string schoolYear)
    {
        try
        {
            var sectionIds = await _context.TeacherSectionAssignments
                .Where(a => a.TeacherId == teacherId && !a.IsArchived)
                .Select(a => a.SectionId)
                .Distinct()
                .ToListAsync();

            return await _context.Sections
                .Where(s => sectionIds.Contains(s.SectionId))
                .Include(s => s.GradeLevel)
                .OrderBy(s => s.GradeLevelId)
                .ThenBy(s => s.SectionName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting assigned sections for grade entry for teacher {TeacherId}", teacherId);
            return new List<Section>();
        }
    }

    /// <summary>
    /// Gets subjects assigned to a teacher for a specific section.
    /// </summary>
    public async Task<List<Subject>> GetAssignedSubjectsForGradeEntryAsync(int teacherId, int sectionId)
    {
        try
        {
            var subjectIds = await _context.TeacherSectionAssignments
                .Where(a => a.TeacherId == teacherId 
                         && a.SectionId == sectionId 
                         && !a.IsArchived
                         && a.SubjectId != null)
                .Select(a => a.SubjectId!.Value)
                .Distinct()
                .ToListAsync();

            return await _context.Subjects
                .Where(s => subjectIds.Contains(s.SubjectId))
                .OrderBy(s => s.SubjectName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting assigned subjects for grade entry for teacher {TeacherId}, section {SectionId}", teacherId, sectionId);
            return new List<Subject>();
        }
    }

    /// <summary>
    /// Validates if a teacher is assigned to a section and subject.
    /// </summary>
    public async Task<bool> IsTeacherAssignedToSectionSubjectAsync(int teacherId, int sectionId, int subjectId)
    {
        try
        {
            return await _context.TeacherSectionAssignments
                .AnyAsync(a => a.TeacherId == teacherId
                            && a.SectionId == sectionId
                            && a.SubjectId == subjectId
                            && !a.IsArchived);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error validating teacher assignment for teacher {TeacherId}, section {SectionId}, subject {SubjectId}", teacherId, sectionId, subjectId);
            return false;
        }
    }

    /// <summary>
    /// Gets class roster (enrolled students) for a section.
    /// </summary>
    public async Task<List<ClassRosterItem>> GetClassRosterAsync(int sectionId, string schoolYear)
    {
        try
        {
            var enrollments = await _context.StudentSectionEnrollments
                .Where(e => e.SectionId == sectionId &&
                           e.Status == "Enrolled" &&
                           e.SchoolYear == schoolYear)
                .Include(e => e.Student)
                .OrderBy(e => e.Student!.LastName)
                .ThenBy(e => e.Student!.FirstName)
                .ToListAsync();

            return enrollments.Select(e => new ClassRosterItem
            {
                StudentId = e.StudentId,
                StudentName = $"{e.Student!.FirstName} {e.Student.MiddleName} {e.Student.LastName}".Trim(),
                StudentSystemId = e.Student.StudentId,
                EnrollmentDate = e.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting class roster for section {SectionId}", sectionId);
            return new List<ClassRosterItem>();
        }
    }

    /// <summary>
    /// Gets detailed section information including adviser and co-teachers.
    /// </summary>
    public async Task<SectionDetailsItem?> GetSectionDetailsAsync(int sectionId, string? schoolYear = null)
    {
        try
        {
            var currentSchoolYear = schoolYear ?? await GetCurrentSchoolYearAsync();

            var section = await _context.Sections
                .Include(s => s.GradeLevel)
                .Include(s => s.Classroom)
                .Include(s => s.Adviser)
                .FirstOrDefaultAsync(s => s.SectionId == sectionId);

            if (section == null)
                return null;

            // Get all teachers assigned to this section
            var teacherAssignments = await _context.TeacherSectionAssignments
                .Where(a => a.SectionId == sectionId && !a.IsArchived)
                .Include(a => a.Teacher)
                .Include(a => a.Subject)
                .ToListAsync();

            // Get adviser - first from section.AdviserId, then from assignments
            var adviser = section.Adviser;
            if (adviser == null)
            {
                adviser = teacherAssignments
                    .FirstOrDefault(a => a.Role == "adviser" || a.Role == "homeroom_all_subjects")?.Teacher;
            }

            var adviserUserId = adviser?.UserId;
            var coTeachers = teacherAssignments
                .Where(a => a.Role != "adviser" && 
                           a.Role != "homeroom_all_subjects" && 
                           a.Teacher != null &&
                           (adviserUserId == null || a.Teacher.UserId != adviserUserId))
                .Select(a => a.Teacher!)
                .GroupBy(t => t.UserId)
                .Select(g => g.First())
                .ToList();

            // Get subjects handled
            var subjects = teacherAssignments
                .Where(a => a.Subject != null)
                .Select(a => a.Subject!.SubjectName)
                .Distinct()
                .ToList();

            // Get student count
            var studentCount = await _context.StudentSectionEnrollments
                .CountAsync(e => e.SectionId == sectionId &&
                                e.Status == "Enrolled" &&
                                e.SchoolYear == currentSchoolYear);

            return new SectionDetailsItem
            {
                SectionId = section.SectionId,
                SectionName = section.SectionName,
                GradeLevel = section.GradeLevel?.GradeLevelName ?? "",
                SchoolYear = currentSchoolYear,
                AdviserName = adviser != null ? $"{adviser.FirstName} {adviser.LastName}".Trim() : "",
                CoTeachers = coTeachers.Select(t => $"{t.FirstName} {t.LastName}".Trim()).ToList(),
                Subjects = subjects,
                StudentCount = studentCount,
                Capacity = section.Capacity,
                ClassroomName = section.Classroom?.RoomName ?? "TBA",
                BuildingName = section.Classroom?.BuildingName ?? ""
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting section details for section {SectionId}", sectionId);
            return null;
        }
    }

    /// <summary>
    /// Gets all enrolled students in a section with their enrollment status.
    /// </summary>
    public async Task<List<EnrolledStudentItem>> GetEnrolledStudentsAsync(int sectionId, string schoolYear)
    {
        try
        {
            var enrollments = await _context.StudentSectionEnrollments
                .Where(e => e.SectionId == sectionId &&
                           e.Status == "Enrolled" &&
                           e.SchoolYear == schoolYear)
                .Include(e => e.Student)
                .OrderBy(e => e.Student!.LastName)
                .ThenBy(e => e.Student!.FirstName)
                .ToListAsync();

            return enrollments.Select(e => new EnrolledStudentItem
            {
                StudentId = e.StudentId,
                StudentSystemId = e.Student!.StudentId,
                StudentName = $"{e.Student.FirstName} {e.Student.MiddleName} {e.Student.LastName}".Trim(),
                Status = e.Status,
                EnrollmentDate = e.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting enrolled students for section {SectionId}", sectionId);
            return new List<EnrolledStudentItem>();
        }
    }

    /// <summary>
    /// Gets complete student information including personal, guardian, and enrollment details.
    /// </summary>
    public async Task<StudentDetailsItem?> GetStudentDetailsAsync(string studentId, int sectionId, string schoolYear)
    {
        try
        {
            // Use the same method as StudentRecord module for consistency
            var student = await _context.Students
                .Include(s => s.Guardian)
                .Include(s => s.SectionEnrollments)
                    .ThenInclude(e => e.Section)
                        .ThenInclude(sec => sec.GradeLevel)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return null;

            // Get enrollment info - try exact match first, then fallback to latest enrollment
            var enrollment = await _context.StudentSectionEnrollments
                .Include(e => e.Section!)
                    .ThenInclude(s => s.GradeLevel)
                .FirstOrDefaultAsync(e => e.StudentId == studentId &&
                                        e.SectionId == sectionId &&
                                        e.SchoolYear == schoolYear);

            // Fallback: if no exact match, get latest enrollment for this student
            if (enrollment == null)
            {
                enrollment = await _context.StudentSectionEnrollments
                    .Include(e => e.Section!)
                        .ThenInclude(s => s.GradeLevel)
                    .Where(e => e.StudentId == studentId && e.Status == "Enrolled")
                    .OrderByDescending(e => e.CreatedAt)
                    .FirstOrDefaultAsync();
            }

            // Build address strings with null safety
            var currentAddress = BuildAddressString(
                student.HseNo, student.Street, student.Brngy, 
                student.City, student.Province, student.Country, student.ZipCode);

            var permanentAddress = BuildAddressString(
                student.PhseNo, student.Pstreet, student.Pbrngy,
                student.Pcity, student.Pprovince, student.Pcountry, student.PzipCode);

            var guardianAddress = ""; // Guardian doesn't have address in current model

            // Log for debugging
            _logger?.LogInformation("Student found: {StudentId}, Name: {FirstName} {LastName}, Guardian: {GuardianId}", 
                student.StudentId, student.FirstName, student.LastName, student.GuardianId);
            
            if (student.Guardian != null)
            {
                _logger?.LogInformation("Guardian found: {GuardianName}, Relationship: {Relationship}, Contact: {ContactNum}",
                    $"{student.Guardian.FirstName} {student.Guardian.LastName}", 
                    student.Guardian.Relationship, 
                    student.Guardian.ContactNum);
            }
            else
            {
                _logger?.LogWarning("Guardian is null for student {StudentId}", student.StudentId);
            }

            // Get contact number from guardian if available
            var contactNumber = student.Guardian?.ContactNum ?? "";

            // Build guardian name safely
            string guardianName = "";
            if (student.Guardian != null)
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(student.Guardian.FirstName)) parts.Add(student.Guardian.FirstName);
                if (!string.IsNullOrWhiteSpace(student.Guardian.MiddleName)) parts.Add(student.Guardian.MiddleName);
                if (!string.IsNullOrWhiteSpace(student.Guardian.LastName)) parts.Add(student.Guardian.LastName);
                if (!string.IsNullOrWhiteSpace(student.Guardian.Suffix)) parts.Add(student.Guardian.Suffix);
                guardianName = string.Join(" ", parts).Trim();
            }

            var result = new StudentDetailsItem
            {
                StudentId = student.StudentId ?? "",
                FirstName = student.FirstName ?? "",
                MiddleName = student.MiddleName ?? "",
                LastName = student.LastName ?? "",
                Suffix = student.Suffix ?? "",
                Birthdate = student.Birthdate,
                CurrentAddress = currentAddress ?? "",
                PermanentAddress = permanentAddress ?? "",
                ContactNumber = contactNumber ?? "",
                GuardianName = guardianName,
                GuardianRelationship = student.Guardian?.Relationship ?? "",
                GuardianPhone = student.Guardian?.ContactNum ?? "",
                GuardianAddress = guardianAddress ?? "",
                GradeLevel = enrollment?.Section?.GradeLevel?.GradeLevelName ?? student.GradeLevel ?? "",
                Section = enrollment?.Section?.SectionName ?? "",
                EnrollmentStatus = enrollment?.Status ?? student.Status ?? "",
                SchoolYear = schoolYear ?? "",
                SpecialNotes = ""
            };

            _logger?.LogInformation("Mapped student details: StudentId={StudentId}, Name={FirstName} {LastName}, Guardian={GuardianName}", 
                result.StudentId, result.FirstName, result.LastName, result.GuardianName);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting student details for student {StudentId}", studentId);
            return null;
        }
    }

    private string BuildAddressString(string? houseNo, string? street, string? barangay, 
        string? city, string? province, string? country, string? zipCode)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(houseNo)) parts.Add(houseNo);
        if (!string.IsNullOrEmpty(street)) parts.Add(street);
        if (!string.IsNullOrEmpty(barangay)) parts.Add(barangay);
        if (!string.IsNullOrEmpty(city)) parts.Add(city);
        if (!string.IsNullOrEmpty(province)) parts.Add(province);
        if (!string.IsNullOrEmpty(country)) parts.Add(country);
        if (!string.IsNullOrEmpty(zipCode)) parts.Add(zipCode);
        return string.Join(", ", parts);
    }

    #endregion

    #region Helper Methods

    private string GetDayOfWeekAbbreviation(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => "M",
            DayOfWeek.Tuesday => "T",
            DayOfWeek.Wednesday => "W",
            DayOfWeek.Thursday => "TH",
            DayOfWeek.Friday => "F",
            DayOfWeek.Saturday => "Sat",
            DayOfWeek.Sunday => "Sun",
            _ => ""
        };
    }

    private async Task<string> GetCurrentSchoolYearAsync()
    {
        try
        {
            // Get active school year from database
            var activeSchoolYear = await _context.SchoolYears
                .Where(sy => sy.IsActive && sy.IsOpen)
                .Select(sy => sy.SchoolYearName)
                .FirstOrDefaultAsync();
            
            if (!string.IsNullOrEmpty(activeSchoolYear))
            {
                return activeSchoolYear;
            }
            
            // Fallback to calculated school year
            var currentYear = DateTime.Now.Year;
            var currentMonth = DateTime.Now.Month;
            if (currentMonth >= 6)
            {
                return $"{currentYear}-{currentYear + 1}";
            }
            else
            {
                return $"{currentYear - 1}-{currentYear}";
            }
        }
        catch
        {
            // Final fallback
            var currentYear = DateTime.Now.Year;
            var nextYear = currentYear + 1;
            return $"{currentYear}-{nextYear}";
        }
    }

    private string GetCurrentGradingPeriod()
    {
        var month = DateTime.Now.Month;
        return month switch
        {
            >= 1 and <= 3 => "Q1",
            >= 4 and <= 6 => "Q2",
            >= 7 and <= 9 => "Q3",
            >= 10 and <= 12 => "Q4",
            _ => "Q1"
        };
    }

    private int GetDayOrder(string dayOfWeek)
    {
        return dayOfWeek.ToUpper() switch
        {
            "M" => 1,
            "T" => 2,
            "W" => 3,
            "TH" => 4,
            "F" => 5,
            "SAT" => 6,
            "SUN" => 7,
            _ => 99
        };
    }

    #endregion
}

#region DTOs

public class TodayScheduleItem
{
    public int ScheduleId { get; set; }
    public string SectionName { get; set; } = "";
    public string GradeLevel { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public string RoomName { get; set; } = "";
    public string BuildingName { get; set; } = "";
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Role { get; set; } = "";
}

public class PendingAttendanceItem
{
    public int SectionId { get; set; }
    public string SectionName { get; set; } = "";
    public string GradeLevel { get; set; } = "";
    public int StudentCount { get; set; }
}

public class PendingGradeItem
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public int SectionId { get; set; }
    public string SectionName { get; set; } = "";
    public string GradeLevel { get; set; } = "";
    public string GradingPeriod { get; set; } = "";
    public int StudentCount { get; set; }
}

public class AssignedSectionItem
{
    public int SectionId { get; set; }
    public string SectionName { get; set; } = "";
    public string GradeLevel { get; set; } = "";
    public int GradeLevelId { get; set; }
    public string ClassroomName { get; set; } = "";
    public string BuildingName { get; set; } = "";
    public int Capacity { get; set; }
    public int StudentCount { get; set; }
    public List<string> Subjects { get; set; } = new();
    public bool IsAdviser { get; set; }
    public List<SectionScheduleItem> Schedules { get; set; } = new();
}

public class SectionScheduleItem
{
    public int ScheduleId { get; set; }
    public string DayOfWeek { get; set; } = "";
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string RoomName { get; set; } = "";
    public string SubjectName { get; set; } = "";
}

public class ClassRosterItem
{
    public string StudentId { get; set; } = "";
    public string StudentSystemId { get; set; } = "";
    public string StudentName { get; set; } = "";
    public DateTime EnrollmentDate { get; set; }
}

public class SectionDetailsItem
{
    public int SectionId { get; set; }
    public string SectionName { get; set; } = "";
    public string GradeLevel { get; set; } = "";
    public string SchoolYear { get; set; } = "";
    public string AdviserName { get; set; } = "";
    public List<string> CoTeachers { get; set; } = new();
    public List<string> Subjects { get; set; } = new();
    public int StudentCount { get; set; }
    public int Capacity { get; set; }
    public string ClassroomName { get; set; } = "";
    public string BuildingName { get; set; } = "";
}

public class EnrolledStudentItem
{
    public string StudentId { get; set; } = "";
    public string StudentSystemId { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime EnrollmentDate { get; set; }
}

public class StudentDetailsItem
{
    public string StudentId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string MiddleName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Suffix { get; set; } = "";
    public DateTime Birthdate { get; set; }
    public string CurrentAddress { get; set; } = "";
    public string PermanentAddress { get; set; } = "";
    public string ContactNumber { get; set; } = "";
    public string GuardianName { get; set; } = "";
    public string GuardianRelationship { get; set; } = "";
    public string GuardianPhone { get; set; } = "";
    public string GuardianAddress { get; set; } = "";
    public string GradeLevel { get; set; } = "";
    public string Section { get; set; } = "";
    public string EnrollmentStatus { get; set; } = "";
    public string SchoolYear { get; set; } = "";
    public string SpecialNotes { get; set; } = "";
}

#endregion

