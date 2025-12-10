using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Services.Business.Academic;
using BrightEnroll_DES.Services.Business.Audit;
using BrightEnroll_DES.Services.Business.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.HR;

public class SalaryChangeRequestService
{
    private readonly AppDbContext _context;
    private readonly SchoolYearService _schoolYearService;
    private readonly AuditLogService _auditLogService;
    private readonly NotificationService _notificationService;
    private readonly ILogger<SalaryChangeRequestService>? _logger;

    public SalaryChangeRequestService(
        AppDbContext context,
        SchoolYearService schoolYearService,
        AuditLogService auditLogService,
        NotificationService notificationService,
        ILogger<SalaryChangeRequestService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _schoolYearService = schoolYearService ?? throw new ArgumentNullException(nameof(schoolYearService));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger;
    }

    public async Task<SalaryChangeRequest> CreateRequestAsync(
        int userId,
        decimal currentBaseSalary,
        decimal currentAllowance,
        decimal requestedBaseSalary,
        decimal requestedAllowance,
        string? reason,
        int requestedBy,
        bool isInitialRegistration = false)
    {
        try
        {
            var currentSchoolYear = _schoolYearService.GetCurrentSchoolYear();

            _logger?.LogInformation("DEBUG: Creating salary change request - UserId={UserId}, CurrentBase={CurrentBase}, RequestedBase={RequestedBase}", 
                userId, currentBaseSalary, requestedBaseSalary);

            // IMPORTANT: Create a NEW salary change request - each change creates a separate log entry
            // This ensures a complete audit trail - never updates existing records
            // Even if the same employee has multiple salary changes, each creates a new record
            var request = new SalaryChangeRequest
            {
                UserId = userId,
                CurrentBaseSalary = currentBaseSalary,
                CurrentAllowance = currentAllowance,
                RequestedBaseSalary = requestedBaseSalary,
                RequestedAllowance = requestedAllowance,
                Reason = reason,
                Status = "Pending",
                RequestedBy = requestedBy,
                RequestedAt = DateTime.Now,
                SchoolYear = currentSchoolYear,
                IsInitialRegistration = isInitialRegistration,
                EffectiveDate = null // Will be set on approval
            };

            _logger?.LogInformation("DEBUG: SalaryChangeRequest object created - RequestId will be assigned, EffectiveDate={EffectiveDate}", 
                request.EffectiveDate);

            _context.SalaryChangeRequests.Add(request);
            
            _logger?.LogInformation("DEBUG: Request added to context, attempting SaveChangesAsync...");
            
            try
            {
                await _context.SaveChangesAsync();
                _logger?.LogInformation("DEBUG: SaveChangesAsync completed successfully - RequestId={RequestId}", request.RequestId);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger?.LogError(dbEx, "DEBUG: DbUpdateException occurred during SaveChangesAsync");
                _logger?.LogError("DEBUG: Inner Exception Type: {InnerType}, Message: {InnerMessage}", 
                    dbEx.InnerException?.GetType().Name ?? "None", 
                    dbEx.InnerException?.Message ?? "None");
                
                if (dbEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
                {
                    _logger?.LogError("DEBUG: SQL Exception Details:");
                    _logger?.LogError("  - Error Number: {ErrorNumber}", sqlEx.Number);
                    _logger?.LogError("  - Error State: {State}", sqlEx.State);
                    _logger?.LogError("  - Error Class: {Class}", sqlEx.Class);
                    _logger?.LogError("  - Error Message: {Message}", sqlEx.Message);
                    _logger?.LogError("  - Server: {Server}", sqlEx.Server);
                    _logger?.LogError("  - Procedure: {Procedure}", sqlEx.Procedure ?? "N/A");
                    _logger?.LogError("  - Line Number: {LineNumber}", sqlEx.LineNumber);
                    
                    if (sqlEx.Number == 207) // Invalid column name
                    {
                        _logger?.LogError("DEBUG: CRITICAL - Invalid column name error detected!");
                        _logger?.LogError("DEBUG: The 'effective_date' column does not exist in tbl_salary_change_requests table.");
                        _logger?.LogError("DEBUG: Please run the migration script: Database_Scripts/Add_effective_date_to_salary_change_requests.sql");
                        
                        // Check if column exists
                        var columnExists = await CheckColumnExistsAsync("tbl_salary_change_requests", "effective_date");
                        _logger?.LogError("DEBUG: Column 'effective_date' exists check result: {Exists}", columnExists);
                    }
                }
                
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "DEBUG: General exception during SaveChangesAsync - Type: {Type}, Message: {Message}", 
                    ex.GetType().Name, ex.Message);
                throw;
            }

            // Get employee and requester names for audit log
            var employee = await _context.Users.FindAsync(userId);
            var requester = await _context.Users.FindAsync(requestedBy);
            var employeeName = employee != null ? $"{employee.FirstName} {employee.LastName}" : "Unknown";
            var requesterName = requester != null ? $"{requester.FirstName} {requester.LastName}" : "Unknown";
            var requesterRole = requester?.UserRole ?? "Unknown";

            // Create audit log
            await _auditLogService.CreateLogAsync(
                action: isInitialRegistration ? "Salary Change Request Created (New Employee)" : "Salary Change Request Created",
                module: "HR",
                description: $"Requested salary change for {employeeName}. Current: ₱{currentBaseSalary:N2} + ₱{currentAllowance:N2}, Requested: ₱{requestedBaseSalary:N2} + ₱{requestedAllowance:N2}. Reason: {reason ?? "N/A"}. School Year: {currentSchoolYear}",
                userName: requesterName,
                userRole: requesterRole,
                userId: requestedBy,
                status: "Success",
                severity: "Medium"
            );

            _logger?.LogInformation("Salary change request created: RequestId={RequestId}, UserId={UserId}, Status={Status}",
                request.RequestId, userId, request.Status);

            return request;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DEBUG: FATAL ERROR in CreateRequestAsync - Type: {Type}, Message: {Message}, StackTrace: {StackTrace}", 
                ex.GetType().Name, ex.Message, ex.StackTrace);
            throw;
        }
    }

    public async Task<List<SalaryChangeRequest>> GetPendingRequestsAsync()
    {
        try
        {
            _logger?.LogInformation("DEBUG: GetPendingRequestsAsync - Starting query...");
            var result = await _context.SalaryChangeRequests
                .Include(r => r.User)
                .Include(r => r.RequestedByUser)
                .Where(r => r.Status == "Pending")
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
            _logger?.LogInformation("DEBUG: GetPendingRequestsAsync - Query completed, found {Count} requests", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DEBUG: ERROR in GetPendingRequestsAsync - Type: {Type}, Message: {Message}", 
                ex.GetType().Name, ex.Message);
            if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && sqlEx.Number == 207)
            {
                _logger?.LogError("DEBUG: CRITICAL - Invalid column name 'effective_date' in GetPendingRequestsAsync");
                var columnExists = await CheckColumnExistsAsync("tbl_salary_change_requests", "effective_date");
                _logger?.LogError("DEBUG: Column 'effective_date' exists check result: {Exists}", columnExists);
            }
            throw;
        }
    }

    public async Task<SalaryChangeRequest?> GetRequestByIdAsync(int requestId)
    {
        return await _context.SalaryChangeRequests
            .Include(r => r.User)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ApprovedByUser)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);
    }

    public async Task<bool> ApproveRequestAsync(int requestId, int approvedBy, DateTime? effectiveDate = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var request = await _context.SalaryChangeRequests
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null || request.Status != "Pending")
            {
                return false;
            }

            // Update request status
            request.Status = "Approved";
            request.ApprovedBy = approvedBy;
            request.ApprovedAt = DateTime.Now;
            
            // Save the effective date (use provided date or default to today)
            var finalEffectiveDate = effectiveDate ?? DateTime.Today;
            request.EffectiveDate = finalEffectiveDate.Date;

            // Find existing salary record (could be inactive if from initial registration)
            var existingSalary = await _context.SalaryInfos
                .Where(s => s.UserId == request.UserId)
                .OrderByDescending(s => s.SalaryId)
                .FirstOrDefaultAsync();

            // Check if effective date is in the future
            bool isFutureEffectiveDate = finalEffectiveDate.Date > DateTime.Today;

            if (existingSalary != null)
            {
                if (isFutureEffectiveDate)
                {
                    // If effective date is in the future, create a NEW salary record
                    // Keep the old salary record active so it can be used for periods before the effective date
                    var newSalary = new SalaryInfo
                    {
                        UserId = request.UserId,
                        BaseSalary = request.RequestedBaseSalary,
                        Allowance = request.RequestedAllowance,
                        DateEffective = finalEffectiveDate.Date,
                        IsActive = true,
                        SchoolYear = request.SchoolYear
                    };
                    _context.SalaryInfos.Add(newSalary);
                    // Keep existing salary record as-is (don't update or deactivate it)
                }
                else
                {
                    // If effective date is today or in the past, update the existing salary record
                    existingSalary.BaseSalary = request.RequestedBaseSalary;
                    existingSalary.Allowance = request.RequestedAllowance;
                    existingSalary.DateEffective = finalEffectiveDate.Date;
                    existingSalary.IsActive = true;
                    existingSalary.SchoolYear = request.SchoolYear;
                }
            }
            else
            {
                // Create new salary record with approved amounts (shouldn't happen, but safety check)
                var newSalary = new SalaryInfo
                {
                    UserId = request.UserId,
                    BaseSalary = request.RequestedBaseSalary,
                    Allowance = request.RequestedAllowance,
                    DateEffective = finalEffectiveDate.Date, // Use the effective date from approval
                    IsActive = true,
                    SchoolYear = request.SchoolYear
                };
                _context.SalaryInfos.Add(newSalary);
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Get employee and approver info for notification
            var employee = await _context.Users.FindAsync(request.UserId);
            var approver = await _context.Users.FindAsync(approvedBy);
            var employeeName = employee != null ? $"{employee.FirstName} {employee.LastName}" : "Unknown";
            var approverName = approver != null ? $"{approver.FirstName} {approver.LastName}" : "Unknown";

            // Create notification for the HR user who requested the change
            await _notificationService.CreateNotificationAsync(
                notificationType: "SalaryChange",
                title: "Salary Change Request Approved",
                message: $"Salary change request for {employeeName} has been approved. New salary: ₱{request.RequestedBaseSalary:N2} + ₱{request.RequestedAllowance:N2}. Effective Date: {finalEffectiveDate:yyyy-MM-dd}",
                referenceType: "SalaryChangeRequest",
                referenceId: requestId,
                actionUrl: "/human-resource?tab=SalaryChangeLog",
                priority: "Normal",
                createdBy: approvedBy
            );

            // Create audit log
            await _auditLogService.CreateLogAsync(
                action: "Salary Change Request Approved",
                module: "Payroll",
                description: $"Approved salary change for {employeeName}. Previous: ₱{request.CurrentBaseSalary:N2} + ₱{request.CurrentAllowance:N2}, New: ₱{request.RequestedBaseSalary:N2} + ₱{request.RequestedAllowance:N2}. Effective Date: {finalEffectiveDate:yyyy-MM-dd}. School Year: {request.SchoolYear}. Request ID: {requestId}",
                userName: approverName,
                userRole: approver?.UserRole ?? "Unknown",
                userId: approvedBy,
                status: "Success",
                severity: "Medium"
            );

            _logger?.LogInformation("Salary change request approved: RequestId={RequestId}, UserId={UserId}",
                requestId, request.UserId);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger?.LogError(ex, "Error approving salary change request: RequestId={RequestId}", requestId);
            throw;
        }
    }

    public async Task<bool> RejectRequestAsync(int requestId, int rejectedBy, string rejectionReason)
    {
        var request = await _context.SalaryChangeRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);

        if (request == null || request.Status != "Pending")
        {
            return false;
        }

        // Get rejector info
        var rejector = await _context.Users.FindAsync(rejectedBy);
        var rejectorName = rejector != null ? $"{rejector.FirstName} {rejector.LastName}" : "Unknown";
        var rejectorRole = rejector?.UserRole ?? "Unknown";
        var employeeName = request.User != null ? $"{request.User.FirstName} {request.User.LastName}" : "Unknown";

        request.Status = "Rejected";
        request.ApprovedBy = rejectedBy; // Using ApprovedBy field for rejector
        request.ApprovedAt = DateTime.Now;
        request.RejectionReason = rejectionReason;

        await _context.SaveChangesAsync();

        // Create notification for the HR user who requested the change
        await _notificationService.CreateNotificationAsync(
            notificationType: "SalaryChange",
            title: "Salary Change Request Rejected",
            message: $"Salary change request for {employeeName} has been rejected. Reason: {rejectionReason}",
            referenceType: "SalaryChangeRequest",
            referenceId: requestId,
            actionUrl: "/human-resource?tab=SalaryChangeLog",
            priority: "Normal",
            createdBy: rejectedBy
        );

        // Create audit log
        await _auditLogService.CreateLogAsync(
            action: "Salary Change Request Rejected",
            module: "Payroll",
            description: $"Rejected salary change for {employeeName}. Requested: ₱{request.RequestedBaseSalary:N2} + ₱{request.RequestedAllowance:N2}. Reason: {rejectionReason}. School Year: {request.SchoolYear}. Request ID: {requestId}",
            userName: rejectorName,
            userRole: rejectorRole,
            userId: rejectedBy,
            status: "Success",
            severity: "Medium"
        );

        _logger?.LogInformation("Salary change request rejected: RequestId={RequestId}, UserId={UserId}, Reason={Reason}",
            requestId, request.UserId, rejectionReason);

        return true;
    }

    /// <summary>
    /// Gets all salary change requests (Pending, Approved, Rejected) for the Salary Records tab
    /// </summary>
    public async Task<List<SalaryChangeRequest>> GetAllRequestsAsync()
    {
        return await _context.SalaryChangeRequests
            .Include(r => r.User)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ApprovedByUser)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets salary change requests filtered by status
    /// </summary>
    public async Task<List<SalaryChangeRequest>> GetRequestsByStatusAsync(string status)
    {
        return await _context.SalaryChangeRequests
            .Include(r => r.User)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ApprovedByUser)
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets salary change requests filtered by school year
    /// </summary>
    public async Task<List<SalaryChangeRequest>> GetRequestsBySchoolYearAsync(string schoolYear)
    {
        return await _context.SalaryChangeRequests
            .Include(r => r.User)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ApprovedByUser)
            .Where(r => r.SchoolYear == schoolYear)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets salary history for an employee (all salary changes across school years)
    /// Combines data from SalaryChangeRequests and SalaryInfo for complete history
    /// </summary>
    public async Task<List<SalaryHistoryDto>> GetSalaryHistoryAsync(int? userId = null)
    {
        var history = new List<SalaryHistoryDto>();

        // Get all salary change requests (these show the approval workflow)
        var requests = userId.HasValue
            ? await _context.SalaryChangeRequests
                .Include(r => r.User)
                .Include(r => r.RequestedByUser)
                .Include(r => r.ApprovedByUser)
                .Where(r => r.UserId == userId.Value)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync()
            : await _context.SalaryChangeRequests
                .Include(r => r.User)
                .Include(r => r.RequestedByUser)
                .Include(r => r.ApprovedByUser)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

        foreach (var request in requests)
        {
            history.Add(new SalaryHistoryDto
            {
                RecordId = request.RequestId,
                UserId = request.UserId,
                EmployeeName = request.User != null ? $"{request.User.FirstName} {request.User.LastName}" : "Unknown",
                Role = request.User?.UserRole ?? "Unknown",
                BaseSalary = request.RequestedBaseSalary,
                Allowance = request.RequestedAllowance,
                TotalSalary = request.RequestedBaseSalary + request.RequestedAllowance,
                SchoolYear = request.SchoolYear,
                Status = request.Status,
                Reason = request.Reason,
                RejectionReason = request.RejectionReason,
                RequestedBy = request.RequestedByUser != null ? $"{request.RequestedByUser.FirstName} {request.RequestedByUser.LastName}" : "Unknown",
                ApprovedBy = request.ApprovedByUser != null ? $"{request.ApprovedByUser.FirstName} {request.ApprovedByUser.LastName}" : null,
                RequestedAt = request.RequestedAt,
                ApprovedAt = request.ApprovedAt,
                DateEffective = request.ApprovedAt ?? request.RequestedAt,
                RecordType = "Request",
                IsInitialRegistration = request.IsInitialRegistration
            });
        }

        // Get all salary info records (these show the actual active salary at different periods)
        var salaryInfos = userId.HasValue
            ? await _context.SalaryInfos
                .Include(s => s.User)
                .Where(s => s.UserId == userId.Value)
                .OrderByDescending(s => s.DateEffective)
                .ThenByDescending(s => s.SalaryId)
                .ToListAsync()
            : await _context.SalaryInfos
                .Include(s => s.User)
                .OrderByDescending(s => s.DateEffective)
                .ThenByDescending(s => s.SalaryId)
                .ToListAsync();

        foreach (var salary in salaryInfos)
        {
            // Only add if not already covered by a request (avoid duplicates)
            var existingRequest = requests.FirstOrDefault(r => 
                r.UserId == salary.UserId && 
                r.RequestedBaseSalary == salary.BaseSalary && 
                r.RequestedAllowance == salary.Allowance &&
                r.SchoolYear == salary.SchoolYear &&
                r.Status == "Approved");

            if (existingRequest == null)
            {
                history.Add(new SalaryHistoryDto
                {
                    RecordId = salary.SalaryId,
                    UserId = salary.UserId,
                    EmployeeName = salary.User != null ? $"{salary.User.FirstName} {salary.User.LastName}" : "Unknown",
                    Role = salary.User?.UserRole ?? "Unknown",
                    BaseSalary = salary.BaseSalary,
                    Allowance = salary.Allowance,
                    TotalSalary = salary.BaseSalary + salary.Allowance,
                    SchoolYear = salary.SchoolYear,
                    Status = salary.IsActive ? "Active" : "Inactive",
                    Reason = null,
                    RejectionReason = null,
                    RequestedBy = null,
                    ApprovedBy = null,
                    RequestedAt = salary.DateEffective,
                    ApprovedAt = null,
                    DateEffective = salary.DateEffective,
                    RecordType = "Salary Record",
                    IsInitialRegistration = false
                });
            }
        }

        return history.OrderByDescending(h => h.DateEffective).ThenByDescending(h => h.RequestedAt).ToList();
    }

    /// <summary>
    /// Gets unified Salary Change Log - all salary changes (Pending, Approved, Rejected) in one view
    /// This is the main method for the unified Salary Change Log table
    /// </summary>
    public async Task<List<SalaryChangeLogDto>> GetSalaryChangeLogAsync(int? userId = null)
    {
        try
        {
            _logger?.LogInformation("DEBUG: GetSalaryChangeLogAsync - Starting query, UserId={UserId}", userId);
            var logEntries = new List<SalaryChangeLogDto>();

            // Get all salary change requests
            _logger?.LogInformation("DEBUG: GetSalaryChangeLogAsync - Querying SalaryChangeRequests table...");
            var requests = userId.HasValue
                ? await _context.SalaryChangeRequests
                    .Include(r => r.User)
                    .Include(r => r.RequestedByUser)
                    .Include(r => r.ApprovedByUser)
                    .Where(r => r.UserId == userId.Value)
                    .OrderByDescending(r => r.RequestedAt)
                    .ToListAsync()
                : await _context.SalaryChangeRequests
                    .Include(r => r.User)
                    .Include(r => r.RequestedByUser)
                    .Include(r => r.ApprovedByUser)
                    .OrderByDescending(r => r.RequestedAt)
                    .ToListAsync();
            
            _logger?.LogInformation("DEBUG: GetSalaryChangeLogAsync - Query completed, found {Count} requests", requests.Count);

        // For each employee, get their original base salary (first approved salary or initial salary)
        var employeeOriginalSalaries = new Dictionary<int, decimal>();
        
        foreach (var request in requests.OrderBy(r => r.RequestedAt))
        {
            if (!employeeOriginalSalaries.ContainsKey(request.UserId))
            {
                // Get the first approved salary or the first salary info record
                var firstApproved = requests
                    .Where(r => r.UserId == request.UserId && r.Status == "Approved")
                    .OrderBy(r => r.RequestedAt)
                    .FirstOrDefault();
                
                if (firstApproved != null)
                {
                    employeeOriginalSalaries[request.UserId] = firstApproved.CurrentBaseSalary;
                }
                else
                {
                    // Fallback to current base salary from the request
                    employeeOriginalSalaries[request.UserId] = request.CurrentBaseSalary;
                }
            }
        }

        foreach (var request in requests)
        {
            var previousTotal = request.CurrentBaseSalary + request.CurrentAllowance;
            var newTotal = request.RequestedBaseSalary + request.RequestedAllowance;
            var changeAmount = newTotal - previousTotal;
            var changePercentage = previousTotal > 0 ? (changeAmount / previousTotal) * 100 : 0;
            
            // Calculate cumulative increase relative to original base salary
            var originalBase = employeeOriginalSalaries.GetValueOrDefault(request.UserId, request.CurrentBaseSalary);
            var cumulativeIncrease = originalBase > 0 
                ? ((request.RequestedBaseSalary - originalBase) / originalBase) * 100 
                : 0;

            // Use stored effective date if available, otherwise fall back to approval date or request date
            var effectiveDate = request.EffectiveDate?.Date 
                ?? request.ApprovedAt?.Date 
                ?? request.RequestedAt.Date;

            logEntries.Add(new SalaryChangeLogDto
            {
                RecordId = request.RequestId,
                UserId = request.UserId,
                EmployeeId = request.User?.SystemId ?? "",
                EmployeeName = request.User != null ? $"{request.User.FirstName} {request.User.LastName}" : "Unknown",
                Role = request.User?.UserRole ?? "Unknown",
                PreviousBaseSalary = request.CurrentBaseSalary,
                PreviousAllowance = request.CurrentAllowance,
                PreviousTotalSalary = previousTotal,
                NewBaseSalary = request.RequestedBaseSalary,
                NewAllowance = request.RequestedAllowance,
                NewTotalSalary = newTotal,
                ChangeAmount = changeAmount,
                ChangePercentage = Math.Round(changePercentage, 2),
                CumulativeIncreasePercentage = Math.Round(cumulativeIncrease, 2),
                EffectiveDate = effectiveDate,
                SchoolYear = request.SchoolYear,
                Status = request.Status,
                ApproverName = request.ApprovedByUser != null ? $"{request.ApprovedByUser.FirstName} {request.ApprovedByUser.LastName}" : null,
                ApproverRole = request.ApprovedByUser?.UserRole,
                ApprovalDate = request.ApprovedAt,
                Reason = request.Reason,
                RejectionReason = request.RejectionReason,
                RequestedAt = request.RequestedAt,
                RequestedByName = request.RequestedByUser != null ? $"{request.RequestedByUser.FirstName} {request.RequestedByUser.LastName}" : "Unknown",
                IsInitialRegistration = request.IsInitialRegistration,
                OriginalBaseSalary = employeeOriginalSalaries.GetValueOrDefault(request.UserId, request.CurrentBaseSalary)
            });
        }

            // Order by most recent first - prioritize request/approval date to show latest changes at top
            // This ensures the latest salary changes appear at the top of the log
            var result = logEntries
                .OrderByDescending(e => e.RequestedAt) // Primary: Most recent requests first
                .ThenByDescending(e => e.ApprovalDate ?? DateTime.MinValue) // Secondary: Approved changes after pending
                .ThenByDescending(e => e.EffectiveDate) // Tertiary: Effective date for tie-breaking
                .ToList();
            _logger?.LogInformation("DEBUG: GetSalaryChangeLogAsync - Processing completed, returning {Count} log entries", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DEBUG: ERROR in GetSalaryChangeLogAsync - Type: {Type}, Message: {Message}, StackTrace: {StackTrace}", 
                ex.GetType().Name, ex.Message, ex.StackTrace);
            
            if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
            {
                _logger?.LogError("DEBUG: SQL Exception Details in GetSalaryChangeLogAsync:");
                _logger?.LogError("  - Error Number: {ErrorNumber}", sqlEx.Number);
                _logger?.LogError("  - Error State: {State}", sqlEx.State);
                _logger?.LogError("  - Error Class: {Class}", sqlEx.Class);
                _logger?.LogError("  - Error Message: {Message}", sqlEx.Message);
                
                if (sqlEx.Number == 207) // Invalid column name
                {
                    _logger?.LogError("DEBUG: CRITICAL - Invalid column name 'effective_date' in GetSalaryChangeLogAsync");
                    var columnExists = await CheckColumnExistsAsync("tbl_salary_change_requests", "effective_date");
                    _logger?.LogError("DEBUG: Column 'effective_date' exists check result: {Exists}", columnExists);
                }
            }
            throw;
        }
    }

    /// <summary>
    /// Debug method to check if a column exists in a table
    /// </summary>
    private async Task<bool> CheckColumnExistsAsync(string tableName, string columnName)
    {
        try
        {
            var sql = @"
                SELECT COUNT(*) AS ColumnCount
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = @p0 
                AND COLUMN_NAME = @p1";
            
            var result = await _context.Database
                .SqlQueryRaw<int>(sql, tableName, columnName)
                .FirstOrDefaultAsync();
            
            _logger?.LogInformation("DEBUG: Column check - Table: {Table}, Column: {Column}, Exists: {Exists}", 
                tableName, columnName, result > 0);
            
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DEBUG: Error checking if column exists: Table={Table}, Column={Column}, Error: {Error}", 
                tableName, columnName, ex.Message);
            return false;
        }
    }
}

/// <summary>
/// DTO for unified Salary Change Log display
/// </summary>
public class SalaryChangeLogDto
{
    public int RecordId { get; set; }
    public int UserId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Used as Department/Team
    public decimal PreviousBaseSalary { get; set; }
    public decimal PreviousAllowance { get; set; }
    public decimal PreviousTotalSalary { get; set; }
    public decimal NewBaseSalary { get; set; }
    public decimal NewAllowance { get; set; }
    public decimal NewTotalSalary { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal ChangePercentage { get; set; }
    public decimal CumulativeIncreasePercentage { get; set; } // Relative to original base salary
    public DateTime EffectiveDate { get; set; }
    public string SchoolYear { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Pending, Approved, Rejected
    public string? ApproverName { get; set; }
    public string? ApproverRole { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? Reason { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime RequestedAt { get; set; }
    public string? RequestedByName { get; set; }
    public bool IsInitialRegistration { get; set; }
    public decimal OriginalBaseSalary { get; set; } // For cumulative calculation
}

/// <summary>
/// DTO for salary history display (legacy, kept for backward compatibility)
/// </summary>
public class SalaryHistoryDto
{
    public int RecordId { get; set; }
    public int UserId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public decimal BaseSalary { get; set; }
    public decimal Allowance { get; set; }
    public decimal TotalSalary { get; set; }
    public string SchoolYear { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Pending, Approved, Rejected, Active, Inactive
    public string? Reason { get; set; }
    public string? RejectionReason { get; set; }
    public string? RequestedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime DateEffective { get; set; }
    public string RecordType { get; set; } = string.Empty; // "Request" or "Salary Record"
    public bool IsInitialRegistration { get; set; }
}

