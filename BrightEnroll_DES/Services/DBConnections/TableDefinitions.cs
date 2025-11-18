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
                // Student registration tables (order matters: guardians first, then students, then requirements)
                GetGuardiansTableDefinition(),
                GetStudentsTableDefinition(),
                GetStudentRequirementsTableDefinition(),
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

        // ============================================
        // ADD NEW TABLE DEFINITIONS BELOW
        // ============================================

        /// <summary>
        /// Definition for guardians_tbl table
        /// Must be created before students_tbl due to foreign key relationship
        /// </summary>
        public static TableDefinition GetGuardiansTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "guardians_tbl",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[guardians_tbl](
                        [guardian_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [first_name] VARCHAR(50) NOT NULL,
                        [middle_name] VARCHAR(50) NULL,
                        [last_name] VARCHAR(50) NOT NULL,
                        [suffix] VARCHAR(10) NULL,
                        [contact_num] VARCHAR(20) NULL,
                        [relationship] VARCHAR(50) NULL
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_guardians_tbl_LastName' AND object_id = OBJECT_ID('dbo.guardians_tbl'))
                        CREATE INDEX IX_guardians_tbl_LastName ON [dbo].[guardians_tbl]([last_name])"
                }
            };
        }

        /// <summary>
        /// Definition for students_tbl table
        /// Must be created after guardians_tbl due to foreign key relationship
        /// </summary>
        public static TableDefinition GetStudentsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "students_tbl",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[students_tbl](
                        [student_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [first_name] VARCHAR(50) NOT NULL,
                        [middle_name] VARCHAR(50) NOT NULL,
                        [last_name] VARCHAR(50) NOT NULL,
                        [suffix] VARCHAR(10) NULL,
                        [birthdate] DATE NOT NULL,
                        [age] INT NOT NULL,
                        [place_of_birth] VARCHAR(100) NULL,
                        [sex] VARCHAR(10) NOT NULL,
                        [mother_tongue] VARCHAR(50) NULL,
                        [ip_comm] BIT DEFAULT 0,
                        [ip_specify] VARCHAR(50) NULL,
                        [four_ps] BIT DEFAULT 0,
                        [four_ps_hseID] VARCHAR(50) NULL,
                        [hse_no] VARCHAR(20) NULL,
                        [street] VARCHAR(100) NULL,
                        [brngy] VARCHAR(50) NULL,
                        [province] VARCHAR(50) NULL,
                        [city] VARCHAR(50) NULL,
                        [country] VARCHAR(50) NULL,
                        [zip_code] VARCHAR(10) NULL,
                        [phse_no] VARCHAR(20) NULL,
                        [pstreet] VARCHAR(100) NULL,
                        [pbrngy] VARCHAR(50) NULL,
                        [pprovince] VARCHAR(50) NULL,
                        [pcity] VARCHAR(50) NULL,
                        [pcountry] VARCHAR(50) NULL,
                        [pzip_code] VARCHAR(10) NULL,
                        [student_type] VARCHAR(20) NOT NULL,
                        [LRN] VARCHAR(20) NULL,
                        [school_yr] VARCHAR(20) NULL,
                        [grade_level] VARCHAR(10) NULL,
                        [guardian_id] INT NOT NULL,
                        CONSTRAINT FK_students_guardians FOREIGN KEY ([guardian_id]) REFERENCES [dbo].[guardians_tbl]([guardian_id])
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_students_tbl_GuardianId' AND object_id = OBJECT_ID('dbo.students_tbl'))
                        CREATE INDEX IX_students_tbl_GuardianId ON [dbo].[students_tbl]([guardian_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_students_tbl_LastName' AND object_id = OBJECT_ID('dbo.students_tbl'))
                        CREATE INDEX IX_students_tbl_LastName ON [dbo].[students_tbl]([last_name])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_students_tbl_StudentType' AND object_id = OBJECT_ID('dbo.students_tbl'))
                        CREATE INDEX IX_students_tbl_StudentType ON [dbo].[students_tbl]([student_type])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_students_tbl_LRN' AND object_id = OBJECT_ID('dbo.students_tbl'))
                        CREATE INDEX IX_students_tbl_LRN ON [dbo].[students_tbl]([LRN]) WHERE [LRN] IS NOT NULL"
                }
            };
        }

        /// <summary>
        /// Definition for student_requirements_tbl table
        /// Must be created after students_tbl due to foreign key relationship
        /// </summary>
        public static TableDefinition GetStudentRequirementsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "student_requirements_tbl",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[student_requirements_tbl](
                        [requirement_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [student_id] INT NOT NULL,
                        [requirement_name] VARCHAR(100) NOT NULL,
                        [status] VARCHAR(20) DEFAULT 'not submitted',
                        [requirement_type] VARCHAR(20) NOT NULL,
                        CONSTRAINT FK_student_requirements_students FOREIGN KEY ([student_id]) REFERENCES [dbo].[students_tbl]([student_id])
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_student_requirements_tbl_StudentId' AND object_id = OBJECT_ID('dbo.student_requirements_tbl'))
                        CREATE INDEX IX_student_requirements_tbl_StudentId ON [dbo].[student_requirements_tbl]([student_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_student_requirements_tbl_RequirementType' AND object_id = OBJECT_ID('dbo.student_requirements_tbl'))
                        CREATE INDEX IX_student_requirements_tbl_RequirementType ON [dbo].[student_requirements_tbl]([requirement_type])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_student_requirements_tbl_Status' AND object_id = OBJECT_ID('dbo.student_requirements_tbl'))
                        CREATE INDEX IX_student_requirements_tbl_Status ON [dbo].[student_requirements_tbl]([status])"
                }
            };
        }
        
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

