using Microsoft.Data.SqlClient;
using BrightEnroll_DES.Services.Database.Definitions;

namespace BrightEnroll_DES.Services.Database.Initialization
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
            catch (Exception)
            {
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
                        try
                        {
                            // Create table
                            using var createCommand = new SqlCommand(tableDef.CreateTableScript, connection);
                            await createCommand.ExecuteNonQueryAsync();
                            anyTableCreated = true;
                            System.Diagnostics.Debug.WriteLine($"Successfully created table: {tableKey}");

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
                        catch (Exception tableEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error creating table {tableKey}: {tableEx.Message}");
                            System.Diagnostics.Debug.WriteLine($"SQL Script preview: {tableDef.CreateTableScript.Substring(0, Math.Min(200, tableDef.CreateTableScript.Length))}...");
                            // Continue with other tables even if one fails
                        }
                    }
                }

                return anyTableCreated;
            }
            catch (Exception)
            {
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
                    
                    string updateExistingQuery = @"
                        UPDATE [dbo].[tbl_Users]
                        SET [status] = 'active'
                        WHERE [status] IS NULL OR [status] = ''";

                    using var updateCommand = new SqlCommand(updateExistingQuery, connection);
                    await updateCommand.ExecuteNonQueryAsync();
                    
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
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
                    catch (Exception)
                    {
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
                    catch (Exception)
                    {
                    }
                }

                // Check and add archive_reason column
                string checkArchiveReasonQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('dbo.tbl_Students') 
                    AND name = 'archive_reason'";

                using var checkArchiveCommand = new SqlCommand(checkArchiveReasonQuery, connection);
                var archiveResult = await checkArchiveCommand.ExecuteScalarAsync();
                var archiveColumnExists = archiveResult != null ? (int)archiveResult : 0;

                if (archiveColumnExists == 0)
                {
                    string addArchiveColumnQuery = @"
                        ALTER TABLE [dbo].[tbl_Students]
                        ADD [archive_reason] NVARCHAR(MAX) NULL";

                    using var addArchiveCommand = new SqlCommand(addArchiveColumnQuery, connection);
                    await addArchiveCommand.ExecuteNonQueryAsync();
                    anyColumnAdded = true;
                }

                // Check and add amount_paid column
                string checkAmountPaidQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('dbo.tbl_Students') 
                    AND name = 'amount_paid'";

                using var checkAmountCommand = new SqlCommand(checkAmountPaidQuery, connection);
                var amountResult = await checkAmountCommand.ExecuteScalarAsync();
                var amountColumnExists = amountResult != null ? (int)amountResult : 0;

                if (amountColumnExists == 0)
                {
                    string addAmountColumnQuery = @"
                        ALTER TABLE [dbo].[tbl_Students]
                        ADD [amount_paid] DECIMAL(18,2) NOT NULL DEFAULT 0";

                    using var addAmountCommand = new SqlCommand(addAmountColumnQuery, connection);
                    await addAmountCommand.ExecuteNonQueryAsync();
                    anyColumnAdded = true;
                }

                // Check and add payment_status column
                string checkPaymentStatusQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('dbo.tbl_Students') 
                    AND name = 'payment_status'";

                using var checkPaymentCommand = new SqlCommand(checkPaymentStatusQuery, connection);
                var paymentResult = await checkPaymentCommand.ExecuteScalarAsync();
                var paymentColumnExists = paymentResult != null ? (int)paymentResult : 0;

                if (paymentColumnExists == 0)
                {
                    string addPaymentColumnQuery = @"
                        ALTER TABLE [dbo].[tbl_Students]
                        ADD [payment_status] VARCHAR(20) NULL DEFAULT 'Unpaid'";

                    using var addPaymentCommand = new SqlCommand(addPaymentColumnQuery, connection);
                    await addPaymentCommand.ExecuteNonQueryAsync();
                    anyColumnAdded = true;
                }

                return anyColumnAdded;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Adds is_verified column to tbl_StudentRequirements if it doesn't exist (for existing databases)
        public async Task<bool> AddStudentRequirementsColumnsIfNotExistAsync()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = _databaseName;
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();

                bool anyColumnAdded = false;

                // Check and add is_verified column
                string checkIsVerifiedQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('dbo.tbl_StudentRequirements') 
                    AND name = 'is_verified'";

                using var checkIsVerifiedCommand = new SqlCommand(checkIsVerifiedQuery, connection);
                var isVerifiedResult = await checkIsVerifiedCommand.ExecuteScalarAsync();
                var isVerifiedColumnExists = isVerifiedResult != null ? (int)isVerifiedResult : 0;

                if (isVerifiedColumnExists == 0)
                {
                    string addIsVerifiedColumnQuery = @"
                        ALTER TABLE [dbo].[tbl_StudentRequirements]
                        ADD [is_verified] BIT NOT NULL DEFAULT 0";

                    using var addIsVerifiedCommand = new SqlCommand(addIsVerifiedColumnQuery, connection);
                    await addIsVerifiedCommand.ExecuteNonQueryAsync();
                    anyColumnAdded = true;
                }

                return anyColumnAdded;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Adds SubjectCode and IsActive columns to tbl_Subjects if they don't exist (for existing databases)
        public async Task<bool> AddSubjectColumnsIfNotExistAsync()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = _databaseName;
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();

                bool anyColumnAdded = false;

                // Check and add SubjectCode column
                string checkSubjectCodeQuery = @"
                    SELECT COUNT(*)
                    FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.tbl_Subjects')
                    AND name = 'SubjectCode'";

                using (var checkSubjectCodeCommand = new SqlCommand(checkSubjectCodeQuery, connection))
                {
                    var subjectCodeResult = await checkSubjectCodeCommand.ExecuteScalarAsync();
                    var subjectCodeExists = subjectCodeResult != null ? (int)subjectCodeResult : 0;

                    if (subjectCodeExists == 0)
                    {
                        string addSubjectCodeQuery = @"
                            ALTER TABLE [dbo].[tbl_Subjects]
                            ADD [SubjectCode] VARCHAR(50) NULL";

                        using var addSubjectCodeCommand = new SqlCommand(addSubjectCodeQuery, connection);
                        await addSubjectCodeCommand.ExecuteNonQueryAsync();
                        anyColumnAdded = true;
                    }
                }

                // Check and add IsActive column
                string checkIsActiveQuery = @"
                    SELECT COUNT(*)
                    FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.tbl_Subjects')
                    AND name = 'IsActive'";

                using (var checkIsActiveCommand = new SqlCommand(checkIsActiveQuery, connection))
                {
                    var isActiveResult = await checkIsActiveCommand.ExecuteScalarAsync();
                    var isActiveExists = isActiveResult != null ? (int)isActiveResult : 0;

                    if (isActiveExists == 0)
                    {
                        string addIsActiveQuery = @"
                            ALTER TABLE [dbo].[tbl_Subjects]
                            ADD [IsActive] BIT NOT NULL DEFAULT 1";

                        using var addIsActiveCommand = new SqlCommand(addIsActiveQuery, connection);
                        await addIsActiveCommand.ExecuteNonQueryAsync();
                        anyColumnAdded = true;

                        // Ensure existing rows are marked active
                        string updateExistingQuery = @"
                            UPDATE [dbo].[tbl_Subjects]
                            SET [IsActive] = 1
                            WHERE [IsActive] IS NULL";

                        using var updateExistingCommand = new SqlCommand(updateExistingQuery, connection);
                        await updateExistingCommand.ExecuteNonQueryAsync();
                    }
                }

                // Create indexes if needed
                if (anyColumnAdded)
                {
                    string createIndexesQuery = @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Subjects_SubjectCode' AND object_id = OBJECT_ID('dbo.tbl_Subjects'))
                            CREATE INDEX IX_tbl_Subjects_SubjectCode ON [dbo].[tbl_Subjects]([SubjectCode]);

                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Subjects_IsActive' AND object_id = OBJECT_ID('dbo.tbl_Subjects'))
                            CREATE INDEX IX_tbl_Subjects_IsActive ON [dbo].[tbl_Subjects]([IsActive]);";

                    using var createIndexesCommand = new SqlCommand(createIndexesQuery, connection);
                    await createIndexesCommand.ExecuteNonQueryAsync();
                }

                return anyColumnAdded;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Adds AdviserID column to tbl_Sections if it doesn't exist (for existing databases)
        public async Task<bool> AddSectionAdviserColumnIfNotExistsAsync()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = _databaseName;
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();

                // Check if AdviserID column exists
                string checkColumnQuery = @"
                    SELECT COUNT(*)
                    FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.tbl_Sections')
                    AND name = 'AdviserID'";

                using var checkCommand = new SqlCommand(checkColumnQuery, connection);
                var result = await checkCommand.ExecuteScalarAsync();
                var columnExists = result != null ? (int)result : 0;

                if (columnExists == 0)
                {
                    // Add AdviserID column
                    string addColumnQuery = @"
                        ALTER TABLE [dbo].[tbl_Sections]
                        ADD [AdviserID] INT NULL";

                    using var addCommand = new SqlCommand(addColumnQuery, connection);
                    await addCommand.ExecuteNonQueryAsync();

                    // Add foreign key constraint and index if they don't exist
                    string fkAndIndexQuery = @"
                        IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_tbl_Sections_tbl_Users_Adviser')
                        BEGIN
                            ALTER TABLE [dbo].[tbl_Sections]
                            ADD CONSTRAINT FK_tbl_Sections_tbl_Users_Adviser FOREIGN KEY ([AdviserID]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE SET NULL;
                        END;

                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Sections_AdviserID' AND object_id = OBJECT_ID('dbo.tbl_Sections'))
                        BEGIN
                            CREATE INDEX IX_tbl_Sections_AdviserID ON [dbo].[tbl_Sections]([AdviserID]);
                        END;";

                    using var fkIndexCommand = new SqlCommand(fkAndIndexQuery, connection);
                    await fkIndexCommand.ExecuteNonQueryAsync();

                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Adds IsArchived column to tbl_TeacherSectionAssignment if it doesn't exist (for existing databases)
        public async Task<bool> AddTeacherAssignmentArchiveColumnIfNotExistsAsync()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = _databaseName;
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();

                // Check if IsArchived column exists
                string checkColumnQuery = @"
                    SELECT COUNT(*)
                    FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.tbl_TeacherSectionAssignment')
                    AND name = 'IsArchived'";

                using var checkCommand = new SqlCommand(checkColumnQuery, connection);
                var result = await checkCommand.ExecuteScalarAsync();
                var columnExists = result != null ? (int)result : 0;

                if (columnExists == 0)
                {
                    // Add IsArchived column with default 0
                    string addColumnQuery = @"
                        ALTER TABLE [dbo].[tbl_TeacherSectionAssignment]
                        ADD [IsArchived] BIT NOT NULL DEFAULT 0";

                    using var addCommand = new SqlCommand(addColumnQuery, connection);
                    await addCommand.ExecuteNonQueryAsync();

                    // Ensure existing rows are non-archived
                    string updateExistingQuery = @"
                        UPDATE [dbo].[tbl_TeacherSectionAssignment]
                        SET [IsArchived] = 0
                        WHERE [IsArchived] IS NULL";

                    using var updateCommand = new SqlCommand(updateExistingQuery, connection);
                    await updateCommand.ExecuteNonQueryAsync();

                    // Create index if it doesn't exist
                    string createIndexQuery = @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_TeacherSectionAssignment_IsArchived' AND object_id = OBJECT_ID('dbo.tbl_TeacherSectionAssignment'))
                        BEGIN
                            CREATE INDEX IX_tbl_TeacherSectionAssignment_IsArchived ON [dbo].[tbl_TeacherSectionAssignment]([IsArchived]);
                        END;";

                    using var indexCommand = new SqlCommand(createIndexQuery, connection);
                    await indexCommand.ExecuteNonQueryAsync();

                    return true;
                }

                return false;
            }
            catch (Exception)
            {
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
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
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
                    }
                    catch (Exception createEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating view {viewKey}: {createEx.Message}");
                    }
                }

                return anyViewCreated;
            }
            catch (Exception)
            {
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
                    return true;
            }
            catch (Exception)
            {
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
                    return false;
                }

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

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Ensures tbl_Grades table exists (for existing databases that might not have it)
        public async Task<bool> EnsureGradesTableExistsAsync()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = _databaseName;
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();

                // Check if table exists
                string checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.tables 
                    WHERE name = 'tbl_Grades' 
                    AND schema_id = SCHEMA_ID('dbo')";

                using var checkCommand = new SqlCommand(checkTableQuery, connection);
                var result = await checkCommand.ExecuteScalarAsync();
                var tableExists = result != null ? (int)result : 0;

                if (tableExists == 0)
                {
                    // Get grades table definition and create it
                    var gradesTableDef = TableDefinitions.GetGradesTableDefinition();
                    
                    using var createCommand = new SqlCommand(gradesTableDef.CreateTableScript, connection);
                    await createCommand.ExecuteNonQueryAsync();

                    // Create indexes
                    if (gradesTableDef.CreateIndexesScripts != null)
                    {
                        foreach (var indexScript in gradesTableDef.CreateIndexesScripts)
                        {
                            if (!string.IsNullOrWhiteSpace(indexScript))
                            {
                                using var indexCommand = new SqlCommand(indexScript, connection);
                                await indexCommand.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Ensures tbl_audit_logs table exists (for existing databases that might not have it)
        public async Task<bool> EnsureAuditLogsTableExistsAsync()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = _databaseName;
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();

                // Check if table exists
                string checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.tables 
                    WHERE name = 'tbl_audit_logs' 
                    AND schema_id = SCHEMA_ID('dbo')";

                using var checkCommand = new SqlCommand(checkTableQuery, connection);
                var result = await checkCommand.ExecuteScalarAsync();
                var tableExists = result != null ? (int)result : 0;

                if (tableExists == 0)
                {
                    // Get the audit logs table definition
                    var auditLogsTableDef = TableDefinitions.GetAuditLogsTableDefinition();
                    
                    // Create table
                    using var createCommand = new SqlCommand(auditLogsTableDef.CreateTableScript, connection);
                    await createCommand.ExecuteNonQueryAsync();

                    // Create indexes
                    if (auditLogsTableDef.CreateIndexesScripts.Any())
                    {
                        foreach (var indexScript in auditLogsTableDef.CreateIndexesScripts)
                        {
                            try
                            {
                                using var indexCommand = new SqlCommand(indexScript, connection);
                                await indexCommand.ExecuteNonQueryAsync();
                            }
                            catch (Exception indexEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Warning: Could not create index for tbl_audit_logs: {indexEx.Message}");
                            }
                        }
                    }
                    
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error ensuring audit logs table exists: {ex.Message}");
                return false;
            }
        }

        // Adds threshold_percentage column to tbl_roles if it doesn't exist (for existing databases)
        public async Task<bool> AddThresholdPercentageColumnIfNotExistsAsync()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = _databaseName;
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();

                // First check if table exists
                string checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.tables 
                    WHERE name = 'tbl_roles' AND schema_id = SCHEMA_ID('dbo')";

                using var checkTableCommand = new SqlCommand(checkTableQuery, connection);
                var tableResult = await checkTableCommand.ExecuteScalarAsync();
                var tableExists = tableResult != null ? (int)tableResult : 0;

                if (tableExists == 0)
                {
                    // Table doesn't exist, it will be created by CreateTablesIfNotExistAsync with the column
                    return false;
                }

                // Table exists, check if column exists
                string checkColumnQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('dbo.tbl_roles') 
                    AND name = 'threshold_percentage'";

                using var checkCommand = new SqlCommand(checkColumnQuery, connection);
                var result = await checkCommand.ExecuteScalarAsync();
                var columnExists = result != null ? (int)result : 0;

                if (columnExists == 0)
                {
                    string addColumnQuery = @"
                        ALTER TABLE [dbo].[tbl_roles]
                        ADD [threshold_percentage] DECIMAL(5,2) NOT NULL DEFAULT 10.00";

                    using var addCommand = new SqlCommand(addColumnQuery, connection);
                    await addCommand.ExecuteNonQueryAsync();
                    
                    System.Diagnostics.Debug.WriteLine("Added threshold_percentage column to tbl_roles table.");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding threshold_percentage column: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AddBatchTimestampColumnIfNotExistsAsync()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = _databaseName;
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();

                // First check if table exists
                string checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.tables 
                    WHERE name = 'tbl_payroll_transactions' AND schema_id = SCHEMA_ID('dbo')";

                using var checkTableCommand = new SqlCommand(checkTableQuery, connection);
                var tableResult = await checkTableCommand.ExecuteScalarAsync();
                var tableExists = tableResult != null ? (int)tableResult : 0;

                if (tableExists == 0)
                {
                    // Table doesn't exist, it will be created by CreateTablesIfNotExistAsync with the column
                    return false;
                }

                // Table exists, check if column exists
                string checkColumnQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('dbo.tbl_payroll_transactions') 
                    AND name = 'batch_timestamp'";

                using var checkCommand = new SqlCommand(checkColumnQuery, connection);
                var result = await checkCommand.ExecuteScalarAsync();
                var columnExists = result != null ? (int)result : 0;

                if (columnExists == 0)
                {
                    // Add the column
                    string addColumnQuery = @"
                        ALTER TABLE [dbo].[tbl_payroll_transactions]
                        ADD [batch_timestamp] DATETIME NULL";

                    using var addCommand = new SqlCommand(addColumnQuery, connection);
                    await addCommand.ExecuteNonQueryAsync();
                    
                    // Create index for batch_timestamp
                    string createIndexQuery = @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_payroll_transactions_batch_timestamp' AND object_id = OBJECT_ID('dbo.tbl_payroll_transactions'))
                        CREATE INDEX IX_tbl_payroll_transactions_batch_timestamp ON [dbo].[tbl_payroll_transactions]([batch_timestamp])";

                    using var indexCommand = new SqlCommand(createIndexQuery, connection);
                    await indexCommand.ExecuteNonQueryAsync();
                    
                    System.Diagnostics.Debug.WriteLine("Added batch_timestamp column and index to tbl_payroll_transactions table.");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding batch_timestamp column: {ex.Message}");
                return false;
            }
        }

        // Adds effective_date column to tbl_salary_change_requests if it doesn't exist (for existing databases)
        public async Task<bool> AddEffectiveDateColumnIfNotExistsAsync()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = _databaseName;
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();

                // First check if table exists
                string checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.tables 
                    WHERE name = 'tbl_salary_change_requests' AND schema_id = SCHEMA_ID('dbo')";

                using var checkTableCommand = new SqlCommand(checkTableQuery, connection);
                var tableResult = await checkTableCommand.ExecuteScalarAsync();
                var tableExists = tableResult != null ? (int)tableResult : 0;

                if (tableExists == 0)
                {
                    // Table doesn't exist, it will be created by CreateTablesIfNotExistAsync with the column
                    return false;
                }

                // Table exists, check if column exists
                string checkColumnQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('dbo.tbl_salary_change_requests') 
                    AND name = 'effective_date'";

                using var checkCommand = new SqlCommand(checkColumnQuery, connection);
                var result = await checkCommand.ExecuteScalarAsync();
                var columnExists = result != null ? (int)result : 0;

                if (columnExists == 0)
                {
                    string addColumnQuery = @"
                        ALTER TABLE [dbo].[tbl_salary_change_requests]
                        ADD [effective_date] DATE NULL";

                    using var addCommand = new SqlCommand(addColumnQuery, connection);
                    await addCommand.ExecuteNonQueryAsync();
                    
                    System.Diagnostics.Debug.WriteLine("Added effective_date column to tbl_salary_change_requests table.");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding effective_date column: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AddPayrollAuditTrailAndCompanyContributionsAsync()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = _databaseName;
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();

                // Check if table exists
                string checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.tables 
                    WHERE name = 'tbl_payroll_transactions' AND schema_id = SCHEMA_ID('dbo')";

                using var checkTableCommand = new SqlCommand(checkTableQuery, connection);
                var tableResult = await checkTableCommand.ExecuteScalarAsync();
                var tableExists = tableResult != null ? (int)tableResult : 0;

                if (tableExists == 0)
                {
                    return false;
                }

                bool anyAdded = false;

                // Add company contribution columns
                string[] companyColumns = new[]
                {
                    "company_sss_contribution", "company_philhealth_contribution", 
                    "company_pagibig_contribution", "total_company_contribution"
                };

                foreach (var colName in companyColumns)
                {
                    string checkColQuery = $@"
                        SELECT COUNT(*) 
                        FROM sys.columns 
                        WHERE object_id = OBJECT_ID('dbo.tbl_payroll_transactions') 
                        AND name = '{colName}'";

                    using var checkColCommand = new SqlCommand(checkColQuery, connection);
                    var colResult = await checkColCommand.ExecuteScalarAsync();
                    var colExists = colResult != null ? (int)colResult : 0;

                    if (colExists == 0)
                    {
                        string addColQuery = $@"
                            ALTER TABLE [dbo].[tbl_payroll_transactions]
                            ADD [{colName}] DECIMAL(12,2) NOT NULL DEFAULT 0.00";

                        using var addCommand = new SqlCommand(addColQuery, connection);
                        await addCommand.ExecuteNonQueryAsync();
                        anyAdded = true;
                    }
                }

                // Add created_by column
                string checkCreatedByQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('dbo.tbl_payroll_transactions') 
                    AND name = 'created_by'";

                using var checkCreatedByCommand = new SqlCommand(checkCreatedByQuery, connection);
                var createdByResult = await checkCreatedByCommand.ExecuteScalarAsync();
                var createdByExists = createdByResult != null ? (int)createdByResult : 0;

                if (createdByExists == 0)
                {
                    string addCreatedByQuery = @"
                        ALTER TABLE [dbo].[tbl_payroll_transactions]
                        ADD [created_by] INT NOT NULL DEFAULT 1";

                    using var addCreatedByCommand = new SqlCommand(addCreatedByQuery, connection);
                    await addCreatedByCommand.ExecuteNonQueryAsync();

                    // Update existing records
                    string updateCreatedByQuery = @"
                        UPDATE [dbo].[tbl_payroll_transactions]
                        SET [created_by] = [processed_by]
                        WHERE [created_by] = 1";

                    using var updateCommand = new SqlCommand(updateCreatedByQuery, connection);
                    await updateCommand.ExecuteNonQueryAsync();

                    // Add foreign key if it doesn't exist
                    string checkFkQuery = @"
                        SELECT COUNT(*) 
                        FROM sys.foreign_keys 
                        WHERE name = 'FK_tbl_payroll_transactions_CreatedBy'";

                    using var checkFkCommand = new SqlCommand(checkFkQuery, connection);
                    var fkExists = await checkFkCommand.ExecuteScalarAsync();
                    if (fkExists != null && (int)fkExists == 0)
                    {
                        string addFkQuery = @"
                            ALTER TABLE [dbo].[tbl_payroll_transactions]
                            ADD CONSTRAINT FK_tbl_payroll_transactions_CreatedBy 
                            FOREIGN KEY ([created_by]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION";

                        using var addFkCommand = new SqlCommand(addFkQuery, connection);
                        await addFkCommand.ExecuteNonQueryAsync();
                    }

                    anyAdded = true;
                }

                // Add approved_by, approved_at
                string[] approvalColumns = new[] { "approved_by", "approved_at" };
                foreach (var colName in approvalColumns)
                {
                    string checkColQuery = $@"
                        SELECT COUNT(*) 
                        FROM sys.columns 
                        WHERE object_id = OBJECT_ID('dbo.tbl_payroll_transactions') 
                        AND name = '{colName}'";

                    using var checkColCommand = new SqlCommand(checkColQuery, connection);
                    var colResult = await checkColCommand.ExecuteScalarAsync();
                    var colExists = colResult != null ? (int)colResult : 0;

                    if (colExists == 0)
                    {
                        string addColQuery = colName == "approved_by"
                            ? @"ALTER TABLE [dbo].[tbl_payroll_transactions] ADD [approved_by] INT NULL"
                            : @"ALTER TABLE [dbo].[tbl_payroll_transactions] ADD [approved_at] DATETIME NULL";

                        using var addCommand = new SqlCommand(addColQuery, connection);
                        await addCommand.ExecuteNonQueryAsync();

                        if (colName == "approved_by")
                        {
                            // Add foreign key
                            string checkFkQuery = @"
                                SELECT COUNT(*) 
                                FROM sys.foreign_keys 
                                WHERE name = 'FK_tbl_payroll_transactions_ApprovedBy'";

                            using var checkFkCommand = new SqlCommand(checkFkQuery, connection);
                            var fkExists = await checkFkCommand.ExecuteScalarAsync();
                            if (fkExists != null && (int)fkExists == 0)
                            {
                                string addFkQuery = @"
                                    ALTER TABLE [dbo].[tbl_payroll_transactions]
                                    ADD CONSTRAINT FK_tbl_payroll_transactions_ApprovedBy 
                                    FOREIGN KEY ([approved_by]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION";

                                using var addFkCommand = new SqlCommand(addFkQuery, connection);
                                await addFkCommand.ExecuteNonQueryAsync();
                            }
                        }

                        anyAdded = true;
                    }
                }

                // Add cancelled_by, cancelled_at, cancellation_reason
                string[] cancelColumns = new[] { "cancelled_by", "cancelled_at", "cancellation_reason" };
                foreach (var colName in cancelColumns)
                {
                    string checkColQuery = $@"
                        SELECT COUNT(*) 
                        FROM sys.columns 
                        WHERE object_id = OBJECT_ID('dbo.tbl_payroll_transactions') 
                        AND name = '{colName}'";

                    using var checkColCommand = new SqlCommand(checkColQuery, connection);
                    var colResult = await checkColCommand.ExecuteScalarAsync();
                    var colExists = colResult != null ? (int)colResult : 0;

                    if (colExists == 0)
                    {
                        string addColQuery = colName switch
                        {
                            "cancelled_by" => @"ALTER TABLE [dbo].[tbl_payroll_transactions] ADD [cancelled_by] INT NULL",
                            "cancelled_at" => @"ALTER TABLE [dbo].[tbl_payroll_transactions] ADD [cancelled_at] DATETIME NULL",
                            "cancellation_reason" => @"ALTER TABLE [dbo].[tbl_payroll_transactions] ADD [cancellation_reason] NVARCHAR(500) NULL",
                            _ => ""
                        };

                        if (!string.IsNullOrEmpty(addColQuery))
                        {
                            using var addCommand = new SqlCommand(addColQuery, connection);
                            await addCommand.ExecuteNonQueryAsync();

                            if (colName == "cancelled_by")
                            {
                                // Add foreign key
                                string checkFkQuery = @"
                                    SELECT COUNT(*) 
                                    FROM sys.foreign_keys 
                                    WHERE name = 'FK_tbl_payroll_transactions_CancelledBy'";

                                using var checkFkCommand = new SqlCommand(checkFkQuery, connection);
                                var fkExists = await checkFkCommand.ExecuteScalarAsync();
                                if (fkExists != null && (int)fkExists == 0)
                                {
                                    string addFkQuery = @"
                                        ALTER TABLE [dbo].[tbl_payroll_transactions]
                                        ADD CONSTRAINT FK_tbl_payroll_transactions_CancelledBy 
                                        FOREIGN KEY ([cancelled_by]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION";

                                    using var addFkCommand = new SqlCommand(addFkQuery, connection);
                                    await addFkCommand.ExecuteNonQueryAsync();
                                }
                            }

                            anyAdded = true;
                        }
                    }
                }

                if (anyAdded)
                {
                    System.Diagnostics.Debug.WriteLine("Added audit trail and company contribution columns to tbl_payroll_transactions table.");
                }

                return anyAdded;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding audit trail and company contribution columns: {ex.Message}");
                return false;
            }
        }

        // Adds discount_id column to tbl_LedgerCharges and ensures tbl_discounts table exists
        public async Task<bool> AddDiscountIdColumnIfNotExistsAsync()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                builder.InitialCatalog = _databaseName;
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();

                // First, ensure tbl_discounts table exists (it should be created by CreateTablesIfNotExistAsync, but verify)
                string checkDiscountsTableQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.tables 
                    WHERE name = 'tbl_discounts' AND schema_id = SCHEMA_ID('dbo')";

                using var checkDiscountsTableCommand = new SqlCommand(checkDiscountsTableQuery, connection);
                var discountsTableResult = await checkDiscountsTableCommand.ExecuteScalarAsync();
                var discountsTableExists = discountsTableResult != null ? (int)discountsTableResult : 0;

                TableDefinition? discountsTableDef = null;
                if (discountsTableExists == 0)
                {
                    // Create tbl_discounts table if it doesn't exist
                    var tableDefinitions = TableDefinitions.GetAllTableDefinitions();
                    discountsTableDef = tableDefinitions.FirstOrDefault(t => t.TableName == "tbl_discounts");
                    
                    if (discountsTableDef != null)
                    {
                        using var createDiscountsCommand = new SqlCommand(discountsTableDef.CreateTableScript, connection);
                        await createDiscountsCommand.ExecuteNonQueryAsync();
                        
                        // Create indexes
                        foreach (var indexScript in discountsTableDef.CreateIndexesScripts)
                        {
                            if (!string.IsNullOrWhiteSpace(indexScript))
                            {
                                using var indexCommand = new SqlCommand(indexScript, connection);
                                await indexCommand.ExecuteNonQueryAsync();
                            }
                        }
                        
                        System.Diagnostics.Debug.WriteLine("Created tbl_discounts table.");
                        discountsTableExists = 1; // Mark as existing now
                    }
                }

                // Now check if tbl_LedgerCharges exists
                string checkLedgerChargesTableQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.tables 
                    WHERE name = 'tbl_LedgerCharges' AND schema_id = SCHEMA_ID('dbo')";

                using var checkLedgerChargesTableCommand = new SqlCommand(checkLedgerChargesTableQuery, connection);
                var ledgerChargesTableResult = await checkLedgerChargesTableCommand.ExecuteScalarAsync();
                var ledgerChargesTableExists = ledgerChargesTableResult != null ? (int)ledgerChargesTableResult : 0;

                if (ledgerChargesTableExists == 0)
                {
                    // Table doesn't exist, it will be created by CreateTablesIfNotExistAsync with the column
                    return false;
                }

                // Table exists, check if discount_id column exists
                string checkColumnQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('dbo.tbl_LedgerCharges') 
                    AND name = 'discount_id'";

                using var checkCommand = new SqlCommand(checkColumnQuery, connection);
                var result = await checkCommand.ExecuteScalarAsync();
                var columnExists = result != null ? (int)result : 0;

                if (columnExists == 0)
                {
                    // Add the column
                    string addColumnQuery = @"
                        ALTER TABLE [dbo].[tbl_LedgerCharges]
                        ADD [discount_id] INT NULL";

                    using var addCommand = new SqlCommand(addColumnQuery, connection);
                    await addCommand.ExecuteNonQueryAsync();
                    
                    // Add foreign key constraint if tbl_discounts exists
                    if (discountsTableExists > 0)
                    {
                        // Check if foreign key already exists
                        string checkFkQuery = @"
                            SELECT COUNT(*) 
                            FROM sys.foreign_keys 
                            WHERE name = 'FK_tbl_LedgerCharges_tbl_discounts'";

                        using var checkFkCommand = new SqlCommand(checkFkQuery, connection);
                        var fkExists = await checkFkCommand.ExecuteScalarAsync();
                        if (fkExists != null && (int)fkExists == 0)
                        {
                            string addFkQuery = @"
                                ALTER TABLE [dbo].[tbl_LedgerCharges]
                                ADD CONSTRAINT FK_tbl_LedgerCharges_tbl_discounts 
                                FOREIGN KEY ([discount_id]) REFERENCES [dbo].[tbl_discounts]([discount_id]) ON DELETE SET NULL";

                            using var addFkCommand = new SqlCommand(addFkQuery, connection);
                            await addFkCommand.ExecuteNonQueryAsync();
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine("Added discount_id column to tbl_LedgerCharges table.");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding discount_id column: {ex.Message}");
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
                await AddStudentRequirementsColumnsIfNotExistAsync();
                await AddSubjectColumnsIfNotExistAsync();
                await AddSectionAdviserColumnIfNotExistsAsync();
                await AddTeacherAssignmentArchiveColumnIfNotExistsAsync();
                await AddThresholdPercentageColumnIfNotExistsAsync(); // Add missing column
                await AddBatchTimestampColumnIfNotExistsAsync(); // Add batch_timestamp column for payroll
                await AddPayrollAuditTrailAndCompanyContributionsAsync(); // Add audit trail and company contributions
                await AddEffectiveDateColumnIfNotExistsAsync(); // Add effective_date column to salary change requests
                await AddDiscountIdColumnIfNotExistsAsync(); // Add discount_id column to tbl_LedgerCharges and ensure tbl_discounts exists
                await InitializeSequenceTableAsync();
                await CreateViewsIfNotExistAsync();
                await CreateStoredProceduresIfNotExistAsync();
                await SeedGradeLevelsAsync();
                
                // Explicitly ensure audit logs table exists (for existing databases)
                await EnsureAuditLogsTableExistsAsync();
                
                return dbCreated || tablesCreated;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

