using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BrightEnroll_DES.Data;
using System.Data;

namespace BrightEnroll_DES.Services.Database.Sync;

// Refactored sync service (Cloud-is-source-of-truth)
// Key goals implemented:
// - Cloud always wins conflict strategy
// - Minimal locking and clear transaction boundaries
// - Single-connection IDENTITY_INSERT handling (when needed)
// - Per-table streaming from cloud, then atomic local replace/update
// - Clearer error handling and logging
// - Simpler, maintainable flow

public interface IDatabaseSyncService
{
    Task<bool> TestCloudConnectionAsync();
    Task<SyncResult> SyncToCloudAsync();
    Task<SyncResult> SyncFromCloudAsync();
    Task<SyncResult> FullSyncAsync();
}

public class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RecordsPulled { get; set; }
    public int RecordsPushed { get; set; }
    public DateTime SyncTime { get; set; } = DateTime.Now;
    public List<string> Errors { get; set; } = new List<string>();
}

public class DatabaseSyncServiceRefactor : IDatabaseSyncService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSyncServiceRefactor>? _logger;
    private readonly string _cloudConnectionString;
    private readonly DbContextOptions<AppDbContext> _localOptions;

    // NOTE: we intentionally avoid reusing an injected AppDbContext for SaveChanges
    // for the reasons described previously (control over connection & transaction)
    public DatabaseSyncServiceRefactor(
        IConfiguration configuration,
        ILogger<DatabaseSyncServiceRefactor>? logger = null,
        DbContextOptions<AppDbContext>? localOptions = null)
    {
        _configuration = configuration;
        _logger = logger;
        _cloudConnectionString = configuration.GetConnectionString("CloudConnection")
            ?? throw new InvalidOperationException("CloudConnection string not found in configuration");

        // Capture existing options if provided (for tests) or build default options
        _localOptions = localOptions ?? new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(configuration.GetConnectionString("LocalConnection"))
            .Options;
    }

    public async Task<bool> TestCloudConnectionAsync()
    {
        try
        {
            await using var cloud = new SqlConnection(_cloudConnectionString);
            await cloud.OpenAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open cloud connection");
            return false;
        }
    }

    public async Task<SyncResult> SyncToCloudAsync()
    {
        var result = new SyncResult { Success = true, SyncTime = DateTime.Now };

        if (!await TestCloudConnectionAsync())
        {
            result.Success = false;
            result.Message = "Cannot connect to cloud database";
            result.Errors.Add("Cloud connection failed");
            return result;
        }

        // Tables & primary keys in an ordered list (parents first)
        // Note: tbl_Classrooms must come before tbl_Sections due to FK constraint
        var tables = new (string TableName, Type EntityType, string PrimaryKeyColumn)[]
        {
            ("tbl_Users", typeof(Data.Models.UserEntity), "user_ID"),
            ("tbl_Guardians", typeof(Data.Models.Guardian), "guardian_id"),
            ("tbl_GradeLevel", typeof(Data.Models.GradeLevel), "gradelevel_ID"),
            ("tbl_Students", typeof(Data.Models.Student), "student_id"),
            ("tbl_StudentRequirements", typeof(Data.Models.StudentRequirement), "requirement_id"),
            ("tbl_StudentPayments", typeof(Data.Models.StudentPayment), "payment_id"),
            ("tbl_Fees", typeof(Data.Models.Fee), "fee_ID"),
            ("tbl_Expenses", typeof(Data.Models.Expense), "expense_id"),
            ("tbl_employee_address", typeof(Data.Models.EmployeeAddress), "address_id"),
            ("tbl_Classrooms", typeof(Data.Models.Classroom), "RoomID"),
            ("tbl_Sections", typeof(Data.Models.Section), "SectionID"),
            ("tbl_Subjects", typeof(Data.Models.Subject), "SubjectID")
        };

        await using var cloudConn = new SqlConnection(_cloudConnectionString);
        await cloudConn.OpenAsync();

        foreach (var t in tables)
        {
            try
            {
                result.RecordsPushed += await SyncSingleTableToCloudAsync(cloudConn, t.TableName, t.EntityType, t.PrimaryKeyColumn);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{t.TableName}: {ex.Message}");
                result.Success = false;
                _logger?.LogError(ex, "Failed to sync table {Table} to cloud", t.TableName);
            }
        }

        result.Message = $"SyncToCloud completed. Pushed: {result.RecordsPushed}. Errors: {result.Errors.Count}";
        return result;
    }

    public async Task<SyncResult> SyncFromCloudAsync()
    {
        var result = new SyncResult { Success = true, SyncTime = DateTime.Now };

        if (!await TestCloudConnectionAsync())
        {
            result.Success = false;
            result.Message = "Cannot connect to cloud database";
            result.Errors.Add("Cloud connection failed");
            return result;
        }

        // Tables & primary keys in an ordered list (parents first)
        // Note: tbl_Classrooms must come before tbl_Sections due to FK constraint
        var tables = new (string TableName, Type EntityType, string PrimaryKeyColumn)[]
        {
            ("tbl_Users", typeof(Data.Models.UserEntity), "user_ID"),
            ("tbl_Guardians", typeof(Data.Models.Guardian), "guardian_id"),
            ("tbl_GradeLevel", typeof(Data.Models.GradeLevel), "gradelevel_ID"),
            ("tbl_Students", typeof(Data.Models.Student), "student_id"),
            ("tbl_StudentRequirements", typeof(Data.Models.StudentRequirement), "requirement_id"),
            ("tbl_StudentPayments", typeof(Data.Models.StudentPayment), "payment_id"),
            ("tbl_Fees", typeof(Data.Models.Fee), "fee_ID"),
            ("tbl_Expenses", typeof(Data.Models.Expense), "expense_id"),
            ("tbl_employee_address", typeof(Data.Models.EmployeeAddress), "address_id"),
            ("tbl_Classrooms", typeof(Data.Models.Classroom), "RoomID"),
            ("tbl_Sections", typeof(Data.Models.Section), "SectionID"),
            ("tbl_Subjects", typeof(Data.Models.Subject), "SubjectID")
        };

        await using var cloudConn = new SqlConnection(_cloudConnectionString);
        await cloudConn.OpenAsync();

        foreach (var t in tables)
        {
            try
            {
                result.RecordsPulled += await SyncSingleTableFromCloudAsync(cloudConn, t.TableName, t.EntityType, t.PrimaryKeyColumn);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{t.TableName}: {ex.Message}");
                result.Success = false;
                _logger?.LogError(ex, "Failed to sync table {Table}", t.TableName);
            }
        }

        result.Message = $"SyncFromCloud completed. Pulled: {result.RecordsPulled}. Errors: {result.Errors.Count}";
        return result;
    }

    public async Task<SyncResult> FullSyncAsync()
    {
        var push = await SyncToCloudAsync();
        var pull = await SyncFromCloudAsync();

        return new SyncResult
        {
            Success = push.Success && pull.Success,
            Message = $"Push: {push.Message}; Pull: {pull.Message}",
            RecordsPulled = pull.RecordsPulled,
            RecordsPushed = push.RecordsPushed,
            SyncTime = DateTime.Now,
            Errors = push.Errors.Concat(pull.Errors).ToList()
        };
    }

    /// <summary>
    /// Core: Stream rows from cloud table and upsert them to local DB. Cloud is source-of-truth.
    /// Implementation strategy:
    /// 1. Load cloud rows into a lightweight DataTable (stream to memory per table).
    /// 2. Create a new AppDbContext bound to the same local DB connection and a transaction.
    /// 3. For each cloud row: try to find a local row by primary key. If found, update local properties.
    ///    If not found, insert new local entity.
    /// 4. If the PK is an identity column we enable IDENTITY_INSERT on the same connection/transaction
    ///    before calling SaveChangesAsync so that explicit PKs can be inserted.
    /// 5. Cloud always wins: we overwrite local values unconditionally for the mapped columns.
    /// </summary>
    private async Task<int> SyncSingleTableFromCloudAsync(SqlConnection cloudConnection, string tableName, Type entityType, string primaryKeyColumn)
    {
        int processed = 0;

        // 1) stream cloud rows into DataTable
        var selectSql = $"SELECT * FROM [{tableName}]";
        await using var cmd = new SqlCommand(selectSql, cloudConnection);
        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

        var cloudTable = new DataTable();
        cloudTable.Load(reader); // small tables OK; for very big tables replace with streaming handling

        if (cloudTable.Rows.Count == 0)
            return 0;

        // Find the actual primary key column name in the DataTable (case-insensitive)
        string? actualPkColumn = null;
        foreach (DataColumn col in cloudTable.Columns)
        {
            if (string.Equals(col.ColumnName, primaryKeyColumn, StringComparison.OrdinalIgnoreCase))
            {
                actualPkColumn = col.ColumnName;
                break;
            }
        }

        if (actualPkColumn == null)
        {
            throw new InvalidOperationException($"Primary key column '{primaryKeyColumn}' not found in table '{tableName}'. Available columns: {string.Join(", ", cloudTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");
        }

        // 2) create a new local context bound to the same physical connection
        // This guarantees that SET IDENTITY_INSERT commands and SaveChanges happen on the same connection
        var localConnection = new SqlConnection(_localOptions.Extensions.OfType<Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal.SqlServerOptionsExtension>()
            .FirstOrDefault()?.ConnectionString ?? throw new InvalidOperationException("Local connection string missing in options"));

        // If user provided an explicit LocalConnection in configuration we prefer that
        var configLocal = _configuration.GetConnectionString("LocalConnection");
        if (!string.IsNullOrWhiteSpace(configLocal))
            localConnection = new SqlConnection(configLocal);

        await localConnection.OpenAsync();

        var localOptionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(localConnection);

        await using var localContext = new AppDbContext(localOptionsBuilder.Options);

        // 3) Determine EF mapping (properties, pk, identity)
        var modelEntity = localContext.Model.FindEntityType(entityType);
        if (modelEntity == null)
            throw new InvalidOperationException($"Entity {entityType.Name} not found in local model");

        var properties = modelEntity.GetProperties().ToArray();
        var pkProp = properties.FirstOrDefault(p =>
            string.Equals(p.GetColumnName(), primaryKeyColumn, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p.Name, primaryKeyColumn.Replace("_", ""), StringComparison.OrdinalIgnoreCase));

        if (pkProp == null)
            throw new InvalidOperationException($"PK " + primaryKeyColumn + " not found in entity " + entityType.Name);

        bool pkIsIdentity = pkProp.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd;

        // 4) Build a dictionary for quick column->property lookups
        var colToProp = new Dictionary<string, Microsoft.EntityFrameworkCore.Metadata.IProperty>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in properties)
        {
            colToProp[p.GetColumnName()] = p;
        }

        // 5) Begin a transaction on the local connection
        await using var localTransaction = await localContext.Database.BeginTransactionAsync();
        var identityInserted = false;

        try
        {
            // If pk is identity and we have rows containing explicit PK values we must enable identity insert
            if (pkIsIdentity)
            {
                // only enable if any cloud row has a non-null PK value (practically all will)
                var anyPkNonNull = cloudTable.Rows.Cast<DataRow>().Any(r => r[actualPkColumn] != DBNull.Value);
                if (anyPkNonNull)
                {
                    // Use ExecuteSqlRaw which works with the current transaction
                    await localContext.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{tableName}] ON");
                    identityInserted = true;
                    _logger?.LogDebug("SET IDENTITY_INSERT ON for {Table}", tableName);
                }
            }

            // 6) For each cloud row, upsert into local context (cloud wins)
            foreach (DataRow row in cloudTable.Rows)
            {
                processed++;

                // Get pk value using the actual column name
                var pkValue = row[actualPkColumn];

                object? keyForFind = pkValue == null || pkValue == DBNull.Value ? null : pkValue;

                object? localEntity = null;

                if (keyForFind != null)
                {
                    // attempt find by PK using localContext (tracked lookup)
                    localEntity = await localContext.FindAsync(entityType, new object?[] { keyForFind });
                }

                if (localEntity == null)
                {
                    // create new instance
                    localEntity = Activator.CreateInstance(entityType) ?? throw new InvalidOperationException("Cannot create entity instance");
                    // set PK if provided
                    if (keyForFind != null && pkProp.PropertyInfo != null)
                        pkProp.PropertyInfo.SetValue(localEntity, Convert.ChangeType(keyForFind, pkProp.PropertyInfo.PropertyType));

                    // set other properties from cloud row
                    foreach (DataColumn col in cloudTable.Columns)
                    {
                        if (!colToProp.TryGetValue(col.ColumnName, out var prop))
                            continue;

                        var propInfo = prop.PropertyInfo;
                        if (propInfo == null) continue;

                        var val = row[col.ColumnName];
                        if (val == DBNull.Value) continue;

                        var converted = ConvertValue(val, propInfo.PropertyType);
                        propInfo.SetValue(localEntity, converted);
                    }

                    localContext.Add(localEntity);
                }
                else
                {
                    // update existing entity - cloud wins, so overwrite mapped columns
                    foreach (DataColumn col in cloudTable.Columns)
                    {
                        if (!colToProp.TryGetValue(col.ColumnName, out var prop))
                            continue;

                        var propInfo = prop.PropertyInfo;
                        if (propInfo == null) continue;

                        var val = row[col.ColumnName];
                        if (val == DBNull.Value)
                        {
                            propInfo.SetValue(localEntity, null);
                            continue;
                        }

                        var converted = ConvertValue(val, propInfo.PropertyType);
                        propInfo.SetValue(localEntity, converted);
                    }

                    localContext.Entry(localEntity).State = EntityState.Modified;
                }

                // periodic flush to keep memory low on very large tables
                if (processed % 200 == 0)
                {
                    await localContext.SaveChangesAsync();
                }
            }

            // final save
            await localContext.SaveChangesAsync();

            // commit local transaction
            await localTransaction.CommitAsync();

            _logger?.LogInformation("Synced {Count} rows for table {Table}", processed, tableName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed syncing table {Table} � rolling back", tableName);
            await localTransaction.RollbackAsync();
            throw;
        }
        finally
        {
            // disable identity insert if we enabled it
            if (identityInserted)
            {
                try
                {
                    // Use ExecuteSqlRaw which works with the current transaction
                    await localContext.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{tableName}] OFF");
                    _logger?.LogDebug("SET IDENTITY_INSERT OFF for {Table}", tableName);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to disable IDENTITY_INSERT for {Table}", tableName);
                }
            }

            // close connection created here
            await localConnection.CloseAsync();
        }

        return processed;
    }

    /// <summary>
    /// Core: Stream rows from local table and upsert them to cloud DB. Local is source-of-truth for push.
    /// Implementation strategy:
    /// 1. Load local rows into a lightweight DataTable (stream to memory per table).
    /// 2. Create a connection to cloud database and begin a transaction.
    /// 3. For each local row: try to find a cloud row by primary key. If found, update cloud properties.
    ///    If not found, insert new cloud entity.
    /// 4. If the PK is an identity column we enable IDENTITY_INSERT on the cloud connection/transaction
    ///    before inserting so that explicit PKs can be inserted.
    /// 5. Local wins: we overwrite cloud values unconditionally for the mapped columns.
    /// </summary>
    private async Task<int> SyncSingleTableToCloudAsync(SqlConnection cloudConnection, string tableName, Type entityType, string primaryKeyColumn)
    {
        int processed = 0;

        // 1) Get local connection and read local rows
        var localConnection = new SqlConnection(_localOptions.Extensions.OfType<Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal.SqlServerOptionsExtension>()
            .FirstOrDefault()?.ConnectionString ?? throw new InvalidOperationException("Local connection string missing in options"));

        var configLocal = _configuration.GetConnectionString("LocalConnection");
        if (!string.IsNullOrWhiteSpace(configLocal))
            localConnection = new SqlConnection(configLocal);

        await localConnection.OpenAsync();

        var localOptionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(localConnection);

        await using var localContext = new AppDbContext(localOptionsBuilder.Options);

        // 2) Determine EF mapping (properties, pk, identity)
        var modelEntity = localContext.Model.FindEntityType(entityType);
        if (modelEntity == null)
            throw new InvalidOperationException($"Entity {entityType.Name} not found in local model");

        var properties = modelEntity.GetProperties().ToArray();
        var pkProp = properties.FirstOrDefault(p =>
            string.Equals(p.GetColumnName(), primaryKeyColumn, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p.Name, primaryKeyColumn.Replace("_", ""), StringComparison.OrdinalIgnoreCase));

        if (pkProp == null)
            throw new InvalidOperationException($"PK {primaryKeyColumn} not found in entity {entityType.Name}");

        bool pkIsIdentity = pkProp.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd;

        // 3) Build a dictionary for quick column->property lookups and identify computed columns
        var colToProp = new Dictionary<string, Microsoft.EntityFrameworkCore.Metadata.IProperty>(StringComparer.OrdinalIgnoreCase);
        var computedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in properties)
        {
            colToProp[p.GetColumnName()] = p;
            // Check if property is computed (OnAddOrUpdate typically indicates computed)
            if (p.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate ||
                p.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnUpdate)
            {
                computedColumns.Add(p.GetColumnName());
            }
        }

        // 4) Load local data into DataTable
        var selectSql = $"SELECT * FROM [{tableName}]";
        await using var localCmd = new SqlCommand(selectSql, localConnection);
        await using var localReader = await localCmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

        var localTable = new DataTable();
        localTable.Load(localReader);

        await localConnection.CloseAsync();

        if (localTable.Rows.Count == 0)
            return 0;

        // Find the actual primary key column name in the DataTable (case-insensitive)
        string? actualPkColumn = null;
        foreach (DataColumn col in localTable.Columns)
        {
            if (string.Equals(col.ColumnName, primaryKeyColumn, StringComparison.OrdinalIgnoreCase))
            {
                actualPkColumn = col.ColumnName;
                break;
            }
        }

        if (actualPkColumn == null)
        {
            throw new InvalidOperationException($"Primary key column '{primaryKeyColumn}' not found in local table '{tableName}'. Available columns: {string.Join(", ", localTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");
        }

        // 5) Begin a transaction on the cloud connection
        await using var cloudTransaction = cloudConnection.BeginTransaction();
        var identityInserted = false;

        try
        {
            // If pk is identity and we have rows containing explicit PK values we must enable identity insert
            if (pkIsIdentity)
            {
                var anyPkNonNull = localTable.Rows.Cast<DataRow>().Any(r => r[actualPkColumn] != DBNull.Value);
                if (anyPkNonNull)
                {
                    var enableCmd = cloudConnection.CreateCommand();
                    enableCmd.Transaction = cloudTransaction;
                    enableCmd.CommandText = $"SET IDENTITY_INSERT [{tableName}] ON";
                    await enableCmd.ExecuteNonQueryAsync();
                    identityInserted = true;
                    _logger?.LogDebug("SET IDENTITY_INSERT ON for {Table} on cloud", tableName);
                }
            }

            // 6) For each local row, upsert into cloud (local wins for push)
            foreach (DataRow row in localTable.Rows)
            {
                processed++;

                var pkValue = row[actualPkColumn];
                object? keyForFind = pkValue == null || pkValue == DBNull.Value ? null : pkValue;

                // Check if record exists in cloud
                bool exists = false;
                if (keyForFind != null)
                {
                    var checkCmd = cloudConnection.CreateCommand();
                    checkCmd.Transaction = cloudTransaction;
                    checkCmd.CommandText = $"SELECT COUNT(*) FROM [{tableName}] WHERE [{actualPkColumn}] = @pk";
                    checkCmd.Parameters.AddWithValue("@pk", keyForFind);
                    var count = await checkCmd.ExecuteScalarAsync();
                    exists = Convert.ToInt32(count) > 0;
                }

                if (exists)
                {
                    // Update existing record
                    var updateParts = new List<string>();
                    var updateCmd = cloudConnection.CreateCommand();
                    updateCmd.Transaction = cloudTransaction;

                    foreach (DataColumn col in localTable.Columns)
                    {
                        if (string.Equals(col.ColumnName, actualPkColumn, StringComparison.OrdinalIgnoreCase))
                            continue; // Skip PK in UPDATE

                        // Skip computed columns - they are calculated by the database
                        if (computedColumns.Contains(col.ColumnName))
                            continue;

                        var paramName = $"@p{updateParts.Count}";
                        updateParts.Add($"[{col.ColumnName}] = {paramName}");
                        var val = row[col.ColumnName];
                        updateCmd.Parameters.AddWithValue(paramName, val == DBNull.Value ? DBNull.Value : val);
                    }

                    updateCmd.CommandText = $"UPDATE [{tableName}] SET {string.Join(", ", updateParts)} WHERE [{actualPkColumn}] = @pk";
                    updateCmd.Parameters.AddWithValue("@pk", keyForFind);
                    await updateCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    // Insert new record
                    var insertCols = new List<string>();
                    var insertValues = new List<string>();
                    var insertCmd = cloudConnection.CreateCommand();
                    insertCmd.Transaction = cloudTransaction;

                    foreach (DataColumn col in localTable.Columns)
                    {
                        // If PK is identity and IDENTITY_INSERT is not enabled, skip the PK column
                        // (let SQL Server generate it automatically)
                        if (pkIsIdentity && !identityInserted && string.Equals(col.ColumnName, actualPkColumn, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // Skip computed columns - they are calculated by the database
                        if (computedColumns.Contains(col.ColumnName))
                            continue;

                        insertCols.Add($"[{col.ColumnName}]");
                        var paramName = $"@p{insertValues.Count}";
                        insertValues.Add(paramName);
                        var val = row[col.ColumnName];
                        insertCmd.Parameters.AddWithValue(paramName, val == DBNull.Value ? DBNull.Value : val);
                    }

                    if (insertCols.Count > 0)
                    {
                        insertCmd.CommandText = $"INSERT INTO [{tableName}] ({string.Join(", ", insertCols)}) VALUES ({string.Join(", ", insertValues)})";
                        await insertCmd.ExecuteNonQueryAsync();
                    }
                }

                // periodic flush to keep memory low on very large tables
                if (processed % 200 == 0)
                {
                    // Note: SQL Server transactions don't need explicit flush, but we can commit in batches if needed
                }
            }

            // commit cloud transaction
            await cloudTransaction.CommitAsync();

            _logger?.LogInformation("Synced {Count} rows for table {Table} to cloud", processed, tableName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed syncing table {Table} to cloud - rolling back", tableName);
            await cloudTransaction.RollbackAsync();
            throw;
        }
        finally
        {
            // disable identity insert if we enabled it
            if (identityInserted)
            {
                try
                {
                    var disableCmd = cloudConnection.CreateCommand();
                    disableCmd.CommandText = $"SET IDENTITY_INSERT [{tableName}] OFF";
                    await disableCmd.ExecuteNonQueryAsync();
                    _logger?.LogDebug("SET IDENTITY_INSERT OFF for {Table} on cloud", tableName);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to disable IDENTITY_INSERT for {Table} on cloud", tableName);
                }
            }
        }

        return processed;
    }

    #region Helpers
    private object? ConvertValue(object value, Type targetType)
    {
        if (value == null || value == DBNull.Value) return null;
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying == typeof(Guid))
        {
            if (value is Guid g) return g;
            if (value is string s) return Guid.Parse(s);
            return new Guid(Convert.ToString(value)!);
        }

        if (underlying.IsEnum)
        {
            if (value is string sv) return Enum.Parse(underlying, sv);
            return Enum.ToObject(underlying, value);
        }

        if (underlying == typeof(byte[]) && value is byte[] b) return b;

        return Convert.ChangeType(value, underlying);
    }
    #endregion
}
