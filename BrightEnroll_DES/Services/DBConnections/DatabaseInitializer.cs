using Microsoft.Data.SqlClient;

namespace BrightEnroll_DES.Services.DBConnections
{
    // Creates the database and all tables automatically on startup if they don't exist
    public class DatabaseInitializer
    {
        private readonly string _connectionString;
        private readonly string _databaseName;

        public DatabaseInitializer(string connectionString)
        {
            _connectionString = connectionString;
            
            var builder = new SqlConnectionStringBuilder(connectionString);
            _databaseName = builder.InitialCatalog ?? "DB_BrightEnroll_DES";
            
            // Connect to master database to create the target database
            builder.InitialCatalog = "master";
            _connectionString = builder.ConnectionString;
        }

        // Creates the database if it doesn't already exist
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
                var exists = result != null ? (int)result : 0;
                
                if (exists == 0)
                {
                    string createDbQuery = $@"
                        CREATE DATABASE [{_databaseName}]";
                    
                    using var createCommand = new SqlCommand(createDbQuery, connection);
                    await createCommand.ExecuteNonQueryAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating database: {ex.Message}");
                return false;
            }
        }

        // Creates all tables from TableDefinitions if they don't exist (optimized batch processing)
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

                var tableDefinitions = TableDefinitions.GetAllTableDefinitions();
                
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
                        // Create table
                        using var createCommand = new SqlCommand(tableDef.CreateTableScript, connection);
                        await createCommand.ExecuteNonQueryAsync();
                        anyTableCreated = true;

                        // Create all indexes for this table in one batch
                        if (tableDef.CreateIndexesScripts.Any())
                        {
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
                        }
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

        // Adds status column to tbl_Users if it doesn't exist (for existing databases)
        public async Task<bool> AddStatusColumnIfNotExistsAsync()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = _databaseName;
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();

                string checkColumnQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('dbo.tbl_Users') 
                    AND name = 'status'";

                using var checkCommand = new SqlCommand(checkColumnQuery, connection);
                var result = await checkCommand.ExecuteScalarAsync();
                var columnExists = result != null ? (int)result : 0;

                if (columnExists == 0)
                {
                    string addColumnQuery = @"
                        ALTER TABLE [dbo].[tbl_Users]
                        ADD [status] VARCHAR(20) NOT NULL DEFAULT 'active'";

                    using var addCommand = new SqlCommand(addColumnQuery, connection);
                    await addCommand.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine("Status column added to tbl_Users table successfully.");
                    
                    string updateExistingQuery = @"
                        UPDATE [dbo].[tbl_Users]
                        SET [status] = 'active'
                        WHERE [status] IS NULL OR [status] = ''";

                    using var updateCommand = new SqlCommand(updateExistingQuery, connection);
                    await updateCommand.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine("Existing records updated with 'active' status.");
                    
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding status column: {ex.Message}");
                return false;
            }
        }

        // Adds date_registered and status columns to tbl_Students if they don't exist (for existing databases)
        public async Task<bool> AddStudentColumnsIfNotExistAsync()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = _databaseName;
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();

                bool anyColumnAdded = false;

                // Check and add date_registered column
                string checkDateRegisteredQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('dbo.tbl_Students') 
                    AND name = 'date_registered'";

                using var checkDateCommand = new SqlCommand(checkDateRegisteredQuery, connection);
                var dateResult = await checkDateCommand.ExecuteScalarAsync();
                var dateColumnExists = dateResult != null ? (int)dateResult : 0;

                if (dateColumnExists == 0)
                {
                    string addDateColumnQuery = @"
                        ALTER TABLE [dbo].[tbl_Students]
                        ADD [date_registered] DATETIME NOT NULL DEFAULT GETDATE()";

                    using var addDateCommand = new SqlCommand(addDateColumnQuery, connection);
                    await addDateCommand.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine("date_registered column added to tbl_Students table successfully.");
                    anyColumnAdded = true;
                }

                // Check and add status column
                string checkStatusQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('dbo.tbl_Students') 
                    AND name = 'status'";

                using var checkStatusCommand = new SqlCommand(checkStatusQuery, connection);
                var statusResult = await checkStatusCommand.ExecuteScalarAsync();
                var statusColumnExists = statusResult != null ? (int)statusResult : 0;

                if (statusColumnExists == 0)
                {
                    string addStatusColumnQuery = @"
                        ALTER TABLE [dbo].[tbl_Students]
                        ADD [status] VARCHAR(20) NOT NULL DEFAULT 'Pending'";

                    using var addStatusCommand = new SqlCommand(addStatusColumnQuery, connection);
                    await addStatusCommand.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine("status column added to tbl_Students table successfully.");
                    anyColumnAdded = true;
                }

                // Create indexes if columns were added or if they exist but indexes don't
                if (dateColumnExists > 0 || anyColumnAdded)
                {
                    string createDateIndexQuery = @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Students_DateRegistered' AND object_id = OBJECT_ID('dbo.tbl_Students'))
                        CREATE INDEX IX_tbl_Students_DateRegistered ON [dbo].[tbl_Students]([date_registered])";
                    
                    try
                    {
                        using var dateIndexCommand = new SqlCommand(createDateIndexQuery, connection);
                        await dateIndexCommand.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Could not create date_registered index: {ex.Message}");
                    }
                }

                if (statusColumnExists > 0 || anyColumnAdded)
                {
                    string createStatusIndexQuery = @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Students_Status' AND object_id = OBJECT_ID('dbo.tbl_Students'))
                        CREATE INDEX IX_tbl_Students_Status ON [dbo].[tbl_Students]([status])";
                    
                    try
                    {
                        using var statusIndexCommand = new SqlCommand(createStatusIndexQuery, connection);
                        await statusIndexCommand.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Could not create status index: {ex.Message}");
                    }
                }

                return anyColumnAdded;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding student columns: {ex.Message}");
                return false;
            }
        }

        // Ensures sequence table has initial data
        public async Task<bool> InitializeSequenceTableAsync()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = _databaseName;
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();

                // Check if table exists and has data
                string checkDataQuery = @"
                    SELECT COUNT(*) 
                    FROM [dbo].[tbl_StudentID_Sequence]";

                using var checkCommand = new SqlCommand(checkDataQuery, connection);
                var result = await checkCommand.ExecuteScalarAsync();
                var rowCount = result != null ? (int)result : 0;

                if (rowCount == 0)
                {
                    // Insert initial value
                    string insertQuery = @"
                        INSERT INTO [dbo].[tbl_StudentID_Sequence] ([LastStudentID]) 
                        VALUES (158021)";

                    using var insertCommand = new SqlCommand(insertQuery, connection);
                    await insertCommand.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine("Initial sequence value inserted into tbl_StudentID_Sequence.");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing sequence table: {ex.Message}");
                return false;
            }
        }

        // Creates database views if they don't exist
        public async Task<bool> CreateViewsIfNotExistAsync()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = _databaseName;
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();

                var viewDefinitions = ViewDefinitions.GetAllViewDefinitions();
                
                // Batch check: get all existing views in one query
                var existingViews = new HashSet<string>();
                string checkAllViewsQuery = @"
                    SELECT v.name, s.name as schema_name
                    FROM sys.views v
                    INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
                    WHERE s.name = 'dbo'";

                using var checkCommand = new SqlCommand(checkAllViewsQuery, connection);
                using var reader = await checkCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    existingViews.Add(reader["name"].ToString() ?? string.Empty);
                }
                await reader.CloseAsync();

                bool anyViewCreated = false;

                // Create missing views - always recreate to ensure they're in the Views folder
                foreach (var viewDef in viewDefinitions)
                {
                    string viewKey = viewDef.ViewName;
                    
                    // Always drop and recreate to ensure views are in the correct location
                    string dropViewScript = $@"
                        IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[{viewKey}]'))
                        DROP VIEW [dbo].[{viewKey}];";
                    
                    try
                    {
                        using var dropCommand = new SqlCommand(dropViewScript, connection);
                        await dropCommand.ExecuteNonQueryAsync();
                    }
                    catch (Exception dropEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Could not drop view {viewKey}: {dropEx.Message}");
                    }

                    // Create view in dbo schema (will appear in Views folder, not System Views)
                    try
                    {
                        using var createCommand = new SqlCommand(viewDef.CreateViewScript, connection);
                        await createCommand.ExecuteNonQueryAsync();
                        anyViewCreated = true;
                        System.Diagnostics.Debug.WriteLine($"View [dbo].[{viewKey}] created successfully in Views folder.");
                    }
                    catch (Exception createEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating view {viewKey}: {createEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"View script preview: {viewDef.CreateViewScript.Substring(0, Math.Min(200, viewDef.CreateViewScript.Length))}...");
                        // Don't throw - continue with other views
                    }
                }

                return anyViewCreated;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating views: {ex.Message}");
                return false;
            }
        }

        // Creates stored procedures if they don't exist
        public async Task<bool> CreateStoredProceduresIfNotExistAsync()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = _databaseName;
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();

                // Drop if exists and recreate to ensure it's up to date
                string dropProcScript = @"
                    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_CreateStudent]') AND type in (N'P', N'PC'))
                    DROP PROCEDURE [dbo].[sp_CreateStudent];";
                
                using var dropCommand = new SqlCommand(dropProcScript, connection);
                await dropCommand.ExecuteNonQueryAsync();

                string createProcScript = @"
                    CREATE PROCEDURE [dbo].[sp_CreateStudent]
                            @first_name VARCHAR(50),
                            @middle_name VARCHAR(50),
                            @last_name VARCHAR(50),
                            @suffix VARCHAR(10) = NULL,
                            @birthdate DATE,
                            @age INT,
                            @place_of_birth VARCHAR(100) = NULL,
                            @sex VARCHAR(10),
                            @mother_tongue VARCHAR(50) = NULL,
                            @ip_comm BIT = 0,
                            @ip_specify VARCHAR(50) = NULL,
                            @four_ps BIT = 0,
                            @four_ps_hseID VARCHAR(50) = NULL,
                            @hse_no VARCHAR(20) = NULL,
                            @street VARCHAR(100) = NULL,
                            @brngy VARCHAR(50) = NULL,
                            @province VARCHAR(50) = NULL,
                            @city VARCHAR(50) = NULL,
                            @country VARCHAR(50) = NULL,
                            @zip_code VARCHAR(10) = NULL,
                            @phse_no VARCHAR(20) = NULL,
                            @pstreet VARCHAR(100) = NULL,
                            @pbrngy VARCHAR(50) = NULL,
                            @pprovince VARCHAR(50) = NULL,
                            @pcity VARCHAR(50) = NULL,
                            @pcountry VARCHAR(50) = NULL,
                            @pzip_code VARCHAR(10) = NULL,
                            @student_type VARCHAR(20),
                            @LRN VARCHAR(20) = NULL,
                            @school_yr VARCHAR(20) = NULL,
                            @grade_level VARCHAR(10) = NULL,
                            @guardian_id INT,
                            @student_id VARCHAR(6) OUTPUT
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            
                            BEGIN TRY
                                BEGIN TRANSACTION;
                                
                                DECLARE @NewID INT;
                                DECLARE @FormattedID VARCHAR(6);
                                
                                -- Lock the sequence table and get the next ID atomically
                                SELECT @NewID = [LastStudentID]
                                FROM [dbo].[tbl_StudentID_Sequence] WITH (UPDLOCK, HOLDLOCK);
                                
                                -- Check if we've exceeded the 6-digit limit
                                IF @NewID >= 999999
                                BEGIN
                                    ROLLBACK TRANSACTION;
                                    RAISERROR('Student ID limit reached (999999). Cannot generate more student IDs.', 16, 1);
                                    RETURN;
                                END;
                                
                                -- Increment the ID
                                SET @NewID = @NewID + 1;
                                
                                -- Format to 6 digits with leading zeros
                                SET @FormattedID = RIGHT('000000' + CAST(@NewID AS VARCHAR(6)), 6);
                                
                                -- Update the sequence table
                                UPDATE [dbo].[tbl_StudentID_Sequence]
                                SET [LastStudentID] = @NewID;
                                
                                -- Insert the student record with the generated ID
                                INSERT INTO [dbo].[tbl_Students] (
                                    [student_id], [first_name], [middle_name], [last_name], [suffix],
                                    [birthdate], [age], [place_of_birth], [sex], [mother_tongue],
                                    [ip_comm], [ip_specify], [four_ps], [four_ps_hseID],
                                    [hse_no], [street], [brngy], [province], [city], [country], [zip_code],
                                    [phse_no], [pstreet], [pbrngy], [pprovince], [pcity], [pcountry], [pzip_code],
                                    [student_type], [LRN], [school_yr], [grade_level], [guardian_id],
                                    [date_registered], [status]
                                )
                                VALUES (
                                    @FormattedID, @first_name, @middle_name, @last_name, @suffix,
                                    @birthdate, @age, @place_of_birth, @sex, @mother_tongue,
                                    @ip_comm, @ip_specify, @four_ps, @four_ps_hseID,
                                    @hse_no, @street, @brngy, @province, @city, @country, @zip_code,
                                    @phse_no, @pstreet, @pbrngy, @pprovince, @pcity, @pcountry, @pzip_code,
                                    @student_type, @LRN, @school_yr, @grade_level, @guardian_id,
                                    GETDATE(), 'Pending'
                                );
                                
                                -- Set output parameter
                                SET @student_id = @FormattedID;
                                
                                COMMIT TRANSACTION;
                            END TRY
                            BEGIN CATCH
                                IF @@TRANCOUNT > 0
                                    ROLLBACK TRANSACTION;
                                
                                DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
                                DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
                                DECLARE @ErrorState INT = ERROR_STATE();
                                
                                RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
                            END CATCH;
                        END;";

                    using var createCommand = new SqlCommand(createProcScript, connection);
                    await createCommand.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine("Stored procedure [sp_CreateStudent] created successfully.");
                    return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating stored procedures: {ex.Message}");
                return false;
            }
        }

        // Seeds grade levels if they don't exist
        public async Task<bool> SeedGradeLevelsAsync()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = _databaseName;
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();

                // Check if grade levels already exist
                string checkQuery = "SELECT COUNT(*) FROM [dbo].[tbl_GradeLevel]";
                using var checkCommand = new SqlCommand(checkQuery, connection);
                var result = await checkCommand.ExecuteScalarAsync();
                var count = result != null ? (int)result : 0;

                if (count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("Grade levels already exist, skipping seed.");
                    return false;
                }

                // Insert grade levels
                string insertQuery = @"
                    INSERT INTO [dbo].[tbl_GradeLevel] ([grade_level_name]) VALUES
                    ('Pre-School'),
                    ('Kinder'),
                    ('Grade 1'),
                    ('Grade 2'),
                    ('Grade 3'),
                    ('Grade 4'),
                    ('Grade 5'),
                    ('Grade 6');";

                using var insertCommand = new SqlCommand(insertQuery, connection);
                await insertCommand.ExecuteNonQueryAsync();

                System.Diagnostics.Debug.WriteLine("Grade levels seeded successfully.");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error seeding grade levels: {ex.Message}");
                return false;
            }
        }

        // Initializes everything - creates database and all tables
        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                bool dbCreated = await CreateDatabaseIfNotExistsAsync();
                bool tablesCreated = await CreateTablesIfNotExistAsync();
                
                await AddStatusColumnIfNotExistsAsync();
                await AddStudentColumnsIfNotExistAsync();
                await InitializeSequenceTableAsync();
                await CreateViewsIfNotExistAsync();
                await CreateStoredProceduresIfNotExistAsync();
                await SeedGradeLevelsAsync();
                
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

