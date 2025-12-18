using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Services.Database.Connections;
using System.Data;
using System.Text;

namespace BrightEnroll_DES.Services.Database.Sync;

public interface IDatabaseSyncService
{
    Task<SyncResult> SyncToCloudAsync(string? cloudConnectionString = null, string? localConnectionString = null);
    Task<SyncResult> SyncFromCloudAsync(string? cloudConnectionString = null, string? localConnectionString = null);
    Task<SyncResult> FullSyncAsync(string? cloudConnectionString = null, string? localConnectionString = null);
    Task<SyncResult> IncrementalSyncAsync(DateTime? since = null, string? cloudConnectionString = null, string? localConnectionString = null);
    Task<bool> TestCloudConnectionAsync(string? cloudConnectionString = null);
}

public class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RecordsPushed { get; set; }
    public int RecordsPulled { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime SyncTime { get; set; } = DateTime.Now;
}

public class DatabaseSyncService : IDatabaseSyncService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly string? _cloudConnectionString;
    private readonly ILogger<DatabaseSyncService>? _logger;

    public DatabaseSyncService(
        IDbContextFactory<AppDbContext> contextFactory,
        IConfiguration configuration,
        ILogger<DatabaseSyncService>? logger = null)
    {
        _contextFactory = contextFactory;
        _cloudConnectionString = configuration.GetConnectionString("CloudConnection");
        _logger = logger;
        
        if (string.IsNullOrWhiteSpace(_cloudConnectionString))
        {
            _logger?.LogWarning("CloudConnection string not found in configuration. Cloud sync features will be disabled.");
        }
    }

    public async Task<bool> TestCloudConnectionAsync(string? cloudConnectionString = null)
    {
        var connectionString = cloudConnectionString ?? _cloudConnectionString;
        
        // Log which connection string is being used
        if (cloudConnectionString != null)
        {
            _logger?.LogInformation("Testing customer-specific cloud connection");
        }
        else
        {
            _logger?.LogInformation("Testing global cloud connection (first/seeded admin)");
        }
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger?.LogWarning("CloudConnection string is not configured. Cannot test cloud connection.");
            return false;
        }
        
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect to cloud database");
            return false;
        }
    }

    public async Task<SyncResult> SyncToCloudAsync(string? cloudConnectionString = null, string? localConnectionString = null)
    {
        var result = new SyncResult { Success = true };
        
        // Use customer-specific connection strings if provided, otherwise use global ones (for development/first DB)
        var cloudConnString = cloudConnectionString ?? _cloudConnectionString;
        var localConnString = localConnectionString;
        
        // Log which connection strings are being used
        if (cloudConnectionString != null)
        {
            _logger?.LogInformation("Using customer-specific cloud connection string for sync");
        }
        else
        {
            _logger?.LogInformation("Using global cloud connection string from configuration (development/first DB)");
        }
        
        if (localConnectionString != null)
        {
            _logger?.LogInformation("Using customer-specific local connection string for sync (account-separated)");
        }
        else
        {
            _logger?.LogInformation("Using default local connection string (shared database)");
        }
        
        if (string.IsNullOrWhiteSpace(cloudConnString))
        {
            result.Success = false;
            result.Message = "CloudConnection string is not configured. Please configure CloudConnection in appsettings.json or ensure customer has a cloud connection string configured.";
            result.Errors.Add("CloudConnection not configured");
            return result;
        }
        
        try
        {
            // Test cloud connection first
            if (!await TestCloudConnectionAsync(cloudConnString))
            {
                result.Success = false;
                result.Message = "Cannot connect to cloud database. Please check your connection settings.";
                result.Errors.Add("Cloud connection failed");
                return result;
            }

            using var cloudConnection = new SqlConnection(cloudConnString);
            await cloudConnection.OpenAsync();

            // Sync in order: Parents first, then dependent tables
            // Core reference data
            result.RecordsPushed += await SyncTableToCloudAsync<SchoolYear>(cloudConnection, "tbl_SchoolYear", "school_year_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<Role>(cloudConnection, "tbl_roles", "role_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<UserEntity>(cloudConnection, "tbl_Users", "user_ID", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<GradeLevel>(cloudConnection, "tbl_GradeLevel", "gradelevel_ID", localConnString);
            
            // Employee data
            result.RecordsPushed += await SyncTableToCloudAsync<EmployeeAddress>(cloudConnection, "tbl_employee_address", "address_ID", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<EmployeeEmergencyContact>(cloudConnection, "tbl_employee_emergency_contact", "emergency_ID", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<SalaryInfo>(cloudConnection, "tbl_salary_info", "salary_ID", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<Deduction>(cloudConnection, "tbl_deductions", "deduction_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<SalaryChangeRequest>(cloudConnection, "tbl_salary_change_requests", "request_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<TimeRecord>(cloudConnection, "tbl_TimeRecords", "time_record_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<PayrollTransaction>(cloudConnection, "tbl_payroll_transactions", "transaction_id", localConnString);
            
            // Student data
            result.RecordsPushed += await SyncTableToCloudAsync<Guardian>(cloudConnection, "tbl_Guardians", "guardian_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<Student>(cloudConnection, "tbl_Students", "student_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<StudentRequirement>(cloudConnection, "tbl_StudentRequirements", "requirement_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<StudentPayment>(cloudConnection, "tbl_StudentPayments", "payment_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<Discount>(cloudConnection, "tbl_discounts", "discount_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<StudentLedger>(cloudConnection, "tbl_StudentLedgers", "id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<LedgerCharge>(cloudConnection, "tbl_LedgerCharges", "id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<LedgerPayment>(cloudConnection, "tbl_LedgerPayments", "id", localConnString);
            
            // Finance data
            result.RecordsPushed += await SyncTableToCloudAsync<Fee>(cloudConnection, "tbl_Fees", "fee_ID", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<FeeBreakdown>(cloudConnection, "tbl_FeeBreakdown", "breakdown_ID", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<Expense>(cloudConnection, "tbl_Expenses", "expense_ID", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<ExpenseAttachment>(cloudConnection, "tbl_ExpenseAttachments", "attachment_ID", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<ChartOfAccount>(cloudConnection, "tbl_ChartOfAccounts", "account_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<JournalEntry>(cloudConnection, "tbl_JournalEntries", "journal_entry_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<JournalEntryLine>(cloudConnection, "tbl_JournalEntryLines", "line_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<AccountingPeriod>(cloudConnection, "tbl_AccountingPeriods", "period_id", localConnString);
            
            // Curriculum data
            result.RecordsPushed += await SyncTableToCloudAsync<Building>(cloudConnection, "tbl_Buildings", "BuildingID", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<Classroom>(cloudConnection, "tbl_Classrooms", "RoomID", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<Section>(cloudConnection, "tbl_Sections", "SectionID", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<Subject>(cloudConnection, "tbl_Subjects", "SubjectID", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<SubjectSection>(cloudConnection, "tbl_SubjectSection", "ID", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<SubjectSchedule>(cloudConnection, "tbl_SubjectSchedule", "ScheduleID", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<TeacherSectionAssignment>(cloudConnection, "tbl_TeacherSectionAssignment", "AssignmentID", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<ClassSchedule>(cloudConnection, "tbl_ClassSchedule", "ScheduleID", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<StudentSectionEnrollment>(cloudConnection, "tbl_StudentSectionEnrollment", "enrollment_id", localConnString);
            
            // Grade data
            result.RecordsPushed += await SyncTableToCloudAsync<Grade>(cloudConnection, "tbl_Grades", "grade_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<GradeWeight>(cloudConnection, "tbl_GradeWeights", "weight_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<GradeHistory>(cloudConnection, "tbl_GradeHistory", "history_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<Attendance>(cloudConnection, "tbl_Attendance", "AttendanceID", localConnString);
            
            // Inventory & Assets
            result.RecordsPushed += await SyncTableToCloudAsync<Asset>(cloudConnection, "tbl_Assets", "asset_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<InventoryItem>(cloudConnection, "tbl_InventoryItems", "item_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<AssetAssignment>(cloudConnection, "tbl_AssetAssignments", "assignment_id", localConnString);
            
            // Logs and notifications
            result.RecordsPushed += await SyncTableToCloudAsync<Notification>(cloudConnection, "tbl_Notifications", "notification_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<UserStatusLog>(cloudConnection, "tbl_user_status_logs", "log_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<StudentStatusLog>(cloudConnection, "tbl_student_status_logs", "log_id", localConnString);
            result.RecordsPushed += await SyncTableToCloudAsync<TeacherActivityLog>(cloudConnection, "tbl_TeacherActivityLogs", "id");
            result.RecordsPushed += await SyncTableToCloudAsync<AuditLog>(cloudConnection, "tbl_audit_logs", "log_id");

            result.Message = $"Successfully synced {result.RecordsPushed} records to cloud.";
            _logger?.LogInformation("Sync to cloud completed: {Count} records", result.RecordsPushed);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error syncing to cloud: {ex.Message}";
            result.Errors.Add(ex.Message);
            _logger?.LogError(ex, "Error during sync to cloud");
        }

        return result;
    }

    public async Task<SyncResult> SyncFromCloudAsync(string? cloudConnectionString = null, string? localConnectionString = null)
    {
        var result = new SyncResult { Success = true };
        
        // Use customer-specific connection strings if provided, otherwise use global ones (for first/seeded admin)
        var cloudConnString = cloudConnectionString ?? _cloudConnectionString;
        var localConnString = localConnectionString;
        
        // Log which connection strings are being used
        if (cloudConnectionString != null)
        {
            _logger?.LogInformation("Using customer-specific cloud connection string for sync");
        }
        else
        {
            _logger?.LogInformation("Using global cloud connection string from configuration (first/seeded admin)");
        }
        
        if (localConnectionString != null)
        {
            _logger?.LogInformation("Using customer-specific local connection string for sync (account-separated)");
        }
        else
        {
            _logger?.LogInformation("Using default local connection string (shared database)");
        }
        
        if (string.IsNullOrWhiteSpace(cloudConnString))
        {
            result.Success = false;
            result.Message = "CloudConnection string is not configured. Please configure CloudConnection in appsettings.json or ensure customer has a cloud connection string configured.";
            result.Errors.Add("CloudConnection not configured");
            return result;
        }
        
        try
        {
            if (!await TestCloudConnectionAsync(cloudConnString))
            {
                result.Success = false;
                result.Message = "Cannot connect to cloud database. Please check your connection settings.";
                result.Errors.Add("Cloud connection failed");
                return result;
            }

            using var cloudConnection = new SqlConnection(cloudConnString);
            await cloudConnection.OpenAsync();

            // Sync in order: Parents first, then dependent tables
            // Core reference data
            result.RecordsPulled += await SyncTableFromCloudAsync<SchoolYear>(cloudConnection, "tbl_SchoolYear", "school_year_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<Role>(cloudConnection, "tbl_roles", "role_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<UserEntity>(cloudConnection, "tbl_Users", "user_ID", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<GradeLevel>(cloudConnection, "tbl_GradeLevel", "gradelevel_ID", localConnString);
            
            // Employee data
            result.RecordsPulled += await SyncTableFromCloudAsync<EmployeeAddress>(cloudConnection, "tbl_employee_address", "address_ID", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<EmployeeEmergencyContact>(cloudConnection, "tbl_employee_emergency_contact", "emergency_ID", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<SalaryInfo>(cloudConnection, "tbl_salary_info", "salary_ID", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<Deduction>(cloudConnection, "tbl_deductions", "deduction_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<SalaryChangeRequest>(cloudConnection, "tbl_salary_change_requests", "request_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<TimeRecord>(cloudConnection, "tbl_TimeRecords", "time_record_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<PayrollTransaction>(cloudConnection, "tbl_payroll_transactions", "transaction_id", localConnString);
            
            // Student data
            result.RecordsPulled += await SyncTableFromCloudAsync<Guardian>(cloudConnection, "tbl_Guardians", "guardian_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<Student>(cloudConnection, "tbl_Students", "student_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<StudentRequirement>(cloudConnection, "tbl_StudentRequirements", "requirement_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<StudentPayment>(cloudConnection, "tbl_StudentPayments", "payment_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<Discount>(cloudConnection, "tbl_discounts", "discount_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<StudentLedger>(cloudConnection, "tbl_StudentLedgers", "id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<LedgerCharge>(cloudConnection, "tbl_LedgerCharges", "id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<LedgerPayment>(cloudConnection, "tbl_LedgerPayments", "id", localConnString);
            
            // Finance data
            result.RecordsPulled += await SyncTableFromCloudAsync<Fee>(cloudConnection, "tbl_Fees", "fee_ID", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<FeeBreakdown>(cloudConnection, "tbl_FeeBreakdown", "breakdown_ID", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<Expense>(cloudConnection, "tbl_Expenses", "expense_ID", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<ExpenseAttachment>(cloudConnection, "tbl_ExpenseAttachments", "attachment_ID", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<ChartOfAccount>(cloudConnection, "tbl_ChartOfAccounts", "account_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<JournalEntry>(cloudConnection, "tbl_JournalEntries", "journal_entry_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<JournalEntryLine>(cloudConnection, "tbl_JournalEntryLines", "line_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<AccountingPeriod>(cloudConnection, "tbl_AccountingPeriods", "period_id", localConnString);
            
            // Curriculum data
            result.RecordsPulled += await SyncTableFromCloudAsync<Building>(cloudConnection, "tbl_Buildings", "BuildingID", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<Classroom>(cloudConnection, "tbl_Classrooms", "RoomID", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<Section>(cloudConnection, "tbl_Sections", "SectionID", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<Subject>(cloudConnection, "tbl_Subjects", "SubjectID", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<SubjectSection>(cloudConnection, "tbl_SubjectSection", "ID", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<SubjectSchedule>(cloudConnection, "tbl_SubjectSchedule", "ScheduleID", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<TeacherSectionAssignment>(cloudConnection, "tbl_TeacherSectionAssignment", "AssignmentID", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<ClassSchedule>(cloudConnection, "tbl_ClassSchedule", "ScheduleID", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<StudentSectionEnrollment>(cloudConnection, "tbl_StudentSectionEnrollment", "enrollment_id", localConnString);
            
            // Grade data
            result.RecordsPulled += await SyncTableFromCloudAsync<Grade>(cloudConnection, "tbl_Grades", "grade_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<GradeWeight>(cloudConnection, "tbl_GradeWeights", "weight_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<GradeHistory>(cloudConnection, "tbl_GradeHistory", "history_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<Attendance>(cloudConnection, "tbl_Attendance", "AttendanceID", localConnString);
            
            // Inventory & Assets
            result.RecordsPulled += await SyncTableFromCloudAsync<Asset>(cloudConnection, "tbl_Assets", "asset_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<InventoryItem>(cloudConnection, "tbl_InventoryItems", "item_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<AssetAssignment>(cloudConnection, "tbl_AssetAssignments", "assignment_id", localConnString);
            
            // Logs and notifications
            result.RecordsPulled += await SyncTableFromCloudAsync<Notification>(cloudConnection, "tbl_Notifications", "notification_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<UserStatusLog>(cloudConnection, "tbl_user_status_logs", "log_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<StudentStatusLog>(cloudConnection, "tbl_student_status_logs", "log_id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<TeacherActivityLog>(cloudConnection, "tbl_TeacherActivityLogs", "id", localConnString);
            result.RecordsPulled += await SyncTableFromCloudAsync<AuditLog>(cloudConnection, "tbl_audit_logs", "log_id", localConnString);

            result.Message = $"Successfully synced {result.RecordsPulled} records from cloud.";
            _logger?.LogInformation("Sync from cloud completed: {Count} records", result.RecordsPulled);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error syncing from cloud: {ex.Message}";
            result.Errors.Add(ex.Message);
            _logger?.LogError(ex, "Error during sync from cloud");
        }

        return result;
    }

    public async Task<SyncResult> FullSyncAsync(string? cloudConnectionString = null, string? localConnectionString = null)
    {
        var result = new SyncResult { Success = true };
        
        // Log which connection strings are being used
        if (cloudConnectionString != null)
        {
            _logger?.LogInformation("Full sync: Using customer-specific cloud connection string");
        }
        else
        {
            _logger?.LogInformation("Full sync: Using global cloud connection string (first/seeded admin)");
        }
        
        if (localConnectionString != null)
        {
            _logger?.LogInformation("Full sync: Using customer-specific local connection string (account-separated)");
        }
        else
        {
            _logger?.LogInformation("Full sync: Using default local connection string (shared database)");
        }
        
        try
        {
            // First push local changes to cloud
            var pushResult = await SyncToCloudAsync(cloudConnectionString, localConnectionString);
            result.RecordsPushed = pushResult.RecordsPushed;
            result.Errors.AddRange(pushResult.Errors);

            // Then pull cloud changes to local
            var pullResult = await SyncFromCloudAsync(cloudConnectionString, localConnectionString);
            result.RecordsPulled = pullResult.RecordsPulled;
            result.Errors.AddRange(pullResult.Errors);

            if (pushResult.Success && pullResult.Success)
            {
                result.Message = $"Full sync completed: {result.RecordsPushed} pushed, {result.RecordsPulled} pulled.";
            }
            else
            {
                result.Success = false;
                result.Message = $"Sync completed with errors: {result.Errors.Count} error(s).";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error during full sync: {ex.Message}";
            result.Errors.Add(ex.Message);
            _logger?.LogError(ex, "Error during full sync");
        }

        return result;
    }

    public async Task<SyncResult> IncrementalSyncAsync(DateTime? since = null, string? cloudConnectionString = null, string? localConnectionString = null)
    {
        var result = new SyncResult { Success = true };
        since ??= DateTime.Now.AddDays(-7); // Default: last 7 days
        
        // Use customer-specific connection strings if provided, otherwise use global ones
        var cloudConnString = cloudConnectionString ?? _cloudConnectionString;
        var localConnString = localConnectionString;
        
        if (string.IsNullOrWhiteSpace(cloudConnString))
        {
            result.Success = false;
            result.Message = "CloudConnection string is not configured. Please configure CloudConnection in appsettings.json or ensure customer has a cloud connection string configured.";
            result.Errors.Add("CloudConnection not configured");
            return result;
        }
        
        if (localConnectionString != null)
        {
            _logger?.LogInformation("Incremental sync: Using customer-specific local connection string (account-separated)");
        }
        else
        {
            _logger?.LogInformation("Incremental sync: Using default local connection string (shared database)");
        }
        
        try
        {
            if (!await TestCloudConnectionAsync(cloudConnString))
            {
                result.Success = false;
                result.Message = "Cannot connect to cloud database.";
                result.Errors.Add("Cloud connection failed");
                return result;
            }

            using var cloudConnection = new SqlConnection(cloudConnString);
            await cloudConnection.OpenAsync();

            // Incremental sync - only sync changed records
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<UserEntity>(cloudConnection, "tbl_Users", "user_ID", since.Value, localConnString);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<Guardian>(cloudConnection, "tbl_Guardians", "guardian_id", since.Value, localConnString);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<GradeLevel>(cloudConnection, "tbl_GradeLevel", "gradelevel_ID", since.Value, localConnString);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<Student>(cloudConnection, "tbl_Students", "student_id", since.Value, localConnString);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<StudentRequirement>(cloudConnection, "tbl_StudentRequirements", "requirement_id", since.Value, localConnString);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<StudentPayment>(cloudConnection, "tbl_StudentPayments", "payment_id", since.Value, localConnString);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<Fee>(cloudConnection, "tbl_Fees", "fee_ID", since.Value, localConnString);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<Expense>(cloudConnection, "tbl_Expenses", "expense_id", since.Value, localConnString);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<EmployeeAddress>(cloudConnection, "tbl_employee_address", "address_id", since.Value, localConnString);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<Classroom>(cloudConnection, "tbl_Classrooms", "RoomID", since.Value, localConnString);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<Section>(cloudConnection, "tbl_Sections", "section_id", since.Value, localConnString);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<Subject>(cloudConnection, "tbl_Subjects", "subject_id", since.Value, localConnString);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<StudentSectionEnrollment>(cloudConnection, "tbl_StudentSectionEnrollment", "enrollment_id", since.Value, localConnString);

            // Pull changes from cloud
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<UserEntity>(cloudConnection, "tbl_Users", "user_ID", since.Value, localConnString);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<Guardian>(cloudConnection, "tbl_Guardians", "guardian_id", since.Value, localConnString);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<GradeLevel>(cloudConnection, "tbl_GradeLevel", "gradelevel_ID", since.Value, localConnString);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<Student>(cloudConnection, "tbl_Students", "student_id", since.Value, localConnString);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<StudentRequirement>(cloudConnection, "tbl_StudentRequirements", "requirement_id", since.Value, localConnString);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<StudentPayment>(cloudConnection, "tbl_StudentPayments", "payment_id", since.Value, localConnString);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<Fee>(cloudConnection, "tbl_Fees", "fee_ID", since.Value, localConnString);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<Expense>(cloudConnection, "tbl_Expenses", "expense_id", since.Value, localConnString);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<EmployeeAddress>(cloudConnection, "tbl_employee_address", "address_id", since.Value, localConnString);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<Classroom>(cloudConnection, "tbl_Classrooms", "RoomID", since.Value, localConnString);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<Section>(cloudConnection, "tbl_Sections", "section_id", since.Value, localConnString);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<Subject>(cloudConnection, "tbl_Subjects", "subject_id", since.Value, localConnString);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<StudentSectionEnrollment>(cloudConnection, "tbl_StudentSectionEnrollment", "enrollment_id", since.Value, localConnString);

            result.Message = $"Incremental sync completed: {result.RecordsPushed} pushed, {result.RecordsPulled} pulled.";
            _logger?.LogInformation("Incremental sync completed: {Pushed} pushed, {Pulled} pulled", result.RecordsPushed, result.RecordsPulled);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error during incremental sync: {ex.Message}";
            result.Errors.Add(ex.Message);
            _logger?.LogError(ex, "Error during incremental sync");
        }

        return result;
    }

    // Helper method to check if a property is a computed column
    /// <summary>
    /// OPTIMIZED: Syncs a batch of records using SQL MERGE statement
    /// This is much faster than individual INSERT/UPDATE queries
    /// </summary>
    private async Task<int> SyncBatchUsingMergeAsync<T>(
        SqlConnection cloudConnection,
        string tableName,
        string primaryKeyColumn,
        List<T> records,
        IList<Microsoft.EntityFrameworkCore.Metadata.IProperty> allProperties,
        Microsoft.EntityFrameworkCore.Metadata.IProperty pkProperty,
        List<Microsoft.EntityFrameworkCore.Metadata.IProperty> syncProperties,
        bool isIdentity,
        Type entityType) where T : class
    {
        if (!records.Any())
            return 0;
        
        int recordsSynced = 0;
        
        try
        {
            using var transaction = cloudConnection.BeginTransaction();
            try
            {
                // Set IDENTITY_INSERT ON if needed
                if (isIdentity)
                {
                    var identityOnCmd = new SqlCommand($"SET IDENTITY_INSERT [{tableName}] ON", cloudConnection, transaction);
                    await identityOnCmd.ExecuteNonQueryAsync();
                }
                
                // For larger batches, use temp table approach (faster)
                if (records.Count > 100)
                {
                    recordsSynced = await SyncBatchUsingTempTableAsync(
                        cloudConnection, transaction, tableName, primaryKeyColumn, records,
                        allProperties, pkProperty, syncProperties, isIdentity);
                }
                else
                {
                    // For smaller batches, execute MERGE directly
                    var mergeSql = BuildMergeStatement(tableName, primaryKeyColumn, syncProperties, isIdentity);
                    
                    foreach (var record in records)
                    {
                        try
                        {
                            // FK validation for StudentSectionEnrollment
                            if (entityType == typeof(StudentSectionEnrollment))
                            {
                                if (!await ValidateStudentSectionEnrollmentFKs(cloudConnection, transaction, record, allProperties))
                                    continue;
                            }
                            
                            var parameters = BuildMergeParameters(record, pkProperty, syncProperties);
                            using var mergeCmd = new SqlCommand(mergeSql, cloudConnection, transaction);
                            
                            foreach (var param in parameters)
                                mergeCmd.Parameters.Add(param);
                            
                            await mergeCmd.ExecuteNonQueryAsync();
                            recordsSynced++;
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Failed to merge record in batch: {Message}", ex.Message);
                        }
                    }
                }
                
                // Set IDENTITY_INSERT OFF if needed
                if (isIdentity)
                {
                    var identityOffCmd = new SqlCommand($"SET IDENTITY_INSERT [{tableName}] OFF", cloudConnection, transaction);
                    await identityOffCmd.ExecuteNonQueryAsync();
                }
                
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in batch merge operation for {Table}: {Message}", tableName, ex.Message);
            throw;
        }
        
        return recordsSynced;
    }
    
    /// <summary>
    /// Uses temp table + SqlBulkCopy for very large batches (fastest approach)
    /// </summary>
    private async Task<int> SyncBatchUsingTempTableAsync<T>(
        SqlConnection cloudConnection,
        SqlTransaction transaction,
        string tableName,
        string primaryKeyColumn,
        List<T> records,
        IList<Microsoft.EntityFrameworkCore.Metadata.IProperty> allProperties,
        Microsoft.EntityFrameworkCore.Metadata.IProperty pkProperty,
        List<Microsoft.EntityFrameworkCore.Metadata.IProperty> syncProperties,
        bool isIdentity) where T : class
    {
        var tempTableName = $"#Temp_{tableName.Replace("[", "").Replace("]", "")}_{Guid.NewGuid().ToString("N")[..8]}";
        int recordsSynced = 0;
        
        try
        {
            // Create temp table
            var createTempTableSql = BuildTempTableSql(tempTableName, primaryKeyColumn, syncProperties, isIdentity);
            using var createCmd = new SqlCommand(createTempTableSql, cloudConnection, transaction);
            await createCmd.ExecuteNonQueryAsync();
            
            // Bulk insert into temp table
            using var bulkCopy = new SqlBulkCopy(cloudConnection, SqlBulkCopyOptions.Default, transaction);
            bulkCopy.DestinationTableName = tempTableName;
            bulkCopy.BatchSize = 1000;
            bulkCopy.BulkCopyTimeout = 300;
            
            if (isIdentity)
                bulkCopy.ColumnMappings.Add(primaryKeyColumn, primaryKeyColumn);
            foreach (var prop in syncProperties)
                bulkCopy.ColumnMappings.Add(prop.GetColumnName(), prop.GetColumnName());
            
            var dataTable = CreateDataTableFromRecords(records, pkProperty, syncProperties, isIdentity);
            await bulkCopy.WriteToServerAsync(dataTable);
            
            // Execute MERGE from temp table
            var mergeFromTempSql = BuildMergeFromTempTableSql(tableName, tempTableName, primaryKeyColumn, syncProperties, isIdentity);
            using var mergeCmd = new SqlCommand(mergeFromTempSql, cloudConnection, transaction);
            recordsSynced = await mergeCmd.ExecuteNonQueryAsync();
            
            // Drop temp table
            using var dropCmd = new SqlCommand($"DROP TABLE {tempTableName}", cloudConnection, transaction);
            await dropCmd.ExecuteNonQueryAsync();
        }
        catch
        {
            try
            {
                using var dropCmd = new SqlCommand($"IF OBJECT_ID('tempdb..{tempTableName}') IS NOT NULL DROP TABLE {tempTableName}", cloudConnection, transaction);
                await dropCmd.ExecuteNonQueryAsync();
            }
            catch { }
            throw;
        }
        
        return recordsSynced;
    }
    
    /// <summary>
    /// Builds SQL MERGE statement
    /// </summary>
    private string BuildMergeStatement(
        string tableName,
        string primaryKeyColumn,
        List<Microsoft.EntityFrameworkCore.Metadata.IProperty> syncProperties,
        bool isIdentity)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"MERGE [{tableName}] AS target");
        sb.AppendLine($"USING (SELECT @pk AS [{primaryKeyColumn}]");
        foreach (var prop in syncProperties)
            sb.AppendLine($", @{prop.GetColumnName()} AS [{prop.GetColumnName()}]");
        sb.AppendLine(") AS source");
        sb.AppendLine($"ON target.[{primaryKeyColumn}] = source.[{primaryKeyColumn}]");
        sb.AppendLine("WHEN MATCHED THEN UPDATE SET ");
        sb.Append(string.Join(", ", syncProperties.Select(p => $"target.[{p.GetColumnName()}] = source.[{p.GetColumnName()}]")));
        sb.AppendLine();
        sb.AppendLine("WHEN NOT MATCHED THEN");
        sb.Append($"INSERT ([{primaryKeyColumn}]");
        foreach (var prop in syncProperties)
            sb.Append($", [{prop.GetColumnName()}]");
        sb.Append(") VALUES (source.[");
        sb.Append(primaryKeyColumn);
        sb.Append("]");
        foreach (var prop in syncProperties)
            sb.Append($", source.[{prop.GetColumnName()}]");
        sb.AppendLine(");");
        return sb.ToString();
    }
    
    /// <summary>
    /// Builds MERGE statement from temp table
    /// </summary>
    private string BuildMergeFromTempTableSql(
        string targetTable,
        string tempTable,
        string primaryKeyColumn,
        List<Microsoft.EntityFrameworkCore.Metadata.IProperty> syncProperties,
        bool isIdentity)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"MERGE [{targetTable}] AS target");
        sb.AppendLine($"USING [{tempTable}] AS source");
        sb.AppendLine($"ON target.[{primaryKeyColumn}] = source.[{primaryKeyColumn}]");
        sb.AppendLine("WHEN MATCHED THEN UPDATE SET ");
        sb.Append(string.Join(", ", syncProperties.Select(p => $"target.[{p.GetColumnName()}] = source.[{p.GetColumnName()}]")));
        sb.AppendLine();
        sb.AppendLine("WHEN NOT MATCHED THEN");
        sb.Append($"INSERT ([{primaryKeyColumn}]");
        foreach (var prop in syncProperties)
            sb.Append($", [{prop.GetColumnName()}]");
        sb.Append(") VALUES (source.[");
        sb.Append(primaryKeyColumn);
        sb.Append("]");
        foreach (var prop in syncProperties)
            sb.Append($", source.[{prop.GetColumnName()}]");
        sb.AppendLine(");");
        return sb.ToString();
    }
    
    /// <summary>
    /// Builds CREATE TABLE statement for temp table
    /// </summary>
    private string BuildTempTableSql(
        string tempTableName,
        string primaryKeyColumn,
        List<Microsoft.EntityFrameworkCore.Metadata.IProperty> syncProperties,
        bool isIdentity)
    {
        var sb = new StringBuilder();
        sb.Append($"CREATE TABLE {tempTableName} ([{primaryKeyColumn}] ");
        sb.Append(isIdentity ? "INT" : "NVARCHAR(450)");
        
        foreach (var prop in syncProperties)
        {
            sb.Append($", [{prop.GetColumnName()}] ");
            if (prop.ClrType == typeof(string))
                sb.Append("NVARCHAR(MAX)");
            else if (prop.ClrType == typeof(int) || prop.ClrType == typeof(int?))
                sb.Append("INT");
            else if (prop.ClrType == typeof(decimal) || prop.ClrType == typeof(decimal?))
                sb.Append("DECIMAL(18,2)");
            else if (prop.ClrType == typeof(DateTime) || prop.ClrType == typeof(DateTime?))
                sb.Append("DATETIME");
            else if (prop.ClrType == typeof(bool) || prop.ClrType == typeof(bool?))
                sb.Append("BIT");
            else
                sb.Append("NVARCHAR(MAX)");
        }
        sb.Append(")");
        return sb.ToString();
    }
    
    /// <summary>
    /// Creates DataTable from entity records for bulk insert
    /// </summary>
    private DataTable CreateDataTableFromRecords<T>(
        List<T> records,
        Microsoft.EntityFrameworkCore.Metadata.IProperty pkProperty,
        List<Microsoft.EntityFrameworkCore.Metadata.IProperty> syncProperties,
        bool isIdentity) where T : class
    {
        var dataTable = new DataTable();
        var pkType = Nullable.GetUnderlyingType(pkProperty.ClrType) ?? pkProperty.ClrType;
        dataTable.Columns.Add(pkProperty.GetColumnName(), pkType);
        
        foreach (var prop in syncProperties)
        {
            var columnType = Nullable.GetUnderlyingType(prop.ClrType) ?? prop.ClrType;
            dataTable.Columns.Add(prop.GetColumnName(), columnType);
        }
        
        foreach (var record in records)
        {
            var row = dataTable.NewRow();
            row[pkProperty.GetColumnName()] = pkProperty.PropertyInfo?.GetValue(record) ?? DBNull.Value;
            foreach (var prop in syncProperties)
            {
                var value = prop.PropertyInfo?.GetValue(record);
                row[prop.GetColumnName()] = value ?? DBNull.Value;
            }
            dataTable.Rows.Add(row);
        }
        
        return dataTable;
    }
    
    /// <summary>
    /// Builds SqlParameter collection for MERGE
    /// </summary>
    private List<SqlParameter> BuildMergeParameters<T>(
        T record,
        Microsoft.EntityFrameworkCore.Metadata.IProperty pkProperty,
        List<Microsoft.EntityFrameworkCore.Metadata.IProperty> syncProperties) where T : class
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@pk", pkProperty.PropertyInfo?.GetValue(record) ?? DBNull.Value)
        };
        
        foreach (var prop in syncProperties)
        {
            var value = prop.PropertyInfo?.GetValue(record);
            parameters.Add(new SqlParameter($"@{prop.GetColumnName()}", value ?? DBNull.Value));
        }
        
        return parameters;
    }
    
    /// <summary>
    /// Validates foreign keys for StudentSectionEnrollment (optimized with EXISTS)
    /// </summary>
    private async Task<bool> ValidateStudentSectionEnrollmentFKs<T>(
        SqlConnection cloudConnection,
        SqlTransaction transaction,
        T record,
        IList<Microsoft.EntityFrameworkCore.Metadata.IProperty> properties)
    {
        var studentIdProp = properties.FirstOrDefault(p => p.GetColumnName() == "student_id" || p.Name == "StudentId");
        var sectionIdProp = properties.FirstOrDefault(p => p.GetColumnName() == "SectionID" || p.Name == "SectionId");
        
        if (studentIdProp?.PropertyInfo == null || sectionIdProp?.PropertyInfo == null)
            return true;
        
        var studentId = studentIdProp.PropertyInfo.GetValue(record)?.ToString();
        var sectionId = sectionIdProp.PropertyInfo.GetValue(record);
        
        var checkSql = @"
            IF EXISTS(SELECT 1 FROM [tbl_Students] WHERE [student_id] = @studentId) 
               AND EXISTS(SELECT 1 FROM [tbl_Sections] WHERE [SectionID] = @sectionId)
            SELECT 1 ELSE SELECT 0";
        
        using var checkCmd = new SqlCommand(checkSql, cloudConnection, transaction);
        checkCmd.Parameters.AddWithValue("@studentId", studentId ?? (object)DBNull.Value);
        checkCmd.Parameters.AddWithValue("@sectionId", sectionId ?? DBNull.Value);
        
        var result = await checkCmd.ExecuteScalarAsync();
        return result != null && (int)result == 1;
    }
    
    private static bool IsComputedColumn(Microsoft.EntityFrameworkCore.Metadata.IProperty property)
    {
        // Check if property has computed column SQL (EF Core way)
        var computedColumnSql = property.GetComputedColumnSql();
        if (!string.IsNullOrEmpty(computedColumnSql))
            return true;
        
        // Check if property has DatabaseGeneratedOption.Computed attribute via reflection
        var propertyInfo = property.PropertyInfo;
        if (propertyInfo != null)
        {
            var dbGeneratedAttr = propertyInfo.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute), false)
                .FirstOrDefault() as System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute;
            if (dbGeneratedAttr != null && dbGeneratedAttr.DatabaseGeneratedOption == System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Computed)
                return true;
        }
        
        return false;
    }

    private async Task<int> SyncTableToCloudAsync<T>(SqlConnection cloudConnection, string tableName, string primaryKeyColumn, string? localConnectionString = null) where T : class
    {
        // OPTIMIZED: Use batch processing with MERGE statements instead of row-by-row processing
        // This eliminates N+1 query problem and reduces sync time by 10-100x
        return await SyncTableToCloudAsyncOptimized<T>(cloudConnection, tableName, primaryKeyColumn, localConnectionString);
    }
    
    // OPTIMIZED VERSION - Processes records in batches using MERGE statements
    private async Task<int> SyncTableToCloudAsyncOptimized<T>(SqlConnection cloudConnection, string tableName, string primaryKeyColumn, string? localConnectionString = null) where T : class
    {
        int totalRecordsSynced = 0;
        const int batchSize = 1000; // Process in batches to manage memory
        
        try
        {
            // Create local context
            AppDbContext localContext = CreateLocalContext(localConnectionString);
            
            using (localContext)
            {
                // Get total count for logging
                var totalCount = await localContext.Set<T>().CountAsync();
                
                var dbType = localConnectionString != null ? "customer-specific" : "default";
                _logger?.LogInformation("Syncing {Table}: Processing {Count} records in batches of {BatchSize} (using {DbType} local database)", 
                    tableName, totalCount, batchSize, dbType);
                
                if (totalCount == 0)
                    return 0;

                // Get EF Core metadata
                var entityType = localContext.Model.FindEntityType(typeof(T));
                if (entityType == null) return 0;

                var properties = entityType.GetProperties().ToList(); // Convert to IList for method parameter
                var pkProperty = properties.FirstOrDefault(p => p.GetColumnName() == primaryKeyColumn || p.Name == primaryKeyColumn.Replace("_", ""));
                if (pkProperty == null)
                {
                    _logger?.LogWarning("Primary key property not found for {Table} with column {Column}", tableName, primaryKeyColumn);
                    return 0;
                }

                // Filter out computed and auto-generated columns
                var syncProperties = properties.Where(p => 
                    p.GetColumnName() != primaryKeyColumn && 
                    p.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate &&
                    !IsComputedColumn(p)).ToList();
                
                var isIdentity = pkProperty.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd;
                
                // Process in batches
                int skip = 0;
                while (true)
                {
                    var batch = await localContext.Set<T>()
                        .Skip(skip)
                        .Take(batchSize)
                        .AsNoTracking() // No change tracking needed for read-only
                        .ToListAsync();
                    
                    if (!batch.Any())
                        break;
                    
                    // Use MERGE statement for batch (much faster than individual INSERT/UPDATE)
                    int batchSynced = await SyncBatchUsingMergeAsync(
                        cloudConnection, 
                        tableName, 
                        primaryKeyColumn, 
                        batch, 
                        properties, 
                        pkProperty, 
                        syncProperties, 
                        isIdentity,
                        typeof(T));
                    
                    totalRecordsSynced += batchSynced;
                    skip += batchSize;
                    
                    if (skip % 5000 == 0) // Log progress every 5000 records
                    {
                        _logger?.LogDebug("Synced batch: {Synced}/{Total} records for {Table}", totalRecordsSynced, totalCount, tableName);
                    }
                }
                
                _logger?.LogInformation("Completed syncing {Table}: {Count} records synced", tableName, totalRecordsSynced);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error syncing table {Table} to cloud: {Message}", tableName, ex.Message);
            throw;
        }

        return totalRecordsSynced;
    }
    
    // Helper method to create local context
    private AppDbContext CreateLocalContext(string? localConnectionString)
    {
        if (!string.IsNullOrWhiteSpace(localConnectionString))
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(localConnectionString);
            return new AppDbContext(optionsBuilder.Options);
        }
        else
        {
            return _contextFactory.CreateDbContext();
        }
    }
    
    // OLD IMPLEMENTATION - Kept for reference (slower row-by-row approach)
    private async Task<int> SyncTableToCloudAsyncOld<T>(SqlConnection cloudConnection, string tableName, string primaryKeyColumn, string? localConnectionString = null) where T : class
    {
        int recordsSynced = 0;
        
        try
        {
            // Create a new DbContext for this operation (thread-safe)
            // Use customer-specific local connection string if provided, otherwise use default factory
            AppDbContext localContext;
            if (!string.IsNullOrWhiteSpace(localConnectionString))
            {
                // Create context with customer-specific connection string (account-separated)
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseSqlServer(localConnectionString);
                localContext = new AppDbContext(optionsBuilder.Options);
            }
            else
            {
                // Use default factory (shared database)
                localContext = _contextFactory.CreateDbContext();
            }
            
            using (localContext)
            {
                // Get all records from local database (customer-specific if localConnectionString provided)
                var localRecords = await localContext.Set<T>().ToListAsync();
            
                var dbType = localConnectionString != null ? "customer-specific" : "default";
                _logger?.LogInformation("Syncing {Table}: Found {Count} local records (using {DbType} local database)", 
                    tableName, localRecords.Count, dbType);
                
                if (!localRecords.Any())
                    return 0;

                // Get EF Core metadata for column mapping
                var entityType = localContext.Model.FindEntityType(typeof(T));
                if (entityType == null) return 0;

                var properties = entityType.GetProperties();
                var pkProperty = properties.FirstOrDefault(p => p.GetColumnName() == primaryKeyColumn || p.Name == primaryKeyColumn.Replace("_", ""));
                if (pkProperty == null)
                {
                    _logger?.LogWarning("Primary key property not found for {Table} with column {Column}", tableName, primaryKeyColumn);
                    return 0;
                }
                
                _logger?.LogDebug("Using primary key property: {PropertyName} (Column: {ColumnName}) for {Table}", 
                    pkProperty.Name, pkProperty.GetColumnName(), tableName);

                foreach (var record in localRecords)
            {
                try
                {
                    var pkValue = pkProperty.PropertyInfo?.GetValue(record);
                    if (pkValue == null) continue;

                    // Special validation for StudentSectionEnrollment: Check if foreign keys exist in cloud
                    if (typeof(T) == typeof(StudentSectionEnrollment))
                    {
                        var studentIdProp = properties.FirstOrDefault(p => p.GetColumnName() == "student_id" || p.Name == "StudentId");
                        var sectionIdProp = properties.FirstOrDefault(p => p.GetColumnName() == "SectionID" || p.Name == "SectionId");
                        
                        if (studentIdProp?.PropertyInfo != null && sectionIdProp?.PropertyInfo != null)
                        {
                            var studentId = studentIdProp.PropertyInfo.GetValue(record)?.ToString();
                            var sectionId = sectionIdProp.PropertyInfo.GetValue(record);
                            
                            // Check if Student exists in cloud
                            if (!string.IsNullOrEmpty(studentId))
                            {
                                var studentExistsQuery = "SELECT COUNT(*) FROM [tbl_Students] WHERE [student_id] = @studentId";
                                using var studentCheckCmd = new SqlCommand(studentExistsQuery, cloudConnection);
                                studentCheckCmd.Parameters.AddWithValue("@studentId", studentId);
                                var studentExists = await studentCheckCmd.ExecuteScalarAsync();
                                if (studentExists == null || (int)studentExists == 0)
                                {
                                    _logger?.LogWarning("Skipping StudentSectionEnrollment record: StudentID {StudentId} does not exist in cloud", studentId);
                                    continue;
                                }
                            }
                            
                            // Check if Section exists in cloud
                            if (sectionId != null)
                            {
                                var sectionExistsQuery = "SELECT COUNT(*) FROM [tbl_Sections] WHERE [SectionID] = @sectionId";
                                using var sectionCheckCmd = new SqlCommand(sectionExistsQuery, cloudConnection);
                                sectionCheckCmd.Parameters.AddWithValue("@sectionId", sectionId);
                                var sectionExists = await sectionCheckCmd.ExecuteScalarAsync();
                                if (sectionExists == null || (int)sectionExists == 0)
                                {
                                    _logger?.LogWarning("Skipping StudentSectionEnrollment record: SectionID {SectionId} does not exist in cloud", sectionId);
                                    continue;
                                }
                            }
                        }
                    }

                    // Check if record exists in cloud
                    var existsQuery = $"SELECT COUNT(*) FROM [{tableName}] WHERE [{primaryKeyColumn}] = @pk";
                    using var checkCmd = new SqlCommand(existsQuery, cloudConnection);
                    checkCmd.Parameters.AddWithValue("@pk", pkValue);
                    var result = await checkCmd.ExecuteScalarAsync();
                    var exists = result != null && (int)result > 0;

                    if (exists)
                    {
                        // Update existing record - exclude computed columns
                        var updateProps = properties.Where(p => 
                            p.GetColumnName() != primaryKeyColumn && 
                            p.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate &&
                            !IsComputedColumn(p));
                        var updateSet = string.Join(", ", updateProps.Select(p => $"[{p.GetColumnName()}] = @{p.GetColumnName()}"));
                        var updateQuery = $"UPDATE [{tableName}] SET {updateSet} WHERE [{primaryKeyColumn}] = @pk";
                        
                        using var updateCmd = new SqlCommand(updateQuery, cloudConnection);
                        updateCmd.Parameters.AddWithValue("@pk", pkValue);
                        
                        foreach (var prop in updateProps)
                        {
                            var value = prop.PropertyInfo?.GetValue(record) ?? DBNull.Value;
                            updateCmd.Parameters.AddWithValue($"@{prop.GetColumnName()}", value);
                        }
                        
                        await updateCmd.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        // Insert new record
                        // Check if primary key is identity column
                        var isIdentity = pkProperty.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd;
                        
                        if (isIdentity)
                        {
                            // For identity columns, use SET IDENTITY_INSERT ON
                            await cloudConnection.ExecuteNonQueryAsync($"SET IDENTITY_INSERT [{tableName}] ON");
                            
                            try
                            {
                                // Exclude computed columns from insert
                                var insertProps = properties.Where(p => 
                                    p.GetColumnName() != primaryKeyColumn && 
                                    !IsComputedColumn(p));
                                var insertColumns = $"[{primaryKeyColumn}], " + string.Join(", ", insertProps.Select(p => $"[{p.GetColumnName()}]"));
                                var insertValues = $"@pk, " + string.Join(", ", insertProps.Select(p => $"@{p.GetColumnName()}"));
                                var insertQuery = $"INSERT INTO [{tableName}] ({insertColumns}) VALUES ({insertValues})";
                                
                                using var insertCmd = new SqlCommand(insertQuery, cloudConnection);
                                insertCmd.Parameters.AddWithValue("@pk", pkValue);
                                
                                foreach (var prop in insertProps)
                                {
                                    var value = prop.PropertyInfo?.GetValue(record);
                                    if (value == null && prop.IsNullable)
                                        insertCmd.Parameters.AddWithValue($"@{prop.GetColumnName()}", DBNull.Value);
                                    else
                                        insertCmd.Parameters.AddWithValue($"@{prop.GetColumnName()}", value ?? DBNull.Value);
                                }
                                
                                await insertCmd.ExecuteNonQueryAsync();
                            }
                            finally
                            {
                                await cloudConnection.ExecuteNonQueryAsync($"SET IDENTITY_INSERT [{tableName}] OFF");
                            }
                        }
                        else
                        {
                            // Non-identity primary key - normal insert - exclude computed columns
                            var insertProps = properties.Where(p => 
                                p.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd &&
                                p.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate &&
                                !IsComputedColumn(p));
                            
                            var insertColumns = string.Join(", ", insertProps.Select(p => $"[{p.GetColumnName()}]"));
                            var insertValues = string.Join(", ", insertProps.Select(p => $"@{p.GetColumnName()}"));
                            var insertQuery = $"INSERT INTO [{tableName}] ({insertColumns}) VALUES ({insertValues})";
                            
                            using var insertCmd = new SqlCommand(insertQuery, cloudConnection);
                            
                            foreach (var prop in insertProps)
                            {
                                var value = prop.PropertyInfo?.GetValue(record);
                                if (value == null && prop.IsNullable)
                                    insertCmd.Parameters.AddWithValue($"@{prop.GetColumnName()}", DBNull.Value);
                                else
                                    insertCmd.Parameters.AddWithValue($"@{prop.GetColumnName()}", value ?? DBNull.Value);
                            }
                            
                            await insertCmd.ExecuteNonQueryAsync();
                        }
                    }
                    
                        recordsSynced++;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to sync record from table {Table}: {Message}", tableName, ex.Message);
                    }
                }
                
                _logger?.LogInformation("Completed syncing {Table}: {Count} records synced", tableName, recordsSynced);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error syncing table {Table} to cloud: {Message}", tableName, ex.Message);
            throw;
        }

        return recordsSynced;
    }

    private async Task<int> SyncTableFromCloudAsync<T>(SqlConnection cloudConnection, string tableName, string primaryKeyColumn, string? localConnectionString = null) where T : class
    {
        int recordsSynced = 0;
        
        try
        {
            // Get all records from cloud - read into memory first to avoid DbContext concurrency issues
            var query = $"SELECT * FROM [{tableName}]";
            using var cmd = new SqlCommand(query, cloudConnection);
            using var reader = await cmd.ExecuteReaderAsync();
            
            // Read all records into memory first (close reader before using DbContext)
            var cloudRecords = new List<Dictionary<string, object>>();
            while (await reader.ReadAsync())
            {
                var record = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                    record[columnName] = value;
                }
                cloudRecords.Add(record);
            }
            // Reader is now closed - safe to use DbContext
            
            if (!cloudRecords.Any())
                return 0;
            
            // Create a new DbContext for this operation (thread-safe)
            // Use customer-specific local connection string if provided, otherwise use default factory
            AppDbContext localContext;
            if (!string.IsNullOrWhiteSpace(localConnectionString))
            {
                // Create context with customer-specific connection string (account-separated)
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseSqlServer(localConnectionString);
                localContext = new AppDbContext(optionsBuilder.Options);
            }
            else
            {
                // Use default factory (shared database)
                localContext = _contextFactory.CreateDbContext();
            }
            
            using (localContext)
            {
                // Get EF Core metadata for column mapping
                var entityType = localContext.Model.FindEntityType(typeof(T));
                if (entityType == null) return 0;

                var properties = entityType.GetProperties();
                var localSet = localContext.Set<T>();
                
                var dbType = localConnectionString != null ? "customer-specific" : "default";
                _logger?.LogInformation("Syncing {Table} from cloud: Found {Count} cloud records (using {DbType} local database)", 
                    tableName, cloudRecords.Count, dbType);
                
                // Process records from memory (no active DataReader)
                foreach (var record in cloudRecords)
            {
                try
                {
                    if (!record.ContainsKey(primaryKeyColumn))
                        continue;
                        
                    var pkValue = record[primaryKeyColumn];
                    if (pkValue == DBNull.Value)
                        continue;
                        
                    var pkProperty = properties.FirstOrDefault(p => p.GetColumnName() == primaryKeyColumn || p.Name == primaryKeyColumn.Replace("_", ""));
                    if (pkProperty == null) continue;

                    // Convert pkValue to correct type for FindAsync
                    object? convertedPkValue = pkValue;
                    if (pkProperty.PropertyInfo != null)
                    {
                        var pkType = pkProperty.PropertyInfo.PropertyType;
                        if (pkValue.GetType() != pkType)
                        {
                            convertedPkValue = Convert.ChangeType(pkValue, Nullable.GetUnderlyingType(pkType) ?? pkType);
                        }
                    }

                    var existing = await localSet.FindAsync(convertedPkValue);
                    
                    if (existing == null)
                    {
                        // Check if primary key is identity column
                        var isIdentity = pkProperty.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd;
                        
                        if (isIdentity)
                        {
                            // For identity columns, use raw SQL with IDENTITY_INSERT ON
                            // This is necessary because EF Core's Add() doesn't handle explicit identity values
                            // Use the same local context connection if available, otherwise create new
                            var localConnection = localContext.Database.GetDbConnection();
                            var wasOpen = localConnection.State == System.Data.ConnectionState.Open;
                            if (!wasOpen)
                            {
                                await localConnection.OpenAsync();
                            }
                            
                            try
                            {
                                // Turn IDENTITY_INSERT ON
                                using (var cmdOn = localConnection.CreateCommand())
                                {
                                    cmdOn.CommandText = $"SET IDENTITY_INSERT [{tableName}] ON";
                                    await cmdOn.ExecuteNonQueryAsync();
                                }
                                
                                try
                                {
                                    // Build INSERT statement with all columns
                                    var insertProps = properties.Where(p => 
                                        p.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate);
                                    
                                    var insertColumns = string.Join(", ", insertProps.Select(p => $"[{p.GetColumnName()}]"));
                                    var insertValues = string.Join(", ", insertProps.Select((p, idx) => $"@p{idx}"));
                                    var insertQuery = $"INSERT INTO [{tableName}] ({insertColumns}) VALUES ({insertValues})";
                                    
                                    using var insertCmd = localConnection.CreateCommand();
                                    insertCmd.CommandText = insertQuery;
                                    
                                    // Build parameters from record data (now from memory, not reader)
                                    int paramIndex = 0;
                                    foreach (var prop in insertProps)
                                    {
                                        var columnName = prop.GetColumnName();
                                        var param = insertCmd.CreateParameter();
                                        param.ParameterName = $"@p{paramIndex}";
                                        
                                        if (record.ContainsKey(columnName))
                                        {
                                            var value = record[columnName];
                                            param.Value = value == DBNull.Value ? DBNull.Value : value;
                                        }
                                        else
                                        {
                                            param.Value = DBNull.Value;
                                        }
                                        
                                        insertCmd.Parameters.Add(param);
                                        paramIndex++;
                                    }
                                    
                                    await insertCmd.ExecuteNonQueryAsync();
                                    recordsSynced++;
                                }
                                finally
                                {
                                    // Turn IDENTITY_INSERT OFF
                                    using (var cmdOff = localConnection.CreateCommand())
                                    {
                                        cmdOff.CommandText = $"SET IDENTITY_INSERT [{tableName}] OFF";
                                        await cmdOff.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                            finally
                            {
                                if (!wasOpen)
                                {
                                    await localConnection.CloseAsync();
                                }
                            }
                        }
                        else
                        {
                            // Non-identity primary key - use EF Core Add()
                            var entity = Activator.CreateInstance<T>();
                            
                            // Create case-insensitive dictionary for record lookup
                            var recordCaseInsensitive = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                            foreach (var kvp in record)
                            {
                                recordCaseInsensitive[kvp.Key] = kvp.Value;
                            }
                            
                            foreach (var prop in properties)
                            {
                                var columnName = prop.GetColumnName();
                                
                                // Try exact match first, then case-insensitive match
                                object? value = null;
                                if (record.ContainsKey(columnName))
                                {
                                    value = record[columnName];
                                }
                                else if (recordCaseInsensitive.ContainsKey(columnName))
                                {
                                    value = recordCaseInsensitive[columnName];
                                }
                                
                                if (value != null && value != DBNull.Value && prop.PropertyInfo != null)
                                {
                                    try
                                    {
                                        // Convert value to property type if needed
                                        var convertedValue = ConvertValue(value, prop.PropertyInfo.PropertyType);
                                        prop.PropertyInfo.SetValue(entity, convertedValue);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger?.LogWarning(ex, "Failed to set property {Property} with value {Value} for table {Table}", 
                                            prop.Name, value, tableName);
                                    }
                                }
                            }
                            
                            try
                            {
                                localSet.Add(entity);
                                recordsSynced++;
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "Failed to add entity to {Table}. PK: {PK}", tableName, pkValue);
                                // Continue with next record instead of failing entire sync
                            }
                        }
                    }
                    else
                    {
                        // Conflict resolution: Cloud wins (check LastModified if available)
                        var lastModifiedProp = properties.FirstOrDefault(p => 
                            p.GetColumnName().ToLower().Contains("lastmodified") || 
                            p.GetColumnName().ToLower().Contains("updated") ||
                            p.GetColumnName().ToLower().Contains("modified"));
                        
                        bool shouldUpdate = true;
                        if (lastModifiedProp != null && lastModifiedProp.PropertyInfo != null)
                        {
                            var columnName = lastModifiedProp.GetColumnName();
                            if (record.ContainsKey(columnName))
                            {
                                var cloudLastModified = record[columnName];
                                var localLastModified = lastModifiedProp.PropertyInfo.GetValue(existing);
                                
                                if (cloudLastModified != DBNull.Value && localLastModified != null)
                                {
                                    // Only update if cloud version is newer (cloud wins)
                                    if (cloudLastModified is DateTime cloudDate && localLastModified is DateTime localDate)
                                    {
                                        shouldUpdate = cloudDate >= localDate;
                                    }
                                }
                            }
                        }
                        
                        if (shouldUpdate)
                        {
                            // Update existing entity (cloud wins)
                            // Create case-insensitive dictionary for record lookup
                            var recordCaseInsensitive = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                            foreach (var kvp in record)
                            {
                                recordCaseInsensitive[kvp.Key] = kvp.Value;
                            }
                            
                            foreach (var prop in properties)
                            {
                                var columnName = prop.GetColumnName();
                                
                                // Skip primary key and auto-generated properties
                                if (prop.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd)
                                    continue;
                                
                                // Try exact match first, then case-insensitive match
                                object? value = null;
                                if (record.ContainsKey(columnName))
                                {
                                    value = record[columnName];
                                }
                                else if (recordCaseInsensitive.ContainsKey(columnName))
                                {
                                    value = recordCaseInsensitive[columnName];
                                }
                                
                                if (value != null && value != DBNull.Value && prop.PropertyInfo != null)
                                {
                                    try
                                    {
                                        var convertedValue = ConvertValue(value, prop.PropertyInfo.PropertyType);
                                        prop.PropertyInfo.SetValue(existing, convertedValue);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger?.LogWarning(ex, "Failed to update property {Property} with value {Value} for table {Table}", 
                                            prop.Name, value, tableName);
                                    }
                                }
                            }
                            
                            try
                            {
                                localSet.Update(existing);
                                recordsSynced++;
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "Failed to update entity in {Table}. PK: {PK}", tableName, pkValue);
                                // Continue with next record instead of failing entire sync
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to sync record from cloud table {Table}", tableName);
                }
            }
            
            // Save all changes for non-identity primary keys
            // Identity columns use raw SQL which executes immediately, but EF Core operations need SaveChanges
            if (recordsSynced > 0)
            {
                try
                {
                    await localContext.SaveChangesAsync();
                    _logger?.LogInformation("Saved {Count} records for table {Table}", recordsSynced, tableName);
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
                {
                    _logger?.LogError(dbEx, "Database update error saving {Count} records for table {Table}. Inner exception: {InnerEx}", 
                        recordsSynced, tableName, dbEx.InnerException?.Message);
                    
                    // Log validation errors if available
                    if (dbEx.Entries != null && dbEx.Entries.Any())
                    {
                        foreach (var entry in dbEx.Entries)
                        {
                            _logger?.LogError("Failed entity: {EntityType}, State: {State}", 
                                entry.Entity.GetType().Name, entry.State);
                        }
                    }
                    throw; // Re-throw to fail the sync operation
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Unexpected error saving {Count} records for table {Table}", recordsSynced, tableName);
                    throw;
                }
            }
            
            _logger?.LogInformation("Completed syncing {Table} from cloud: {Count} records synced", tableName, recordsSynced);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error syncing table {Table} from cloud", tableName);
            throw;
        }

        return recordsSynced;
    }

    private object? ConvertValue(object value, Type targetType)
    {
        if (value == null || value == DBNull.Value)
            return null;

        if (targetType.IsAssignableFrom(value.GetType()))
            return value;

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Handle common conversions
        if (underlyingType == typeof(DateTime) && value is DateTime)
            return value;

        if (underlyingType == typeof(decimal) && value is decimal)
            return value;

        if (underlyingType.IsEnum && value is int)
            return Enum.ToObject(underlyingType, value);

        return Convert.ChangeType(value, underlyingType);
    }

    // Incremental sync methods
    private async Task<int> IncrementalSyncTableToCloudAsync<T>(SqlConnection cloudConnection, string tableName, string primaryKeyColumn, DateTime since, string? localConnectionString = null) where T : class
    {
        int recordsSynced = 0;
        
        try
        {
            // Create a new DbContext for this operation (thread-safe)
            // Use customer-specific local connection string if provided, otherwise use default factory
            AppDbContext localContext;
            if (!string.IsNullOrWhiteSpace(localConnectionString))
            {
                // Create context with customer-specific connection string (account-separated)
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseSqlServer(localConnectionString);
                localContext = new AppDbContext(optionsBuilder.Options);
            }
            else
            {
                // Use default factory (shared database)
                localContext = _contextFactory.CreateDbContext();
            }
            
            using (localContext)
            {
                // Get records modified since 'since' date
                // Check for LastModified, UpdatedAt, UpdatedDate, or CreatedAt columns
                var entityType = localContext.Model.FindEntityType(typeof(T));
                if (entityType == null) return 0;

                var properties = entityType.GetProperties();
            var dateProp = properties.FirstOrDefault(p => 
                p.GetColumnName().ToLower().Contains("lastmodified") || 
                p.GetColumnName().ToLower().Contains("updated") ||
                p.GetColumnName().ToLower().Contains("created"));

            IQueryable<T> query = localContext.Set<T>();
            
            if (dateProp != null && dateProp.PropertyInfo != null)
            {
                // Filter by date if column exists
                var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, dateProp.PropertyInfo);
                var constant = System.Linq.Expressions.Expression.Constant(since);
                var comparison = System.Linq.Expressions.Expression.GreaterThanOrEqual(property, constant);
                var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(comparison, parameter);
                query = query.Where(lambda);
            }

            var localRecords = await query.ToListAsync();
            
            if (!localRecords.Any())
                return 0;

            var pkProperty = properties.FirstOrDefault(p => p.GetColumnName() == primaryKeyColumn || p.Name == primaryKeyColumn.Replace("_", ""));
            if (pkProperty == null) return 0;

            // Use existing sync logic but only for filtered records
            foreach (var record in localRecords)
            {
                try
                {
                    var pkValue = pkProperty.PropertyInfo?.GetValue(record);
                    if (pkValue == null) continue;

                    // Special validation for StudentSectionEnrollment: Check if foreign keys exist in cloud
                    if (typeof(T) == typeof(StudentSectionEnrollment))
                    {
                        var studentIdProp = properties.FirstOrDefault(p => p.GetColumnName() == "student_id" || p.Name == "StudentId");
                        var sectionIdProp = properties.FirstOrDefault(p => p.GetColumnName() == "SectionID" || p.Name == "SectionId");
                        
                        if (studentIdProp?.PropertyInfo != null && sectionIdProp?.PropertyInfo != null)
                        {
                            var studentId = studentIdProp.PropertyInfo.GetValue(record)?.ToString();
                            var sectionId = sectionIdProp.PropertyInfo.GetValue(record);
                            
                            // Check if Student exists in cloud
                            if (!string.IsNullOrEmpty(studentId))
                            {
                                var studentExistsQuery = "SELECT COUNT(*) FROM [tbl_Students] WHERE [student_id] = @studentId";
                                using var studentCheckCmd = new SqlCommand(studentExistsQuery, cloudConnection);
                                studentCheckCmd.Parameters.AddWithValue("@studentId", studentId);
                                var studentExists = await studentCheckCmd.ExecuteScalarAsync();
                                if (studentExists == null || (int)studentExists == 0)
                                {
                                    _logger?.LogWarning("Skipping StudentSectionEnrollment record: StudentID {StudentId} does not exist in cloud", studentId);
                                    continue;
                                }
                            }
                            
                            // Check if Section exists in cloud
                            if (sectionId != null)
                            {
                                var sectionExistsQuery = "SELECT COUNT(*) FROM [tbl_Sections] WHERE [SectionID] = @sectionId";
                                using var sectionCheckCmd = new SqlCommand(sectionExistsQuery, cloudConnection);
                                sectionCheckCmd.Parameters.AddWithValue("@sectionId", sectionId);
                                var sectionExists = await sectionCheckCmd.ExecuteScalarAsync();
                                if (sectionExists == null || (int)sectionExists == 0)
                                {
                                    _logger?.LogWarning("Skipping StudentSectionEnrollment record: SectionID {SectionId} does not exist in cloud", sectionId);
                                    continue;
                                }
                            }
                        }
                    }

                    var existsQuery = $"SELECT COUNT(*) FROM [{tableName}] WHERE [{primaryKeyColumn}] = @pk";
                    using var checkCmd = new SqlCommand(existsQuery, cloudConnection);
                    checkCmd.Parameters.AddWithValue("@pk", pkValue);
                    var result = await checkCmd.ExecuteScalarAsync();
                    var exists = result != null && (int)result > 0;

                    if (exists)
                    {
                        // Update - exclude computed columns
                        var updateProps = properties.Where(p => 
                            p.GetColumnName() != primaryKeyColumn && 
                            p.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate &&
                            !IsComputedColumn(p));
                        var updateSet = string.Join(", ", updateProps.Select(p => $"[{p.GetColumnName()}] = @{p.GetColumnName()}"));
                        var updateQuery = $"UPDATE [{tableName}] SET {updateSet} WHERE [{primaryKeyColumn}] = @pk";
                        
                        using var updateCmd = new SqlCommand(updateQuery, cloudConnection);
                        updateCmd.Parameters.AddWithValue("@pk", pkValue);
                        
                        foreach (var prop in updateProps)
                        {
                            var value = prop.PropertyInfo?.GetValue(record) ?? DBNull.Value;
                            updateCmd.Parameters.AddWithValue($"@{prop.GetColumnName()}", value);
                        }
                        
                        await updateCmd.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        // Insert - use same logic as SyncTableToCloudAsync with identity handling
                        var isIdentity = pkProperty.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd;
                        
                        if (isIdentity)
                        {
                            await cloudConnection.ExecuteNonQueryAsync($"SET IDENTITY_INSERT [{tableName}] ON");
                            try
                            {
                                // Exclude computed columns from insert
                                var insertProps = properties.Where(p => 
                                    p.GetColumnName() != primaryKeyColumn && 
                                    !IsComputedColumn(p));
                                var insertColumns = $"[{primaryKeyColumn}], " + string.Join(", ", insertProps.Select(p => $"[{p.GetColumnName()}]"));
                                var insertValues = $"@pk, " + string.Join(", ", insertProps.Select(p => $"@{p.GetColumnName()}"));
                                var insertQuery = $"INSERT INTO [{tableName}] ({insertColumns}) VALUES ({insertValues})";
                                
                                using var insertCmd = new SqlCommand(insertQuery, cloudConnection);
                                insertCmd.Parameters.AddWithValue("@pk", pkValue);
                                
                                foreach (var prop in insertProps)
                                {
                                    var value = prop.PropertyInfo?.GetValue(record);
                                    insertCmd.Parameters.AddWithValue($"@{prop.GetColumnName()}", value ?? DBNull.Value);
                                }
                                
                                await insertCmd.ExecuteNonQueryAsync();
                            }
                            finally
                            {
                                await cloudConnection.ExecuteNonQueryAsync($"SET IDENTITY_INSERT [{tableName}] OFF");
                            }
                        }
                        else
                        {
                            // Exclude computed columns from insert
                            var insertProps = properties.Where(p => 
                                p.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd &&
                                !IsComputedColumn(p));
                            var insertColumns = string.Join(", ", insertProps.Select(p => $"[{p.GetColumnName()}]"));
                            var insertValues = string.Join(", ", insertProps.Select(p => $"@{p.GetColumnName()}"));
                            var insertQuery = $"INSERT INTO [{tableName}] ({insertColumns}) VALUES ({insertValues})";
                            
                            using var insertCmd = new SqlCommand(insertQuery, cloudConnection);
                            foreach (var prop in insertProps)
                            {
                                var value = prop.PropertyInfo?.GetValue(record);
                                insertCmd.Parameters.AddWithValue($"@{prop.GetColumnName()}", value ?? DBNull.Value);
                            }
                            await insertCmd.ExecuteNonQueryAsync();
                        }
                    }
                    
                    recordsSynced++;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to sync record from table {Table}", tableName);
                }
            }
            
            _logger?.LogInformation("Completed incremental syncing {Table} to cloud: {Count} records synced", tableName, recordsSynced);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in incremental sync to cloud for table {Table}", tableName);
        }

        return recordsSynced;
    }

    private async Task<int> IncrementalSyncTableFromCloudAsync<T>(SqlConnection cloudConnection, string tableName, string primaryKeyColumn, DateTime since, string? localConnectionString = null) where T : class
    {
        int recordsSynced = 0;
        
        try
        {
            // Query cloud for records modified since 'since'
            var dateColumns = new[] { "LastModified", "UpdatedAt", "UpdatedDate", "CreatedAt", "created_at", "updated_at" };
            
            string query = $"SELECT * FROM [{tableName}]";
            bool hasDateColumn = false;
            
            foreach (var col in dateColumns)
            {
                try
                {
                    var testQuery = $"SELECT TOP 1 [{col}] FROM [{tableName}]";
                    using var testCmd = new SqlCommand(testQuery, cloudConnection);
                    await testCmd.ExecuteScalarAsync();
                    hasDateColumn = true;
                    query = $"SELECT * FROM [{tableName}] WHERE [{col}] >= @since";
                    break;
                }
                catch
                {
                    // Column doesn't exist, try next
                }
            }

            using var cmd = new SqlCommand(query, cloudConnection);
            if (hasDateColumn)
            {
                cmd.Parameters.AddWithValue("@since", since);
            }
            
            using var reader = await cmd.ExecuteReaderAsync();
            
            // Read all records into memory first (close reader before using DbContext)
            var cloudRecords = new List<Dictionary<string, object>>();
            while (await reader.ReadAsync())
            {
                var record = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                    record[columnName] = value;
                }
                cloudRecords.Add(record);
            }
            // Reader is now closed - safe to use DbContext
            
            if (!cloudRecords.Any())
                return 0;
            
            // Create a new DbContext for this operation (thread-safe)
            // Use customer-specific local connection string if provided, otherwise use default factory
            AppDbContext localContext;
            if (!string.IsNullOrWhiteSpace(localConnectionString))
            {
                // Create context with customer-specific connection string (account-separated)
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseSqlServer(localConnectionString);
                localContext = new AppDbContext(optionsBuilder.Options);
            }
            else
            {
                // Use default factory (shared database)
                localContext = _contextFactory.CreateDbContext();
            }
            
            using (localContext)
            {
                var entityType = localContext.Model.FindEntityType(typeof(T));
                if (entityType == null) return 0;

                var properties = entityType.GetProperties();
                var localSet = localContext.Set<T>();
                
                var dbType = localConnectionString != null ? "customer-specific" : "default";
                _logger?.LogInformation("Incremental syncing {Table} from cloud: Found {Count} cloud records (using {DbType} local database)", 
                    tableName, cloudRecords.Count, dbType);
                
                // Process records from memory (no active DataReader)
                foreach (var record in cloudRecords)
            {
                try
                {
                    if (!record.ContainsKey(primaryKeyColumn))
                        continue;
                        
                    var pkValue = record[primaryKeyColumn];
                    if (pkValue == DBNull.Value)
                        continue;
                        
                    var pkProperty = properties.FirstOrDefault(p => p.GetColumnName() == primaryKeyColumn || p.Name == primaryKeyColumn.Replace("_", ""));
                    if (pkProperty == null) continue;

                    // Convert pkValue to correct type for FindAsync
                    object? convertedPkValue = pkValue;
                    if (pkProperty.PropertyInfo != null)
                    {
                        var pkType = pkProperty.PropertyInfo.PropertyType;
                        if (pkValue.GetType() != pkType)
                        {
                            convertedPkValue = Convert.ChangeType(pkValue, Nullable.GetUnderlyingType(pkType) ?? pkType);
                        }
                    }

                    var existing = await localSet.FindAsync(convertedPkValue);
                    
                    if (existing == null)
                    {
                        // Create new entity
                        var entity = Activator.CreateInstance<T>();
                        
                        // Create case-insensitive dictionary for record lookup
                        var recordCaseInsensitive = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        foreach (var kvp in record)
                        {
                            recordCaseInsensitive[kvp.Key] = kvp.Value;
                        }
                        
                        foreach (var prop in properties)
                        {
                            var columnName = prop.GetColumnName();
                            
                            // Try exact match first, then case-insensitive match
                            object? value = null;
                            if (record.ContainsKey(columnName))
                            {
                                value = record[columnName];
                            }
                            else if (recordCaseInsensitive.ContainsKey(columnName))
                            {
                                value = recordCaseInsensitive[columnName];
                            }
                            
                            if (value != null && value != DBNull.Value && prop.PropertyInfo != null)
                            {
                                try
                                {
                                    var convertedValue = ConvertValue(value, prop.PropertyInfo.PropertyType);
                                    prop.PropertyInfo.SetValue(entity, convertedValue);
                                }
                                catch (Exception ex)
                                {
                                    _logger?.LogWarning(ex, "Failed to set property {Property} with value {Value} for table {Table}", 
                                        prop.Name, value, tableName);
                                }
                            }
                        }
                        
                        try
                        {
                            localSet.Add(entity);
                            recordsSynced++;
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Failed to add entity to {Table}. PK: {PK}", tableName, pkValue);
                            // Continue with next record instead of failing entire sync
                        }
                    }
                    else
                    {
                        // Update existing (cloud wins) - use same logic as SyncTableFromCloudAsync
                        var lastModifiedProp = properties.FirstOrDefault(p => 
                            p.GetColumnName().ToLower().Contains("lastmodified") || 
                            p.GetColumnName().ToLower().Contains("updated"));
                        
                        bool shouldUpdate = true;
                        if (lastModifiedProp != null && lastModifiedProp.PropertyInfo != null)
                        {
                            var columnName = lastModifiedProp.GetColumnName();
                            if (record.ContainsKey(columnName))
                            {
                                var cloudLastModified = record[columnName];
                                var localLastModified = lastModifiedProp.PropertyInfo.GetValue(existing);
                                
                                if (cloudLastModified != DBNull.Value && localLastModified != null)
                                {
                                    if (cloudLastModified is DateTime cloudDate && localLastModified is DateTime localDate)
                                    {
                                        shouldUpdate = cloudDate >= localDate;
                                    }
                                }
                            }
                        }
                        
                        if (shouldUpdate)
                        {
                            foreach (var prop in properties)
                            {
                                var columnName = prop.GetColumnName();
                                if (record.ContainsKey(columnName) && prop.PropertyInfo != null && 
                                    prop.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd)
                                {
                                    var value = record[columnName];
                                    if (value != DBNull.Value)
                                    {
                                        var convertedValue = ConvertValue(value, prop.PropertyInfo.PropertyType);
                                        prop.PropertyInfo.SetValue(existing, convertedValue);
                                    }
                                }
                            }
                            
                            localSet.Update(existing);
                            recordsSynced++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to sync record from cloud table {Table}", tableName);
                }
            }
            
            // Save all changes before disposing context
            if (recordsSynced > 0)
            {
                try
                {
                    await localContext.SaveChangesAsync();
                    _logger?.LogInformation("Saved {Count} records for incremental sync of table {Table}", recordsSynced, tableName);
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
                {
                    _logger?.LogError(dbEx, "Database update error saving {Count} records for incremental sync of table {Table}. Inner exception: {InnerEx}", 
                        recordsSynced, tableName, dbEx.InnerException?.Message);
                    
                    // Log validation errors if available
                    if (dbEx.Entries != null && dbEx.Entries.Any())
                    {
                        foreach (var entry in dbEx.Entries)
                        {
                            _logger?.LogError("Failed entity: {EntityType}, State: {State}", 
                                entry.Entity.GetType().Name, entry.State);
                        }
                    }
                    throw; // Re-throw to fail the sync operation
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Unexpected error saving {Count} records for incremental sync of table {Table}", recordsSynced, tableName);
                    throw;
                }
            }
            
            // Save all changes
            if (recordsSynced > 0)
            {
                try
                {
                    await localContext.SaveChangesAsync();
                    _logger?.LogInformation("Saved {Count} records for table {Table} from incremental sync", recordsSynced, tableName);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error saving {Count} records for table {Table} from incremental sync", recordsSynced, tableName);
                }
            }
            
            _logger?.LogInformation("Completed incremental syncing {Table} from cloud: {Count} records synced", tableName, recordsSynced);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in incremental sync from cloud for table {Table}", tableName);
        }

        return recordsSynced;
    }
}

// Extension method for executing SQL commands
public static class SqlConnectionExtensions
{
    public static async Task<int> ExecuteNonQueryAsync(this SqlConnection connection, string sql)
    {
        using var cmd = new SqlCommand(sql, connection);
        return await cmd.ExecuteNonQueryAsync();
    }
}

// Extension method to check if DataReader has column
public static class SqlDataReaderExtensions
{
    public static bool HasColumn(this SqlDataReader reader, string columnName)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}

