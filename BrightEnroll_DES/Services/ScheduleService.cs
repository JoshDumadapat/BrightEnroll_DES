using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services;

// Handles schedule management for teachers
public class ScheduleService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ScheduleService>? _logger;

    public ScheduleService(AppDbContext context, ILogger<ScheduleService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    // Get all schedules for a specific teacher (through their classes)
    public async Task<List<Schedule>> GetSchedulesByTeacherIdAsync(int teacherId)
    {
        try
        {
            return await _context.Schedules
                .Include(s => s.Class)
                .Where(s => s.Class != null && s.Class.TeacherId == teacherId && s.IsActive)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting schedules for teacher {TeacherId}", teacherId);
            throw;
        }
    }

    // Get schedules for a specific teacher filtered by day of week
    public async Task<List<Schedule>> GetSchedulesByTeacherIdAndDayAsync(int teacherId, string dayOfWeek)
    {
        try
        {
            return await _context.Schedules
                .Include(s => s.Class)
                .Where(s => s.Class != null && 
                           s.Class.TeacherId == teacherId && 
                           s.DayOfWeek == dayOfWeek && 
                           s.IsActive)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting schedules for teacher {TeacherId} on {DayOfWeek}", teacherId, dayOfWeek);
            throw;
        }
    }

    // Get all schedules for a specific class
    public async Task<List<Schedule>> GetSchedulesByClassIdAsync(int classId)
    {
        try
        {
            return await _context.Schedules
                .Include(s => s.Class)
                .Where(s => s.ClassId == classId && s.IsActive)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting schedules for class {ClassId}", classId);
            throw;
        }
    }

    // Get schedules for a teacher with class information for display
    public async Task<List<ScheduleWithClassInfo>> GetSchedulesWithClassInfoByTeacherIdAsync(int teacherId)
    {
        try
        {
            return await _context.Schedules
                .Include(s => s.Class)
                .Where(s => s.Class != null && s.Class.TeacherId == teacherId && s.IsActive)
                .Select(s => new ScheduleWithClassInfo
                {
                    ScheduleId = s.ScheduleId,
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    DurationMinutes = s.DurationMinutes,
                    Subject = s.Class!.Subject,
                    ClassName = $"{s.Class.GradeLevel} - {s.Class.Section}",
                    Room = s.Class.Room,
                    ClassId = s.ClassId
                })
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting schedules with class info for teacher {TeacherId}", teacherId);
            throw;
        }
    }

    // Get today's schedules for a teacher
    public async Task<List<TodayScheduleInfo>> GetTodaySchedulesByTeacherIdAsync(int teacherId)
    {
        try
        {
            var today = DateTime.Now;
            var dayOfWeek = today.DayOfWeek.ToString();
            
            // Map DayOfWeek enum to string format used in database
            var dayMapping = new Dictionary<DayOfWeek, string>
            {
                { DayOfWeek.Monday, "Monday" },
                { DayOfWeek.Tuesday, "Tuesday" },
                { DayOfWeek.Wednesday, "Wednesday" },
                { DayOfWeek.Thursday, "Thursday" },
                { DayOfWeek.Friday, "Friday" },
                { DayOfWeek.Saturday, "Saturday" },
                { DayOfWeek.Sunday, "Sunday" }
            };

            var todayDayName = dayMapping[today.DayOfWeek];

            return await _context.Schedules
                .Include(s => s.Class)
                .Where(s => s.Class != null && 
                           s.Class.TeacherId == teacherId && 
                           s.DayOfWeek == todayDayName && 
                           s.IsActive)
                .Select(s => new TodayScheduleInfo
                {
                    Subject = s.Class!.Subject,
                    GradeLevel = s.Class.GradeLevel,
                    Section = s.Class.Section,
                    Time = $"{s.StartTime} - {s.EndTime}",
                    Room = s.Class.Room
                })
                .OrderBy(s => s.Time)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting today's schedules for teacher {TeacherId}", teacherId);
            throw;
        }
    }

    // Get count of classes scheduled for today
    public async Task<int> GetTodayClassesCountAsync(int teacherId)
    {
        try
        {
            var today = DateTime.Now;
            var dayMapping = new Dictionary<DayOfWeek, string>
            {
                { DayOfWeek.Monday, "Monday" },
                { DayOfWeek.Tuesday, "Tuesday" },
                { DayOfWeek.Wednesday, "Wednesday" },
                { DayOfWeek.Thursday, "Thursday" },
                { DayOfWeek.Friday, "Friday" },
                { DayOfWeek.Saturday, "Saturday" },
                { DayOfWeek.Sunday, "Sunday" }
            };

            var todayDayName = dayMapping[today.DayOfWeek];

            return await _context.Schedules
                .Include(s => s.Class)
                .Where(s => s.Class != null && 
                           s.Class.TeacherId == teacherId && 
                           s.DayOfWeek == todayDayName && 
                           s.IsActive)
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting today's classes count for teacher {TeacherId}", teacherId);
            throw;
        }
    }

    // Create a new schedule
    public async Task<Schedule> CreateScheduleAsync(Schedule schedule)
    {
        try
        {
            schedule.CreatedDate = DateTime.Now;
            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();
            
            _logger?.LogInformation("Schedule created with ID: {ScheduleId}", schedule.ScheduleId);
            return schedule;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating schedule");
            throw;
        }
    }

    // Create multiple schedules at once (for a class with multiple time slots)
    public async Task<List<Schedule>> CreateSchedulesAsync(List<Schedule> schedules)
    {
        try
        {
            foreach (var schedule in schedules)
            {
                schedule.CreatedDate = DateTime.Now;
            }
            
            _context.Schedules.AddRange(schedules);
            await _context.SaveChangesAsync();
            
            _logger?.LogInformation("Created {Count} schedules", schedules.Count);
            return schedules;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating schedules");
            throw;
        }
    }

    // Update an existing schedule
    public async Task<Schedule> UpdateScheduleAsync(Schedule schedule)
    {
        try
        {
            schedule.UpdatedDate = DateTime.Now;
            _context.Schedules.Update(schedule);
            await _context.SaveChangesAsync();
            
            _logger?.LogInformation("Schedule updated with ID: {ScheduleId}", schedule.ScheduleId);
            return schedule;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating schedule {ScheduleId}", schedule.ScheduleId);
            throw;
        }
    }

    // Delete a schedule (soft delete by setting IsActive to false)
    public async Task<bool> DeleteScheduleAsync(int scheduleId)
    {
        try
        {
            var schedule = await _context.Schedules.FindAsync(scheduleId);
            if (schedule == null)
            {
                return false;
            }

            schedule.IsActive = false;
            schedule.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            
            _logger?.LogInformation("Schedule deleted (soft) with ID: {ScheduleId}", scheduleId);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting schedule {ScheduleId}", scheduleId);
            throw;
        }
    }

    // Hard delete a schedule
    public async Task<bool> HardDeleteScheduleAsync(int scheduleId)
    {
        try
        {
            var schedule = await _context.Schedules.FindAsync(scheduleId);
            if (schedule == null)
            {
                return false;
            }

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();
            
            _logger?.LogInformation("Schedule hard deleted with ID: {ScheduleId}", scheduleId);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error hard deleting schedule {ScheduleId}", scheduleId);
            throw;
        }
    }

    // Get a single schedule by ID
    public async Task<Schedule?> GetScheduleByIdAsync(int scheduleId)
    {
        try
        {
            return await _context.Schedules
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting schedule {ScheduleId}", scheduleId);
            throw;
        }
    }
}

// DTO for schedule with class information
public class ScheduleWithClassInfo
{
    public int ScheduleId { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int? DurationMinutes { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public int ClassId { get; set; }
}

// DTO for today's schedule information
public class TodayScheduleInfo
{
    public string Subject { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
}

