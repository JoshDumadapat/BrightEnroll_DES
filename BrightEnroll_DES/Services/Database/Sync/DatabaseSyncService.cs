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
    private readonly AppDbContext _localContext;
    private readonly string _cloudConnectionString;
    private readonly ILogger<DatabaseSyncService>? _logger;

    public DatabaseSyncService(
        AppDbContext localContext,
        IConfiguration configuration,
        ILogger<DatabaseSyncService>? logger = null)
    {
        _localContext = localContext;
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
            result.RecordsPushed += await SyncTableToCloudAsync<UserEntity>(cloudConnection, "tbl_Users", "user_ID");
            result.RecordsPushed += await SyncTableToCloudAsync<Guardian>(cloudConnection, "tbl_Guardians", "guardian_id");
            result.RecordsPushed += await SyncTableToCloudAsync<GradeLevel>(cloudConnection, "tbl_GradeLevel", "gradelevel_ID");
            result.RecordsPushed += await SyncTableToCloudAsync<Student>(cloudConnection, "tbl_Students", "student_id");
            result.RecordsPushed += await SyncTableToCloudAsync<StudentRequirement>(cloudConnection, "tbl_StudentRequirements", "requirement_id");
            result.RecordsPushed += await SyncTableToCloudAsync<StudentPayment>(cloudConnection, "tbl_StudentPayments", "payment_id");
            result.RecordsPushed += await SyncTableToCloudAsync<Fee>(cloudConnection, "tbl_Fees", "fee_ID");
            result.RecordsPushed += await SyncTableToCloudAsync<Expense>(cloudConnection, "tbl_Expenses", "expense_id");
            result.RecordsPushed += await SyncTableToCloudAsync<EmployeeAddress>(cloudConnection, "tbl_employee_address", "address_id");
            result.RecordsPushed += await SyncTableToCloudAsync<Section>(cloudConnection, "tbl_Sections", "section_id");
            result.RecordsPushed += await SyncTableToCloudAsync<Subject>(cloudConnection, "tbl_Subjects", "subject_id");

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
            result.RecordsPulled += await SyncTableFromCloudAsync<UserEntity>(cloudConnection, "tbl_Users", "user_ID");
            result.RecordsPulled += await SyncTableFromCloudAsync<Guardian>(cloudConnection, "tbl_Guardians", "guardian_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<GradeLevel>(cloudConnection, "tbl_GradeLevel", "gradelevel_ID");
            result.RecordsPulled += await SyncTableFromCloudAsync<Student>(cloudConnection, "tbl_Students", "student_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<StudentRequirement>(cloudConnection, "tbl_StudentRequirements", "requirement_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<StudentPayment>(cloudConnection, "tbl_StudentPayments", "payment_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<Fee>(cloudConnection, "tbl_Fees", "fee_ID");
            result.RecordsPulled += await SyncTableFromCloudAsync<Expense>(cloudConnection, "tbl_Expenses", "expense_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<EmployeeAddress>(cloudConnection, "tbl_employee_address", "address_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<Section>(cloudConnection, "tbl_Sections", "section_id");
            result.RecordsPulled += await SyncTableFromCloudAsync<Subject>(cloudConnection, "tbl_Subjects", "subject_id");

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

    private async Task<int> SyncTableToCloudAsync<T>(SqlConnection cloudConnection, string tableName, string primaryKeyColumn) where T : class
    {
        int recordsSynced = 0;
        
        try
        {
            // Get all records from local database
            var localRecords = await _localContext.Set<T>().ToListAsync();
            
            if (!localRecords.Any())
                return 0;

            // Get EF Core metadata for column mapping
            var entityType = _localContext.Model.FindEntityType(typeof(T));
            if (entityType == null) return 0;

            var properties = entityType.GetProperties();
            var pkProperty = properties.FirstOrDefault(p => p.GetColumnName() == primaryKeyColumn || p.Name == primaryKeyColumn.Replace("_", ""));
            if (pkProperty == null) return 0;

            foreach (var record in localRecords)
            {
                try
                {
                    var pkValue = pkProperty.PropertyInfo?.GetValue(record);
                    if (pkValue == null) continue;

                    // Check if record exists in cloud
                    var existsQuery = $"SELECT COUNT(*) FROM [{tableName}] WHERE [{primaryKeyColumn}] = @pk";
                    using var checkCmd = new SqlCommand(existsQuery, cloudConnection);
                    checkCmd.Parameters.AddWithValue("@pk", pkValue);
                    var exists = (int)await checkCmd.ExecuteScalarAsync() > 0;

                    if (exists)
                    {
                        // Update existing record
                        var updateProps = properties.Where(p => p.GetColumnName() != primaryKeyColumn);
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
                        // Exclude properties that are database-generated (identity, computed, etc.)
                        var insertProps = properties.Where(p => 
                            p.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd &&
                            p.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate);
                        
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
            _logger?.LogError(ex, "Error syncing table {Table} to cloud", tableName);
            throw;
        }

        return recordsSynced;
    }

    private async Task<int> SyncTableFromCloudAsync<T>(SqlConnection cloudConnection, string tableName, string primaryKeyColumn) where T : class
    {
        int recordsSynced = 0;
        
        try
        {
            // Get all records from cloud
            var query = $"SELECT * FROM [{tableName}]";
            using var cmd = new SqlCommand(query, cloudConnection);
            using var reader = await cmd.ExecuteReaderAsync();
            
            // Get EF Core metadata for column mapping
            var entityType = _localContext.Model.FindEntityType(typeof(T));
            if (entityType == null) return 0;

            var properties = entityType.GetProperties();
            var localSet = _localContext.Set<T>();
            
            while (await reader.ReadAsync())
            {
                try
                {
                    var pkValue = reader[primaryKeyColumn];
                    var pkProperty = properties.FirstOrDefault(p => p.GetColumnName() == primaryKeyColumn || p.Name == primaryKeyColumn.Replace("_", ""));
                    if (pkProperty == null) continue;

                    var existing = await localSet.FindAsync(pkValue);
                    
                    if (existing == null)
                    {
                        // Create new entity from cloud data
                        var entity = Activator.CreateInstance<T>();
                        
                        foreach (var prop in properties)
                        {
                            var columnName = prop.GetColumnName();
                            if (reader.HasColumn(columnName))
                            {
                                var value = reader[columnName];
                                if (value != DBNull.Value && prop.PropertyInfo != null)
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
                    else
                    {
                        // Update existing entity (cloud wins for now - can be made configurable)
                        foreach (var prop in properties)
                        {
                            var columnName = prop.GetColumnName();
                            if (reader.HasColumn(columnName) && prop.PropertyInfo != null)
                            {
                                var value = reader[columnName];
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
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to sync record from cloud table {Table}", tableName);
                }
            }
            
            await _localContext.SaveChangesAsync();
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

