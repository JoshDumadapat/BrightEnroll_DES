using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Services.Business.Audit;
using BrightEnroll_DES.Services.Authentication;

namespace BrightEnroll_DES.Services.Business.HR;

public class TimeRecordService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TimeRecordService>? _logger;
    private readonly IServiceScopeFactory? _serviceScopeFactory;
    private readonly IAuthService? _authService;

    public TimeRecordService(
        AppDbContext context, 
        ILogger<TimeRecordService>? logger = null,
        IServiceScopeFactory? serviceScopeFactory = null,
        IAuthService? authService = null)
    {
        _context = context;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _authService = authService;
    }

    /// <summary>
    /// Saves time records for a specific period. If records already exist, they will be replaced.
    /// </summary>
    public async Task<int> SaveTimeRecordsAsync(List<TimeRecordUploadDto> records, string period)
    {
        try
        {
            if (records == null || !records.Any())
            {
                _logger?.LogWarning("No time records to save for period {Period}", period);
                return 0;
            }

            int savedCount = 0;
            var now = DateTime.Now;

            foreach (var recordDto in records)
            {
                // Find user by EmployeeId (SystemId)
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.SystemId == recordDto.EmployeeId);

                if (user == null)
                {
                    _logger?.LogWarning("User with EmployeeId {EmployeeId} not found, skipping time record", recordDto.EmployeeId);
                    continue;
                }

                // Check if record already exists for this user and period
                var existingRecord = await _context.TimeRecords
                    .FirstOrDefaultAsync(tr => tr.UserId == user.UserId && tr.Period == period);

                if (existingRecord != null)
                {
                    // Update existing record
                    existingRecord.TimeIn = recordDto.TimeIn;
                    existingRecord.TimeOut = recordDto.TimeOut;
                    existingRecord.RegularHours = recordDto.RegularHours;
                    existingRecord.OvertimeHours = recordDto.OvertimeHours;
                    existingRecord.LeaveDays = recordDto.LeaveDays;
                    existingRecord.LateMinutes = recordDto.LateMinutes;
                    existingRecord.TotalDaysAbsent = recordDto.TotalDaysAbsent;
                    existingRecord.UpdatedAt = now;
                }
                else
                {
                    // Create new record
                    var timeRecord = new TimeRecord
                    {
                        UserId = user.UserId,
                        Period = period,
                        TimeIn = recordDto.TimeIn,
                        TimeOut = recordDto.TimeOut,
                        RegularHours = recordDto.RegularHours,
                        OvertimeHours = recordDto.OvertimeHours,
                        LeaveDays = recordDto.LeaveDays,
                        LateMinutes = recordDto.LateMinutes,
                        TotalDaysAbsent = recordDto.TotalDaysAbsent,
                        CreatedAt = now
                    };

                    _context.TimeRecords.Add(timeRecord);
                }

                savedCount++;
            }

            await _context.SaveChangesAsync();
            _logger?.LogInformation("Saved {Count} time records for period {Period}", savedCount, period);

            // Audit logging (non-blocking, background task)
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_serviceScopeFactory != null)
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var auditLogService = scope.ServiceProvider.GetRequiredService<AuditLogService>();
                        var authService = scope.ServiceProvider.GetService<IAuthService>();
                        
                        var currentUser = authService?.CurrentUser;
                        var userName = currentUser != null ? $"{currentUser.first_name} {currentUser.last_name}".Trim() : "System";
                        var userRole = currentUser?.user_role ?? "System";
                        var userId = currentUser?.user_ID;

                        await auditLogService.CreateTransactionLogAsync(
                            action: "Upload Time Records",
                            module: "HR",
                            description: $"Uploaded {savedCount} time records for period {period}",
                            userName: userName,
                            userRole: userRole,
                            userId: userId,
                            entityType: "TimeRecord",
                            entityId: period,
                            oldValues: null,
                            newValues: $"Records: {savedCount}, Period: {period}",
                            status: "Success",
                            severity: "High"
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to create audit log for time record upload: {Message}", ex.Message);
                }
            });

            return savedCount;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving time records for period {Period}: {Message}", period, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets time records for a specific period
    /// </summary>
    public async Task<List<TimeRecordDto>> GetTimeRecordsByPeriodAsync(string period)
    {
        try
        {
            var records = await _context.TimeRecords
                .Include(tr => tr.User)
                .Where(tr => tr.Period == period)
                .ToListAsync();

            var result = records.Select(tr => new TimeRecordDto
            {
                EmployeeId = tr.User?.SystemId ?? "",
                Name = $"{tr.User?.FirstName} {tr.User?.LastName}".Trim(),
                Role = tr.User?.UserRole ?? "",
                TimeIn = tr.TimeIn ?? "",
                TimeOut = tr.TimeOut ?? "",
                RegularHours = tr.RegularHours,
                OvertimeHours = tr.OvertimeHours,
                LeaveDays = tr.LeaveDays,
                LateMinutes = tr.LateMinutes,
                TotalDaysAbsent = tr.TotalDaysAbsent
            }).ToList();

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting time records for period {Period}: {Message}", period, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets time record for a specific user and period
    /// </summary>
    public async Task<TimeRecord?> GetTimeRecordAsync(int userId, string period)
    {
        try
        {
            return await _context.TimeRecords
                .FirstOrDefaultAsync(tr => tr.UserId == userId && tr.Period == period);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting time record for user {UserId} and period {Period}: {Message}", userId, period, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Deletes time records for a specific period (used when re-uploading)
    /// </summary>
    public async Task<int> DeleteTimeRecordsByPeriodAsync(string period)
    {
        try
        {
            var records = await _context.TimeRecords
                .Where(tr => tr.Period == period)
                .ToListAsync();

            if (records.Any())
            {
                _context.TimeRecords.RemoveRange(records);
                await _context.SaveChangesAsync();
                _logger?.LogInformation("Deleted {Count} time records for period {Period}", records.Count, period);
                return records.Count;
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting time records for period {Period}: {Message}", period, ex.Message);
            throw;
        }
    }
}

/// <summary>
/// DTO for time record data (used in UI)
/// </summary>
public class TimeRecordDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string TimeIn { get; set; } = string.Empty;
    public string TimeOut { get; set; } = string.Empty;
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal LeaveDays { get; set; }
    public int LateMinutes { get; set; }
    public int TotalDaysAbsent { get; set; }
}

