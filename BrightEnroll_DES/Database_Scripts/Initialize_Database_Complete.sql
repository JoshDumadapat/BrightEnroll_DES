-- =============================================
-- BrightEnroll_DES Complete Database Initialization Script (Optimized)
-- This script creates ALL tables, indexes, views, and stored procedures
-- Run this script to set up a fresh database or update existing database
-- =============================================

USE [DB_BrightEnroll_DES];
GO

-- Disable foreign key checks temporarily for faster execution
SET NOCOUNT ON;
GO

-- =============================================
-- STEP 1: CREATE ALL TABLES (in dependency order)
-- =============================================

-- 1.1 tbl_Users (base table for employees)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Users' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
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
        [date_hired] DATETIME NOT NULL DEFAULT GETDATE(),
        [status] VARCHAR(20) NOT NULL DEFAULT 'active'
    );
    PRINT 'Table [tbl_Users] created.';
END
GO

-- 1.2 tbl_Guardians (must be before tbl_Students)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Guardians' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_Guardians](
        [guardian_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [first_name] VARCHAR(50) NOT NULL,
        [middle_name] VARCHAR(50) NULL,
        [last_name] VARCHAR(50) NOT NULL,
        [suffix] VARCHAR(10) NULL,
        [contact_num] VARCHAR(20) NULL,
        [relationship] VARCHAR(50) NULL
    );
    PRINT 'Table [tbl_Guardians] created.';
END
GO

-- 1.3 tbl_StudentID_Sequence (must be before tbl_Students)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_StudentID_Sequence' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_StudentID_Sequence](
        [sequence_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [LastStudentID] INT NOT NULL DEFAULT 158021
    );
    -- Insert initial sequence value if table is empty
    IF NOT EXISTS (SELECT * FROM [dbo].[tbl_StudentID_Sequence])
        INSERT INTO [dbo].[tbl_StudentID_Sequence] ([LastStudentID]) VALUES (158021);
    PRINT 'Table [tbl_StudentID_Sequence] created.';
END
GO

-- 1.4 tbl_Students (depends on tbl_Guardians)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Students' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
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
        [status] VARCHAR(50) NOT NULL DEFAULT 'Pending',
        CONSTRAINT FK_tbl_Students_tbl_Guardians FOREIGN KEY ([guardian_id]) REFERENCES [dbo].[tbl_Guardians]([guardian_id])
    );
    PRINT 'Table [tbl_Students] created.';
END
GO

-- 1.5 tbl_StudentRequirements (depends on tbl_Students)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_StudentRequirements' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_StudentRequirements](
        [requirement_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [student_id] VARCHAR(6) NOT NULL,
        [requirement_name] VARCHAR(100) NOT NULL,
        [status] VARCHAR(20) DEFAULT 'not submitted',
        [requirement_type] VARCHAR(20) NOT NULL,
        [is_verified] BIT DEFAULT 0,
        CONSTRAINT FK_tbl_StudentRequirements_tbl_Students FOREIGN KEY ([student_id]) REFERENCES [dbo].[tbl_Students]([student_id]) ON DELETE CASCADE
    );
    PRINT 'Table [tbl_StudentRequirements] created.';
END
GO

-- 1.6 tbl_employee_address (depends on tbl_Users)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_employee_address' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
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
    );
    PRINT 'Table [tbl_employee_address] created.';
END
GO

-- 1.7 tbl_employee_emergency_contact (depends on tbl_Users)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_employee_emergency_contact' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
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
    );
    PRINT 'Table [tbl_employee_emergency_contact] created.';
END
GO

-- 1.8 tbl_salary_info (depends on tbl_Users)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_salary_info' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_salary_info](
        [salary_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [user_ID] INT NOT NULL,
        [base_salary] DECIMAL(12,2) NOT NULL,
        [allowance] DECIMAL(12,2) DEFAULT 0.00,
        [date_effective] DATE DEFAULT GETDATE(),
        [is_active] BIT DEFAULT 1,
        CONSTRAINT FK_tbl_salary_info_tbl_Users FOREIGN KEY ([user_ID]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE CASCADE
    );
    PRINT 'Table [tbl_salary_info] created.';
END
GO

-- 1.9 tbl_user_status_logs (depends on tbl_Users)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_user_status_logs' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
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
    );
    PRINT 'Table [tbl_user_status_logs] created.';
END
GO

-- 1.10 tbl_GradeLevel (standalone, must be before fees and curriculum tables)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_GradeLevel' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_GradeLevel](
        [gradelevel_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [grade_level_name] VARCHAR(50) NOT NULL UNIQUE,
        [is_active] BIT NOT NULL DEFAULT 1
    );
    PRINT 'Table [tbl_GradeLevel] created.';
END
GO

-- 1.11 tbl_Fees (depends on tbl_GradeLevel)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Fees' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
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
    );
    PRINT 'Table [tbl_Fees] created.';
END
GO

-- 1.12 tbl_FeeBreakdown (depends on tbl_Fees)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_FeeBreakdown' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
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
    );
    PRINT 'Table [tbl_FeeBreakdown] created.';
END
GO

-- 1.13 tbl_Expenses (standalone finance table)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Expenses' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
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
        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
        [updated_at] DATETIME NULL
    );
    PRINT 'Table [tbl_Expenses] created.';
END
GO

-- 1.14 tbl_ExpenseAttachments (depends on tbl_Expenses)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_ExpenseAttachments' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_ExpenseAttachments](
        [attachment_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [expense_ID] INT NOT NULL,
        [file_name] VARCHAR(255) NOT NULL,
        [file_path] VARCHAR(500) NOT NULL,
        [uploaded_at] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_tbl_ExpenseAttachments_tbl_Expenses FOREIGN KEY ([expense_ID]) REFERENCES [dbo].[tbl_Expenses]([expense_ID]) ON DELETE CASCADE
    );
    PRINT 'Table [tbl_ExpenseAttachments] created.';
END
GO

-- 1.15 tbl_Buildings (standalone)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Buildings' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_Buildings](
        [BuildingID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [BuildingName] VARCHAR(100) NOT NULL,
        [FloorCount] INT NULL,
        [Description] VARCHAR(500) NULL
    );
    PRINT 'Table [tbl_Buildings] created.';
END
GO

-- 1.16 tbl_Classrooms (depends on tbl_Buildings optionally)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Classrooms' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
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
    );
    PRINT 'Table [tbl_Classrooms] created.';
END
GO

-- 1.17 tbl_Sections (depends on tbl_GradeLevel and tbl_Classrooms)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Sections' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_Sections](
        [SectionID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SectionName] VARCHAR(100) NOT NULL,
        [GradeLvlID] INT NOT NULL,
        [ClassroomID] INT NULL,
        [Capacity] INT NOT NULL,
        [Notes] VARCHAR(500) NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME NULL,
        CONSTRAINT FK_tbl_Sections_tbl_GradeLevel FOREIGN KEY ([GradeLvlID]) REFERENCES [dbo].[tbl_GradeLevel]([gradelevel_ID]),
        CONSTRAINT FK_tbl_Sections_tbl_Classrooms FOREIGN KEY ([ClassroomID]) REFERENCES [dbo].[tbl_Classrooms]([RoomID]) ON DELETE SET NULL
    );
    PRINT 'Table [tbl_Sections] created.';
END
GO

-- 1.18 tbl_Subjects (depends on tbl_GradeLevel)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Subjects' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_Subjects](
        [SubjectID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [GradeLvlID] INT NOT NULL,
        [SubjectName] VARCHAR(100) NOT NULL,
        [Description] VARCHAR(500) NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME NULL,
        CONSTRAINT FK_tbl_Subjects_tbl_GradeLevel FOREIGN KEY ([GradeLvlID]) REFERENCES [dbo].[tbl_GradeLevel]([gradelevel_ID])
    );
    PRINT 'Table [tbl_Subjects] created.';
END
GO

-- 1.19 tbl_SubjectSection (depends on tbl_Sections and tbl_Subjects)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_SubjectSection' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_SubjectSection](
        [ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SectionID] INT NOT NULL,
        [SubjectID] INT NOT NULL,
        CONSTRAINT FK_tbl_SubjectSection_tbl_Sections FOREIGN KEY ([SectionID]) REFERENCES [dbo].[tbl_Sections]([SectionID]) ON DELETE CASCADE,
        CONSTRAINT FK_tbl_SubjectSection_tbl_Subjects FOREIGN KEY ([SubjectID]) REFERENCES [dbo].[tbl_Subjects]([SubjectID]) ON DELETE CASCADE
    );
    PRINT 'Table [tbl_SubjectSection] created.';
END
GO

-- 1.20 tbl_SubjectSchedule (depends on tbl_Subjects and tbl_GradeLevel)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_SubjectSchedule' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
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
    );
    PRINT 'Table [tbl_SubjectSchedule] created.';
END
GO

-- 1.21 tbl_TeacherSectionAssignment (depends on tbl_Users, tbl_Sections, tbl_Subjects)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_TeacherSectionAssignment' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_TeacherSectionAssignment](
        [AssignmentID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [TeacherID] INT NOT NULL,
        [SectionID] INT NOT NULL,
        [SubjectID] INT NULL,
        [Role] VARCHAR(50) NOT NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME NULL,
        CONSTRAINT FK_tbl_TeacherSectionAssignment_tbl_Users FOREIGN KEY ([TeacherID]) REFERENCES [dbo].[tbl_Users]([user_ID]),
        CONSTRAINT FK_tbl_TeacherSectionAssignment_tbl_Sections FOREIGN KEY ([SectionID]) REFERENCES [dbo].[tbl_Sections]([SectionID]) ON DELETE CASCADE,
        CONSTRAINT FK_tbl_TeacherSectionAssignment_tbl_Subjects FOREIGN KEY ([SubjectID]) REFERENCES [dbo].[tbl_Subjects]([SubjectID]) ON DELETE SET NULL
    );
    PRINT 'Table [tbl_TeacherSectionAssignment] created.';
END
GO

-- 1.22 tbl_ClassSchedule (depends on tbl_TeacherSectionAssignment and tbl_Classrooms)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_ClassSchedule' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
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
    );
    PRINT 'Table [tbl_ClassSchedule] created.';
END
GO

-- 1.23 tbl_roles (standalone payroll table)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_roles' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_roles](
        [role_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [role_name] VARCHAR(50) NOT NULL UNIQUE,
        [base_salary] DECIMAL(12,2) NOT NULL,
        [allowance] DECIMAL(12,2) NOT NULL DEFAULT 0.00,
        [is_active] BIT NOT NULL DEFAULT 1,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [updated_date] DATETIME NULL
    );
    PRINT 'Table [tbl_roles] created.';
END
GO

-- 1.24 tbl_deductions (standalone payroll table)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_deductions' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
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
    );
    PRINT 'Table [tbl_deductions] created.';
END
GO

-- =============================================
-- STEP 2: ADD MISSING COLUMNS (for existing databases)
-- =============================================

-- Add status column to tbl_Users if missing
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Users' AND schema_id = SCHEMA_ID('dbo'))
    AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Users') AND name = 'status')
BEGIN
    ALTER TABLE [dbo].[tbl_Users] ADD [status] VARCHAR(20) NOT NULL DEFAULT 'active';
    UPDATE [dbo].[tbl_Users] SET [status] = 'active' WHERE [status] IS NULL OR [status] = '';
    PRINT 'Column [status] added to [tbl_Users].';
END
GO

-- Add date_registered and status columns to tbl_Students if missing
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Students' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Students') AND name = 'date_registered')
    BEGIN
        ALTER TABLE [dbo].[tbl_Students] ADD [date_registered] DATETIME NOT NULL DEFAULT GETDATE();
        PRINT 'Column [date_registered] added to [tbl_Students].';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Students') AND name = 'status')
    BEGIN
        ALTER TABLE [dbo].[tbl_Students] ADD [status] VARCHAR(50) NOT NULL DEFAULT 'Pending';
        PRINT 'Column [status] added to [tbl_Students].';
    END
    ELSE
    BEGIN
        -- Update status column size if it's too small
        DECLARE @StatusSize INT;
        SELECT @StatusSize = CHARACTER_MAXIMUM_LENGTH 
        FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'tbl_Students' AND COLUMN_NAME = 'status';
        
        IF @StatusSize < 50
        BEGIN
            ALTER TABLE [dbo].[tbl_Students] ALTER COLUMN [status] VARCHAR(50) NOT NULL;
            PRINT 'Column [status] size updated to VARCHAR(50) in [tbl_Students].';
        END
    END
END
GO

-- Add is_verified column to tbl_StudentRequirements if missing
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_StudentRequirements' AND schema_id = SCHEMA_ID('dbo'))
    AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_StudentRequirements') AND name = 'is_verified')
BEGIN
    ALTER TABLE [dbo].[tbl_StudentRequirements] ADD [is_verified] BIT DEFAULT 0;
    PRINT 'Column [is_verified] added to [tbl_StudentRequirements].';
END
GO

-- =============================================
-- STEP 3: CREATE ALL INDEXES (optimized batch creation)
-- =============================================

-- Indexes for tbl_Users
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Users_Email' AND object_id = OBJECT_ID('dbo.tbl_Users'))
    CREATE INDEX IX_tbl_Users_Email ON [dbo].[tbl_Users]([email]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Users_SystemID' AND object_id = OBJECT_ID('dbo.tbl_Users'))
    CREATE INDEX IX_tbl_Users_SystemID ON [dbo].[tbl_Users]([system_ID]);
GO

-- Indexes for tbl_Guardians
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Guardians_LastName' AND object_id = OBJECT_ID('dbo.tbl_Guardians'))
    CREATE INDEX IX_tbl_Guardians_LastName ON [dbo].[tbl_Guardians]([last_name]);
GO

-- Indexes for tbl_Students
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Students_GuardianId' AND object_id = OBJECT_ID('dbo.tbl_Students'))
    CREATE INDEX IX_tbl_Students_GuardianId ON [dbo].[tbl_Students]([guardian_id]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Students_LastName' AND object_id = OBJECT_ID('dbo.tbl_Students'))
    CREATE INDEX IX_tbl_Students_LastName ON [dbo].[tbl_Students]([last_name]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Students_StudentType' AND object_id = OBJECT_ID('dbo.tbl_Students'))
    CREATE INDEX IX_tbl_Students_StudentType ON [dbo].[tbl_Students]([student_type]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Students_LRN' AND object_id = OBJECT_ID('dbo.tbl_Students'))
    CREATE INDEX IX_tbl_Students_LRN ON [dbo].[tbl_Students]([LRN]) WHERE [LRN] IS NOT NULL;
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Students_DateRegistered' AND object_id = OBJECT_ID('dbo.tbl_Students'))
    CREATE INDEX IX_tbl_Students_DateRegistered ON [dbo].[tbl_Students]([date_registered]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Students_Status' AND object_id = OBJECT_ID('dbo.tbl_Students'))
    CREATE INDEX IX_tbl_Students_Status ON [dbo].[tbl_Students]([status]);
GO

-- Indexes for tbl_StudentRequirements
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentRequirements_StudentId' AND object_id = OBJECT_ID('dbo.tbl_StudentRequirements'))
    CREATE INDEX IX_tbl_StudentRequirements_StudentId ON [dbo].[tbl_StudentRequirements]([student_id]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentRequirements_RequirementType' AND object_id = OBJECT_ID('dbo.tbl_StudentRequirements'))
    CREATE INDEX IX_tbl_StudentRequirements_RequirementType ON [dbo].[tbl_StudentRequirements]([requirement_type]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentRequirements_Status' AND object_id = OBJECT_ID('dbo.tbl_StudentRequirements'))
    CREATE INDEX IX_tbl_StudentRequirements_Status ON [dbo].[tbl_StudentRequirements]([status]);
GO

-- Indexes for employee tables
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_employee_address_user_ID' AND object_id = OBJECT_ID('dbo.tbl_employee_address'))
    CREATE INDEX IX_tbl_employee_address_user_ID ON [dbo].[tbl_employee_address]([user_ID]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_employee_emergency_contact_user_ID' AND object_id = OBJECT_ID('dbo.tbl_employee_emergency_contact'))
    CREATE INDEX IX_tbl_employee_emergency_contact_user_ID ON [dbo].[tbl_employee_emergency_contact]([user_ID]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_salary_info_user_ID' AND object_id = OBJECT_ID('dbo.tbl_salary_info'))
    CREATE INDEX IX_tbl_salary_info_user_ID ON [dbo].[tbl_salary_info]([user_ID]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_salary_info_is_active' AND object_id = OBJECT_ID('dbo.tbl_salary_info'))
    CREATE INDEX IX_tbl_salary_info_is_active ON [dbo].[tbl_salary_info]([is_active]);
GO

-- Indexes for user status logs
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_user_status_logs_user_id' AND object_id = OBJECT_ID('dbo.tbl_user_status_logs'))
    CREATE INDEX IX_tbl_user_status_logs_user_id ON [dbo].[tbl_user_status_logs]([user_id]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_user_status_logs_changed_by' AND object_id = OBJECT_ID('dbo.tbl_user_status_logs'))
    CREATE INDEX IX_tbl_user_status_logs_changed_by ON [dbo].[tbl_user_status_logs]([changed_by]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_user_status_logs_created_at' AND object_id = OBJECT_ID('dbo.tbl_user_status_logs'))
    CREATE INDEX IX_tbl_user_status_logs_created_at ON [dbo].[tbl_user_status_logs]([created_at]);
GO

-- Indexes for finance tables
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_GradeLevel_is_active' AND object_id = OBJECT_ID('dbo.tbl_GradeLevel'))
    CREATE INDEX IX_tbl_GradeLevel_is_active ON [dbo].[tbl_GradeLevel]([is_active]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Fees_gradelevel_ID' AND object_id = OBJECT_ID('dbo.tbl_Fees'))
    CREATE INDEX IX_tbl_Fees_gradelevel_ID ON [dbo].[tbl_Fees]([gradelevel_ID]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Fees_is_active' AND object_id = OBJECT_ID('dbo.tbl_Fees'))
    CREATE INDEX IX_tbl_Fees_is_active ON [dbo].[tbl_Fees]([is_active]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_FeeBreakdown_fee_ID' AND object_id = OBJECT_ID('dbo.tbl_FeeBreakdown'))
    CREATE INDEX IX_tbl_FeeBreakdown_fee_ID ON [dbo].[tbl_FeeBreakdown]([fee_ID]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_FeeBreakdown_breakdown_type' AND object_id = OBJECT_ID('dbo.tbl_FeeBreakdown'))
    CREATE INDEX IX_tbl_FeeBreakdown_breakdown_type ON [dbo].[tbl_FeeBreakdown]([breakdown_type]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Expenses_ExpenseDate' AND object_id = OBJECT_ID('dbo.tbl_Expenses'))
    CREATE INDEX IX_tbl_Expenses_ExpenseDate ON [dbo].[tbl_Expenses]([expense_date]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Expenses_Category' AND object_id = OBJECT_ID('dbo.tbl_Expenses'))
    CREATE INDEX IX_tbl_Expenses_Category ON [dbo].[tbl_Expenses]([category]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Expenses_Status' AND object_id = OBJECT_ID('dbo.tbl_Expenses'))
    CREATE INDEX IX_tbl_Expenses_Status ON [dbo].[tbl_Expenses]([status]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_ExpenseAttachments_ExpenseId' AND object_id = OBJECT_ID('dbo.tbl_ExpenseAttachments'))
    CREATE INDEX IX_tbl_ExpenseAttachments_ExpenseId ON [dbo].[tbl_ExpenseAttachments]([expense_ID]);
GO

-- Indexes for curriculum tables
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Buildings_BuildingName' AND object_id = OBJECT_ID('dbo.tbl_Buildings'))
    CREATE INDEX IX_tbl_Buildings_BuildingName ON [dbo].[tbl_Buildings]([BuildingName]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Classrooms_RoomName' AND object_id = OBJECT_ID('dbo.tbl_Classrooms'))
    CREATE INDEX IX_tbl_Classrooms_RoomName ON [dbo].[tbl_Classrooms]([RoomName]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Classrooms_Status' AND object_id = OBJECT_ID('dbo.tbl_Classrooms'))
    CREATE INDEX IX_tbl_Classrooms_Status ON [dbo].[tbl_Classrooms]([Status]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Sections_SectionName' AND object_id = OBJECT_ID('dbo.tbl_Sections'))
    CREATE INDEX IX_tbl_Sections_SectionName ON [dbo].[tbl_Sections]([SectionName]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Sections_GradeLvlID' AND object_id = OBJECT_ID('dbo.tbl_Sections'))
    CREATE INDEX IX_tbl_Sections_GradeLvlID ON [dbo].[tbl_Sections]([GradeLvlID]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Sections_ClassroomID' AND object_id = OBJECT_ID('dbo.tbl_Sections'))
    CREATE INDEX IX_tbl_Sections_ClassroomID ON [dbo].[tbl_Sections]([ClassroomID]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Subjects_SubjectName' AND object_id = OBJECT_ID('dbo.tbl_Subjects'))
    CREATE INDEX IX_tbl_Subjects_SubjectName ON [dbo].[tbl_Subjects]([SubjectName]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Subjects_GradeLvlID' AND object_id = OBJECT_ID('dbo.tbl_Subjects'))
    CREATE INDEX IX_tbl_Subjects_GradeLvlID ON [dbo].[tbl_Subjects]([GradeLvlID]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SubjectSection_SectionID' AND object_id = OBJECT_ID('dbo.tbl_SubjectSection'))
    CREATE INDEX IX_tbl_SubjectSection_SectionID ON [dbo].[tbl_SubjectSection]([SectionID]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SubjectSection_SubjectID' AND object_id = OBJECT_ID('dbo.tbl_SubjectSection'))
    CREATE INDEX IX_tbl_SubjectSection_SubjectID ON [dbo].[tbl_SubjectSection]([SubjectID]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SubjectSchedule_SubjectID' AND object_id = OBJECT_ID('dbo.tbl_SubjectSchedule'))
    CREATE INDEX IX_tbl_SubjectSchedule_SubjectID ON [dbo].[tbl_SubjectSchedule]([SubjectID]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SubjectSchedule_GradeLvlID' AND object_id = OBJECT_ID('dbo.tbl_SubjectSchedule'))
    CREATE INDEX IX_tbl_SubjectSchedule_GradeLvlID ON [dbo].[tbl_SubjectSchedule]([GradeLvlID]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SubjectSchedule_Subject_Grade_Day' AND object_id = OBJECT_ID('dbo.tbl_SubjectSchedule'))
    CREATE INDEX IX_tbl_SubjectSchedule_Subject_Grade_Day ON [dbo].[tbl_SubjectSchedule]([SubjectID], [GradeLvlID], [DayOfWeek]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_SubjectSchedule_IsDefault' AND object_id = OBJECT_ID('dbo.tbl_SubjectSchedule'))
    CREATE INDEX IX_tbl_SubjectSchedule_IsDefault ON [dbo].[tbl_SubjectSchedule]([IsDefault]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_TeacherSectionAssignment_TeacherID' AND object_id = OBJECT_ID('dbo.tbl_TeacherSectionAssignment'))
    CREATE INDEX IX_tbl_TeacherSectionAssignment_TeacherID ON [dbo].[tbl_TeacherSectionAssignment]([TeacherID]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_TeacherSectionAssignment_SectionID' AND object_id = OBJECT_ID('dbo.tbl_TeacherSectionAssignment'))
    CREATE INDEX IX_tbl_TeacherSectionAssignment_SectionID ON [dbo].[tbl_TeacherSectionAssignment]([SectionID]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_TeacherSectionAssignment_SubjectID' AND object_id = OBJECT_ID('dbo.tbl_TeacherSectionAssignment'))
    CREATE INDEX IX_tbl_TeacherSectionAssignment_SubjectID ON [dbo].[tbl_TeacherSectionAssignment]([SubjectID]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_TeacherSectionAssignment_Role' AND object_id = OBJECT_ID('dbo.tbl_TeacherSectionAssignment'))
    CREATE INDEX IX_tbl_TeacherSectionAssignment_Role ON [dbo].[tbl_TeacherSectionAssignment]([Role]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_ClassSchedule_AssignmentID' AND object_id = OBJECT_ID('dbo.tbl_ClassSchedule'))
    CREATE INDEX IX_tbl_ClassSchedule_AssignmentID ON [dbo].[tbl_ClassSchedule]([AssignmentID]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_ClassSchedule_RoomID' AND object_id = OBJECT_ID('dbo.tbl_ClassSchedule'))
    CREATE INDEX IX_tbl_ClassSchedule_RoomID ON [dbo].[tbl_ClassSchedule]([RoomID]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_ClassSchedule_DayOfWeek' AND object_id = OBJECT_ID('dbo.tbl_ClassSchedule'))
    CREATE INDEX IX_tbl_ClassSchedule_DayOfWeek ON [dbo].[tbl_ClassSchedule]([DayOfWeek]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_ClassSchedule_Assignment_Day_Time' AND object_id = OBJECT_ID('dbo.tbl_ClassSchedule'))
    CREATE INDEX IX_tbl_ClassSchedule_Assignment_Day_Time ON [dbo].[tbl_ClassSchedule]([AssignmentID], [DayOfWeek], [StartTime], [EndTime]);
GO

-- Indexes for payroll tables
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_roles_role_name' AND object_id = OBJECT_ID('dbo.tbl_roles'))
    CREATE INDEX IX_tbl_roles_role_name ON [dbo].[tbl_roles]([role_name]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_roles_is_active' AND object_id = OBJECT_ID('dbo.tbl_roles'))
    CREATE INDEX IX_tbl_roles_is_active ON [dbo].[tbl_roles]([is_active]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_deductions_deduction_type' AND object_id = OBJECT_ID('dbo.tbl_deductions'))
    CREATE INDEX IX_tbl_deductions_deduction_type ON [dbo].[tbl_deductions]([deduction_type]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_deductions_is_active' AND object_id = OBJECT_ID('dbo.tbl_deductions'))
    CREATE INDEX IX_tbl_deductions_is_active ON [dbo].[tbl_deductions]([is_active]);
GO

-- =============================================
-- STEP 4: CREATE VIEWS
-- =============================================

-- Drop and recreate views to ensure they're up to date
IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[vw_EmployeeData]'))
    DROP VIEW [dbo].[vw_EmployeeData];
GO

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
    addr.[house_no] AS HouseNo,
    addr.[street_name] AS StreetName,
    addr.[province] AS Province,
    addr.[city] AS City,
    addr.[barangay] AS Barangay,
    addr.[country] AS Country,
    addr.[zip_code] AS ZipCode,
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
    ec.[first_name] AS EmergencyContactFirstName,
    ec.[mid_name] AS EmergencyContactMiddleName,
    ec.[last_name] AS EmergencyContactLastName,
    ec.[suffix] AS EmergencyContactSuffix,
    ec.[relationship] AS EmergencyContactRelationship,
    ec.[contact_number] AS EmergencyContactNumber,
    ec.[address] AS EmergencyContactAddress,
    sal.[base_salary] AS BaseSalary,
    sal.[allowance] AS Allowance,
    (sal.[base_salary] + sal.[allowance]) AS TotalSalary,
    sal.[date_effective] AS SalaryDateEffective,
    sal.[is_active] AS SalaryIsActive
FROM [dbo].[tbl_Users] u
LEFT JOIN [dbo].[tbl_employee_address] addr ON u.[user_ID] = addr.[user_ID]
LEFT JOIN [dbo].[tbl_employee_emergency_contact] ec ON u.[user_ID] = ec.[user_ID]
LEFT JOIN [dbo].[tbl_salary_info] sal ON u.[user_ID] = sal.[user_ID] AND sal.[is_active] = 1;
GO

IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[vw_StudentData]'))
    DROP VIEW [dbo].[vw_StudentData];
GO

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
    s.[hse_no] AS CurrentHouseNo,
    s.[street] AS CurrentStreet,
    s.[brngy] AS CurrentBarangay,
    s.[province] AS CurrentProvince,
    s.[city] AS CurrentCity,
    s.[country] AS CurrentCountry,
    s.[zip_code] AS CurrentZipCode,
    s.[phse_no] AS PermanentHouseNo,
    s.[pstreet] AS PermanentStreet,
    s.[pbrngy] AS PermanentBarangay,
    s.[pprovince] AS PermanentProvince,
    s.[pcity] AS PermanentCity,
    s.[pcountry] AS PermanentCountry,
    s.[pzip_code] AS PermanentZipCode,
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
    s.[student_type] AS StudentType,
    s.[LRN] AS LRN,
    s.[school_yr] AS SchoolYear,
    s.[grade_level] AS GradeLevel,
    s.[date_registered] AS DateRegistered,
    s.[status] AS Status,
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
LEFT JOIN [dbo].[tbl_Guardians] g ON s.[guardian_id] = g.[guardian_id];
GO

-- =============================================
-- STEP 5: CREATE STORED PROCEDURES
-- =============================================

-- Drop and recreate stored procedure
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_CreateStudent]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_CreateStudent];
GO

CREATE PROCEDURE [dbo].[sp_CreateStudent]
    @first_name VARCHAR(50),
    @middle_name VARCHAR(50) = NULL,
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
END;
GO

-- =============================================
-- STEP 6: INITIALIZE SEQUENCE TABLE
-- =============================================

IF NOT EXISTS (SELECT * FROM [dbo].[tbl_StudentID_Sequence])
    INSERT INTO [dbo].[tbl_StudentID_Sequence] ([LastStudentID]) VALUES (158021);
GO

-- =============================================
-- COMPLETION MESSAGE
-- =============================================

PRINT '========================================';
PRINT 'Database initialization completed successfully!';
PRINT 'All tables, indexes, views, and stored procedures have been created.';
PRINT '========================================';
GO

SET NOCOUNT OFF;
GO

