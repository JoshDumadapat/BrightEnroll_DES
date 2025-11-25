namespace BrightEnroll_DES.Services.DBConnections
{
    /// <summary>
    /// Centralized table definitions for automatic database initialization
    /// Add new table creation scripts here when you create new tables
    /// </summary>
    public static class TableDefinitions
    {
        /// <summary>
        /// Gets all table creation scripts
        /// Add new tables here by creating a new method and adding it to the return list
        /// </summary>
        public static List<TableDefinition> GetAllTableDefinitions()
        {
            return new List<TableDefinition>
            {
                GetUsersTableDefinition(),
                GetEmployeesTableDefinition(),
                // Add new table definitions here:
                // GetYourNewTableDefinition(),
            };
        }

        /// <summary>
        /// Definition for tbl_Users table
        /// </summary>
        public static TableDefinition GetUsersTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_Users",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_Users](
                        [user_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [system_ID] VARCHAR(50) NOT NULL UNIQUE,
                        [first_name] VARCHAR(50) NOT NULL,
                        [mid_name] VARCHAR(50) NULL,
                        [last_name] VARCHAR(50) NOT NULL,
                        [suffix] VARCHAR(10) NULL,
                        [birthdate] DATE NOT NULL,
                        [age] TINYINT NOT NULL,
                        [gender] VARCHAR(20) NOT NULL,
                        [contact_num] VARCHAR(20) NOT NULL,
                        [user_role] VARCHAR(50) NOT NULL,
                        [email] VARCHAR(150) NOT NULL UNIQUE,
                        [password] VARCHAR(255) NOT NULL,
                        [date_hired] DATETIME NOT NULL
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Users_Email' AND object_id = OBJECT_ID('dbo.tbl_Users'))
                        CREATE INDEX IX_tbl_Users_Email ON [dbo].[tbl_Users]([email])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Users_SystemID' AND object_id = OBJECT_ID('dbo.tbl_Users'))
                        CREATE INDEX IX_tbl_Users_SystemID ON [dbo].[tbl_Users]([system_ID])"
                }
            };
        }

        /// <summary>
        /// Definition for tbl_Employees table
        /// </summary>
        public static TableDefinition GetEmployeesTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_Employees",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_Employees](
                        [employee_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [system_ID] VARCHAR(50) NOT NULL UNIQUE,
                        [first_name] VARCHAR(50) NOT NULL,
                        [mid_name] VARCHAR(50) NULL,
                        [last_name] VARCHAR(50) NOT NULL,
                        [suffix] VARCHAR(10) NULL,
                        [birthdate] DATE NOT NULL,
                        [age] TINYINT NOT NULL,
                        [gender] VARCHAR(20) NOT NULL,
                        [contact_num] VARCHAR(20) NOT NULL,
                        [email] VARCHAR(150) NOT NULL UNIQUE,
                        [role] VARCHAR(50) NOT NULL,
                        [status] VARCHAR(20) NOT NULL DEFAULT 'Active',
                        [inactive_reason] VARCHAR(MAX) NULL,
                        [date_hired] DATETIME NOT NULL,
                        [house_no] VARCHAR(50) NULL,
                        [street_name] VARCHAR(100) NULL,
                        [barangay] VARCHAR(100) NULL,
                        [city] VARCHAR(100) NULL,
                        [province] VARCHAR(100) NULL,
                        [country] VARCHAR(100) NULL DEFAULT 'Philippines',
                        [zip_code] VARCHAR(10) NULL,
                        [emergency_contact_first_name] VARCHAR(50) NULL,
                        [emergency_contact_mid_name] VARCHAR(50) NULL,
                        [emergency_contact_last_name] VARCHAR(50) NULL,
                        [emergency_contact_suffix] VARCHAR(10) NULL,
                        [emergency_contact_relationship] VARCHAR(50) NULL,
                        [emergency_contact_number] VARCHAR(20) NULL,
                        [emergency_contact_address] VARCHAR(500) NULL,
                        [base_salary] DECIMAL(18,2) NULL,
                        [allowance] DECIMAL(18,2) NULL,
                        [bonus] DECIMAL(18,2) NULL,
                        [deductions] DECIMAL(18,2) NULL,
                        [total_salary] DECIMAL(18,2) NULL
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Employees_Email' AND object_id = OBJECT_ID('dbo.tbl_Employees'))
                        CREATE INDEX IX_tbl_Employees_Email ON [dbo].[tbl_Employees]([email])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Employees_SystemID' AND object_id = OBJECT_ID('dbo.tbl_Employees'))
                        CREATE INDEX IX_tbl_Employees_SystemID ON [dbo].[tbl_Employees]([system_ID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Employees_Status' AND object_id = OBJECT_ID('dbo.tbl_Employees'))
                        CREATE INDEX IX_tbl_Employees_Status ON [dbo].[tbl_Employees]([status])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Employees_Role' AND object_id = OBJECT_ID('dbo.tbl_Employees'))
                        CREATE INDEX IX_tbl_Employees_Role ON [dbo].[tbl_Employees]([role])"
                }
            };
        }

        // ============================================
        // ADD NEW TABLE DEFINITIONS BELOW
        // ============================================
        
        // Example template for adding a new table:
        /*
        public static TableDefinition GetYourNewTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_YourTableName",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_YourTableName](
                        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [column1] VARCHAR(50) NOT NULL,
                        [column2] INT NULL,
                        [created_date] DATETIME NOT NULL DEFAULT GETDATE()
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_YourTableName_Column1' AND object_id = OBJECT_ID('dbo.tbl_YourTableName'))
                        CREATE INDEX IX_tbl_YourTableName_Column1 ON [dbo].[tbl_YourTableName]([column1])"
                }
            };
        }
        */
    }

    /// <summary>
    /// Represents a table definition for automatic creation
    /// </summary>
    public class TableDefinition
    {
        public string TableName { get; set; } = string.Empty;
        public string SchemaName { get; set; } = "dbo";
        public string CreateTableScript { get; set; } = string.Empty;
        public List<string> CreateIndexesScripts { get; set; } = new List<string>();
    }
}

