using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Audit;

// Service for creating and managing audit log entries
public class AuditLogService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditLogService>? _logger;

    public AuditLogService(AppDbContext context, ILogger<AuditLogService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    // Create general audit log entry
    public async Task CreateLogAsync(
        string action,
        string? module = null,
        string? description = null,
        string? userName = null,
        string? userRole = null,
        int? userId = null,
        string? ipAddress = null,
        string? status = "Success",
        string? severity = "Low")
    {
        try
        {
            // Check if the audit logs table exists before attempting to save
            if (!await TableExistsAsync("tbl_audit_logs"))
            {
                _logger?.LogWarning("Audit log table 'tbl_audit_logs' does not exist. Skipping audit log creation. Please ensure database initialization runs.");
                return;
            }

            var log = new AuditLog
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
                Severity = severity
            };

            // Add log and save
            _context.AuditLogs.Add(log);
            var saved = await _context.SaveChangesAsync();
            
            if (saved > 0)
            {
                _logger?.LogInformation("Audit log created: {Action} by {UserName} (LogId: {LogId})", action, userName ?? "Unknown", log.LogId);
                System.Diagnostics.Debug.WriteLine($"✓ Audit log saved: {action} by {userName ?? "Unknown"} at {DateTime.Now:yyyy-MM-dd HH:mm:ss} (LogId: {log.LogId})");
            }
            else
            {
                _logger?.LogWarning("Audit log was not saved: {Action} by {UserName}", action, userName ?? "Unknown");
                System.Diagnostics.Debug.WriteLine($"✗ Audit log was not saved: {action} by {userName ?? "Unknown"}");
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
        {
            _logger?.LogWarning("Audit log table 'tbl_audit_logs' does not exist. Skipping audit log creation. Error: {Message}", sqlEx.Message);
            System.Diagnostics.Debug.WriteLine($"✗ Audit log table does not exist: {sqlEx.Message}");
            // Don't throw - audit logging should not break the main operation
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 207 || sqlEx.Number == 8152) // Invalid column name or String or binary data would be truncated
        {
            _logger?.LogWarning("Audit log table missing columns. Error: {Message}. Please run Add_Transaction_Columns_To_AuditLogs.sql", sqlEx.Message);
            System.Diagnostics.Debug.WriteLine($"✗ Audit log table missing columns: {sqlEx.Message}");
            // Don't throw - audit logging should not break the main operation
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating audit log entry: {Message}", ex.Message);
            System.Diagnostics.Debug.WriteLine($"✗ Error creating audit log: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
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
        string? severity = "Low")
    {
        try
        {
            // Check if the audit logs table exists before attempting to save
            if (!await TableExistsAsync("tbl_audit_logs"))
            {
                _logger?.LogWarning("Audit log table 'tbl_audit_logs' does not exist. Skipping audit log creation. Please ensure database initialization runs.");
                return;
            }

            var log = new AuditLog
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
                Severity = severity
            };

            _context.AuditLogs.Add(log);
            var saved = await _context.SaveChangesAsync();
            
            if (saved > 0)
            {
                _logger?.LogInformation("Transaction audit log created: {Action} by {UserName} (LogId: {LogId})", action, userName ?? "Unknown", log.LogId);
                System.Diagnostics.Debug.WriteLine($"✓ Transaction audit log saved: {action} by {userName ?? "Unknown"} at {DateTime.Now:yyyy-MM-dd HH:mm:ss} (LogId: {log.LogId})");
            }
            else
            {
                _logger?.LogWarning("Transaction audit log was not saved: {Action} by {UserName}", action, userName ?? "Unknown");
                System.Diagnostics.Debug.WriteLine($"✗ Transaction audit log was not saved: {action} by {userName ?? "Unknown"}");
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
        {
            _logger?.LogWarning("Audit log table 'tbl_audit_logs' does not exist. Skipping audit log creation. Error: {Message}", sqlEx.Message);
            System.Diagnostics.Debug.WriteLine($"✗ Audit log table does not exist: {sqlEx.Message}");
            // Don't throw - audit logging should not break the main operation
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 207 || sqlEx.Number == 8152) // Invalid column name or String or binary data would be truncated
        {
            _logger?.LogWarning("Audit log table missing columns. Error: {Message}. Please run Add_Transaction_Columns_To_AuditLogs.sql", sqlEx.Message);
            System.Diagnostics.Debug.WriteLine($"✗ Audit log table missing columns: {sqlEx.Message}");
            // Don't throw - audit logging should not break the main operation
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating transaction audit log entry: {Message}", ex.Message);
            System.Diagnostics.Debug.WriteLine($"✗ Error creating transaction audit log: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            // Don't throw - audit logging should not break the main operation
        }
    }

    // Create enhanced audit log entry for student registration
    public async Task CreateStudentRegistrationLogAsync(
        string studentId,
        string studentName,
        string? grade,
        string studentStatus,
        int? registrarId,
        string? registrarName = null,
        string? ipAddress = null)
    {
        try
        {
            // Check if the audit logs table exists before attempting to save
            if (!await TableExistsAsync("tbl_audit_logs"))
            {
                _logger?.LogWarning("Audit log table 'tbl_audit_logs' does not exist. Skipping audit log creation. Please ensure database initialization runs.");
                return;
            }

            var description = $"Student registered: {studentName} (ID: {studentId}, Grade: {grade ?? "N/A"}, Status: {studentStatus})";
            
            var log = new AuditLog
            {
                Timestamp = DateTime.Now,
                Action = "Student Registration",
                Module = "Student Registration",
                Description = description,
                UserName = registrarName,
                UserRole = "Registrar",
                UserId = registrarId,
                RegistrarId = registrarId,
                StudentId = studentId,
                StudentName = studentName,
                Grade = grade,
                StudentStatus = studentStatus,
                IpAddress = ipAddress,
                Status = "Success",
                Severity = "Medium"
            };

            _context.AuditLogs.Add(log);
            var saved = await _context.SaveChangesAsync();
            
            if (saved > 0)
            {
                _logger?.LogInformation(
                    "Student registration audit log created: Student {StudentId} ({StudentName}) registered by Registrar {RegistrarId} (LogId: {LogId})",
                    studentId, studentName, registrarId, log.LogId);
                System.Diagnostics.Debug.WriteLine($"✓ Student registration audit log saved: {studentId} by Registrar {registrarId} (LogId: {log.LogId})");
            }
            else
            {
                _logger?.LogWarning("Student registration audit log was not saved: Student {StudentId}", studentId);
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
        {
            _logger?.LogWarning("Audit log table 'tbl_audit_logs' does not exist. Skipping audit log creation. Error: {Message}", sqlEx.Message);
            // Don't throw - audit logging should not break the main operation
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating student registration audit log: {Message}", ex.Message);
            // Don't throw - audit logging should not break the main operation
        }
    }

    // Check if table exists in database
    private async Task<bool> TableExistsAsync(string tableName)
    {
        try
        {
            // Simple approach: try to query the table directly
            // This is more reliable than checking sys.tables
            await _context.Database.ExecuteSqlRawAsync($"SELECT TOP 1 log_id FROM [dbo].[{tableName}]");
            return true;
        }
        catch (SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
        {
            // Table doesn't exist
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error checking if table {TableName} exists: {Message}", tableName, ex.Message);
            return false;
        }
    }

    // Get audit logs with optional filtering
    public async Task<List<AuditLog>> GetAuditLogsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? module = null,
        string? action = null,
        string? status = null,
        int? limit = null)
    {
        try
        {
            // Check if table exists first
            if (!await TableExistsAsync("tbl_audit_logs"))
            {
                _logger?.LogWarning("Audit log table 'tbl_audit_logs' does not exist. Returning empty list.");
                return new List<AuditLog>();
            }

            var query = _context.AuditLogs.AsQueryable();

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
            _logger?.LogInformation("Retrieved {Count} audit logs from database", logs.Count);
            return logs;
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
        {
            _logger?.LogWarning("Audit log table 'tbl_audit_logs' does not exist. Returning empty list. Error: {Message}", sqlEx.Message);
            return new List<AuditLog>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving audit logs: {Message}", ex.Message);
            System.Diagnostics.Debug.WriteLine($"Error retrieving audit logs: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            // Return empty list instead of throwing to prevent UI crashes
            return new List<AuditLog>();
        }
    }

    // Get total count of audit logs
    public async Task<int> GetTotalLogsCountAsync()
    {
        try
        {
            if (!await TableExistsAsync("tbl_audit_logs"))
                return 0;

            return await _context.AuditLogs.CountAsync();
        }
        catch
        {
            return 0;
        }
    }

    // Get count of failed audit logs
    public async Task<int> GetFailedLogsCountAsync()
    {
        try
        {
            if (!await TableExistsAsync("tbl_audit_logs"))
                return 0;

            return await _context.AuditLogs
                .Where(l => l.Status == "Failed")
                .CountAsync();
        }
        catch
        {
            return 0;
        }
    }

    // Get count of high severity audit logs
    public async Task<int> GetHighSeverityLogsCountAsync()
    {
        try
        {
            if (!await TableExistsAsync("tbl_audit_logs"))
                return 0;

            return await _context.AuditLogs
                .Where(l => l.Severity == "High" || l.Status == "Failed")
                .CountAsync();
        }
        catch
        {
            return 0;
        }
    }

    // Get count of recent audit logs (last 7 days)
    public async Task<int> GetRecentLogsCountAsync()
    {
        try
        {
            if (!await TableExistsAsync("tbl_audit_logs"))
                return 0;

            var sevenDaysAgo = DateTime.Now.AddDays(-7);
            return await _context.AuditLogs
                .Where(l => l.Timestamp >= sevenDaysAgo)
                .CountAsync();
        }
        catch
        {
            return 0;
        }
    }
}

