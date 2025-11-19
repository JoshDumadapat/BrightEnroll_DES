namespace BrightEnroll_DES.Services.DBConnections
{
    // Holds all table creation scripts - add new tables here when needed
    public static class TableDefinitions
    {
        // Returns all table definitions - add new table methods to this list
        public static List<TableDefinition> GetAllTableDefinitions()
        {
            return new List<TableDefinition>
            {
                GetUsersTableDefinition(),
                // Student registration tables (order matters: guardians first, then sequence, then students, then requirements)
                GetGuardiansTableDefinition(),
                GetStudentIDSequenceTableDefinition(),
                GetStudentsTableDefinition(),
                GetStudentRequirementsTableDefinition(),
                // Employee tables (order matters: users first, then employee tables)
                GetEmployeeAddressTableDefinition(),
                GetEmployeeEmergencyContactTableDefinition(),
                GetSalaryInfoTableDefinition(),
                // Finance tables (order matters: grade level first, then fees, then breakdown)
                GetGradeLevelTableDefinition(),
                GetFeesTableDefinition(),
                GetFeeBreakdownTableDefinition(),
                // User status logging table (must be after tbl_Users)
                GetUserStatusLogsTableDefinition(),
            };
        }

        // Creates tbl_Users table
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
                        [date_hired] DATETIME NOT NULL,
                        [status] VARCHAR(20) NOT NULL DEFAULT 'active'
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

        // Creates tbl_Guardians table - must be created before tbl_Students (foreign key dependency)
        public static TableDefinition GetGuardiansTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_Guardians",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_Guardians](
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
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Guardians_LastName' AND object_id = OBJECT_ID('dbo.tbl_Guardians'))
                        CREATE INDEX IX_tbl_Guardians_LastName ON [dbo].[tbl_Guardians]([last_name])"
                }
            };
        }

        // Creates tbl_StudentID_Sequence table - stores the last used student ID for auto-generation
        public static TableDefinition GetStudentIDSequenceTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_StudentID_Sequence",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_StudentID_Sequence](
                        [sequence_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [LastStudentID] INT NOT NULL DEFAULT 158021
                    );
                    -- Insert initial sequence value
                    INSERT INTO [dbo].[tbl_StudentID_Sequence] ([LastStudentID]) VALUES (158021);",
                CreateIndexesScripts = new List<string>()
            };
        }

        // Creates tbl_Students table - must be created after tbl_Guardians and tbl_StudentID_Sequence (foreign key dependency)
        public static TableDefinition GetStudentsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_Students",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_Students](
                        [student_id] VARCHAR(6) NOT NULL PRIMARY KEY,
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
                        [date_registered] DATETIME NOT NULL DEFAULT GETDATE(),
                        [status] VARCHAR(20) NOT NULL DEFAULT 'Pending',
                        CONSTRAINT FK_tbl_Students_tbl_Guardians FOREIGN KEY ([guardian_id]) REFERENCES [dbo].[tbl_Guardians]([guardian_id])
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Students_GuardianId' AND object_id = OBJECT_ID('dbo.tbl_Students'))
                        CREATE INDEX IX_tbl_Students_GuardianId ON [dbo].[tbl_Students]([guardian_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Students_LastName' AND object_id = OBJECT_ID('dbo.tbl_Students'))
                        CREATE INDEX IX_tbl_Students_LastName ON [dbo].[tbl_Students]([last_name])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Students_StudentType' AND object_id = OBJECT_ID('dbo.tbl_Students'))
                        CREATE INDEX IX_tbl_Students_StudentType ON [dbo].[tbl_Students]([student_type])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Students_LRN' AND object_id = OBJECT_ID('dbo.tbl_Students'))
                        CREATE INDEX IX_tbl_Students_LRN ON [dbo].[tbl_Students]([LRN]) WHERE [LRN] IS NOT NULL",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Students_DateRegistered' AND object_id = OBJECT_ID('dbo.tbl_Students'))
                        CREATE INDEX IX_tbl_Students_DateRegistered ON [dbo].[tbl_Students]([date_registered])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Students_Status' AND object_id = OBJECT_ID('dbo.tbl_Students'))
                        CREATE INDEX IX_tbl_Students_Status ON [dbo].[tbl_Students]([status])"
                }
            };
        }

        // Creates tbl_StudentRequirements table - must be created after tbl_Students (foreign key dependency)
        public static TableDefinition GetStudentRequirementsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_StudentRequirements",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_StudentRequirements](
                        [requirement_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [student_id] VARCHAR(6) NOT NULL,
                        [requirement_name] VARCHAR(100) NOT NULL,
                        [status] VARCHAR(20) DEFAULT 'not submitted',
                        [requirement_type] VARCHAR(20) NOT NULL,
                        CONSTRAINT FK_tbl_StudentRequirements_tbl_Students FOREIGN KEY ([student_id]) REFERENCES [dbo].[tbl_Students]([student_id])
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentRequirements_StudentId' AND object_id = OBJECT_ID('dbo.tbl_StudentRequirements'))
                        CREATE INDEX IX_tbl_StudentRequirements_StudentId ON [dbo].[tbl_StudentRequirements]([student_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentRequirements_RequirementType' AND object_id = OBJECT_ID('dbo.tbl_StudentRequirements'))
                        CREATE INDEX IX_tbl_StudentRequirements_RequirementType ON [dbo].[tbl_StudentRequirements]([requirement_type])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentRequirements_Status' AND object_id = OBJECT_ID('dbo.tbl_StudentRequirements'))
                        CREATE INDEX IX_tbl_StudentRequirements_Status ON [dbo].[tbl_StudentRequirements]([status])"
                }
            };
        }

        // Creates tbl_employee_address table - must be created after tbl_Users (foreign key dependency)
        public static TableDefinition GetEmployeeAddressTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_employee_address",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_employee_address](
                        [address_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [user_ID] INT NOT NULL,
                        [house_no] VARCHAR(50) NULL,
                        [street_name] VARCHAR(150) NULL,
                        [province] VARCHAR(100) NULL,
                        [city] VARCHAR(100) NULL,
                        [barangay] VARCHAR(150) NULL,
                        [country] VARCHAR(100) NULL,
                        [zip_code] VARCHAR(10) NULL,
                        CONSTRAINT FK_tbl_employee_address_tbl_Users FOREIGN KEY ([user_ID]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE CASCADE
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_employee_address_user_ID' AND object_id = OBJECT_ID('dbo.tbl_employee_address'))
                        CREATE INDEX IX_tbl_employee_address_user_ID ON [dbo].[tbl_employee_address]([user_ID])"
                }
            };
        }

        // Creates tbl_employee_emergency_contact table - must be created after tbl_Users (foreign key dependency)
        public static TableDefinition GetEmployeeEmergencyContactTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_employee_emergency_contact",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_employee_emergency_contact](
                        [emergency_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [user_ID] INT NOT NULL,
                        [first_name] VARCHAR(50) NOT NULL,
                        [mid_name] VARCHAR(50) NULL,
                        [last_name] VARCHAR(50) NOT NULL,
                        [suffix] VARCHAR(10) NULL,
                        [relationship] VARCHAR(50) NULL,
                        [contact_number] VARCHAR(20) NULL,
                        [address] VARCHAR(255) NULL,
                        CONSTRAINT FK_tbl_employee_emergency_contact_tbl_Users FOREIGN KEY ([user_ID]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE CASCADE
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_employee_emergency_contact_user_ID' AND object_id = OBJECT_ID('dbo.tbl_employee_emergency_contact'))
                        CREATE INDEX IX_tbl_employee_emergency_contact_user_ID ON [dbo].[tbl_employee_emergency_contact]([user_ID])"
                }
            };
        }

        // Creates tbl_salary_info table - must be created after tbl_Users (foreign key dependency)
        public static TableDefinition GetSalaryInfoTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_salary_info",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_salary_info](
                        [salary_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [user_ID] INT NOT NULL,
                        [base_salary] DECIMAL(12,2) NOT NULL,
                        [allowance] DECIMAL(12,2) DEFAULT 0.00,
                        [date_effective] DATE DEFAULT GETDATE(),
                        [is_active] BIT DEFAULT 1,
                        CONSTRAINT FK_tbl_salary_info_tbl_Users FOREIGN KEY ([user_ID]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE CASCADE
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_salary_info_user_ID' AND object_id = OBJECT_ID('dbo.tbl_salary_info'))
                        CREATE INDEX IX_tbl_salary_info_user_ID ON [dbo].[tbl_salary_info]([user_ID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_salary_info_is_active' AND object_id = OBJECT_ID('dbo.tbl_salary_info'))
                        CREATE INDEX IX_tbl_salary_info_is_active ON [dbo].[tbl_salary_info]([is_active])"
                }
            };
        }
        
        // Creates tbl_GradeLevel table - must be created before tbl_Fees (foreign key dependency)
        public static TableDefinition GetGradeLevelTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_GradeLevel",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_GradeLevel](
                        [gradelevel_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [grade_level_name] VARCHAR(50) NOT NULL UNIQUE,
                        [is_active] BIT NOT NULL DEFAULT 1
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_GradeLevel_is_active' AND object_id = OBJECT_ID('dbo.tbl_GradeLevel'))
                        CREATE INDEX IX_tbl_GradeLevel_is_active ON [dbo].[tbl_GradeLevel]([is_active])"
                }
            };
        }

        // Creates tbl_Fees table - must be created after tbl_GradeLevel (foreign key dependency)
        public static TableDefinition GetFeesTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_Fees",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_Fees](
                        [fee_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [gradelevel_ID] INT NOT NULL,
                        [tuition_fee] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                        [misc_fee] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                        [other_fee] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                        [total_fee] AS ([tuition_fee] + [misc_fee] + [other_fee]) PERSISTED,
                        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
                        [updated_date] DATETIME NULL,
                        [created_by] VARCHAR(50) NULL,
                        [updated_by] VARCHAR(50) NULL,
                        [is_active] BIT NOT NULL DEFAULT 1,
                        CONSTRAINT FK_tbl_Fees_GradeLevel FOREIGN KEY ([gradelevel_ID]) REFERENCES [dbo].[tbl_GradeLevel]([gradelevel_ID])
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Fees_gradelevel_ID' AND object_id = OBJECT_ID('dbo.tbl_Fees'))
                        CREATE INDEX IX_tbl_Fees_gradelevel_ID ON [dbo].[tbl_Fees]([gradelevel_ID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Fees_is_active' AND object_id = OBJECT_ID('dbo.tbl_Fees'))
                        CREATE INDEX IX_tbl_Fees_is_active ON [dbo].[tbl_Fees]([is_active])"
                }
            };
        }

        // Creates tbl_FeeBreakdown table - must be created after tbl_Fees (foreign key dependency)
        public static TableDefinition GetFeeBreakdownTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_FeeBreakdown",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_FeeBreakdown](
                        [breakdown_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [fee_ID] INT NOT NULL,
                        [breakdown_type] VARCHAR(20) NOT NULL,
                        [item_name] VARCHAR(200) NOT NULL,
                        [amount] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                        [display_order] INT NOT NULL DEFAULT 0,
                        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
                        [updated_date] DATETIME NULL,
                        CONSTRAINT FK_tbl_FeeBreakdown_tbl_Fees FOREIGN KEY ([fee_ID]) REFERENCES [dbo].[tbl_Fees]([fee_ID]) ON DELETE CASCADE
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_FeeBreakdown_fee_ID' AND object_id = OBJECT_ID('dbo.tbl_FeeBreakdown'))
                        CREATE INDEX IX_tbl_FeeBreakdown_fee_ID ON [dbo].[tbl_FeeBreakdown]([fee_ID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_FeeBreakdown_breakdown_type' AND object_id = OBJECT_ID('dbo.tbl_FeeBreakdown'))
                        CREATE INDEX IX_tbl_FeeBreakdown_breakdown_type ON [dbo].[tbl_FeeBreakdown]([breakdown_type])"
                }
            };
        }

        // Creates tbl_user_status_logs table - must be created after tbl_Users (foreign key dependency)
        public static TableDefinition GetUserStatusLogsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_user_status_logs",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_user_status_logs](
                        [log_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [user_id] INT NOT NULL,
                        [changed_by] INT NOT NULL,
                        [old_status] VARCHAR(20) NOT NULL,
                        [new_status] VARCHAR(20) NOT NULL,
                        [reason] TEXT NULL,
                        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_StatusLogs_User FOREIGN KEY ([user_id]) REFERENCES [dbo].[tbl_Users]([user_ID]),
                        CONSTRAINT FK_StatusLogs_Admin FOREIGN KEY ([changed_by]) REFERENCES [dbo].[tbl_Users]([user_ID])
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_user_status_logs_user_id' AND object_id = OBJECT_ID('dbo.tbl_user_status_logs'))
                        CREATE INDEX IX_tbl_user_status_logs_user_id ON [dbo].[tbl_user_status_logs]([user_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_user_status_logs_changed_by' AND object_id = OBJECT_ID('dbo.tbl_user_status_logs'))
                        CREATE INDEX IX_tbl_user_status_logs_changed_by ON [dbo].[tbl_user_status_logs]([changed_by])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_user_status_logs_created_at' AND object_id = OBJECT_ID('dbo.tbl_user_status_logs'))
                        CREATE INDEX IX_tbl_user_status_logs_created_at ON [dbo].[tbl_user_status_logs]([created_at])"
                }
            };
        }
    }

    // Holds table creation script and index definitions
    public class TableDefinition
    {
        public string TableName { get; set; } = string.Empty;
        public string SchemaName { get; set; } = "dbo";
        public string CreateTableScript { get; set; } = string.Empty;
        public List<string> CreateIndexesScripts { get; set; } = new List<string>();
    }

    // Holds view creation scripts
    public static class ViewDefinitions
    {
        // Returns all view definitions
        public static List<ViewDefinition> GetAllViewDefinitions()
        {
            return new List<ViewDefinition>
            {
                GetEmployeeViewDefinition(),
                GetStudentViewDefinition()
            };
        }

        // Creates vw_EmployeeData view - combines all employee-related data
        public static ViewDefinition GetEmployeeViewDefinition()
        {
            return new ViewDefinition
            {
                ViewName = "vw_EmployeeData",
                SchemaName = "dbo",
                CreateViewScript = @"
                    CREATE VIEW [dbo].[vw_EmployeeData]
                    AS
                    SELECT 
                        u.[user_ID] AS UserId,
                        u.[system_ID] AS SystemId,
                        u.[first_name] AS FirstName,
                        u.[mid_name] AS MiddleName,
                        u.[last_name] AS LastName,
                        u.[suffix] AS Suffix,
                        CASE 
                            WHEN u.[suffix] IS NOT NULL AND u.[suffix] != '' 
                            THEN u.[first_name] + ' ' + ISNULL(u.[mid_name] + ' ', '') + u.[last_name] + ' ' + u.[suffix]
                            ELSE u.[first_name] + ' ' + ISNULL(u.[mid_name] + ' ', '') + u.[last_name]
                        END AS FullName,
                        u.[birthdate] AS BirthDate,
                        u.[age] AS Age,
                        u.[gender] AS Gender,
                        u.[contact_num] AS ContactNumber,
                        u.[user_role] AS Role,
                        u.[email] AS Email,
                        u.[date_hired] AS DateHired,
                        CASE 
                            WHEN u.[status] IS NULL OR u.[status] = '' THEN 'Active'
                            ELSE UPPER(LEFT(u.[status], 1)) + LOWER(SUBSTRING(u.[status], 2, LEN(u.[status])))
                        END AS Status,
                        -- Address fields
                        addr.[house_no] AS HouseNo,
                        addr.[street_name] AS StreetName,
                        addr.[province] AS Province,
                        addr.[city] AS City,
                        addr.[barangay] AS Barangay,
                        addr.[country] AS Country,
                        addr.[zip_code] AS ZipCode,
                        -- Formatted address
                        CASE 
                            WHEN addr.[house_no] IS NOT NULL AND addr.[house_no] != '' 
                                AND addr.[street_name] IS NOT NULL AND addr.[street_name] != ''
                                AND addr.[barangay] IS NOT NULL AND addr.[barangay] != ''
                                AND addr.[city] IS NOT NULL AND addr.[city] != ''
                                AND addr.[province] IS NOT NULL AND addr.[province] != ''
                            THEN addr.[house_no] + ' ' + addr.[street_name] + ', ' + addr.[barangay] + ', ' + addr.[city] + ', ' + addr.[province]
                            WHEN addr.[barangay] IS NOT NULL AND addr.[barangay] != ''
                                AND addr.[city] IS NOT NULL AND addr.[city] != ''
                                AND addr.[province] IS NOT NULL AND addr.[province] != ''
                            THEN addr.[barangay] + ', ' + addr.[city] + ', ' + addr.[province]
                            WHEN addr.[city] IS NOT NULL AND addr.[city] != ''
                                AND addr.[province] IS NOT NULL AND addr.[province] != ''
                            THEN addr.[city] + ', ' + addr.[province]
                            WHEN addr.[province] IS NOT NULL AND addr.[province] != ''
                            THEN addr.[province]
                            ELSE 'N/A'
                        END AS FormattedAddress,
                        -- Emergency contact fields
                        ec.[first_name] AS EmergencyContactFirstName,
                        ec.[mid_name] AS EmergencyContactMiddleName,
                        ec.[last_name] AS EmergencyContactLastName,
                        ec.[suffix] AS EmergencyContactSuffix,
                        ec.[relationship] AS EmergencyContactRelationship,
                        ec.[contact_number] AS EmergencyContactNumber,
                        ec.[address] AS EmergencyContactAddress,
                        -- Salary fields
                        sal.[base_salary] AS BaseSalary,
                        sal.[allowance] AS Allowance,
                        (sal.[base_salary] + sal.[allowance]) AS TotalSalary,
                        sal.[date_effective] AS SalaryDateEffective,
                        sal.[is_active] AS SalaryIsActive
                    FROM [dbo].[tbl_Users] u
                    LEFT JOIN [dbo].[tbl_employee_address] addr ON u.[user_ID] = addr.[user_ID]
                    LEFT JOIN [dbo].[tbl_employee_emergency_contact] ec ON u.[user_ID] = ec.[user_ID]
                    LEFT JOIN [dbo].[tbl_salary_info] sal ON u.[user_ID] = sal.[user_ID] AND sal.[is_active] = 1;"
            };
        }

        // Creates vw_StudentData view - combines all student-related data
        public static ViewDefinition GetStudentViewDefinition()
        {
            return new ViewDefinition
            {
                ViewName = "vw_StudentData",
                SchemaName = "dbo",
                CreateViewScript = @"
                    CREATE VIEW [dbo].[vw_StudentData]
                    AS
                    SELECT 
                        s.[student_id] AS StudentId,
                        s.[first_name] AS FirstName,
                        s.[middle_name] AS MiddleName,
                        s.[last_name] AS LastName,
                        s.[suffix] AS Suffix,
                        CASE 
                            WHEN s.[suffix] IS NOT NULL AND s.[suffix] != '' 
                            THEN s.[first_name] + ' ' + s.[middle_name] + ' ' + s.[last_name] + ' ' + s.[suffix]
                            ELSE s.[first_name] + ' ' + s.[middle_name] + ' ' + s.[last_name]
                        END AS FullName,
                        s.[birthdate] AS BirthDate,
                        s.[age] AS Age,
                        s.[place_of_birth] AS PlaceOfBirth,
                        s.[sex] AS Sex,
                        s.[mother_tongue] AS MotherTongue,
                        s.[ip_comm] AS IsIPCommunity,
                        s.[ip_specify] AS IPCommunitySpecify,
                        s.[four_ps] AS Is4PsBeneficiary,
                        s.[four_ps_hseID] AS FourPsHouseholdId,
                        -- Current address fields
                        s.[hse_no] AS CurrentHouseNo,
                        s.[street] AS CurrentStreet,
                        s.[brngy] AS CurrentBarangay,
                        s.[province] AS CurrentProvince,
                        s.[city] AS CurrentCity,
                        s.[country] AS CurrentCountry,
                        s.[zip_code] AS CurrentZipCode,
                        -- Permanent address fields
                        s.[phse_no] AS PermanentHouseNo,
                        s.[pstreet] AS PermanentStreet,
                        s.[pbrngy] AS PermanentBarangay,
                        s.[pprovince] AS PermanentProvince,
                        s.[pcity] AS PermanentCity,
                        s.[pcountry] AS PermanentCountry,
                        s.[pzip_code] AS PermanentZipCode,
                        -- Formatted current address
                        CASE 
                            WHEN s.[hse_no] IS NOT NULL AND s.[hse_no] != '' 
                                AND s.[street] IS NOT NULL AND s.[street] != ''
                                AND s.[brngy] IS NOT NULL AND s.[brngy] != ''
                                AND s.[city] IS NOT NULL AND s.[city] != ''
                                AND s.[province] IS NOT NULL AND s.[province] != ''
                            THEN s.[hse_no] + ' ' + s.[street] + ', ' + s.[brngy] + ', ' + s.[city] + ', ' + s.[province]
                            WHEN s.[brngy] IS NOT NULL AND s.[brngy] != ''
                                AND s.[city] IS NOT NULL AND s.[city] != ''
                                AND s.[province] IS NOT NULL AND s.[province] != ''
                            THEN s.[brngy] + ', ' + s.[city] + ', ' + s.[province]
                            WHEN s.[city] IS NOT NULL AND s.[city] != ''
                                AND s.[province] IS NOT NULL AND s.[province] != ''
                            THEN s.[city] + ', ' + s.[province]
                            WHEN s.[province] IS NOT NULL AND s.[province] != ''
                            THEN s.[province]
                            ELSE 'N/A'
                        END AS FormattedCurrentAddress,
                        -- Enrollment fields
                        s.[student_type] AS StudentType,
                        s.[LRN] AS LRN,
                        s.[school_yr] AS SchoolYear,
                        s.[grade_level] AS GradeLevel,
                        s.[date_registered] AS DateRegistered,
                        s.[status] AS Status,
                        -- Guardian fields
                        g.[guardian_id] AS GuardianId,
                        g.[first_name] AS GuardianFirstName,
                        g.[middle_name] AS GuardianMiddleName,
                        g.[last_name] AS GuardianLastName,
                        g.[suffix] AS GuardianSuffix,
                        CASE 
                            WHEN g.[suffix] IS NOT NULL AND g.[suffix] != '' 
                            THEN g.[first_name] + ' ' + ISNULL(g.[middle_name] + ' ', '') + g.[last_name] + ' ' + g.[suffix]
                            ELSE g.[first_name] + ' ' + ISNULL(g.[middle_name] + ' ', '') + g.[last_name]
                        END AS GuardianFullName,
                        g.[contact_num] AS GuardianContactNumber,
                        g.[relationship] AS GuardianRelationship
                    FROM [dbo].[tbl_Students] s
                    LEFT JOIN [dbo].[tbl_Guardians] g ON s.[guardian_id] = g.[guardian_id];"
            };
        }
    }

    // Holds view creation script
    public class ViewDefinition
    {
        public string ViewName { get; set; } = string.Empty;
        public string SchemaName { get; set; } = "dbo";
        public string CreateViewScript { get; set; } = string.Empty;
    }
}

