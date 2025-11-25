using Microsoft.Data.SqlClient;

namespace BrightEnroll_DES.Services.DBConnections
{
    /// <summary>
    /// Automatically creates the database and tables if they don't exist
    /// </summary>
    public class DatabaseInitializer
    {
        private readonly string _connectionString;
        private readonly string _databaseName;

        public DatabaseInitializer(string connectionString)
        {
            _connectionString = connectionString;
            
            // Extract database name from connection string
            var builder = new SqlConnectionStringBuilder(connectionString);
            _databaseName = builder.InitialCatalog ?? "DB_BrightEnroll_DES";
            
            // Remove database from connection string to connect to master
            builder.InitialCatalog = "master";
            _connectionString = builder.ConnectionString;
        }

        /// <summary>
        /// Creates the database if it doesn't exist
        /// </summary>
        public async Task<bool> CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if database exists
                string checkDbQuery = $@"
                    SELECT COUNT(*) 
                    FROM sys.databases 
                    WHERE name = @DatabaseName";

                using var checkCommand = new SqlCommand(checkDbQuery, connection);
                checkCommand.Parameters.AddWithValue("@DatabaseName", _databaseName);
                
                var exists = (int)await checkCommand.ExecuteScalarAsync();
                
                if (exists == 0)
                {
                    // Create database
                    string createDbQuery = $@"
                        CREATE DATABASE [{_databaseName}]";
                    
                    using var createCommand = new SqlCommand(createDbQuery, connection);
                    await createCommand.ExecuteNonQueryAsync();
                    return true;
                }

                return false; // Database already exists
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating database: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates all tables defined in TableDefinitions if they don't exist
        /// </summary>
        public async Task<bool> CreateTablesIfNotExistAsync()
        {
            try
            {
                // Build connection string with the target database
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = _databaseName;
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();

                bool anyTableCreated = false;
                var tableDefinitions = TableDefinitions.GetAllTableDefinitions();

                foreach (var tableDef in tableDefinitions)
                {
                    // Check if table exists
                    string checkTableQuery = $@"
                        SELECT COUNT(*) 
                        FROM sys.tables 
                        WHERE name = @TableName AND schema_id = SCHEMA_ID(@SchemaName)";

                    using var checkCommand = new SqlCommand(checkTableQuery, connection);
                    checkCommand.Parameters.AddWithValue("@TableName", tableDef.TableName);
                    checkCommand.Parameters.AddWithValue("@SchemaName", tableDef.SchemaName);
                    
                    var tableExists = (int)await checkCommand.ExecuteScalarAsync();

                    if (tableExists == 0)
                    {
                        // Create table
                        using var createCommand = new SqlCommand(tableDef.CreateTableScript, connection);
                        await createCommand.ExecuteNonQueryAsync();
                        System.Diagnostics.Debug.WriteLine($"Table [{tableDef.SchemaName}].[{tableDef.TableName}] created successfully.");

                        // Create indexes
                        foreach (var indexScript in tableDef.CreateIndexesScripts)
                        {
                            try
                            {
                                using var indexCommand = new SqlCommand(indexScript, connection);
                                await indexCommand.ExecuteNonQueryAsync();
                            }
                            catch (Exception indexEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Warning: Could not create index for {tableDef.TableName}: {indexEx.Message}");
                            }
                        }

                        anyTableCreated = true;
                    }
                }

                return anyTableCreated;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating tables: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Initializes the entire database (database + tables)
        /// </summary>
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
                System.Diagnostics.Debug.WriteLine($"Error initializing database: {ex.Message}");
                return false;
            }
        }
    }
}

