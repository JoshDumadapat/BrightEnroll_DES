using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Data.Models.SuperAdmin;
using System.Data;
using System.Reflection;
using System.Text;
using System.Linq.Expressions;

namespace BrightEnroll_DES.Services.Database.SuperAdmin_Sync;

public interface ISuperAdminDatabaseSyncService
{
    Task<SuperAdminSyncResult> SyncToCloudAsync();
    Task<SuperAdminSyncResult> SyncFromCloudAsync();
    Task<SuperAdminSyncResult> FullSyncAsync();
    Task<bool> TestCloudConnectionAsync();
}

public class SuperAdminSyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RecordsPushed { get; set; }
    public int RecordsPulled { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime SyncTime { get; set; } = DateTime.Now;
}

public class SuperAdminDatabaseSyncService : ISuperAdminDatabaseSyncService
{
    private readonly SuperAdminDbContext _context;
    private readonly string? _cloudConnectionString;
    private readonly ILogger<SuperAdminDatabaseSyncService>? _logger;
    private static DateTime? _lastSyncTime = null;

    public SuperAdminDatabaseSyncService(
        SuperAdminDbContext context,
        IConfiguration configuration,
        ILogger<SuperAdminDatabaseSyncService>? logger = null)
    {
        _context = context;
        _cloudConnectionString = configuration.GetConnectionString("SuperAdminCloudConnection") 
            ?? configuration.GetConnectionString("CloudConnection");
        _logger = logger;
        
        if (string.IsNullOrWhiteSpace(_cloudConnectionString))
        {
            _logger?.LogWarning("SuperAdmin CloudConnection string not found in configuration. Cloud sync features will be disabled.");
        }
    }

    public async Task<bool> TestCloudConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(_cloudConnectionString))
        {
            _logger?.LogWarning("CloudConnection string is not configured. Cannot test cloud connection.");
            return false;
        }
        
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

    public async Task<SuperAdminSyncResult> SyncToCloudAsync()
    {
        var result = new SuperAdminSyncResult { Success = true };
        
        if (string.IsNullOrWhiteSpace(_cloudConnectionString))
        {
            result.Success = false;
            result.Message = "CloudConnection string is not configured. Please configure SuperAdminCloudConnection in appsettings.json to enable cloud sync.";
            result.Errors.Add("CloudConnection not configured");
            return result;
        }
        
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

            // Sync SuperAdmin tables in order: Customers first, then dependent tables
            // Using optimized incremental sync
            result.RecordsPushed += await SyncTableToCloudOptimizedAsync<Customer>(cloudConnection, "tbl_Customers", "customer_id", "updated_at", "date_registered");
            result.RecordsPushed += await SyncTableToCloudOptimizedAsync<CustomerInvoice>(cloudConnection, "tbl_CustomerInvoices", "invoice_id", "updated_at", "created_at");
            result.RecordsPushed += await SyncTableToCloudOptimizedAsync<CustomerPayment>(cloudConnection, "tbl_CustomerPayments", "payment_id", null, "created_at");
            result.RecordsPushed += await SyncTableToCloudOptimizedAsync<SupportTicket>(cloudConnection, "tbl_SupportTickets", "ticket_id", "updated_at", "created_at");
            result.RecordsPushed += await SyncTableToCloudOptimizedAsync<SystemUpdate>(cloudConnection, "tbl_SystemUpdates", "update_id", null, "release_date");
            result.RecordsPushed += await SyncTableToCloudOptimizedAsync<SuperAdminBIRInfo>(cloudConnection, "tbl_SuperAdminBIRInfo", "bir_info_id", null, null);

            _lastSyncTime = DateTime.Now;
            result.Message = $"Successfully synced {result.RecordsPushed} SuperAdmin records to cloud.";
            _logger?.LogInformation("SuperAdmin sync to cloud completed: {Count} records", result.RecordsPushed);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error syncing SuperAdmin data to cloud: {ex.Message}";
            result.Errors.Add(ex.Message);
            _logger?.LogError(ex, "Error during SuperAdmin sync to cloud");
        }

        return result;
    }

    public async Task<SuperAdminSyncResult> SyncFromCloudAsync()
    {
        var result = new SuperAdminSyncResult { Success = true };
        
        if (string.IsNullOrWhiteSpace(_cloudConnectionString))
        {
            result.Success = false;
            result.Message = "CloudConnection string is not configured. Please configure SuperAdminCloudConnection in appsettings.json to enable cloud sync.";
            result.Errors.Add("CloudConnection not configured");
            return result;
        }
        
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

            // Pull SuperAdmin tables in order: Customers first, then dependent tables
            // Using optimized incremental sync
            result.RecordsPulled += await SyncTableFromCloudOptimizedAsync<Customer>(cloudConnection, "tbl_Customers", "customer_id", "updated_at", "date_registered");
            result.RecordsPulled += await SyncTableFromCloudOptimizedAsync<CustomerInvoice>(cloudConnection, "tbl_CustomerInvoices", "invoice_id", "updated_at", "created_at");
            result.RecordsPulled += await SyncTableFromCloudOptimizedAsync<CustomerPayment>(cloudConnection, "tbl_CustomerPayments", "payment_id", null, "created_at");
            result.RecordsPulled += await SyncTableFromCloudOptimizedAsync<SupportTicket>(cloudConnection, "tbl_SupportTickets", "ticket_id", "updated_at", "created_at");
            result.RecordsPulled += await SyncTableFromCloudOptimizedAsync<SystemUpdate>(cloudConnection, "tbl_SystemUpdates", "update_id", null, "release_date");
            result.RecordsPulled += await SyncTableFromCloudOptimizedAsync<SuperAdminBIRInfo>(cloudConnection, "tbl_SuperAdminBIRInfo", "bir_info_id", null, null);

            _lastSyncTime = DateTime.Now;
            result.Message = $"Successfully synced {result.RecordsPulled} SuperAdmin records from cloud.";
            _logger?.LogInformation("SuperAdmin sync from cloud completed: {Count} records", result.RecordsPulled);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error syncing SuperAdmin data from cloud: {ex.Message}";
            result.Errors.Add(ex.Message);
            _logger?.LogError(ex, "Error during SuperAdmin sync from cloud");
        }

        return result;
    }

    public async Task<SuperAdminSyncResult> FullSyncAsync()
    {
        var result = new SuperAdminSyncResult { Success = true };
        
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
                result.Message = $"Full SuperAdmin sync completed: {result.RecordsPushed} pushed, {result.RecordsPulled} pulled.";
            }
            else
            {
                result.Success = false;
                result.Message = $"SuperAdmin sync completed with errors: {result.Errors.Count} error(s).";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error during full SuperAdmin sync: {ex.Message}";
            result.Errors.Add(ex.Message);
            _logger?.LogError(ex, "Error during full SuperAdmin sync");
        }

        return result;
    }

    /// <summary>
    /// Optimized sync to cloud - only syncs new or updated records using timestamps
    /// </summary>
    private async Task<int> SyncTableToCloudOptimizedAsync<T>(
        SqlConnection cloudConnection, 
        string tableName, 
        string primaryKeyColumn,
        string? updatedAtColumn,
        string? createdAtColumn) where T : class
    {
        int recordsSynced = 0;
        
        try
        {
            var entityType = _context.Model.FindEntityType(typeof(T));
            if (entityType == null) return 0;

            var properties = entityType.GetProperties();
            var pkProperty = properties.FirstOrDefault(p => 
                p.GetColumnName().Equals(primaryKeyColumn, StringComparison.OrdinalIgnoreCase) || 
                p.Name.Equals(primaryKeyColumn.Replace("_", ""), StringComparison.OrdinalIgnoreCase));
            
            if (pkProperty == null)
            {
                _logger?.LogWarning("Primary key property not found for {Table} with column {Column}", tableName, primaryKeyColumn);
                return 0;
            }

            // Build query to get only new/updated records
            // Load all records first, then filter in memory to avoid LINQ translation issues with reflection
            var allLocalRecords = await _context.Set<T>().ToListAsync();
            List<T> localRecords;
            
            // If we have a last sync time and an updated_at column, filter records updated since then
            if (_lastSyncTime.HasValue && !string.IsNullOrEmpty(updatedAtColumn))
            {
                var updatedAtProperty = properties.FirstOrDefault(p => 
                    p.GetColumnName().Equals(updatedAtColumn, StringComparison.OrdinalIgnoreCase));
                
                if (updatedAtProperty != null && updatedAtProperty.PropertyInfo != null)
                {
                    // Filter in memory using reflection (safe now that data is loaded)
                    localRecords = allLocalRecords.Where(r =>
                    {
                        var value = updatedAtProperty.PropertyInfo.GetValue(r);
                        if (value == null) return true; // Include null values
                        
                        if (value is DateTime dt)
                        {
                            return dt >= _lastSyncTime.Value;
                        }
                        var dtNullable = value as DateTime?;
                        if (dtNullable.HasValue)
                        {
                            return dtNullable.Value >= _lastSyncTime.Value;
                        }
                        return true;
                    }).ToList();
                }
                else
                {
                    localRecords = allLocalRecords;
                }
            }
            // If no updated_at but we have created_at, filter records created since last sync
            else if (_lastSyncTime.HasValue && !string.IsNullOrEmpty(createdAtColumn))
            {
                var createdAtProperty = properties.FirstOrDefault(p => 
                    p.GetColumnName().Equals(createdAtColumn, StringComparison.OrdinalIgnoreCase));
                
                if (createdAtProperty != null && createdAtProperty.PropertyInfo != null)
                {
                    // Filter in memory using reflection (safe now that data is loaded)
                    localRecords = allLocalRecords.Where(r =>
                    {
                        var value = createdAtProperty.PropertyInfo.GetValue(r);
                        if (value == null) return false; // Exclude null created dates
                        
                        if (value is DateTime dt)
                        {
                            return dt >= _lastSyncTime.Value;
                        }
                        var dtNullable = value as DateTime?;
                        if (dtNullable.HasValue)
                        {
                            return dtNullable.Value >= _lastSyncTime.Value;
                        }
                        return false;
                    }).ToList();
                }
                else
                {
                    localRecords = allLocalRecords;
                }
            }
            else
            {
                // No filtering needed, use all records
                localRecords = allLocalRecords;
            }
            
            _logger?.LogInformation("Syncing {Table}: Found {Count} local records to sync", tableName, localRecords.Count);
            
            if (!localRecords.Any())
                return 0;

            // Get existing PKs from cloud in bulk
            var existingPksQuery = $"SELECT [{primaryKeyColumn}] FROM [{tableName}]";
            var existingPks = new HashSet<object>();
            
            using (var cmd = new SqlCommand(existingPksQuery, cloudConnection))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var pkValue = reader.GetValue(0);
                    if (pkValue != DBNull.Value)
                        existingPks.Add(pkValue);
                }
            }

            // Separate new and existing records
            var newRecords = new List<T>();
            var existingRecords = new List<T>();

            foreach (var record in localRecords)
            {
                var pkValue = pkProperty.PropertyInfo?.GetValue(record);
                if (pkValue == null) continue;

                if (existingPks.Contains(pkValue))
                {
                    existingRecords.Add(record);
                }
                else
                {
                    newRecords.Add(record);
                }
            }

            // Bulk insert new records
            if (newRecords.Any())
            {
                recordsSynced += await BulkInsertToCloudAsync(cloudConnection, tableName, primaryKeyColumn, newRecords, properties, pkProperty);
            }

            // Bulk update existing records (only if they've been updated)
            if (existingRecords.Any())
            {
                recordsSynced += await BulkUpdateToCloudAsync(cloudConnection, tableName, primaryKeyColumn, existingRecords, properties, pkProperty, updatedAtColumn);
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

    /// <summary>
    /// Optimized sync from cloud - only syncs new or updated records using timestamps
    /// </summary>
    private async Task<int> SyncTableFromCloudOptimizedAsync<T>(
        SqlConnection cloudConnection, 
        string tableName, 
        string primaryKeyColumn,
        string? updatedAtColumn,
        string? createdAtColumn) where T : class
    {
        int recordsSynced = 0;
        
        try
        {
            var entityType = _context.Model.FindEntityType(typeof(T));
            if (entityType == null) return 0;

            var properties = entityType.GetProperties();
            var pkProperty = properties.FirstOrDefault(p => 
                p.GetColumnName().Equals(primaryKeyColumn, StringComparison.OrdinalIgnoreCase) || 
                p.Name.Equals(primaryKeyColumn.Replace("_", ""), StringComparison.OrdinalIgnoreCase));
            
            if (pkProperty == null) return 0;

            // Build query to get only new/updated records from cloud
            // First check if columns exist before using them in WHERE clause
            var cloudQuery = new StringBuilder($"SELECT * FROM [{tableName}]");
            var hasWhere = false;
            var canUseUpdatedAt = false;
            var canUseCreatedAt = false;

            // Check if updated_at column exists in cloud database
            if (!string.IsNullOrEmpty(updatedAtColumn))
            {
                try
                {
                    var checkColumnQuery = $@"
                        SELECT COUNT(*) 
                        FROM sys.columns 
                        WHERE object_id = OBJECT_ID('{tableName}') 
                        AND name = '{updatedAtColumn}'";
                    
                    using var checkColumnCmd = new SqlCommand(checkColumnQuery, cloudConnection);
                    canUseUpdatedAt = (int)await checkColumnCmd.ExecuteScalarAsync() > 0;
                }
                catch
                {
                    canUseUpdatedAt = false;
                }
            }

            // Check if created_at column exists in cloud database
            if (!string.IsNullOrEmpty(createdAtColumn))
            {
                try
                {
                    var checkColumnQuery = $@"
                        SELECT COUNT(*) 
                        FROM sys.columns 
                        WHERE object_id = OBJECT_ID('{tableName}') 
                        AND name = '{createdAtColumn}'";
                    
                    using var checkColumnCmd = new SqlCommand(checkColumnQuery, cloudConnection);
                    canUseCreatedAt = (int)await checkColumnCmd.ExecuteScalarAsync() > 0;
                }
                catch
                {
                    canUseCreatedAt = false;
                }
            }

            if (_lastSyncTime.HasValue && !string.IsNullOrEmpty(updatedAtColumn) && canUseUpdatedAt)
            {
                cloudQuery.Append($" WHERE [{updatedAtColumn}] >= @lastSync");
                hasWhere = true;
            }
            else if (_lastSyncTime.HasValue && !string.IsNullOrEmpty(createdAtColumn) && canUseCreatedAt)
            {
                cloudQuery.Append($" WHERE [{createdAtColumn}] >= @lastSync");
                hasWhere = true;
            }

            // Get local PKs to check what exists - load all records and extract PKs in memory
            // This avoids LINQ translation issues with reflection
            var allLocalEntities = await _context.Set<T>().ToListAsync();
            var localPkSet = new HashSet<object>();
            
            foreach (var entity in allLocalEntities)
            {
                var pkValue = pkProperty.PropertyInfo?.GetValue(entity);
                if (pkValue != null)
                {
                    localPkSet.Add(pkValue);
                }
            }

            using var cloudCmd = new SqlCommand(cloudQuery.ToString(), cloudConnection);
            if (hasWhere && _lastSyncTime.HasValue)
            {
                cloudCmd.Parameters.AddWithValue("@lastSync", _lastSyncTime.Value);
            }

            using var reader = await cloudCmd.ExecuteReaderAsync();
            
            var newRecords = new List<Dictionary<string, object>>();
            var existingRecords = new List<Dictionary<string, object>>();

            while (await reader.ReadAsync())
            {
                var record = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    record[reader.GetName(i)] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                }

                var pkValue = record[primaryKeyColumn];
                if (pkValue == null || pkValue == DBNull.Value) continue;

                if (localPkSet.Contains(pkValue))
                {
                    existingRecords.Add(record);
                }
                else
                {
                    newRecords.Add(record);
                }
            }
            await reader.CloseAsync();

            _logger?.LogInformation("Syncing {Table}: Found {New} new and {Existing} existing cloud records", 
                tableName, newRecords.Count, existingRecords.Count);

            // Bulk insert new records
            if (newRecords.Any())
            {
                recordsSynced += await BulkInsertFromCloudAsync<T>(newRecords, properties, pkProperty, entityType);
            }

            // Bulk update existing records (with conflict resolution)
            if (existingRecords.Any())
            {
                recordsSynced += await BulkUpdateFromCloudAsync<T>(existingRecords, properties, pkProperty, updatedAtColumn);
            }

            await _context.SaveChangesAsync();
            _logger?.LogInformation("Completed syncing {Table}: {Count} records synced", tableName, recordsSynced);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error syncing table {Table} from cloud: {Message}", tableName, ex.Message);
            throw;
        }
        
        return recordsSynced;
    }

    private async Task<int> BulkInsertToCloudAsync<T>(
        SqlConnection cloudConnection,
        string tableName,
        string primaryKeyColumn,
        List<T> records,
        IEnumerable<Microsoft.EntityFrameworkCore.Metadata.IProperty> properties,
        Microsoft.EntityFrameworkCore.Metadata.IProperty pkProperty) where T : class
    {
        if (!records.Any()) return 0;

        int inserted = 0;
        var isIdentity = pkProperty.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd;

        // Process in batches of 100 for better performance
        const int batchSize = 100;
        for (int i = 0; i < records.Count; i += batchSize)
        {
            var batch = records.Skip(i).Take(batchSize).ToList();
            
            try
            {
                if (isIdentity)
                {
                    await cloudConnection.ExecuteNonQueryAsync($"SET IDENTITY_INSERT [{tableName}] ON");
                }

                try
                {
                    foreach (var record in batch)
                    {
                        var pkValue = pkProperty.PropertyInfo?.GetValue(record);
                        if (pkValue == null) continue;

                        var insertProps = properties.Where(p => 
                            p.GetColumnName() != primaryKeyColumn &&
                            p.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate);

                        var insertColumns = isIdentity 
                            ? $"[{primaryKeyColumn}], " + string.Join(", ", insertProps.Select(p => $"[{p.GetColumnName()}]"))
                            : string.Join(", ", insertProps.Select(p => $"[{p.GetColumnName()}]"));
                        
                        var insertValues = isIdentity
                            ? $"@pk_{inserted}, " + string.Join(", ", insertProps.Select((p, idx) => $"@p_{inserted}_{idx}"))
                            : string.Join(", ", insertProps.Select((p, idx) => $"@p_{inserted}_{idx}"));

                        var insertQuery = $"INSERT INTO [{tableName}] ({insertColumns}) VALUES ({insertValues})";
                        
                        using var insertCmd = new SqlCommand(insertQuery, cloudConnection);
                        
                        if (isIdentity)
                        {
                            insertCmd.Parameters.AddWithValue($"@pk_{inserted}", pkValue);
                        }

                        int paramIdx = 0;
                        foreach (var prop in insertProps)
                        {
                            var value = prop.PropertyInfo?.GetValue(record);
                            insertCmd.Parameters.AddWithValue($"@p_{inserted}_{paramIdx}", value ?? DBNull.Value);
                            paramIdx++;
                        }

                        await insertCmd.ExecuteNonQueryAsync();
                        inserted++;
                    }
                }
                finally
                {
                    if (isIdentity)
                    {
                        await cloudConnection.ExecuteNonQueryAsync($"SET IDENTITY_INSERT [{tableName}] OFF");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error in bulk insert batch for {Table}", tableName);
                // Continue with next batch
            }
        }

        return inserted;
    }

    private async Task<int> BulkUpdateToCloudAsync<T>(
        SqlConnection cloudConnection,
        string tableName,
        string primaryKeyColumn,
        List<T> records,
        IEnumerable<Microsoft.EntityFrameworkCore.Metadata.IProperty> properties,
        Microsoft.EntityFrameworkCore.Metadata.IProperty pkProperty,
        string? updatedAtColumn) where T : class
    {
        if (!records.Any()) return 0;

        int updated = 0;

        // Process in batches
        const int batchSize = 100;
        for (int i = 0; i < records.Count; i += batchSize)
        {
            var batch = records.Skip(i).Take(batchSize).ToList();

            foreach (var record in batch)
            {
                try
                {
                    var pkValue = pkProperty.PropertyInfo?.GetValue(record);
                    if (pkValue == null) continue;

                    // Only update if record has been modified (check updated_at if available and column exists)
                    if (!string.IsNullOrEmpty(updatedAtColumn))
                    {
                        var updatedAtProp = properties.FirstOrDefault(p => 
                            p.GetColumnName().Equals(updatedAtColumn, StringComparison.OrdinalIgnoreCase));
                        
                        if (updatedAtProp?.PropertyInfo != null)
                        {
                            var localUpdatedAt = updatedAtProp.PropertyInfo.GetValue(record) as DateTime?;
                            
                            // Check if column exists in cloud database before querying
                            try
                            {
                                var checkColumnQuery = $@"
                                    SELECT COUNT(*) 
                                    FROM sys.columns 
                                    WHERE object_id = OBJECT_ID('{tableName}') 
                                    AND name = '{updatedAtColumn}'";
                                
                                using var checkColumnCmd = new SqlCommand(checkColumnQuery, cloudConnection);
                                var columnExists = (int)await checkColumnCmd.ExecuteScalarAsync() > 0;
                                
                                if (columnExists)
                                {
                                    // Check cloud's updated_at
                                    var checkQuery = $"SELECT [{updatedAtColumn}] FROM [{tableName}] WHERE [{primaryKeyColumn}] = @pk";
                                    using var checkCmd = new SqlCommand(checkQuery, cloudConnection);
                                    checkCmd.Parameters.AddWithValue("@pk", pkValue);
                                    var cloudUpdatedAt = await checkCmd.ExecuteScalarAsync() as DateTime?;

                                    // Skip if cloud is newer (conflict resolution: cloud wins)
                                    if (cloudUpdatedAt.HasValue && localUpdatedAt.HasValue && cloudUpdatedAt.Value > localUpdatedAt.Value)
                                    {
                                        continue;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // If column doesn't exist or query fails, continue with update
                                _logger?.LogWarning(ex, "Could not check {Column} column in {Table}, proceeding with update", updatedAtColumn, tableName);
                            }
                        }
                    }

                    var updateProps = properties.Where(p => 
                        p.GetColumnName() != primaryKeyColumn && 
                        p.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate);

                    var updateSet = string.Join(", ", updateProps.Select((p, idx) => $"[{p.GetColumnName()}] = @p_{updated}_{idx}"));
                    var updateQuery = $"UPDATE [{tableName}] SET {updateSet} WHERE [{primaryKeyColumn}] = @pk";
                    
                    using var updateCmd = new SqlCommand(updateQuery, cloudConnection);
                    updateCmd.Parameters.AddWithValue("@pk", pkValue);
                    
                    int paramIdx = 0;
                    foreach (var prop in updateProps)
                    {
                        var value = prop.PropertyInfo?.GetValue(record) ?? DBNull.Value;
                        updateCmd.Parameters.AddWithValue($"@p_{updated}_{paramIdx}", value);
                        paramIdx++;
                    }
                    
                    await updateCmd.ExecuteNonQueryAsync();
                    updated++;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to update record in {Table}", tableName);
                }
            }
        }

        return updated;
    }

    private Task<int> BulkInsertFromCloudAsync<T>(
        List<Dictionary<string, object>> records,
        IEnumerable<Microsoft.EntityFrameworkCore.Metadata.IProperty> properties,
        Microsoft.EntityFrameworkCore.Metadata.IProperty pkProperty,
        Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType) where T : class
    {
        int inserted = 0;

        foreach (var cloudRecord in records)
        {
            try
            {
                var newEntity = Activator.CreateInstance<T>();
                if (newEntity == null) continue;

                foreach (var prop in properties)
                {
                    if (cloudRecord.ContainsKey(prop.GetColumnName()) && prop.PropertyInfo != null)
                    {
                        var value = cloudRecord[prop.GetColumnName()];
                        if (value != DBNull.Value)
                        {
                            try
                            {
                                var convertedValue = Convert.ChangeType(value, prop.ClrType);
                                prop.PropertyInfo.SetValue(newEntity, convertedValue);
                            }
                            catch
                            {
                                // Skip if conversion fails
                            }
                        }
                    }
                }
                
                _context.Set<T>().Add(newEntity);
                inserted++;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to insert record from cloud");
            }
        }

        return Task.FromResult(inserted);
    }

    private async Task<int> BulkUpdateFromCloudAsync<T>(
        List<Dictionary<string, object>> records,
        IEnumerable<Microsoft.EntityFrameworkCore.Metadata.IProperty> properties,
        Microsoft.EntityFrameworkCore.Metadata.IProperty pkProperty,
        string? updatedAtColumn) where T : class
    {
        int updated = 0;

        foreach (var cloudRecord in records)
        {
            try
            {
                var pkValue = cloudRecord[pkProperty.GetColumnName()];
                if (pkValue == null || pkValue == DBNull.Value) continue;

                // Convert PK to correct type
                object? convertedPkValue = pkValue;
                if (pkProperty.PropertyInfo != null)
                {
                    var pkType = pkProperty.PropertyInfo.PropertyType;
                    if (pkValue.GetType() != pkType)
                    {
                        convertedPkValue = Convert.ChangeType(pkValue, Nullable.GetUnderlyingType(pkType) ?? pkType);
                    }
                }

                var existingEntity = await _context.Set<T>().FindAsync(convertedPkValue);

                if (existingEntity == null) continue;

                // Conflict resolution: Check timestamps
                if (!string.IsNullOrEmpty(updatedAtColumn) && cloudRecord.ContainsKey(updatedAtColumn))
                {
                    var cloudUpdatedAt = cloudRecord[updatedAtColumn] as DateTime?;
                    var updatedAtProp = properties.FirstOrDefault(p => 
                        p.GetColumnName().Equals(updatedAtColumn, StringComparison.OrdinalIgnoreCase));
                    
                    if (updatedAtProp?.PropertyInfo != null && cloudUpdatedAt.HasValue)
                    {
                        var localUpdatedAt = updatedAtProp.PropertyInfo.GetValue(existingEntity) as DateTime?;
                        
                        // Skip if local is newer (cloud wins only if cloud is newer or equal)
                        if (localUpdatedAt.HasValue && localUpdatedAt.Value > cloudUpdatedAt.Value)
                        {
                            continue;
                        }
                    }
                }

                // Update entity
                foreach (var prop in properties.Where(p => 
                    p.GetColumnName() != pkProperty.GetColumnName() && 
                    p.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate &&
                    cloudRecord.ContainsKey(p.GetColumnName())))
                {
                    var value = cloudRecord[prop.GetColumnName()];
                    if (value != DBNull.Value && prop.PropertyInfo != null)
                    {
                        try
                        {
                            var convertedValue = Convert.ChangeType(value, prop.ClrType);
                            prop.PropertyInfo.SetValue(existingEntity, convertedValue);
                        }
                        catch
                        {
                            // Skip if conversion fails
                        }
                    }
                }

                updated++;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to update record from cloud");
            }
        }

        return updated;
    }
}

// Extension method for ExecuteNonQueryAsync
public static class SqlConnectionExtensions
{
    public static async Task<int> ExecuteNonQueryAsync(this SqlConnection connection, string commandText)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = commandText;
        return await cmd.ExecuteNonQueryAsync();
    }
}
