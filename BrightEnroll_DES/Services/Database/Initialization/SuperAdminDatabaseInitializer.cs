using Microsoft.Data.SqlClient;
using BrightEnroll_DES.Services.Database.Definitions;

namespace BrightEnroll_DES.Services.Database.Initialization
{
    // Creates the SuperAdmin database and all SuperAdmin tables on startup if they don't exist
    public class SuperAdminDatabaseInitializer
    {
        private readonly string _connectionString;
        private readonly string _databaseName;

        public SuperAdminDatabaseInitializer(string connectionString)
        {
            _connectionString = connectionString;
            
            var builder = new SqlConnectionStringBuilder(connectionString);
            _databaseName = builder.InitialCatalog ?? "DB_BrightEnroll_SuperAdmin";
            
            // Connect to master database to create the target database
            builder.InitialCatalog = "master";
            _connectionString = builder.ConnectionString;
        }

        // Creates the SuperAdmin database if it doesn't already exist
        public async Task<bool> CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                string checkDbQuery = $@"
                    SELECT COUNT(*) 
                    FROM sys.databases 
                    WHERE name = @DatabaseName";

                using var checkCommand = new SqlCommand(checkDbQuery, connection);
                checkCommand.Parameters.AddWithValue("@DatabaseName", _databaseName);
                
                var result = await checkCommand.ExecuteScalarAsync();
                var exists = result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
                
                if (exists == 0)
                {
                    string createDbQuery = $@"
                        CREATE DATABASE [{_databaseName}]";
                    
                    using var createCommand = new SqlCommand(createDbQuery, connection);
                    await createCommand.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine($"SuperAdmin database [{_databaseName}] created successfully.");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating SuperAdmin database: {ex.Message}");
                return false;
            }
        }

        // Creates all SuperAdmin tables from TableDefinitions if they don't exist
        public async Task<bool> CreateTablesIfNotExistAsync()
        {
            try
            {
                // Build connection string to target database (not master)
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = _databaseName;
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();

                var tableDefinitions = TableDefinitions.GetSuperAdminTableDefinitions();
                
                // Batch check: get all existing tables in one query
                var existingTables = new HashSet<string>();
                string checkAllTablesQuery = @"
                    SELECT t.name, s.name as schema_name
                    FROM sys.tables t
                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE s.name = 'dbo'";
                
                using var checkCommand = new SqlCommand(checkAllTablesQuery, connection);
                using var reader = await checkCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    existingTables.Add(reader["name"].ToString() ?? string.Empty);
                }
                await reader.CloseAsync();

                bool anyTableCreated = false;

                // Create missing tables and indexes in batch
                foreach (var tableDef in tableDefinitions)
                {
                    string tableKey = tableDef.TableName;
                    if (!existingTables.Contains(tableKey))
                    {
                        try
                        {
                            // Create table
                            using var createCommand = new SqlCommand(tableDef.CreateTableScript, connection);
                            await createCommand.ExecuteNonQueryAsync();
                            anyTableCreated = true;
                            System.Diagnostics.Debug.WriteLine($"Successfully created SuperAdmin table: {tableKey}");

                            // Create all indexes for this table
                            if (tableDef.CreateIndexesScripts != null)
                            {
                                foreach (var indexScript in tableDef.CreateIndexesScripts)
                                {
                                    if (!string.IsNullOrWhiteSpace(indexScript))
                                    {
                                        try
                                        {
                                            using var indexCommand = new SqlCommand(indexScript, connection);
                                            await indexCommand.ExecuteNonQueryAsync();
                                        }
                                        catch (Exception idxEx)
                                        {
                                            // Log but don't fail - index might already exist or have issues
                                            System.Diagnostics.Debug.WriteLine($"Warning: Could not create index for {tableKey}: {idxEx.Message}");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error creating SuperAdmin table {tableKey}: {ex.Message}");
                            // Continue with other tables even if one fails
                        }
                    }
                }

                if (!anyTableCreated)
                {
                    System.Diagnostics.Debug.WriteLine("All SuperAdmin tables already exist.");
                }

                return anyTableCreated;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating SuperAdmin tables: {ex.Message}");
                return false;
            }
        }

        // Initialize SuperAdmin database and tables
        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                bool dbCreated = await CreateDatabaseIfNotExistsAsync();
                bool tablesCreated = await CreateTablesIfNotExistAsync();
                
                return dbCreated || tablesCreated;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing SuperAdmin database: {ex.Message}");
                return false;
            }
        }
    }
}

