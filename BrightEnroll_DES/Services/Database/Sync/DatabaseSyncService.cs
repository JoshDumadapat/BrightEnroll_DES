using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Services.Database.Connections;
using System.Data;

namespace BrightEnroll_DES.Services.Database.Sync;

public interface IDatabaseSyncService
{
    Task<SyncResult> SyncToCloudAsync();
    Task<SyncResult> SyncFromCloudAsync();
    Task<SyncResult> FullSyncAsync();
    Task<SyncResult> IncrementalSyncAsync(DateTime? since = null);
    Task<bool> TestCloudConnectionAsync();
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
    private readonly string _cloudConnectionString;
    private readonly ILogger<DatabaseSyncService>? _logger;

    public DatabaseSyncService(
        IDbContextFactory<AppDbContext> contextFactory,
        IConfiguration configuration,
        ILogger<DatabaseSyncService>? logger = null)
    {
        _contextFactory = contextFactory;
        _cloudConnectionString = configuration.GetConnectionString("CloudConnection") 
            ?? throw new InvalidOperationException("CloudConnection string not found in configuration");
        _logger = logger;
    }

    public async Task<bool> TestCloudConnectionAsync()
    {
        try
        {
            using var connection = new SqlConnection(_cloudConnectionString);
            await connection.OpenAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect to cloud database");
            return false;
        }
    }

    public async Task<SyncResult> SyncToCloudAsync()
    {
        var result = new SyncResult { Success = true };
        
        try
        {
            // Test cloud connection first
            if (!await TestCloudConnectionAsync())
            {
                result.Success = false;
                result.Message = "Cannot connect to cloud database. Please check your connection settings.";
                result.Errors.Add("Cloud connection failed");
                return result;
            }

            using var cloudConnection = new SqlConnection(_cloudConnectionString);
            await cloudConnection.OpenAsync();

            // Sync in order: Parents first, then dependent tables
            // Core reference data
            result.RecordsPushed += await SyncTableToCloudAsync<SchoolYear>(cloudConnection, "tbl_SchoolYear", "school_year_id");
            result.RecordsPushed += await SyncTableToCloudAsync<Role>(cloudConnection, "tbl_roles", "role_id");
            result.RecordsPushed += await SyncTableToCloudAsync<UserEntity>(cloudConnection, "tbl_Users", "user_ID");
            result.RecordsPushed += await SyncTableToCloudAsync<GradeLevel>(cloudConnection, "tbl_GradeLevel", "gradelevel_ID");
            
            // Employee data
            result.RecordsPushed += await SyncTableToCloudAsync<EmployeeAddress>(cloudConnection, "tbl_employee_address", "address_ID");
            result.RecordsPushed += await SyncTableToCloudAsync<EmployeeEmergencyContact>(cloudConnection, "tbl_employee_emergency_contact", "emergency_ID");
            result.RecordsPushed += await SyncTableToCloudAsync<SalaryInfo>(cloudConnection, "tbl_salary_info", "salary_ID");
            result.RecordsPushed += await SyncTableToCloudAsync<Deduction>(cloudConnection, "tbl_deductions", "deduction_id");
            result.RecordsPushed += await SyncTableToCloudAsync<SalaryChangeRequest>(cloudConnection, "tbl_salary_change_requests", "request_id");
            result.RecordsPushed += await SyncTableToCloudAsync<TimeRecord>(cloudConnection, "tbl_TimeRecords", "time_record_id");
            result.RecordsPushed += await SyncTableToCloudAsync<PayrollTransaction>(cloudConnection, "tbl_payroll_transactions", "transaction_id");
            
            // Student data
            result.RecordsPushed += await SyncTableToCloudAsync<Guardian>(cloudConnection, "tbl_Guardians", "guardian_id");
            result.RecordsPushed += await SyncTableToCloudAsync<Student>(cloudConnection, "tbl_Students", "student_id");
            result.RecordsPushed += await SyncTableToCloudAsync<StudentRequirement>(cloudConnection, "tbl_StudentRequirements", "requirement_id");
            result.RecordsPushed += await SyncTableToCloudAsync<StudentPayment>(cloudConnection, "tbl_StudentPayments", "payment_id");
            result.RecordsPushed += await SyncTableToCloudAsync<Discount>(cloudConnection, "tbl_discounts", "discount_id");
            result.RecordsPushed += await SyncTableToCloudAsync<StudentLedger>(cloudConnection, "tbl_StudentLedgers", "id");
            result.RecordsPushed += await SyncTableToCloudAsync<LedgerCharge>(cloudConnection, "tbl_LedgerCharges", "id");
            result.RecordsPushed += await SyncTableToCloudAsync<LedgerPayment>(cloudConnection, "tbl_LedgerPayments", "id");
            
            // Finance data
            result.RecordsPushed += await SyncTableToCloudAsync<Fee>(cloudConnection, "tbl_Fees", "fee_ID");
            result.RecordsPushed += await SyncTableToCloudAsync<FeeBreakdown>(cloudConnection, "tbl_FeeBreakdown", "breakdown_ID");
            result.RecordsPushed += await SyncTableToCloudAsync<Expense>(cloudConnection, "tbl_Expenses", "expense_ID");
            result.RecordsPushed += await SyncTableToCloudAsync<ExpenseAttachment>(cloudConnection, "tbl_ExpenseAttachments", "attachment_ID");
            result.RecordsPushed += await SyncTableToCloudAsync<ChartOfAccount>(cloudConnection, "tbl_ChartOfAccounts", "account_id");
            result.RecordsPushed += await SyncTableToCloudAsync<JournalEntry>(cloudConnection, "tbl_JournalEntries", "journal_entry_id");
            result.RecordsPushed += await SyncTableToCloudAsync<JournalEntryLine>(cloudConnection, "tbl_JournalEntryLines", "line_id");
            result.RecordsPushed += await SyncTableToCloudAsync<AccountingPeriod>(cloudConnection, "tbl_AccountingPeriods", "period_id");
            
            // Curriculum data
            result.RecordsPushed += await SyncTableToCloudAsync<Building>(cloudConnection, "tbl_Buildings", "BuildingID");
            result.RecordsPushed += await SyncTableToCloudAsync<Classroom>(cloudConnection, "tbl_Classrooms", "RoomID");
            result.RecordsPushed += await SyncTableToCloudAsync<Section>(cloudConnection, "tbl_Sections", "SectionID");
            result.RecordsPushed += await SyncTableToCloudAsync<Subject>(cloudConnection, "tbl_Subjects", "SubjectID");
            result.RecordsPushed += await SyncTableToCloudAsync<SubjectSection>(cloudConnection, "tbl_SubjectSection", "ID");
            result.RecordsPushed += await SyncTableToCloudAsync<SubjectSchedule>(cloudConnection, "tbl_SubjectSchedule", "ScheduleID");
            result.RecordsPushed += await SyncTableToCloudAsync<TeacherSectionAssignment>(cloudConnection, "tbl_TeacherSectionAssignment", "AssignmentID");
            result.RecordsPushed += await SyncTableToCloudAsync<ClassSchedule>(cloudConnection, "tbl_ClassSchedule", "ScheduleID");
            result.RecordsPushed += await SyncTableToCloudAsync<StudentSectionEnrollment>(cloudConnection, "tbl_StudentSectionEnrollment", "enrollment_id");
            
            // Grade data
            result.RecordsPushed += await SyncTableToCloudAsync<Grade>(cloudConnection, "tbl_Grades", "grade_id");
            result.RecordsPushed += await SyncTableToCloudAsync<GradeWeight>(cloudConnection, "tbl_GradeWeights", "weight_id");
            result.RecordsPushed += await SyncTableToCloudAsync<GradeHistory>(cloudConnection, "tbl_GradeHistory", "history_id");
            result.RecordsPushed += await SyncTableToCloudAsync<Attendance>(cloudConnection, "tbl_Attendance", "AttendanceID");
            
            // Inventory & Assets
            result.RecordsPushed += await SyncTableToCloudAsync<Asset>(cloudConnection, "tbl_Assets", "asset_id");
            result.RecordsPushed += await SyncTableToCloudAsync<InventoryItem>(cloudConnection, "tbl_InventoryItems", "item_id");
            result.RecordsPushed += await SyncTableToCloudAsync<AssetAssignment>(cloudConnection, "tbl_AssetAssignments", "assignment_id");
            
            // Logs and notifications
            result.RecordsPushed += await SyncTableToCloudAsync<Notification>(cloudConnection, "tbl_Notifications", "notification_id");
            result.RecordsPushed += await SyncTableToCloudAsync<UserStatusLog>(cloudConnection, "tbl_user_status_logs", "log_id");
            result.RecordsPushed += await SyncTableToCloudAsync<StudentStatusLog>(cloudConnection, "tbl_student_status_logs", "log_id");
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

    public async Task<SyncResult> SyncFromCloudAsync()
    {
        var result = new SyncResult { Success = true };
        
        try
        {
            if (!await TestCloudConnectionAsync())
            {
                result.Success = false;
                result.Message = "Cannot connect to cloud database. Please check your connection settings.";
                result.Errors.Add("Cloud connection failed");
                return result;
            }

            using var cloudConnection = new SqlConnection(_cloudConnectionString);
            await cloudConnection.OpenAsync();

            // Sync in order: Parents first, then dependent tables
            // Core reference data
            result.RecordsPulled += await SyncTableFromCloudAsync<SchoolYear>(cloudConnection, "tbl_SchoolYear", "school_year_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<Role>(cloudConnection, "tbl_roles", "role_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<UserEntity>(cloudConnection, "tbl_Users", "user_ID");
            result.RecordsPulled += await SyncTableFromCloudAsync<GradeLevel>(cloudConnection, "tbl_GradeLevel", "gradelevel_ID");
            
            // Employee data
            result.RecordsPulled += await SyncTableFromCloudAsync<EmployeeAddress>(cloudConnection, "tbl_employee_address", "address_ID");
            result.RecordsPulled += await SyncTableFromCloudAsync<EmployeeEmergencyContact>(cloudConnection, "tbl_employee_emergency_contact", "emergency_ID");
            result.RecordsPulled += await SyncTableFromCloudAsync<SalaryInfo>(cloudConnection, "tbl_salary_info", "salary_ID");
            result.RecordsPulled += await SyncTableFromCloudAsync<Deduction>(cloudConnection, "tbl_deductions", "deduction_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<SalaryChangeRequest>(cloudConnection, "tbl_salary_change_requests", "request_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<TimeRecord>(cloudConnection, "tbl_TimeRecords", "time_record_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<PayrollTransaction>(cloudConnection, "tbl_payroll_transactions", "transaction_id");
            
            // Student data
            result.RecordsPulled += await SyncTableFromCloudAsync<Guardian>(cloudConnection, "tbl_Guardians", "guardian_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<Student>(cloudConnection, "tbl_Students", "student_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<StudentRequirement>(cloudConnection, "tbl_StudentRequirements", "requirement_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<StudentPayment>(cloudConnection, "tbl_StudentPayments", "payment_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<Discount>(cloudConnection, "tbl_discounts", "discount_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<StudentLedger>(cloudConnection, "tbl_StudentLedgers", "id");
            result.RecordsPulled += await SyncTableFromCloudAsync<LedgerCharge>(cloudConnection, "tbl_LedgerCharges", "id");
            result.RecordsPulled += await SyncTableFromCloudAsync<LedgerPayment>(cloudConnection, "tbl_LedgerPayments", "id");
            
            // Finance data
            result.RecordsPulled += await SyncTableFromCloudAsync<Fee>(cloudConnection, "tbl_Fees", "fee_ID");
            result.RecordsPulled += await SyncTableFromCloudAsync<FeeBreakdown>(cloudConnection, "tbl_FeeBreakdown", "breakdown_ID");
            result.RecordsPulled += await SyncTableFromCloudAsync<Expense>(cloudConnection, "tbl_Expenses", "expense_ID");
            result.RecordsPulled += await SyncTableFromCloudAsync<ExpenseAttachment>(cloudConnection, "tbl_ExpenseAttachments", "attachment_ID");
            result.RecordsPulled += await SyncTableFromCloudAsync<ChartOfAccount>(cloudConnection, "tbl_ChartOfAccounts", "account_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<JournalEntry>(cloudConnection, "tbl_JournalEntries", "journal_entry_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<JournalEntryLine>(cloudConnection, "tbl_JournalEntryLines", "line_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<AccountingPeriod>(cloudConnection, "tbl_AccountingPeriods", "period_id");
            
            // Curriculum data
            result.RecordsPulled += await SyncTableFromCloudAsync<Building>(cloudConnection, "tbl_Buildings", "BuildingID");
            result.RecordsPulled += await SyncTableFromCloudAsync<Classroom>(cloudConnection, "tbl_Classrooms", "RoomID");
            result.RecordsPulled += await SyncTableFromCloudAsync<Section>(cloudConnection, "tbl_Sections", "SectionID");
            result.RecordsPulled += await SyncTableFromCloudAsync<Subject>(cloudConnection, "tbl_Subjects", "SubjectID");
            result.RecordsPulled += await SyncTableFromCloudAsync<SubjectSection>(cloudConnection, "tbl_SubjectSection", "ID");
            result.RecordsPulled += await SyncTableFromCloudAsync<SubjectSchedule>(cloudConnection, "tbl_SubjectSchedule", "ScheduleID");
            result.RecordsPulled += await SyncTableFromCloudAsync<TeacherSectionAssignment>(cloudConnection, "tbl_TeacherSectionAssignment", "AssignmentID");
            result.RecordsPulled += await SyncTableFromCloudAsync<ClassSchedule>(cloudConnection, "tbl_ClassSchedule", "ScheduleID");
            result.RecordsPulled += await SyncTableFromCloudAsync<StudentSectionEnrollment>(cloudConnection, "tbl_StudentSectionEnrollment", "enrollment_id");
            
            // Grade data
            result.RecordsPulled += await SyncTableFromCloudAsync<Grade>(cloudConnection, "tbl_Grades", "grade_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<GradeWeight>(cloudConnection, "tbl_GradeWeights", "weight_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<GradeHistory>(cloudConnection, "tbl_GradeHistory", "history_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<Attendance>(cloudConnection, "tbl_Attendance", "AttendanceID");
            
            // Inventory & Assets
            result.RecordsPulled += await SyncTableFromCloudAsync<Asset>(cloudConnection, "tbl_Assets", "asset_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<InventoryItem>(cloudConnection, "tbl_InventoryItems", "item_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<AssetAssignment>(cloudConnection, "tbl_AssetAssignments", "assignment_id");
            
            // Logs and notifications
            result.RecordsPulled += await SyncTableFromCloudAsync<Notification>(cloudConnection, "tbl_Notifications", "notification_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<UserStatusLog>(cloudConnection, "tbl_user_status_logs", "log_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<StudentStatusLog>(cloudConnection, "tbl_student_status_logs", "log_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<TeacherActivityLog>(cloudConnection, "tbl_TeacherActivityLogs", "id");
            result.RecordsPulled += await SyncTableFromCloudAsync<AuditLog>(cloudConnection, "tbl_audit_logs", "log_id");

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

    public async Task<SyncResult> FullSyncAsync()
    {
        var result = new SyncResult { Success = true };
        
        try
        {
            // First push local changes to cloud
            var pushResult = await SyncToCloudAsync();
            result.RecordsPushed = pushResult.RecordsPushed;
            result.Errors.AddRange(pushResult.Errors);

            // Then pull cloud changes to local
            var pullResult = await SyncFromCloudAsync();
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

    public async Task<SyncResult> IncrementalSyncAsync(DateTime? since = null)
    {
        var result = new SyncResult { Success = true };
        since ??= DateTime.Now.AddDays(-7); // Default: last 7 days
        
        try
        {
            if (!await TestCloudConnectionAsync())
            {
                result.Success = false;
                result.Message = "Cannot connect to cloud database.";
                result.Errors.Add("Cloud connection failed");
                return result;
            }

            using var cloudConnection = new SqlConnection(_cloudConnectionString);
            await cloudConnection.OpenAsync();

            // Incremental sync - only sync changed records
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<UserEntity>(cloudConnection, "tbl_Users", "user_ID", since.Value);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<Guardian>(cloudConnection, "tbl_Guardians", "guardian_id", since.Value);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<GradeLevel>(cloudConnection, "tbl_GradeLevel", "gradelevel_ID", since.Value);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<Student>(cloudConnection, "tbl_Students", "student_id", since.Value);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<StudentRequirement>(cloudConnection, "tbl_StudentRequirements", "requirement_id", since.Value);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<StudentPayment>(cloudConnection, "tbl_StudentPayments", "payment_id", since.Value);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<Fee>(cloudConnection, "tbl_Fees", "fee_ID", since.Value);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<Expense>(cloudConnection, "tbl_Expenses", "expense_id", since.Value);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<EmployeeAddress>(cloudConnection, "tbl_employee_address", "address_id", since.Value);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<Classroom>(cloudConnection, "tbl_Classrooms", "RoomID", since.Value);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<Section>(cloudConnection, "tbl_Sections", "section_id", since.Value);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<Subject>(cloudConnection, "tbl_Subjects", "subject_id", since.Value);
            result.RecordsPushed += await IncrementalSyncTableToCloudAsync<StudentSectionEnrollment>(cloudConnection, "tbl_StudentSectionEnrollment", "enrollment_id", since.Value);

            // Pull changes from cloud
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<UserEntity>(cloudConnection, "tbl_Users", "user_ID", since.Value);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<Guardian>(cloudConnection, "tbl_Guardians", "guardian_id", since.Value);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<GradeLevel>(cloudConnection, "tbl_GradeLevel", "gradelevel_ID", since.Value);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<Student>(cloudConnection, "tbl_Students", "student_id", since.Value);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<StudentRequirement>(cloudConnection, "tbl_StudentRequirements", "requirement_id", since.Value);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<StudentPayment>(cloudConnection, "tbl_StudentPayments", "payment_id", since.Value);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<Fee>(cloudConnection, "tbl_Fees", "fee_ID", since.Value);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<Expense>(cloudConnection, "tbl_Expenses", "expense_id", since.Value);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<EmployeeAddress>(cloudConnection, "tbl_employee_address", "address_id", since.Value);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<Classroom>(cloudConnection, "tbl_Classrooms", "RoomID", since.Value);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<Section>(cloudConnection, "tbl_Sections", "section_id", since.Value);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<Subject>(cloudConnection, "tbl_Subjects", "subject_id", since.Value);
            result.RecordsPulled += await IncrementalSyncTableFromCloudAsync<StudentSectionEnrollment>(cloudConnection, "tbl_StudentSectionEnrollment", "enrollment_id", since.Value);

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

    private async Task<int> SyncTableToCloudAsync<T>(SqlConnection cloudConnection, string tableName, string primaryKeyColumn) where T : class
    {
        int recordsSynced = 0;
        
        try
        {
            // Create a new DbContext for this operation (thread-safe)
            using var localContext = _contextFactory.CreateDbContext();
            
            // Get all records from local database
            var localRecords = await localContext.Set<T>().ToListAsync();
            
            _logger?.LogInformation("Syncing {Table}: Found {Count} local records", tableName, localRecords.Count);
            
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
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error syncing table {Table} to cloud: {Message}", tableName, ex.Message);
            throw;
        }

        return recordsSynced;
    }

    private async Task<int> SyncTableFromCloudAsync<T>(SqlConnection cloudConnection, string tableName, string primaryKeyColumn) where T : class
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
            using var localContext = _contextFactory.CreateDbContext();
            
            // Get EF Core metadata for column mapping
            var entityType = localContext.Model.FindEntityType(typeof(T));
            if (entityType == null) return 0;

            var properties = entityType.GetProperties();
            var localSet = localContext.Set<T>();
            
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
                            // Create a new context for the connection
                            using var contextForConnection = _contextFactory.CreateDbContext();
                            var localConnection = contextForConnection.Database.GetDbConnection();
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
                            
                            foreach (var prop in properties)
                            {
                                var columnName = prop.GetColumnName();
                                if (record.ContainsKey(columnName) && prop.PropertyInfo != null)
                                {
                                    var value = record[columnName];
                                    if (value != DBNull.Value)
                                    {
                                        // Convert value to property type if needed
                                        var convertedValue = ConvertValue(value, prop.PropertyInfo.PropertyType);
                                        prop.PropertyInfo.SetValue(entity, convertedValue);
                                    }
                                }
                            }
                            
                            localSet.Add(entity);
                            recordsSynced++;
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
            
            // SaveChanges is already called within the using block, context will be disposed
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

    // Incremental sync methods - only sync records modified since a given date
    private async Task<int> IncrementalSyncTableToCloudAsync<T>(SqlConnection cloudConnection, string tableName, string primaryKeyColumn, DateTime since) where T : class
    {
        int recordsSynced = 0;
        
        try
        {
            // Create a new DbContext for this operation (thread-safe)
            using var localContext = _contextFactory.CreateDbContext();
            
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
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in incremental sync to cloud for table {Table}", tableName);
        }

        return recordsSynced;
    }

    private async Task<int> IncrementalSyncTableFromCloudAsync<T>(SqlConnection cloudConnection, string tableName, string primaryKeyColumn, DateTime since) where T : class
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
            using var localContext = _contextFactory.CreateDbContext();
            
            var entityType = localContext.Model.FindEntityType(typeof(T));
            if (entityType == null) return 0;

            var properties = entityType.GetProperties();
            var localSet = localContext.Set<T>();
            
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
                        
                        foreach (var prop in properties)
                        {
                            var columnName = prop.GetColumnName();
                            if (record.ContainsKey(columnName) && prop.PropertyInfo != null)
                            {
                                var value = record[columnName];
                                if (value != DBNull.Value)
                                {
                                    var convertedValue = ConvertValue(value, prop.PropertyInfo.PropertyType);
                                    prop.PropertyInfo.SetValue(entity, convertedValue);
                                }
                            }
                        }
                        
                        localSet.Add(entity);
                        recordsSynced++;
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
            await localContext.SaveChangesAsync();
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

