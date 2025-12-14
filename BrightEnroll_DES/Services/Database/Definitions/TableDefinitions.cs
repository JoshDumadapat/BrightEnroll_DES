namespace BrightEnroll_DES.Services.Database.Definitions
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
                // Curriculum tables (order matters: grade level first, then classrooms, then sections, then subjects, then linking tables)
                GetBuildingsTableDefinition(),
                GetClassroomsTableDefinition(),
                GetSectionsTableDefinition(),
                GetSubjectsTableDefinition(),
                GetSubjectSectionTableDefinition(),
                GetSubjectScheduleTableDefinition(),
                GetTeacherSectionAssignmentTableDefinition(),
                GetClassScheduleTableDefinition(),
                GetStudentSectionEnrollmentTableDefinition(),
                // Grades table (must be after tbl_Students, tbl_Subjects, tbl_Sections, tbl_Users)
                GetGradesTableDefinition(),
                GetGradeWeightsTableDefinition(),
                GetGradeHistoryTableDefinition(),
                // Payroll tables (standalone, no foreign keys)
                GetRolesTableDefinition(),
                GetDeductionsTableDefinition(),
                GetSalaryChangeRequestsTableDefinition(),
                GetPayrollTransactionsTableDefinition(),
                // Time Records table (must be after tbl_Users)
                GetTimeRecordsTableDefinition(),
                // Finance - expenses
                GetExpensesTableDefinition(),
                GetExpenseAttachmentsTableDefinition(),
                // Finance - student payments (must be after tbl_Students)
                GetStudentPaymentsTableDefinition(),
                // Finance - ledger system (must be after tbl_Students)
                GetDiscountsTableDefinition(), // Must be before LedgerCharges (FK dependency)
                GetStudentLedgersTableDefinition(),
                GetLedgerChargesTableDefinition(),
                GetLedgerPaymentsTableDefinition(),
                // Chart of Accounts and Double-Entry Bookkeeping (must be after tbl_Users)
                GetChartOfAccountsTableDefinition(),
                GetJournalEntriesTableDefinition(),
                GetJournalEntryLinesTableDefinition(),
                GetAccountingPeriodsTableDefinition(),
                // Notifications table (must be after tbl_Users)
                GetNotificationsTableDefinition(),
                // Inventory & Asset Management tables
                GetAssetsTableDefinition(),
                GetInventoryItemsTableDefinition(),
                GetAssetAssignmentsTableDefinition(),
                // Student status logging table (must be after tbl_Students)
                GetStudentStatusLogsTableDefinition(),
                // Audit logging table (must be after tbl_Users and tbl_Students)
                GetAuditLogsTableDefinition(),
                // Teacher-specific tables (must be after tbl_Users, tbl_Sections, tbl_Subjects, tbl_Students)
                GetTeacherActivityLogsTableDefinition(),
                GetAttendanceTableDefinition(),
                // School Year Management (standalone, no dependencies)
                GetSchoolYearTableDefinition(),
                // Sync History and Logs (must be after tbl_SyncHistory for FK in tbl_SyncLogs)
                GetSyncHistoryTableDefinition(),
                GetSyncLogsTableDefinition(),
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
                        [middle_name] VARCHAR(50) NULL,
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
                        [archive_reason] NVARCHAR(MAX) NULL,
                        [amount_paid] DECIMAL(18,2) NOT NULL DEFAULT 0,
                        [payment_status] VARCHAR(20) NULL DEFAULT 'Unpaid',
                        [updated_at] DATETIME NULL,
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
                        [is_verified] BIT NOT NULL DEFAULT 0,
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
                        [school_year] VARCHAR(20) NOT NULL,
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
                        [school_year] VARCHAR(20) NULL,
                        CONSTRAINT FK_tbl_Fees_GradeLevel FOREIGN KEY ([gradelevel_ID]) REFERENCES [dbo].[tbl_GradeLevel]([gradelevel_ID])
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Fees_gradelevel_ID' AND object_id = OBJECT_ID('dbo.tbl_Fees'))
                        CREATE INDEX IX_tbl_Fees_gradelevel_ID ON [dbo].[tbl_Fees]([gradelevel_ID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Fees_is_active' AND object_id = OBJECT_ID('dbo.tbl_Fees'))
                        CREATE INDEX IX_tbl_Fees_is_active ON [dbo].[tbl_Fees]([is_active])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Fees_school_year' AND object_id = OBJECT_ID('dbo.tbl_Fees'))
                        CREATE INDEX IX_tbl_Fees_school_year ON [dbo].[tbl_Fees]([school_year])
                        WHERE [school_year] IS NOT NULL",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Fees_gradelevel_schoolyear' AND object_id = OBJECT_ID('dbo.tbl_Fees'))
                        CREATE INDEX IX_tbl_Fees_gradelevel_schoolyear ON [dbo].[tbl_Fees]([gradelevel_ID], [school_year])
                        WHERE [school_year] IS NOT NULL"
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

        // Creates tbl_Buildings table (optional but recommended)
        public static TableDefinition GetBuildingsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_Buildings",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_Buildings](
                        [BuildingID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [BuildingName] VARCHAR(100) NOT NULL,
                        [FloorCount] INT NULL,
                        [Description] VARCHAR(500) NULL
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Buildings_BuildingName' AND object_id = OBJECT_ID('dbo.tbl_Buildings'))
                        CREATE INDEX IX_tbl_Buildings_BuildingName ON [dbo].[tbl_Buildings]([BuildingName])"
                }
            };
        }

        // Creates tbl_Classrooms table
        public static TableDefinition GetClassroomsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_Classrooms",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_Classrooms](
                        [RoomID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [RoomName] VARCHAR(100) NOT NULL,
                        [BuildingName] VARCHAR(100) NULL,
                        [FloorNumber] INT NULL,
                        [RoomType] VARCHAR(50) NULL,
                        [Capacity] INT NOT NULL,
                        [Status] VARCHAR(20) NOT NULL DEFAULT 'Active',
                        [Notes] VARCHAR(500) NULL,
                        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                        [UpdatedAt] DATETIME NULL
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Classrooms_RoomName' AND object_id = OBJECT_ID('dbo.tbl_Classrooms'))
                        CREATE INDEX IX_tbl_Classrooms_RoomName ON [dbo].[tbl_Classrooms]([RoomName])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Classrooms_Status' AND object_id = OBJECT_ID('dbo.tbl_Classrooms'))
                        CREATE INDEX IX_tbl_Classrooms_Status ON [dbo].[tbl_Classrooms]([Status])"
                }
            };
        }

        // Creates tbl_Sections table - must be created after tbl_GradeLevel and tbl_Classrooms (foreign key dependencies)
        public static TableDefinition GetSectionsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_Sections",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_Sections](
                        [SectionID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [SectionName] VARCHAR(100) NOT NULL,
                        [GradeLvlID] INT NOT NULL,
                        [ClassroomID] INT NULL,
                        [AdviserID] INT NULL,
                        [Capacity] INT NOT NULL,
                        [Notes] VARCHAR(500) NULL,
                        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                        [UpdatedAt] DATETIME NULL,
                        CONSTRAINT FK_tbl_Sections_tbl_GradeLevel FOREIGN KEY ([GradeLvlID]) REFERENCES [dbo].[tbl_GradeLevel]([gradelevel_ID]),
                        CONSTRAINT FK_tbl_Sections_tbl_Classrooms FOREIGN KEY ([ClassroomID]) REFERENCES [dbo].[tbl_Classrooms]([RoomID]) ON DELETE SET NULL,
                        CONSTRAINT FK_tbl_Sections_tbl_Users_Adviser FOREIGN KEY ([AdviserID]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE SET NULL
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Sections_SectionName' AND object_id = OBJECT_ID('dbo.tbl_Sections'))
                        CREATE INDEX IX_tbl_Sections_SectionName ON [dbo].[tbl_Sections]([SectionName])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Sections_GradeLvlID' AND object_id = OBJECT_ID('dbo.tbl_Sections'))
                        CREATE INDEX IX_tbl_Sections_GradeLvlID ON [dbo].[tbl_Sections]([GradeLvlID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Sections_ClassroomID' AND object_id = OBJECT_ID('dbo.tbl_Sections'))
                        CREATE INDEX IX_tbl_Sections_ClassroomID ON [dbo].[tbl_Sections]([ClassroomID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Sections_AdviserID' AND object_id = OBJECT_ID('dbo.tbl_Sections'))
                        CREATE INDEX IX_tbl_Sections_AdviserID ON [dbo].[tbl_Sections]([AdviserID])"
                }
            };
        }

        // Creates tbl_Subjects table - must be created after tbl_GradeLevel (foreign key dependency)
        public static TableDefinition GetSubjectsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_Subjects",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_Subjects](
                        [SubjectID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [SubjectCode] VARCHAR(50) NULL,
                        [GradeLvlID] INT NOT NULL,
                        [SubjectName] VARCHAR(100) NOT NULL,
                        [Description] VARCHAR(500) NULL,
                        [IsActive] BIT NOT NULL DEFAULT 1,
                        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                        [UpdatedAt] DATETIME NULL,
                        CONSTRAINT FK_tbl_Subjects_tbl_GradeLevel FOREIGN KEY ([GradeLvlID]) REFERENCES [dbo].[tbl_GradeLevel]([gradelevel_ID])
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Subjects_SubjectName' AND object_id = OBJECT_ID('dbo.tbl_Subjects'))
                        CREATE INDEX IX_tbl_Subjects_SubjectName ON [dbo].[tbl_Subjects]([SubjectName])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Subjects_GradeLvlID' AND object_id = OBJECT_ID('dbo.tbl_Subjects'))
                        CREATE INDEX IX_tbl_Subjects_GradeLvlID ON [dbo].[tbl_Subjects]([GradeLvlID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Subjects_SubjectCode' AND object_id = OBJECT_ID('dbo.tbl_Subjects'))
                        CREATE INDEX IX_tbl_Subjects_SubjectCode ON [dbo].[tbl_Subjects]([SubjectCode])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Subjects_IsActive' AND object_id = OBJECT_ID('dbo.tbl_Subjects'))
                        CREATE INDEX IX_tbl_Subjects_IsActive ON [dbo].[tbl_Subjects]([IsActive])"
                }
            };
        }

        // Creates tbl_SubjectSection table (many-to-many) - must be created after tbl_Sections and tbl_Subjects
        public static TableDefinition GetSubjectSectionTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_SubjectSection",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_SubjectSection](
                        [ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [SectionID] INT NOT NULL,
                        [SubjectID] INT NOT NULL,
                        CONSTRAINT FK_tbl_SubjectSection_tbl_Sections FOREIGN KEY ([SectionID]) REFERENCES [dbo].[tbl_Sections]([SectionID]) ON DELETE CASCADE,
                        CONSTRAINT FK_tbl_SubjectSection_tbl_Subjects FOREIGN KEY ([SubjectID]) REFERENCES [dbo].[tbl_Subjects]([SubjectID]) ON DELETE CASCADE
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SubjectSection_SectionID' AND object_id = OBJECT_ID('dbo.tbl_SubjectSection'))
                        CREATE INDEX IX_tbl_SubjectSection_SectionID ON [dbo].[tbl_SubjectSection]([SectionID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SubjectSection_SubjectID' AND object_id = OBJECT_ID('dbo.tbl_SubjectSection'))
                        CREATE INDEX IX_tbl_SubjectSection_SubjectID ON [dbo].[tbl_SubjectSection]([SubjectID])"
                }
            };
        }

        // Creates tbl_SubjectSchedule table - must be created after tbl_Subjects and tbl_GradeLevel
        public static TableDefinition GetSubjectScheduleTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_SubjectSchedule",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_SubjectSchedule](
                        [ScheduleID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [SubjectID] INT NOT NULL,
                        [GradeLvlID] INT NOT NULL,
                        [DayOfWeek] VARCHAR(10) NOT NULL,
                        [StartTime] TIME NOT NULL,
                        [EndTime] TIME NOT NULL,
                        [IsDefault] BIT NOT NULL DEFAULT 1,
                        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                        [UpdatedAt] DATETIME NULL,
                        CONSTRAINT FK_tbl_SubjectSchedule_tbl_Subjects FOREIGN KEY ([SubjectID]) REFERENCES [dbo].[tbl_Subjects]([SubjectID]) ON DELETE CASCADE,
                        CONSTRAINT FK_tbl_SubjectSchedule_tbl_GradeLevel FOREIGN KEY ([GradeLvlID]) REFERENCES [dbo].[tbl_GradeLevel]([gradelevel_ID])
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SubjectSchedule_SubjectID' AND object_id = OBJECT_ID('dbo.tbl_SubjectSchedule'))
                        CREATE INDEX IX_tbl_SubjectSchedule_SubjectID ON [dbo].[tbl_SubjectSchedule]([SubjectID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SubjectSchedule_GradeLvlID' AND object_id = OBJECT_ID('dbo.tbl_SubjectSchedule'))
                        CREATE INDEX IX_tbl_SubjectSchedule_GradeLvlID ON [dbo].[tbl_SubjectSchedule]([GradeLvlID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SubjectSchedule_Subject_Grade_Day' AND object_id = OBJECT_ID('dbo.tbl_SubjectSchedule'))
                        CREATE INDEX IX_tbl_SubjectSchedule_Subject_Grade_Day ON [dbo].[tbl_SubjectSchedule]([SubjectID], [GradeLvlID], [DayOfWeek])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SubjectSchedule_IsDefault' AND object_id = OBJECT_ID('dbo.tbl_SubjectSchedule'))
                        CREATE INDEX IX_tbl_SubjectSchedule_IsDefault ON [dbo].[tbl_SubjectSchedule]([IsDefault])"
                }
            };
        }

        // Creates tbl_TeacherSectionAssignment table - must be created after tbl_Users, tbl_Sections, and tbl_Subjects
        public static TableDefinition GetTeacherSectionAssignmentTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_TeacherSectionAssignment",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_TeacherSectionAssignment](
                        [AssignmentID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [TeacherID] INT NOT NULL,
                        [SectionID] INT NOT NULL,
                        [SubjectID] INT NULL,
                        [Role] VARCHAR(50) NOT NULL,
                        [IsArchived] BIT NOT NULL DEFAULT 0,
                        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                        [UpdatedAt] DATETIME NULL,
                        CONSTRAINT FK_tbl_TeacherSectionAssignment_tbl_Users FOREIGN KEY ([TeacherID]) REFERENCES [dbo].[tbl_Users]([user_ID]),
                        CONSTRAINT FK_tbl_TeacherSectionAssignment_tbl_Sections FOREIGN KEY ([SectionID]) REFERENCES [dbo].[tbl_Sections]([SectionID]) ON DELETE CASCADE,
                        CONSTRAINT FK_tbl_TeacherSectionAssignment_tbl_Subjects FOREIGN KEY ([SubjectID]) REFERENCES [dbo].[tbl_Subjects]([SubjectID]) ON DELETE SET NULL
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_TeacherSectionAssignment_TeacherID' AND object_id = OBJECT_ID('dbo.tbl_TeacherSectionAssignment'))
                        CREATE INDEX IX_tbl_TeacherSectionAssignment_TeacherID ON [dbo].[tbl_TeacherSectionAssignment]([TeacherID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_TeacherSectionAssignment_SectionID' AND object_id = OBJECT_ID('dbo.tbl_TeacherSectionAssignment'))
                        CREATE INDEX IX_tbl_TeacherSectionAssignment_SectionID ON [dbo].[tbl_TeacherSectionAssignment]([SectionID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_TeacherSectionAssignment_SubjectID' AND object_id = OBJECT_ID('dbo.tbl_TeacherSectionAssignment'))
                        CREATE INDEX IX_tbl_TeacherSectionAssignment_SubjectID ON [dbo].[tbl_TeacherSectionAssignment]([SubjectID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_TeacherSectionAssignment_Role' AND object_id = OBJECT_ID('dbo.tbl_TeacherSectionAssignment'))
                        CREATE INDEX IX_tbl_TeacherSectionAssignment_Role ON [dbo].[tbl_TeacherSectionAssignment]([Role])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_TeacherSectionAssignment_IsArchived' AND object_id = OBJECT_ID('dbo.tbl_TeacherSectionAssignment'))
                        CREATE INDEX IX_tbl_TeacherSectionAssignment_IsArchived ON [dbo].[tbl_TeacherSectionAssignment]([IsArchived])"
                }
            };
        }

        // Creates tbl_ClassSchedule table - must be created after tbl_TeacherSectionAssignment and tbl_Classrooms
        public static TableDefinition GetClassScheduleTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_ClassSchedule",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_ClassSchedule](
                        [ScheduleID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [AssignmentID] INT NOT NULL,
                        [DayOfWeek] VARCHAR(10) NOT NULL,
                        [StartTime] TIME NOT NULL,
                        [EndTime] TIME NOT NULL,
                        [RoomID] INT NOT NULL,
                        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                        [UpdatedAt] DATETIME NULL,
                        CONSTRAINT FK_tbl_ClassSchedule_tbl_TeacherSectionAssignment FOREIGN KEY ([AssignmentID]) REFERENCES [dbo].[tbl_TeacherSectionAssignment]([AssignmentID]) ON DELETE CASCADE,
                        CONSTRAINT FK_tbl_ClassSchedule_tbl_Classrooms FOREIGN KEY ([RoomID]) REFERENCES [dbo].[tbl_Classrooms]([RoomID])
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_ClassSchedule_AssignmentID' AND object_id = OBJECT_ID('dbo.tbl_ClassSchedule'))
                        CREATE INDEX IX_tbl_ClassSchedule_AssignmentID ON [dbo].[tbl_ClassSchedule]([AssignmentID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_ClassSchedule_RoomID' AND object_id = OBJECT_ID('dbo.tbl_ClassSchedule'))
                        CREATE INDEX IX_tbl_ClassSchedule_RoomID ON [dbo].[tbl_ClassSchedule]([RoomID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_ClassSchedule_DayOfWeek' AND object_id = OBJECT_ID('dbo.tbl_ClassSchedule'))
                        CREATE INDEX IX_tbl_ClassSchedule_DayOfWeek ON [dbo].[tbl_ClassSchedule]([DayOfWeek])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_ClassSchedule_Assignment_Day_Time' AND object_id = OBJECT_ID('dbo.tbl_ClassSchedule'))
                        CREATE INDEX IX_tbl_ClassSchedule_Assignment_Day_Time ON [dbo].[tbl_ClassSchedule]([AssignmentID], [DayOfWeek], [StartTime], [EndTime])"
                }
            };
        }

        // Creates tbl_StudentSectionEnrollment table - links students to sections per school year
        public static TableDefinition GetStudentSectionEnrollmentTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_StudentSectionEnrollment",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_StudentSectionEnrollment](
                        [enrollment_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [student_id] VARCHAR(6) NOT NULL,
                        [SectionID] INT NOT NULL,
                        [school_yr] VARCHAR(20) NOT NULL,
                        [status] VARCHAR(20) NOT NULL DEFAULT 'Enrolled',
                        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        [updated_at] DATETIME NULL,
                        CONSTRAINT FK_tbl_StudentSectionEnrollment_tbl_Students FOREIGN KEY ([student_id]) REFERENCES [dbo].[tbl_Students]([student_id]),
                        CONSTRAINT FK_tbl_StudentSectionEnrollment_tbl_Sections FOREIGN KEY ([SectionID]) REFERENCES [dbo].[tbl_Sections]([SectionID])
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentSectionEnrollment_StudentId' AND object_id = OBJECT_ID('dbo.tbl_StudentSectionEnrollment'))
                        CREATE INDEX IX_tbl_StudentSectionEnrollment_StudentId ON [dbo].[tbl_StudentSectionEnrollment]([student_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentSectionEnrollment_SectionId' AND object_id = OBJECT_ID('dbo.tbl_StudentSectionEnrollment'))
                        CREATE INDEX IX_tbl_StudentSectionEnrollment_SectionId ON [dbo].[tbl_StudentSectionEnrollment]([SectionID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentSectionEnrollment_Section_SchoolYear' AND object_id = OBJECT_ID('dbo.tbl_StudentSectionEnrollment'))
                        CREATE INDEX IX_tbl_StudentSectionEnrollment_Section_SchoolYear ON [dbo].[tbl_StudentSectionEnrollment]([SectionID], [school_yr])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_tbl_StudentSectionEnrollment_Student_SchoolYear' AND object_id = OBJECT_ID('dbo.tbl_StudentSectionEnrollment'))
                        CREATE UNIQUE INDEX UX_tbl_StudentSectionEnrollment_Student_SchoolYear ON [dbo].[tbl_StudentSectionEnrollment]([student_id], [school_yr])"
                }
            };
        }

        // Creates tbl_Grades table - must be created after tbl_Students, tbl_Subjects, tbl_Sections, tbl_Users
        public static TableDefinition GetGradesTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_Grades",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_Grades](
                        [grade_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [student_id] VARCHAR(6) NOT NULL,
                        [subject_id] INT NOT NULL,
                        [section_id] INT NOT NULL,
                        [school_year] VARCHAR(20) NOT NULL,
                        [grading_period] VARCHAR(10) NOT NULL,
                        [written_work] DECIMAL(5,2) NULL,
                        [performance_tasks] DECIMAL(5,2) NULL,
                        [quarterly_assessment] DECIMAL(5,2) NULL,
                        [final_grade] DECIMAL(5,2) NULL,
                        [teacher_id] INT NOT NULL,
                        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        [updated_at] DATETIME NULL,
                        CONSTRAINT FK_tbl_Grades_tbl_Students FOREIGN KEY ([student_id]) REFERENCES [dbo].[tbl_Students]([student_id]) ON DELETE CASCADE,
                        CONSTRAINT FK_tbl_Grades_tbl_Subjects FOREIGN KEY ([subject_id]) REFERENCES [dbo].[tbl_Subjects]([SubjectID]) ON DELETE CASCADE,
                        CONSTRAINT FK_tbl_Grades_tbl_Sections FOREIGN KEY ([section_id]) REFERENCES [dbo].[tbl_Sections]([SectionID]) ON DELETE CASCADE,
                        CONSTRAINT FK_tbl_Grades_tbl_Users FOREIGN KEY ([teacher_id]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Grades_StudentId' AND object_id = OBJECT_ID('dbo.tbl_Grades'))
                        CREATE INDEX IX_tbl_Grades_StudentId ON [dbo].[tbl_Grades]([student_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Grades_SubjectId' AND object_id = OBJECT_ID('dbo.tbl_Grades'))
                        CREATE INDEX IX_tbl_Grades_SubjectId ON [dbo].[tbl_Grades]([subject_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Grades_SectionId' AND object_id = OBJECT_ID('dbo.tbl_Grades'))
                        CREATE INDEX IX_tbl_Grades_SectionId ON [dbo].[tbl_Grades]([section_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Grades_TeacherId' AND object_id = OBJECT_ID('dbo.tbl_Grades'))
                        CREATE INDEX IX_tbl_Grades_TeacherId ON [dbo].[tbl_Grades]([teacher_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Grades_Student_Subject_Section_Period' AND object_id = OBJECT_ID('dbo.tbl_Grades'))
                        CREATE INDEX IX_tbl_Grades_Student_Subject_Section_Period ON [dbo].[tbl_Grades]([student_id], [subject_id], [section_id], [grading_period], [school_year])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_tbl_Grades_Student_Subject_Section_Period_Year' AND object_id = OBJECT_ID('dbo.tbl_Grades'))
                        CREATE UNIQUE INDEX UX_tbl_Grades_Student_Subject_Section_Period_Year ON [dbo].[tbl_Grades]([student_id], [subject_id], [section_id], [grading_period], [school_year])"
                }
            };
        }

        // Creates tbl_GradeWeights table - must be created after tbl_Subjects
        public static TableDefinition GetGradeWeightsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_GradeWeights",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_GradeWeights](
                        [weight_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [subject_id] INT NOT NULL UNIQUE,
                        [written_work_weight] DECIMAL(5,2) NOT NULL DEFAULT 0.20,
                        [performance_tasks_weight] DECIMAL(5,2) NOT NULL DEFAULT 0.60,
                        [quarterly_assessment_weight] DECIMAL(5,2) NOT NULL DEFAULT 0.20,
                        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        [updated_at] DATETIME NULL,
                        CONSTRAINT FK_tbl_GradeWeights_tbl_Subjects FOREIGN KEY ([subject_id]) REFERENCES [dbo].[tbl_Subjects]([SubjectID]) ON DELETE CASCADE
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_GradeWeights_SubjectId' AND object_id = OBJECT_ID('dbo.tbl_GradeWeights'))
                        CREATE UNIQUE INDEX IX_tbl_GradeWeights_SubjectId ON [dbo].[tbl_GradeWeights]([subject_id])"
                }
            };
        }

        // Creates tbl_GradeHistory table - must be created after tbl_Grades, tbl_Users
        public static TableDefinition GetGradeHistoryTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_GradeHistory",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_GradeHistory](
                        [history_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [grade_id] INT NOT NULL,
                        [student_id] VARCHAR(6) NOT NULL,
                        [subject_id] INT NOT NULL,
                        [section_id] INT NOT NULL,
                        [written_work_old] DECIMAL(5,2) NULL,
                        [written_work_new] DECIMAL(5,2) NULL,
                        [performance_tasks_old] DECIMAL(5,2) NULL,
                        [performance_tasks_new] DECIMAL(5,2) NULL,
                        [quarterly_assessment_old] DECIMAL(5,2) NULL,
                        [quarterly_assessment_new] DECIMAL(5,2) NULL,
                        [final_grade_old] DECIMAL(5,2) NULL,
                        [final_grade_new] DECIMAL(5,2) NULL,
                        [changed_by] INT NOT NULL,
                        [change_reason] NVARCHAR(500) NULL,
                        [changed_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_tbl_GradeHistory_tbl_Grades FOREIGN KEY ([grade_id]) REFERENCES [dbo].[tbl_Grades]([grade_id]) ON DELETE CASCADE,
                        CONSTRAINT FK_tbl_GradeHistory_tbl_Users FOREIGN KEY ([changed_by]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_GradeHistory_GradeId' AND object_id = OBJECT_ID('dbo.tbl_GradeHistory'))
                        CREATE INDEX IX_tbl_GradeHistory_GradeId ON [dbo].[tbl_GradeHistory]([grade_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_GradeHistory_StudentId' AND object_id = OBJECT_ID('dbo.tbl_GradeHistory'))
                        CREATE INDEX IX_tbl_GradeHistory_StudentId ON [dbo].[tbl_GradeHistory]([student_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_GradeHistory_ChangedAt' AND object_id = OBJECT_ID('dbo.tbl_GradeHistory'))
                        CREATE INDEX IX_tbl_GradeHistory_ChangedAt ON [dbo].[tbl_GradeHistory]([changed_at])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_GradeHistory_ChangedBy' AND object_id = OBJECT_ID('dbo.tbl_GradeHistory'))
                        CREATE INDEX IX_tbl_GradeHistory_ChangedBy ON [dbo].[tbl_GradeHistory]([changed_by])"
                }
            };
        }

        // Creates tbl_roles table - standalone table for role salary configuration
        public static TableDefinition GetRolesTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_roles",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_roles](
                        [role_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [role_name] VARCHAR(50) NOT NULL UNIQUE,
                        [base_salary] DECIMAL(12,2) NOT NULL,
                        [allowance] DECIMAL(12,2) NOT NULL DEFAULT 0.00,
                        [threshold_percentage] DECIMAL(5,2) NOT NULL DEFAULT 10.00,
                        [is_active] BIT NOT NULL DEFAULT 1,
                        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
                        [updated_date] DATETIME NULL
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_roles_role_name' AND object_id = OBJECT_ID('dbo.tbl_roles'))
                        CREATE INDEX IX_tbl_roles_role_name ON [dbo].[tbl_roles]([role_name])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_roles_is_active' AND object_id = OBJECT_ID('dbo.tbl_roles'))
                        CREATE INDEX IX_tbl_roles_is_active ON [dbo].[tbl_roles]([is_active])"
                }
            };
        }

        // Creates tbl_deductions table - standalone table for deduction configuration
        public static TableDefinition GetDeductionsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_deductions",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_deductions](
                        [deduction_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [deduction_type] VARCHAR(50) NOT NULL UNIQUE,
                        [deduction_name] VARCHAR(100) NOT NULL,
                        [rate_or_value] DECIMAL(12,4) NOT NULL,
                        [is_percentage] BIT NOT NULL DEFAULT 1,
                        [max_amount] DECIMAL(12,2) NULL,
                        [min_amount] DECIMAL(12,2) NULL,
                        [description] VARCHAR(500) NULL,
                        [is_active] BIT NOT NULL DEFAULT 1,
                        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
                        [updated_date] DATETIME NULL
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_deductions_deduction_type' AND object_id = OBJECT_ID('dbo.tbl_deductions'))
                        CREATE INDEX IX_tbl_deductions_deduction_type ON [dbo].[tbl_deductions]([deduction_type])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_deductions_is_active' AND object_id = OBJECT_ID('dbo.tbl_deductions'))
                        CREATE INDEX IX_tbl_deductions_is_active ON [dbo].[tbl_deductions]([is_active])"
                }
            };
        }

        // Creates tbl_salary_change_requests table - tracks salary change requests from HR that require Payroll/Admin approval
        // Must be created after tbl_Users (foreign key dependency)
        public static TableDefinition GetSalaryChangeRequestsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_salary_change_requests",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_salary_change_requests](
                        [request_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [user_id] INT NOT NULL,
                        [current_base_salary] DECIMAL(12,2) NOT NULL,
                        [current_allowance] DECIMAL(12,2) NOT NULL,
                        [requested_base_salary] DECIMAL(12,2) NOT NULL,
                        [requested_allowance] DECIMAL(12,2) NOT NULL,
                        [reason] NVARCHAR(500) NULL,
                        [status] VARCHAR(20) NOT NULL DEFAULT 'Pending',
                        [rejection_reason] NVARCHAR(500) NULL,
                        [requested_by] INT NOT NULL,
                        [approved_by] INT NULL,
                        [requested_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        [approved_at] DATETIME NULL,
                        [effective_date] DATE NULL,
                        [school_year] VARCHAR(20) NOT NULL,
                        [is_initial_registration] BIT NOT NULL DEFAULT 0,
                        CONSTRAINT FK_tbl_salary_change_requests_tbl_Users FOREIGN KEY ([user_id]) 
                            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
                        CONSTRAINT FK_tbl_salary_change_requests_RequestedBy FOREIGN KEY ([requested_by]) 
                            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
                        CONSTRAINT FK_tbl_salary_change_requests_ApprovedBy FOREIGN KEY ([approved_by]) 
                            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
                        CONSTRAINT CK_tbl_salary_change_requests_Status CHECK ([status] IN ('Pending', 'Approved', 'Rejected'))
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_salary_change_requests_user_id' AND object_id = OBJECT_ID('dbo.tbl_salary_change_requests'))
                        CREATE INDEX IX_tbl_salary_change_requests_user_id ON [dbo].[tbl_salary_change_requests]([user_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_salary_change_requests_requested_by' AND object_id = OBJECT_ID('dbo.tbl_salary_change_requests'))
                        CREATE INDEX IX_tbl_salary_change_requests_requested_by ON [dbo].[tbl_salary_change_requests]([requested_by])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_salary_change_requests_status' AND object_id = OBJECT_ID('dbo.tbl_salary_change_requests'))
                        CREATE INDEX IX_tbl_salary_change_requests_status ON [dbo].[tbl_salary_change_requests]([status])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_salary_change_requests_school_year' AND object_id = OBJECT_ID('dbo.tbl_salary_change_requests'))
                        CREATE INDEX IX_tbl_salary_change_requests_school_year ON [dbo].[tbl_salary_change_requests]([school_year])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_salary_change_requests_requested_at' AND object_id = OBJECT_ID('dbo.tbl_salary_change_requests'))
                        CREATE INDEX IX_tbl_salary_change_requests_requested_at ON [dbo].[tbl_salary_change_requests]([requested_at])"
                }
            };
        }

        // Creates tbl_payroll_transactions table - tracks payroll payment transactions and history
        // Must be created after tbl_Users (foreign key dependency)
        public static TableDefinition GetPayrollTransactionsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_payroll_transactions",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_payroll_transactions](
                        [transaction_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [user_id] INT NOT NULL,
                        [school_year] VARCHAR(20) NOT NULL,
                        [pay_period] VARCHAR(20) NOT NULL,
                        [base_salary] DECIMAL(12,2) NOT NULL,
                        [allowance] DECIMAL(12,2) NOT NULL,
                        [gross_salary] DECIMAL(12,2) NOT NULL,
                        [sss_deduction] DECIMAL(12,2) NOT NULL DEFAULT 0.00,
                        [philhealth_deduction] DECIMAL(12,2) NOT NULL DEFAULT 0.00,
                        [pagibig_deduction] DECIMAL(12,2) NOT NULL DEFAULT 0.00,
                        [tax_deduction] DECIMAL(12,2) NOT NULL DEFAULT 0.00,
                        [other_deductions] DECIMAL(12,2) NOT NULL DEFAULT 0.00,
                        [total_deductions] DECIMAL(12,2) NOT NULL DEFAULT 0.00,
                        [net_salary] DECIMAL(12,2) NOT NULL,
                        [status] VARCHAR(20) NOT NULL DEFAULT 'Pending',
                        [payment_date] DATE NULL,
                        [payment_method] VARCHAR(50) NULL,
                        [reference_number] VARCHAR(100) NULL,
                        [processed_by] INT NOT NULL,
                        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        [updated_at] DATETIME NULL,
                        [batch_timestamp] DATETIME NULL,
                        [notes] NVARCHAR(500) NULL,
                        [company_sss_contribution] DECIMAL(12,2) NOT NULL DEFAULT 0.00,
                        [company_philhealth_contribution] DECIMAL(12,2) NOT NULL DEFAULT 0.00,
                        [company_pagibig_contribution] DECIMAL(12,2) NOT NULL DEFAULT 0.00,
                        [total_company_contribution] DECIMAL(12,2) NOT NULL DEFAULT 0.00,
                        [created_by] INT NOT NULL,
                        [approved_by] INT NULL,
                        [approved_at] DATETIME NULL,
                        [cancelled_by] INT NULL,
                        [cancelled_at] DATETIME NULL,
                        [cancellation_reason] NVARCHAR(500) NULL,
                        CONSTRAINT FK_tbl_payroll_transactions_tbl_Users FOREIGN KEY ([user_id]) 
                            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
                        CONSTRAINT FK_tbl_payroll_transactions_ProcessedBy FOREIGN KEY ([processed_by]) 
                            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
                        CONSTRAINT FK_tbl_payroll_transactions_CreatedBy FOREIGN KEY ([created_by]) 
                            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
                        CONSTRAINT FK_tbl_payroll_transactions_ApprovedBy FOREIGN KEY ([approved_by]) 
                            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
                        CONSTRAINT FK_tbl_payroll_transactions_CancelledBy FOREIGN KEY ([cancelled_by]) 
                            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
                        CONSTRAINT CK_tbl_payroll_transactions_Status CHECK ([status] IN ('Pending', 'Pending Approval', 'Paid', 'Cancelled', 'Rejected'))
                        -- Note: Unique constraint for user_id + pay_period is created as a filtered index (excludes cancelled records)
                        -- See CreateIndexesScripts below for UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_payroll_transactions_user_id' AND object_id = OBJECT_ID('dbo.tbl_payroll_transactions'))
                        CREATE INDEX IX_tbl_payroll_transactions_user_id ON [dbo].[tbl_payroll_transactions]([user_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_payroll_transactions_school_year' AND object_id = OBJECT_ID('dbo.tbl_payroll_transactions'))
                        CREATE INDEX IX_tbl_payroll_transactions_school_year ON [dbo].[tbl_payroll_transactions]([school_year])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_payroll_transactions_pay_period' AND object_id = OBJECT_ID('dbo.tbl_payroll_transactions'))
                        CREATE INDEX IX_tbl_payroll_transactions_pay_period ON [dbo].[tbl_payroll_transactions]([pay_period])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_payroll_transactions_status' AND object_id = OBJECT_ID('dbo.tbl_payroll_transactions'))
                        CREATE INDEX IX_tbl_payroll_transactions_status ON [dbo].[tbl_payroll_transactions]([status])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_payroll_transactions_processed_by' AND object_id = OBJECT_ID('dbo.tbl_payroll_transactions'))
                        CREATE INDEX IX_tbl_payroll_transactions_processed_by ON [dbo].[tbl_payroll_transactions]([processed_by])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_payroll_transactions_created_at' AND object_id = OBJECT_ID('dbo.tbl_payroll_transactions'))
                        CREATE INDEX IX_tbl_payroll_transactions_created_at ON [dbo].[tbl_payroll_transactions]([created_at])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_payroll_transactions_user_schoolyear_payperiod' AND object_id = OBJECT_ID('dbo.tbl_payroll_transactions'))
                        CREATE INDEX IX_tbl_payroll_transactions_user_schoolyear_payperiod ON [dbo].[tbl_payroll_transactions]([user_id], [school_year], [pay_period])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_payroll_transactions_batch_timestamp' AND object_id = OBJECT_ID('dbo.tbl_payroll_transactions'))
                        CREATE INDEX IX_tbl_payroll_transactions_batch_timestamp ON [dbo].[tbl_payroll_transactions]([batch_timestamp])",
                    @"
                        -- Filtered unique index: allows multiple cancelled records but only one active record per user_id + pay_period
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly' AND object_id = OBJECT_ID('dbo.tbl_payroll_transactions'))
                        CREATE UNIQUE NONCLUSTERED INDEX UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly
                        ON [dbo].[tbl_payroll_transactions]([user_id], [pay_period])
                        WHERE [status] IN ('Pending', 'Pending Approval', 'Paid')"
                }
            };
        }

        // Creates tbl_TimeRecords table - tracks employee attendance/time records
        // Must be created after tbl_Users (foreign key dependency)
        public static TableDefinition GetTimeRecordsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_TimeRecords",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_TimeRecords](
                        [time_record_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [user_id] INT NOT NULL,
                        [period] VARCHAR(20) NOT NULL, -- e.g., '2025-01' for January 2025
                        [time_in] VARCHAR(10) NULL, -- HH:mm format
                        [time_out] VARCHAR(10) NULL, -- HH:mm format
                        [regular_hours] DECIMAL(5,2) NOT NULL DEFAULT 0.00,
                        [overtime_hours] DECIMAL(5,2) NOT NULL DEFAULT 0.00,
                        [leave_days] DECIMAL(4,1) NOT NULL DEFAULT 0.0,
                        [late_minutes] INT NOT NULL DEFAULT 0,
                        [total_days_absent] INT NOT NULL DEFAULT 0,
                        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        [updated_at] DATETIME NULL,
                        CONSTRAINT FK_tbl_TimeRecords_tbl_Users FOREIGN KEY ([user_id]) 
                            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE CASCADE,
                        CONSTRAINT UQ_tbl_TimeRecords_user_period UNIQUE ([user_id], [period])
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_TimeRecords_user_id' AND object_id = OBJECT_ID('dbo.tbl_TimeRecords'))
                        CREATE INDEX IX_tbl_TimeRecords_user_id ON [dbo].[tbl_TimeRecords]([user_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_TimeRecords_period' AND object_id = OBJECT_ID('dbo.tbl_TimeRecords'))
                        CREATE INDEX IX_tbl_TimeRecords_period ON [dbo].[tbl_TimeRecords]([period])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_TimeRecords_user_period' AND object_id = OBJECT_ID('dbo.tbl_TimeRecords'))
                        CREATE INDEX IX_tbl_TimeRecords_user_period ON [dbo].[tbl_TimeRecords]([user_id], [period])"
                }
            };
        }

        // Creates tbl_Expenses table - standalone finance table for school expenses
        public static TableDefinition GetExpensesTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_Expenses",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_Expenses](
                        [expense_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [expense_code] VARCHAR(40) NOT NULL UNIQUE,
                        [category] VARCHAR(50) NOT NULL,
                        [description] VARCHAR(500) NULL,
                        [amount] DECIMAL(18,2) NOT NULL,
                        [expense_date] DATE NOT NULL,
                        [payee] VARCHAR(150) NULL,
                        [or_number] VARCHAR(50) NULL,
                        [payment_method] VARCHAR(30) NOT NULL,
                        [status] VARCHAR(20) NOT NULL DEFAULT 'Pending',
                        [recorded_by] VARCHAR(100) NULL,
                        [approved_by] VARCHAR(100) NULL,
                        [school_year] VARCHAR(20) NULL,
                        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        [updated_at] DATETIME NULL
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Expenses_ExpenseDate' AND object_id = OBJECT_ID('dbo.tbl_Expenses'))
                        CREATE INDEX IX_tbl_Expenses_ExpenseDate ON [dbo].[tbl_Expenses]([expense_date])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Expenses_Category' AND object_id = OBJECT_ID('dbo.tbl_Expenses'))
                        CREATE INDEX IX_tbl_Expenses_Category ON [dbo].[tbl_Expenses]([category])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Expenses_Status' AND object_id = OBJECT_ID('dbo.tbl_Expenses'))
                        CREATE INDEX IX_tbl_Expenses_Status ON [dbo].[tbl_Expenses]([status])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Expenses_school_year' AND object_id = OBJECT_ID('dbo.tbl_Expenses'))
                        CREATE INDEX IX_tbl_Expenses_school_year ON [dbo].[tbl_Expenses]([school_year])"
                }
            };
        }

        // Creates tbl_ExpenseAttachments table - stores uploaded receipts for expenses
        public static TableDefinition GetExpenseAttachmentsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_ExpenseAttachments",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_ExpenseAttachments](
                        [attachment_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [expense_ID] INT NOT NULL,
                        [file_name] VARCHAR(255) NOT NULL,
                        [file_path] VARCHAR(500) NOT NULL,
                        [uploaded_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_tbl_ExpenseAttachments_tbl_Expenses FOREIGN KEY ([expense_ID]) REFERENCES [dbo].[tbl_Expenses]([expense_ID]) ON DELETE CASCADE
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_ExpenseAttachments_ExpenseId' AND object_id = OBJECT_ID('dbo.tbl_ExpenseAttachments'))
                        CREATE INDEX IX_tbl_ExpenseAttachments_ExpenseId ON [dbo].[tbl_ExpenseAttachments]([expense_ID])"
                }
            };
        }

        // Creates tbl_StudentPayments table - logs all student payments with OR numbers
        public static TableDefinition GetStudentPaymentsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_StudentPayments",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_StudentPayments](
                        [payment_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [student_id] VARCHAR(6) NOT NULL,
                        [amount] DECIMAL(18,2) NOT NULL,
                        [payment_method] VARCHAR(50) NOT NULL,
                        [or_number] VARCHAR(50) NOT NULL,
                        [processed_by] VARCHAR(50) NULL,
                        [school_year] VARCHAR(20) NULL,
                        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_tbl_StudentPayments_tbl_Students FOREIGN KEY ([student_id]) REFERENCES [dbo].[tbl_Students]([student_id]) ON DELETE CASCADE
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentPayments_StudentId' AND object_id = OBJECT_ID('dbo.tbl_StudentPayments'))
                        CREATE INDEX IX_tbl_StudentPayments_StudentId ON [dbo].[tbl_StudentPayments]([student_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentPayments_OrNumber' AND object_id = OBJECT_ID('dbo.tbl_StudentPayments'))
                        CREATE UNIQUE INDEX IX_tbl_StudentPayments_OrNumber ON [dbo].[tbl_StudentPayments]([or_number])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentPayments_CreatedAt' AND object_id = OBJECT_ID('dbo.tbl_StudentPayments'))
                        CREATE INDEX IX_tbl_StudentPayments_CreatedAt ON [dbo].[tbl_StudentPayments]([created_at])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentPayments_school_year' AND object_id = OBJECT_ID('dbo.tbl_StudentPayments'))
                        CREATE INDEX IX_tbl_StudentPayments_school_year ON [dbo].[tbl_StudentPayments]([school_year])
                        WHERE [school_year] IS NOT NULL"
                }
            };
        }

        // Creates tbl_StudentLedgers table - ledger per student per school year
        public static TableDefinition GetStudentLedgersTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_StudentLedgers",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_StudentLedgers](
                        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [student_id] VARCHAR(6) NOT NULL,
                        [school_year] VARCHAR(20) NOT NULL,
                        [grade_level] VARCHAR(50) NULL,
                        [status] VARCHAR(20) NOT NULL DEFAULT 'Unpaid',
                        [total_charges] DECIMAL(18,2) NOT NULL DEFAULT 0,
                        [total_payments] DECIMAL(18,2) NOT NULL DEFAULT 0,
                        [balance] DECIMAL(18,2) NOT NULL DEFAULT 0,
                        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        [updated_at] DATETIME NULL,
                        CONSTRAINT FK_tbl_StudentLedgers_tbl_Students FOREIGN KEY ([student_id]) REFERENCES [dbo].[tbl_Students]([student_id]) ON DELETE CASCADE
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentLedgers_StudentId_SchoolYear' AND object_id = OBJECT_ID('dbo.tbl_StudentLedgers'))
                        CREATE UNIQUE INDEX IX_tbl_StudentLedgers_StudentId_SchoolYear ON [dbo].[tbl_StudentLedgers]([student_id], [school_year])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentLedgers_StudentId' AND object_id = OBJECT_ID('dbo.tbl_StudentLedgers'))
                        CREATE INDEX IX_tbl_StudentLedgers_StudentId ON [dbo].[tbl_StudentLedgers]([student_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentLedgers_SchoolYear' AND object_id = OBJECT_ID('dbo.tbl_StudentLedgers'))
                        CREATE INDEX IX_tbl_StudentLedgers_SchoolYear ON [dbo].[tbl_StudentLedgers]([school_year])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentLedgers_Status' AND object_id = OBJECT_ID('dbo.tbl_StudentLedgers'))
                        CREATE INDEX IX_tbl_StudentLedgers_Status ON [dbo].[tbl_StudentLedgers]([status])"
                }
            };
        }

        // Creates tbl_LedgerCharges table - individual charges and discounts in a ledger
        public static TableDefinition GetLedgerChargesTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_LedgerCharges",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_LedgerCharges](
                        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [ledger_id] INT NOT NULL,
                        [charge_type] VARCHAR(50) NOT NULL,
                        [description] VARCHAR(500) NULL,
                        [amount] DECIMAL(18,2) NOT NULL,
                        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        [updated_at] DATETIME NULL,
                        [discount_id] INT NULL,
                        CONSTRAINT FK_tbl_LedgerCharges_tbl_StudentLedgers FOREIGN KEY ([ledger_id]) REFERENCES [dbo].[tbl_StudentLedgers]([id]) ON DELETE CASCADE,
                        CONSTRAINT FK_tbl_LedgerCharges_tbl_discounts FOREIGN KEY ([discount_id]) REFERENCES [dbo].[tbl_discounts]([discount_id]) ON DELETE SET NULL
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_LedgerCharges_LedgerId' AND object_id = OBJECT_ID('dbo.tbl_LedgerCharges'))
                        CREATE INDEX IX_tbl_LedgerCharges_LedgerId ON [dbo].[tbl_LedgerCharges]([ledger_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_LedgerCharges_ChargeType' AND object_id = OBJECT_ID('dbo.tbl_LedgerCharges'))
                        CREATE INDEX IX_tbl_LedgerCharges_ChargeType ON [dbo].[tbl_LedgerCharges]([charge_type])"
                }
            };
        }

        // Creates tbl_discounts table - standalone table for discount configuration
        public static TableDefinition GetDiscountsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_discounts",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_discounts](
                        [discount_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [discount_type] VARCHAR(50) NOT NULL UNIQUE,
                        [discount_name] VARCHAR(100) NOT NULL,
                        [rate_or_value] DECIMAL(12,4) NOT NULL,
                        [is_percentage] BIT NOT NULL DEFAULT 1,
                        [max_amount] DECIMAL(12,2) NULL,
                        [min_amount] DECIMAL(12,2) NULL,
                        [description] VARCHAR(500) NULL,
                        [is_active] BIT NOT NULL DEFAULT 1,
                        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
                        [updated_date] DATETIME NULL
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_discounts_discount_type' AND object_id = OBJECT_ID('dbo.tbl_discounts'))
                        CREATE INDEX IX_tbl_discounts_discount_type ON [dbo].[tbl_discounts]([discount_type])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_discounts_is_active' AND object_id = OBJECT_ID('dbo.tbl_discounts'))
                        CREATE INDEX IX_tbl_discounts_is_active ON [dbo].[tbl_discounts]([is_active])"
                }
            };
        }

        // Creates tbl_LedgerPayments table - payment records linked to a ledger
        public static TableDefinition GetLedgerPaymentsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_LedgerPayments",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_LedgerPayments](
                        [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [ledger_id] INT NOT NULL,
                        [amount] DECIMAL(18,2) NOT NULL,
                        [or_number] VARCHAR(50) NOT NULL,
                        [payment_method] VARCHAR(50) NOT NULL,
                        [processed_by] VARCHAR(100) NULL,
                        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_tbl_LedgerPayments_tbl_StudentLedgers FOREIGN KEY ([ledger_id]) REFERENCES [dbo].[tbl_StudentLedgers]([id]) ON DELETE CASCADE
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_LedgerPayments_LedgerId' AND object_id = OBJECT_ID('dbo.tbl_LedgerPayments'))
                        CREATE INDEX IX_tbl_LedgerPayments_LedgerId ON [dbo].[tbl_LedgerPayments]([ledger_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_LedgerPayments_OrNumber' AND object_id = OBJECT_ID('dbo.tbl_LedgerPayments'))
                        CREATE UNIQUE INDEX IX_tbl_LedgerPayments_OrNumber ON [dbo].[tbl_LedgerPayments]([or_number])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_LedgerPayments_CreatedAt' AND object_id = OBJECT_ID('dbo.tbl_LedgerPayments'))
                        CREATE INDEX IX_tbl_LedgerPayments_CreatedAt ON [dbo].[tbl_LedgerPayments]([created_at])"
                }
            };
        }

        // Creates tbl_Assets table - asset master data
        public static TableDefinition GetAssetsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_Assets",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_Assets](
                        [asset_id] VARCHAR(50) NOT NULL PRIMARY KEY,
                        [asset_name] VARCHAR(200) NOT NULL,
                        [category] VARCHAR(100) NULL,
                        [brand] VARCHAR(100) NULL,
                        [model] VARCHAR(100) NULL,
                        [serial_number] VARCHAR(100) NULL,
                        [location] VARCHAR(100) NOT NULL,
                        [status] VARCHAR(50) NOT NULL DEFAULT 'Available',
                        [purchase_date] DATE NULL,
                        [purchase_cost] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                        [current_value] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                        [description] VARCHAR(500) NULL,
                        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
                        [updated_date] DATETIME NULL,
                        [is_active] BIT NOT NULL DEFAULT 1
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Assets_AssetId' AND object_id = OBJECT_ID('dbo.tbl_Assets'))
                        CREATE UNIQUE INDEX IX_tbl_Assets_AssetId ON [dbo].[tbl_Assets]([asset_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Assets_Category' AND object_id = OBJECT_ID('dbo.tbl_Assets'))
                        CREATE INDEX IX_tbl_Assets_Category ON [dbo].[tbl_Assets]([category])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Assets_Status' AND object_id = OBJECT_ID('dbo.tbl_Assets'))
                        CREATE INDEX IX_tbl_Assets_Status ON [dbo].[tbl_Assets]([status])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Assets_Location' AND object_id = OBJECT_ID('dbo.tbl_Assets'))
                        CREATE INDEX IX_tbl_Assets_Location ON [dbo].[tbl_Assets]([location])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Assets_IsActive' AND object_id = OBJECT_ID('dbo.tbl_Assets'))
                        CREATE INDEX IX_tbl_Assets_IsActive ON [dbo].[tbl_Assets]([is_active])"
                }
            };
        }

        // Creates tbl_InventoryItems table - inventory item master data
        public static TableDefinition GetInventoryItemsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_InventoryItems",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_InventoryItems](
                        [item_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [item_code] VARCHAR(50) NOT NULL UNIQUE,
                        [item_name] VARCHAR(200) NOT NULL,
                        [category] VARCHAR(100) NULL,
                        [unit] VARCHAR(50) NOT NULL DEFAULT 'Piece',
                        [quantity] INT NOT NULL DEFAULT 0,
                        [reorder_level] INT NOT NULL DEFAULT 10,
                        [max_stock] INT NOT NULL DEFAULT 1000,
                        [unit_price] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                        [supplier] VARCHAR(200) NULL,
                        [description] VARCHAR(500) NULL,
                        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
                        [updated_date] DATETIME NULL,
                        [is_active] BIT NOT NULL DEFAULT 1
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_InventoryItems_ItemCode' AND object_id = OBJECT_ID('dbo.tbl_InventoryItems'))
                        CREATE UNIQUE INDEX IX_tbl_InventoryItems_ItemCode ON [dbo].[tbl_InventoryItems]([item_code])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_InventoryItems_Category' AND object_id = OBJECT_ID('dbo.tbl_InventoryItems'))
                        CREATE INDEX IX_tbl_InventoryItems_Category ON [dbo].[tbl_InventoryItems]([category])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_InventoryItems_IsActive' AND object_id = OBJECT_ID('dbo.tbl_InventoryItems'))
                        CREATE INDEX IX_tbl_InventoryItems_IsActive ON [dbo].[tbl_InventoryItems]([is_active])"
                }
            };
        }

        // Creates tbl_AssetAssignments table - asset assignment tracking
        public static TableDefinition GetAssetAssignmentsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_AssetAssignments",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_AssetAssignments](
                        [assignment_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [asset_id] VARCHAR(50) NOT NULL,
                        [assigned_to_type] VARCHAR(50) NOT NULL,
                        [assigned_to_id] VARCHAR(50) NULL,
                        [assigned_to_name] VARCHAR(200) NULL,
                        [assigned_date] DATETIME NOT NULL DEFAULT GETDATE(),
                        [return_date] DATETIME NULL,
                        [notes] VARCHAR(500) NULL,
                        [status] VARCHAR(50) NOT NULL DEFAULT 'Active',
                        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_tbl_AssetAssignments_tbl_Assets FOREIGN KEY ([asset_id]) REFERENCES [dbo].[tbl_Assets]([asset_id]) ON DELETE CASCADE
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_AssetAssignments_AssetId' AND object_id = OBJECT_ID('dbo.tbl_AssetAssignments'))
                        CREATE INDEX IX_tbl_AssetAssignments_AssetId ON [dbo].[tbl_AssetAssignments]([asset_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_AssetAssignments_AssignedToId' AND object_id = OBJECT_ID('dbo.tbl_AssetAssignments'))
                        CREATE INDEX IX_tbl_AssetAssignments_AssignedToId ON [dbo].[tbl_AssetAssignments]([assigned_to_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_AssetAssignments_Status' AND object_id = OBJECT_ID('dbo.tbl_AssetAssignments'))
                        CREATE INDEX IX_tbl_AssetAssignments_Status ON [dbo].[tbl_AssetAssignments]([status])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_AssetAssignments_AssignedDate' AND object_id = OBJECT_ID('dbo.tbl_AssetAssignments'))
                        CREATE INDEX IX_tbl_AssetAssignments_AssignedDate ON [dbo].[tbl_AssetAssignments]([assigned_date])"
                }
            };
        }

        // Creates tbl_student_status_logs table - student status change audit trail
        public static TableDefinition GetStudentStatusLogsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_student_status_logs",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_student_status_logs](
                        [log_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [student_id] VARCHAR(6) NOT NULL,
                        [old_status] VARCHAR(20) NOT NULL,
                        [new_status] VARCHAR(20) NOT NULL,
                        [changed_by] INT NULL,
                        [changed_by_name] VARCHAR(100) NULL,
                        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_tbl_student_status_logs_tbl_Students FOREIGN KEY ([student_id]) REFERENCES [dbo].[tbl_Students]([student_id]) ON DELETE CASCADE,
                        CONSTRAINT FK_tbl_student_status_logs_tbl_Users FOREIGN KEY ([changed_by]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE SET NULL
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_student_status_logs_StudentId' AND object_id = OBJECT_ID('dbo.tbl_student_status_logs'))
                        CREATE INDEX IX_tbl_student_status_logs_StudentId ON [dbo].[tbl_student_status_logs]([student_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_student_status_logs_CreatedAt' AND object_id = OBJECT_ID('dbo.tbl_student_status_logs'))
                        CREATE INDEX IX_tbl_student_status_logs_CreatedAt ON [dbo].[tbl_student_status_logs]([created_at])"
                }
            };
        }

        // Creates tbl_audit_logs table - enhanced audit logging with student registration details
        public static TableDefinition GetAuditLogsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_audit_logs",
                SchemaName = "dbo",
                CreateTableScript = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_audit_logs' AND schema_id = SCHEMA_ID('dbo'))
                    BEGIN
                        CREATE TABLE [dbo].[tbl_audit_logs](
                            [log_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [timestamp] DATETIME NOT NULL DEFAULT GETDATE(),
                            [user_name] VARCHAR(100) NULL,
                            [user_role] VARCHAR(50) NULL,
                            [user_id] INT NULL,
                            [action] VARCHAR(100) NOT NULL,
                            [module] VARCHAR(100) NULL,
                            [description] NVARCHAR(MAX) NULL,
                            [ip_address] VARCHAR(45) NULL,
                            [status] VARCHAR(20) NULL,
                            [severity] VARCHAR(20) NULL,
                            [entity_type] VARCHAR(100) NULL,
                            [entity_id] VARCHAR(50) NULL,
                            [old_values] NVARCHAR(MAX) NULL,
                            [new_values] NVARCHAR(MAX) NULL,
                            [student_id] VARCHAR(6) NULL,
                            [student_name] VARCHAR(200) NULL,
                            [grade] VARCHAR(10) NULL,
                            [student_status] VARCHAR(20) NULL,
                            [registrar_id] INT NULL
                        );
                    END",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_audit_logs_Timestamp' AND object_id = OBJECT_ID('dbo.tbl_audit_logs'))
                        CREATE INDEX IX_tbl_audit_logs_Timestamp ON [dbo].[tbl_audit_logs]([timestamp] DESC)",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_audit_logs_Module' AND object_id = OBJECT_ID('dbo.tbl_audit_logs'))
                        CREATE INDEX IX_tbl_audit_logs_Module ON [dbo].[tbl_audit_logs]([module])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_audit_logs_Action' AND object_id = OBJECT_ID('dbo.tbl_audit_logs'))
                        CREATE INDEX IX_tbl_audit_logs_Action ON [dbo].[tbl_audit_logs]([action])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_audit_logs_StudentId' AND object_id = OBJECT_ID('dbo.tbl_audit_logs'))
                        CREATE INDEX IX_tbl_audit_logs_StudentId ON [dbo].[tbl_audit_logs]([student_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_audit_logs_RegistrarId' AND object_id = OBJECT_ID('dbo.tbl_audit_logs'))
                        CREATE INDEX IX_tbl_audit_logs_RegistrarId ON [dbo].[tbl_audit_logs]([registrar_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_audit_logs_Module_Timestamp' AND object_id = OBJECT_ID('dbo.tbl_audit_logs'))
                        CREATE INDEX IX_tbl_audit_logs_Module_Timestamp ON [dbo].[tbl_audit_logs]([module], [timestamp] DESC)"
                }
            };
        }

        // Creates tbl_TeacherActivityLogs table - must be created after tbl_Users
        public static TableDefinition GetTeacherActivityLogsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_TeacherActivityLogs",
                SchemaName = "dbo",
                CreateTableScript = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_TeacherActivityLogs' AND schema_id = SCHEMA_ID('dbo'))
                    BEGIN
                        CREATE TABLE [dbo].[tbl_TeacherActivityLogs](
                            [id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [teacher_id] INT NOT NULL,
                            [action] VARCHAR(100) NOT NULL,
                            [details] NVARCHAR(MAX) NULL,
                            [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                            CONSTRAINT FK_tbl_TeacherActivityLogs_tbl_Users FOREIGN KEY ([teacher_id]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE CASCADE
                        );
                    END",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_TeacherActivityLogs_TeacherId' AND object_id = OBJECT_ID('dbo.tbl_TeacherActivityLogs'))
                        CREATE INDEX IX_tbl_TeacherActivityLogs_TeacherId ON [dbo].[tbl_TeacherActivityLogs]([teacher_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_TeacherActivityLogs_CreatedAt' AND object_id = OBJECT_ID('dbo.tbl_TeacherActivityLogs'))
                        CREATE INDEX IX_tbl_TeacherActivityLogs_CreatedAt ON [dbo].[tbl_TeacherActivityLogs]([created_at] DESC)",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_TeacherActivityLogs_TeacherId_CreatedAt' AND object_id = OBJECT_ID('dbo.tbl_TeacherActivityLogs'))
                        CREATE INDEX IX_tbl_TeacherActivityLogs_TeacherId_CreatedAt ON [dbo].[tbl_TeacherActivityLogs]([teacher_id], [created_at] DESC)"
                }
            };
        }

        // Creates tbl_Attendance table - must be created after tbl_Users, tbl_Students, tbl_Sections, tbl_Subjects
        public static TableDefinition GetAttendanceTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_Attendance",
                SchemaName = "dbo",
                CreateTableScript = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Attendance' AND schema_id = SCHEMA_ID('dbo'))
                    BEGIN
                        CREATE TABLE [dbo].[tbl_Attendance](
                            [AttendanceID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [StudentID] VARCHAR(6) NOT NULL,
                            [SectionID] INT NOT NULL,
                            [SubjectID] INT NULL,
                            [AttendanceDate] DATE NOT NULL,
                            [Status] VARCHAR(20) NOT NULL,
                            [TimeIn] TIME NULL,
                            [TimeOut] TIME NULL,
                            [Remarks] VARCHAR(500) NULL,
                            [TeacherID] INT NOT NULL,
                            [SchoolYear] VARCHAR(20) NOT NULL,
                            [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                            [UpdatedAt] DATETIME NULL,
                            CONSTRAINT FK_tbl_Attendance_tbl_Students FOREIGN KEY ([StudentID]) REFERENCES [dbo].[tbl_Students]([student_id]) ON DELETE CASCADE,
                            CONSTRAINT FK_tbl_Attendance_tbl_Sections FOREIGN KEY ([SectionID]) REFERENCES [dbo].[tbl_Sections]([SectionID]),
                            CONSTRAINT FK_tbl_Attendance_tbl_Subjects FOREIGN KEY ([SubjectID]) REFERENCES [dbo].[tbl_Subjects]([SubjectID]) ON DELETE SET NULL,
                            CONSTRAINT FK_tbl_Attendance_tbl_Users FOREIGN KEY ([TeacherID]) REFERENCES [dbo].[tbl_Users]([user_ID])
                        );
                    END",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Attendance_StudentID' AND object_id = OBJECT_ID('dbo.tbl_Attendance'))
                        CREATE INDEX IX_tbl_Attendance_StudentID ON [dbo].[tbl_Attendance]([StudentID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Attendance_SectionID' AND object_id = OBJECT_ID('dbo.tbl_Attendance'))
                        CREATE INDEX IX_tbl_Attendance_SectionID ON [dbo].[tbl_Attendance]([SectionID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Attendance_AttendanceDate' AND object_id = OBJECT_ID('dbo.tbl_Attendance'))
                        CREATE INDEX IX_tbl_Attendance_AttendanceDate ON [dbo].[tbl_Attendance]([AttendanceDate])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Attendance_TeacherID' AND object_id = OBJECT_ID('dbo.tbl_Attendance'))
                        CREATE INDEX IX_tbl_Attendance_TeacherID ON [dbo].[tbl_Attendance]([TeacherID])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Attendance_SectionID_AttendanceDate' AND object_id = OBJECT_ID('dbo.tbl_Attendance'))
                        CREATE INDEX IX_tbl_Attendance_SectionID_AttendanceDate ON [dbo].[tbl_Attendance]([SectionID], [AttendanceDate])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Attendance_StudentID_AttendanceDate' AND object_id = OBJECT_ID('dbo.tbl_Attendance'))
                        CREATE INDEX IX_tbl_Attendance_StudentID_AttendanceDate ON [dbo].[tbl_Attendance]([StudentID], [AttendanceDate])"
                }
            };
        }

        // Creates tbl_SchoolYear table - manages school years with open/closed status
        public static TableDefinition GetSchoolYearTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_SchoolYear",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_SchoolYear](
                        [school_year_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [school_year] VARCHAR(20) NOT NULL UNIQUE,
                        [is_active] BIT NOT NULL DEFAULT 0,
                        [is_open] BIT NOT NULL DEFAULT 0,
                        [start_date] DATE NULL,
                        [end_date] DATE NULL,
                        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        [opened_at] DATETIME NULL,
                        [closed_at] DATETIME NULL
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SchoolYear_school_year' AND object_id = OBJECT_ID('dbo.tbl_SchoolYear'))
                        CREATE UNIQUE INDEX IX_tbl_SchoolYear_school_year ON [dbo].[tbl_SchoolYear]([school_year])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SchoolYear_is_active' AND object_id = OBJECT_ID('dbo.tbl_SchoolYear'))
                        CREATE INDEX IX_tbl_SchoolYear_is_active ON [dbo].[tbl_SchoolYear]([is_active])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SchoolYear_is_open' AND object_id = OBJECT_ID('dbo.tbl_SchoolYear'))
                        CREATE INDEX IX_tbl_SchoolYear_is_open ON [dbo].[tbl_SchoolYear]([is_open])"
                }
            };
        }

        // Creates tbl_ChartOfAccounts table - Foundation of double-entry bookkeeping
        public static TableDefinition GetChartOfAccountsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_ChartOfAccounts",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_ChartOfAccounts](
                        [account_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [account_code] VARCHAR(20) NOT NULL UNIQUE,
                        [account_name] VARCHAR(200) NOT NULL,
                        [account_type] VARCHAR(50) NOT NULL,
                        [parent_account_id] INT NULL,
                        [normal_balance] VARCHAR(10) NOT NULL DEFAULT 'Debit',
                        [is_active] BIT NOT NULL DEFAULT 1,
                        [description] VARCHAR(500) NULL,
                        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        [updated_at] DATETIME NULL,
                        CONSTRAINT FK_tbl_ChartOfAccounts_Parent FOREIGN KEY ([parent_account_id]) 
                            REFERENCES [dbo].[tbl_ChartOfAccounts]([account_id]) ON DELETE NO ACTION,
                        CONSTRAINT CK_tbl_ChartOfAccounts_AccountType CHECK ([account_type] IN ('Asset', 'Liability', 'Equity', 'Revenue', 'Expense')),
                        CONSTRAINT CK_tbl_ChartOfAccounts_NormalBalance CHECK ([normal_balance] IN ('Debit', 'Credit'))
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_ChartOfAccounts_AccountCode' AND object_id = OBJECT_ID('dbo.tbl_ChartOfAccounts'))
                        CREATE UNIQUE INDEX IX_tbl_ChartOfAccounts_AccountCode ON [dbo].[tbl_ChartOfAccounts]([account_code])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_ChartOfAccounts_AccountType' AND object_id = OBJECT_ID('dbo.tbl_ChartOfAccounts'))
                        CREATE INDEX IX_tbl_ChartOfAccounts_AccountType ON [dbo].[tbl_ChartOfAccounts]([account_type])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_ChartOfAccounts_IsActive' AND object_id = OBJECT_ID('dbo.tbl_ChartOfAccounts'))
                        CREATE INDEX IX_tbl_ChartOfAccounts_IsActive ON [dbo].[tbl_ChartOfAccounts]([is_active])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_ChartOfAccounts_ParentAccountId' AND object_id = OBJECT_ID('dbo.tbl_ChartOfAccounts'))
                        CREATE INDEX IX_tbl_ChartOfAccounts_ParentAccountId ON [dbo].[tbl_ChartOfAccounts]([parent_account_id])"
                }
            };
        }

        // Creates tbl_JournalEntries table - Journal entry headers
        public static TableDefinition GetJournalEntriesTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_JournalEntries",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_JournalEntries](
                        [journal_entry_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [entry_number] VARCHAR(50) NOT NULL UNIQUE,
                        [entry_date] DATE NOT NULL,
                        [description] VARCHAR(500) NULL,
                        [reference_type] VARCHAR(50) NOT NULL,
                        [reference_id] INT NULL,
                        [status] VARCHAR(20) NOT NULL DEFAULT 'Posted',
                        [created_by] INT NULL,
                        [approved_by] INT NULL,
                        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        [updated_at] DATETIME NULL,
                        CONSTRAINT FK_tbl_JournalEntries_CreatedBy FOREIGN KEY ([created_by]) 
                            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
                        CONSTRAINT FK_tbl_JournalEntries_ApprovedBy FOREIGN KEY ([approved_by]) 
                            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
                        CONSTRAINT CK_tbl_JournalEntries_Status CHECK ([status] IN ('Draft', 'Posted', 'Reversed'))
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_JournalEntries_EntryNumber' AND object_id = OBJECT_ID('dbo.tbl_JournalEntries'))
                        CREATE UNIQUE INDEX IX_tbl_JournalEntries_EntryNumber ON [dbo].[tbl_JournalEntries]([entry_number])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_JournalEntries_EntryDate' AND object_id = OBJECT_ID('dbo.tbl_JournalEntries'))
                        CREATE INDEX IX_tbl_JournalEntries_EntryDate ON [dbo].[tbl_JournalEntries]([entry_date])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_JournalEntries_ReferenceType' AND object_id = OBJECT_ID('dbo.tbl_JournalEntries'))
                        CREATE INDEX IX_tbl_JournalEntries_ReferenceType ON [dbo].[tbl_JournalEntries]([reference_type])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_JournalEntries_ReferenceId' AND object_id = OBJECT_ID('dbo.tbl_JournalEntries'))
                        CREATE INDEX IX_tbl_JournalEntries_ReferenceId ON [dbo].[tbl_JournalEntries]([reference_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_JournalEntries_Status' AND object_id = OBJECT_ID('dbo.tbl_JournalEntries'))
                        CREATE INDEX IX_tbl_JournalEntries_Status ON [dbo].[tbl_JournalEntries]([status])"
                }
            };
        }

        // Creates tbl_JournalEntryLines table - Individual debit/credit entries
        public static TableDefinition GetJournalEntryLinesTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_JournalEntryLines",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_JournalEntryLines](
                        [line_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [journal_entry_id] INT NOT NULL,
                        [account_id] INT NOT NULL,
                        [line_number] INT NOT NULL,
                        [debit_amount] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                        [credit_amount] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
                        [description] VARCHAR(500) NULL,
                        CONSTRAINT FK_tbl_JournalEntryLines_JournalEntry FOREIGN KEY ([journal_entry_id]) 
                            REFERENCES [dbo].[tbl_JournalEntries]([journal_entry_id]) ON DELETE CASCADE,
                        CONSTRAINT FK_tbl_JournalEntryLines_Account FOREIGN KEY ([account_id]) 
                            REFERENCES [dbo].[tbl_ChartOfAccounts]([account_id]) ON DELETE NO ACTION
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_JournalEntryLines_JournalEntryId' AND object_id = OBJECT_ID('dbo.tbl_JournalEntryLines'))
                        CREATE INDEX IX_tbl_JournalEntryLines_JournalEntryId ON [dbo].[tbl_JournalEntryLines]([journal_entry_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_JournalEntryLines_AccountId' AND object_id = OBJECT_ID('dbo.tbl_JournalEntryLines'))
                        CREATE INDEX IX_tbl_JournalEntryLines_AccountId ON [dbo].[tbl_JournalEntryLines]([account_id])"
                }
            };
        }

        // Creates tbl_AccountingPeriods table - Period closing management
        public static TableDefinition GetAccountingPeriodsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_AccountingPeriods",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_AccountingPeriods](
                        [period_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [period_year] INT NOT NULL,
                        [period_month] INT NOT NULL,
                        [period_name] VARCHAR(50) NOT NULL,
                        [start_date] DATE NOT NULL,
                        [end_date] DATE NOT NULL,
                        [is_closed] BIT NOT NULL DEFAULT 0,
                        [closed_by] INT NULL,
                        [closed_at] DATETIME NULL,
                        [closing_notes] VARCHAR(500) NULL,
                        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        [updated_at] DATETIME NULL,
                        CONSTRAINT FK_tbl_AccountingPeriods_ClosedBy FOREIGN KEY ([closed_by]) 
                            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
                        CONSTRAINT CK_tbl_AccountingPeriods_Month CHECK ([period_month] >= 1 AND [period_month] <= 12),
                        CONSTRAINT CK_tbl_AccountingPeriods_Year CHECK ([period_year] >= 2000 AND [period_year] <= 2100)
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_AccountingPeriods_YearMonth' AND object_id = OBJECT_ID('dbo.tbl_AccountingPeriods'))
                        CREATE UNIQUE INDEX IX_tbl_AccountingPeriods_YearMonth ON [dbo].[tbl_AccountingPeriods]([period_year], [period_month])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_AccountingPeriods_IsClosed' AND object_id = OBJECT_ID('dbo.tbl_AccountingPeriods'))
                        CREATE INDEX IX_tbl_AccountingPeriods_IsClosed ON [dbo].[tbl_AccountingPeriods]([is_closed])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_AccountingPeriods_StartDate' AND object_id = OBJECT_ID('dbo.tbl_AccountingPeriods'))
                        CREATE INDEX IX_tbl_AccountingPeriods_StartDate ON [dbo].[tbl_AccountingPeriods]([start_date])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_AccountingPeriods_EndDate' AND object_id = OBJECT_ID('dbo.tbl_AccountingPeriods'))
                        CREATE INDEX IX_tbl_AccountingPeriods_EndDate ON [dbo].[tbl_AccountingPeriods]([end_date])"
                }
            };
        }

        public static TableDefinition GetNotificationsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_Notifications",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_Notifications](
                        [notification_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [notification_type] VARCHAR(50) NOT NULL,
                        [title] VARCHAR(100) NOT NULL,
                        [message] NVARCHAR(500) NULL,
                        [reference_type] VARCHAR(50) NOT NULL,
                        [reference_id] INT NULL,
                        [is_read] BIT NOT NULL DEFAULT 0,
                        [read_at] DATETIME NULL,
                        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        [action_url] VARCHAR(100) NULL,
                        [priority] VARCHAR(50) NOT NULL DEFAULT 'Normal',
                        [created_by] INT NULL,
                        CONSTRAINT FK_tbl_Notifications_CreatedBy FOREIGN KEY ([created_by]) 
                            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE SET NULL,
                        CONSTRAINT CK_tbl_Notifications_Priority CHECK ([priority] IN ('Low', 'Normal', 'High', 'Urgent'))
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Notifications_IsRead' AND object_id = OBJECT_ID('dbo.tbl_Notifications'))
                        CREATE INDEX IX_tbl_Notifications_IsRead ON [dbo].[tbl_Notifications]([is_read])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Notifications_CreatedAt' AND object_id = OBJECT_ID('dbo.tbl_Notifications'))
                        CREATE INDEX IX_tbl_Notifications_CreatedAt ON [dbo].[tbl_Notifications]([created_at])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Notifications_NotificationType' AND object_id = OBJECT_ID('dbo.tbl_Notifications'))
                        CREATE INDEX IX_tbl_Notifications_NotificationType ON [dbo].[tbl_Notifications]([notification_type])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Notifications_ReferenceType' AND object_id = OBJECT_ID('dbo.tbl_Notifications'))
                        CREATE INDEX IX_tbl_Notifications_ReferenceType ON [dbo].[tbl_Notifications]([reference_type])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Notifications_ReferenceId' AND object_id = OBJECT_ID('dbo.tbl_Notifications'))
                        CREATE INDEX IX_tbl_Notifications_ReferenceId ON [dbo].[tbl_Notifications]([reference_id])"
                }
            };
        }

        public static TableDefinition GetSyncHistoryTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_SyncHistory",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_SyncHistory](
                        [sync_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [sync_type] VARCHAR(50) NOT NULL,
                        [sync_time] DATETIME NOT NULL DEFAULT GETDATE(),
                        [status] VARCHAR(20) NOT NULL DEFAULT 'Success',
                        [records_pushed] INT NOT NULL DEFAULT 0,
                        [records_pulled] INT NOT NULL DEFAULT 0,
                        [message] NVARCHAR(MAX) NULL,
                        [error_details] NVARCHAR(MAX) NULL,
                        [duration_seconds] INT NULL,
                        [initiated_by] VARCHAR(100) NULL,
                        [created_at] DATETIME NOT NULL DEFAULT GETDATE()
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SyncHistory_SyncTime' AND object_id = OBJECT_ID('dbo.tbl_SyncHistory'))
                        CREATE INDEX IX_tbl_SyncHistory_SyncTime ON [dbo].[tbl_SyncHistory]([sync_time])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SyncHistory_SyncType' AND object_id = OBJECT_ID('dbo.tbl_SyncHistory'))
                        CREATE INDEX IX_tbl_SyncHistory_SyncType ON [dbo].[tbl_SyncHistory]([sync_type])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SyncHistory_Status' AND object_id = OBJECT_ID('dbo.tbl_SyncHistory'))
                        CREATE INDEX IX_tbl_SyncHistory_Status ON [dbo].[tbl_SyncHistory]([status])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SyncHistory_SyncType_SyncTime' AND object_id = OBJECT_ID('dbo.tbl_SyncHistory'))
                        CREATE INDEX IX_tbl_SyncHistory_SyncType_SyncTime ON [dbo].[tbl_SyncHistory]([sync_type], [sync_time])"
                }
            };
        }

        public static TableDefinition GetSyncLogsTableDefinition()
        {
            return new TableDefinition
            {
                TableName = "tbl_SyncLogs",
                SchemaName = "dbo",
                CreateTableScript = @"
                    CREATE TABLE [dbo].[tbl_SyncLogs](
                        [log_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [sync_id] INT NULL,
                        [log_type] VARCHAR(50) NOT NULL,
                        [log_message] NVARCHAR(MAX) NOT NULL,
                        [timestamp] DATETIME NOT NULL DEFAULT GETDATE(),
                        [severity] VARCHAR(20) NULL,
                        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_tbl_SyncLogs_tbl_SyncHistory FOREIGN KEY ([sync_id]) 
                            REFERENCES [dbo].[tbl_SyncHistory]([sync_id]) ON DELETE SET NULL
                    )",
                CreateIndexesScripts = new List<string>
                {
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SyncLogs_SyncId' AND object_id = OBJECT_ID('dbo.tbl_SyncLogs'))
                        CREATE INDEX IX_tbl_SyncLogs_SyncId ON [dbo].[tbl_SyncLogs]([sync_id])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SyncLogs_Timestamp' AND object_id = OBJECT_ID('dbo.tbl_SyncLogs'))
                        CREATE INDEX IX_tbl_SyncLogs_Timestamp ON [dbo].[tbl_SyncLogs]([timestamp])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SyncLogs_LogType' AND object_id = OBJECT_ID('dbo.tbl_SyncLogs'))
                        CREATE INDEX IX_tbl_SyncLogs_LogType ON [dbo].[tbl_SyncLogs]([log_type])",
                    @"
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SyncLogs_SyncId_Timestamp' AND object_id = OBJECT_ID('dbo.tbl_SyncLogs'))
                        CREATE INDEX IX_tbl_SyncLogs_SyncId_Timestamp ON [dbo].[tbl_SyncLogs]([sync_id], [timestamp])"
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
                GetStudentViewDefinition(),
                GetFinalClassesViewDefinition()
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

        // Creates tbl_FinalClasses view - flattens section, teacher, subject, schedule, and room info
        public static ViewDefinition GetFinalClassesViewDefinition()
        {
            return new ViewDefinition
            {
                ViewName = "tbl_FinalClasses",
                SchemaName = "dbo",
                CreateViewScript = @"
                    CREATE VIEW [dbo].[tbl_FinalClasses]
                    AS
                    SELECT
                        tsa.[AssignmentID]      AS AssignmentId,
                        tsa.[Role]             AS Role,
                        -- Teacher info
                        CASE 
                            WHEN u.[suffix] IS NOT NULL AND u.[suffix] != '' 
                            THEN u.[first_name] + ' ' + ISNULL(u.[mid_name] + ' ', '') + u.[last_name] + ' ' + u.[suffix]
                            ELSE u.[first_name] + ' ' + ISNULL(u.[mid_name] + ' ', '') + u.[last_name]
                        END                    AS TeacherName,
                        u.[user_ID]            AS TeacherId,
                        -- Section & grade
                        sec.[SectionName]      AS SectionName,
                        gl.[grade_level_name]  AS GradeLevel,
                        -- Subject (may be null for advisers)
                        sub.[SubjectName]      AS SubjectName,
                        sub.[SubjectID]        AS SubjectId,
                        -- Schedule & room
                        cs.[DayOfWeek]         AS DayOfWeek,
                        cs.[StartTime]         AS StartTime,
                        cs.[EndTime]           AS EndTime,
                        ISNULL(cls.[RoomName], '') AS Classroom,
                        cls.[BuildingName]     AS BuildingName,
                        -- Audit
                        tsa.[CreatedAt]        AS AssignmentCreatedAt,
                        tsa.[UpdatedAt]        AS AssignmentUpdatedAt
                    FROM [dbo].[tbl_TeacherSectionAssignment] tsa
                    INNER JOIN [dbo].[tbl_Sections] sec
                        ON tsa.[SectionID] = sec.[SectionID]
                    INNER JOIN [dbo].[tbl_GradeLevel] gl
                        ON sec.[GradeLvlID] = gl.[gradelevel_ID]
                    LEFT JOIN [dbo].[tbl_Subjects] sub
                        ON tsa.[SubjectID] = sub.[SubjectID]
                    LEFT JOIN [dbo].[tbl_ClassSchedule] cs
                        ON cs.[AssignmentID] = tsa.[AssignmentID]
                    LEFT JOIN [dbo].[tbl_Classrooms] cls
                        ON cs.[RoomID] = cls.[RoomID]
                    LEFT JOIN [dbo].[tbl_Users] u
                        ON tsa.[TeacherID] = u.[user_ID];"
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

