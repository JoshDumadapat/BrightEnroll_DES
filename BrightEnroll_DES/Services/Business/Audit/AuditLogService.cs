using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Audit;

/// <summary>
/// Service for creating and managing audit log entries.
/// Enhanced to support detailed student registration logging.
/// </summary>
public class AuditLogService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditLogService>? _logger;

    public AuditLogService(AppDbContext context, ILogger<AuditLogService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    /// <summary>
    /// Creates a general audit log entry.
    /// </summary>
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

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
            
            _logger?.LogInformation("Audit log created: {Action} by {UserName}", action, userName ?? "Unknown");
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
        {
            _logger?.LogWarning("Audit log table 'tbl_audit_logs' does not exist. Skipping audit log creation. Error: {Message}", sqlEx.Message);
            // Don't throw - audit logging should not break the main operation
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating audit log entry: {Message}", ex.Message);
            // Don't throw - audit logging should not break the main operation
        }
    }

    /// <summary>
    /// Creates an enhanced audit log entry for student registration.
    /// Includes: Student ID, Student Name, Grade, Status, and Registrar ID.
    /// </summary>
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
            await _context.SaveChangesAsync();
            
            _logger?.LogInformation(
                "Student registration audit log created: Student {StudentId} ({StudentName}) registered by Registrar {RegistrarId}",
                studentId, studentName, registrarId);
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

    /// <summary>
    /// Checks if a table exists in the database.
    /// </summary>
    private async Task<bool> TableExistsAsync(string tableName)
    {
        try
        {
            var sql = @"
                SELECT COUNT(*) 
                FROM sys.tables 
                WHERE name = {0} 
                AND schema_id = SCHEMA_ID('dbo')";
            
            var result = await _context.Database
                .SqlQueryRaw<int>(sql, tableName)
                .FirstOrDefaultAsync();
            return result > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates an audit log entry for dropping/archiving a student.
    /// </summary>
    public async Task CreateStudentDropLogAsync(
        string studentId,
        string studentName,
        string archiveReason,
        string? droppedBy = null,
        int? droppedById = null,
        string? userRole = null,
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

            var description = $"Student dropped/archived: {studentName} (ID: {studentId}, Status: {archiveReason})";
            
            var log = new AuditLog
            {
                Timestamp = DateTime.Now,
                Action = "Drop Student",
                Module = "Student Record",
                Description = description,
                UserName = droppedBy,
                UserRole = userRole,
                UserId = droppedById,
                StudentId = studentId,
                StudentName = studentName,
                StudentStatus = archiveReason,
                IpAddress = ipAddress,
                Status = "Success",
                Severity = "Medium"
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
            
            _logger?.LogInformation(
                "Student drop audit log created: Student {StudentId} ({StudentName}) dropped by {DroppedBy} (ID: {DroppedById})",
                studentId, studentName, droppedBy ?? "Unknown", droppedById);
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
        {
            _logger?.LogWarning("Audit log table 'tbl_audit_logs' does not exist. Skipping audit log creation. Error: {Message}", sqlEx.Message);
            // Don't throw - audit logging should not break the main operation
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating student drop audit log: {Message}", ex.Message);
            // Don't throw - audit logging should not break the main operation
        }
    }

    /// <summary>
    /// Gets audit logs with optional filtering.
    /// </summary>
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

            return await query.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving audit logs: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets the audit log entry for dropping a specific student.
    /// </summary>
    public async Task<AuditLog?> GetStudentDropLogAsync(string studentId)
    {
        try
        {
            var log = await _context.AuditLogs
                .Where(l => l.StudentId == studentId && l.Action == "Drop Student")
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefaultAsync();

            return log;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving student drop audit log for {StudentId}: {Message}", studentId, ex.Message);
            return null;
        }
    }
}

