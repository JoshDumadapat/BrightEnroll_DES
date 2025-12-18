using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models.SuperAdmin;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.SuperAdmin;

// Service for creating and managing SuperAdmin audit log entries
public class SuperAdminAuditLogService
{
    private readonly SuperAdminDbContext _context;
    private readonly ILogger<SuperAdminAuditLogService>? _logger;

    public SuperAdminAuditLogService(SuperAdminDbContext context, ILogger<SuperAdminAuditLogService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    // Create general SuperAdmin audit log entry
    public async Task CreateLogAsync(
        string action,
        string? module = null,
        string? description = null,
        string? userName = null,
        string? userRole = null,
        int? userId = null,
        string? ipAddress = null,
        string? status = "Success",
        string? severity = "Low",
        string? customerCode = null,
        string? customerName = null)
    {
        try
        {
            // Check if the audit logs table exists before attempting to save
            if (!await TableExistsAsync("tbl_SuperAdminAuditLogs"))
            {
                _logger?.LogWarning("SuperAdmin audit log table 'tbl_SuperAdminAuditLogs' does not exist. Skipping audit log creation.");
                return;
            }

            var log = new SuperAdminAuditLog
            {
                Timestamp = DateTime.Now,
                Action = action,
                Module = module,
                Description = description,
                UserName = userName,
                UserRole = userRole,
                UserId = userId,
                IpAddress = ipAddress,
                Status = status,
                Severity = severity,
                CustomerCode = customerCode,
                CustomerName = customerName
            };

            _context.SuperAdminAuditLogs.Add(log);
            var saved = await _context.SaveChangesAsync();
            
            if (saved > 0)
            {
                _logger?.LogInformation("SuperAdmin audit log created: {Action} by {UserName} (LogId: {LogId})", action, userName ?? "Unknown", log.LogId);
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
        {
            _logger?.LogWarning("SuperAdmin audit log table 'tbl_SuperAdminAuditLogs' does not exist. Skipping audit log creation.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating SuperAdmin audit log entry: {Message}", ex.Message);
            // Don't throw - audit logging should not break the main operation
        }
    }

    // Create transaction audit log entry with entity tracking
    public async Task CreateTransactionLogAsync(
        string action,
        string? module = null,
        string? description = null,
        string? userName = null,
        string? userRole = null,
        int? userId = null,
        string? entityType = null,
        string? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        string? status = "Success",
        string? severity = "Low",
        string? customerCode = null,
        string? customerName = null)
    {
        try
        {
            if (!await TableExistsAsync("tbl_SuperAdminAuditLogs"))
            {
                _logger?.LogWarning("SuperAdmin audit log table 'tbl_SuperAdminAuditLogs' does not exist. Skipping audit log creation.");
                return;
            }

            var log = new SuperAdminAuditLog
            {
                Timestamp = DateTime.Now,
                Action = action,
                Module = module,
                Description = description,
                UserName = userName,
                UserRole = userRole,
                UserId = userId,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = ipAddress,
                Status = status,
                Severity = severity,
                CustomerCode = customerCode,
                CustomerName = customerName
            };

            _context.SuperAdminAuditLogs.Add(log);
            var saved = await _context.SaveChangesAsync();
            
            if (saved > 0)
            {
                _logger?.LogInformation("SuperAdmin transaction audit log created: {Action} by {UserName} (LogId: {LogId})", action, userName ?? "Unknown", log.LogId);
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208)
        {
            _logger?.LogWarning("SuperAdmin audit log table 'tbl_SuperAdminAuditLogs' does not exist. Skipping audit log creation.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating SuperAdmin transaction audit log entry: {Message}", ex.Message);
        }
    }

    // Check if table exists in database
    private async Task<bool> TableExistsAsync(string tableName)
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync($"SELECT TOP 1 log_id FROM [dbo].[{tableName}]");
            return true;
        }
        catch (SqlException sqlEx) when (sqlEx.Number == 208)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error checking if table {TableName} exists: {Message}", tableName, ex.Message);
            return false;
        }
    }

    // Get SuperAdmin audit logs with optional filtering
    public async Task<List<SuperAdminAuditLog>> GetAuditLogsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? module = null,
        string? action = null,
        string? status = null,
        int? limit = null)
    {
        try
        {
            if (!await TableExistsAsync("tbl_SuperAdminAuditLogs"))
            {
                _logger?.LogWarning("SuperAdmin audit log table 'tbl_SuperAdminAuditLogs' does not exist. Returning empty list.");
                return new List<SuperAdminAuditLog>();
            }

            var query = _context.SuperAdminAuditLogs.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(l => l.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(l => l.Timestamp <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(module))
            {
                query = query.Where(l => l.Module == module);
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(l => l.Action == action);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l => l.Status == status);
            }

            query = query.OrderByDescending(l => l.Timestamp);

            if (limit.HasValue)
            {
                query = query.Take(limit.Value);
            }

            var logs = await query.ToListAsync();
            _logger?.LogInformation("Retrieved {Count} SuperAdmin audit logs from database", logs.Count);
            return logs;
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208)
        {
            _logger?.LogWarning("SuperAdmin audit log table 'tbl_SuperAdminAuditLogs' does not exist. Returning empty list.");
            return new List<SuperAdminAuditLog>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving SuperAdmin audit logs: {Message}", ex.Message);
            return new List<SuperAdminAuditLog>();
        }
    }

    // Get total count of SuperAdmin audit logs
    public async Task<int> GetTotalLogsCountAsync()
    {
        try
        {
            if (!await TableExistsAsync("tbl_SuperAdminAuditLogs"))
                return 0;

            return await _context.SuperAdminAuditLogs.CountAsync();
        }
        catch
        {
            return 0;
        }
    }

    // Get count of failed SuperAdmin audit logs
    public async Task<int> GetFailedLogsCountAsync()
    {
        try
        {
            if (!await TableExistsAsync("tbl_SuperAdminAuditLogs"))
                return 0;

            return await _context.SuperAdminAuditLogs
                .Where(l => l.Status == "Failed")
                .CountAsync();
        }
        catch
        {
            return 0;
        }
    }

    // Get count of high severity SuperAdmin audit logs
    public async Task<int> GetHighSeverityLogsCountAsync()
    {
        try
        {
            if (!await TableExistsAsync("tbl_SuperAdminAuditLogs"))
                return 0;

            return await _context.SuperAdminAuditLogs
                .Where(l => l.Severity == "High" || l.Severity == "Critical" || l.Status == "Failed")
                .CountAsync();
        }
        catch
        {
            return 0;
        }
    }

    // Get count of recent SuperAdmin audit logs (last 7 days)
    public async Task<int> GetRecentLogsCountAsync()
    {
        try
        {
            if (!await TableExistsAsync("tbl_SuperAdminAuditLogs"))
                return 0;

            var sevenDaysAgo = DateTime.Now.AddDays(-7);
            return await _context.SuperAdminAuditLogs
                .Where(l => l.Timestamp >= sevenDaysAgo)
                .CountAsync();
        }
        catch
        {
            return 0;
        }
    }
}
