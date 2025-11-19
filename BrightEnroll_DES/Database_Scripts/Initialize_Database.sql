-- =============================================
-- BrightEnroll_DES Database Initialization Script (Optimized)
-- Run this script on a new device to set up the database
-- Note: The application auto-creates the database, this is for manual setup only
-- =============================================

-- STEP 1: CREATE DATABASE
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'DB_BrightEnroll_DES')
BEGIN
    CREATE DATABASE [DB_BrightEnroll_DES];
    PRINT 'Database created successfully.';
END
GO

USE [DB_BrightEnroll_DES];
GO

-- STEP 2: CREATE TABLES (in dependency order)

-- tbl_Users (base table)
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
        [date_hired] DATETIME NOT NULL,
        [status] VARCHAR(20) NOT NULL DEFAULT 'active'
    );
    PRINT 'Table [tbl_Users] created.';
END
GO

-- Add status column if missing (for existing databases)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Users') AND name = 'status')
BEGIN
    ALTER TABLE [dbo].[tbl_Users] ADD [status] VARCHAR(20) NOT NULL DEFAULT 'active';
    UPDATE [dbo].[tbl_Users] SET [status] = 'active' WHERE [status] IS NULL OR [status] = '';
    PRINT 'Status column added to [tbl_Users].';
END
GO

-- tbl_Guardians (must be before tbl_Students)
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

-- tbl_StudentID_Sequence (must be before tbl_Students)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_StudentID_Sequence' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_StudentID_Sequence](
        [sequence_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [LastStudentID] INT NOT NULL DEFAULT 158021
    );
    -- Insert initial sequence value
    INSERT INTO [dbo].[tbl_StudentID_Sequence] ([LastStudentID]) VALUES (158021);
    PRINT 'Table [tbl_StudentID_Sequence] created.';
END
GO

-- tbl_Students (depends on tbl_Guardians and tbl_StudentID_Sequence)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Students' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
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
    );
    PRINT 'Table [tbl_Students] created.';
END
GO

-- tbl_StudentRequirements (depends on tbl_Students)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_StudentRequirements' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_StudentRequirements](
        [requirement_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [student_id] VARCHAR(6) NOT NULL,
        [requirement_name] VARCHAR(100) NOT NULL,
        [status] VARCHAR(20) DEFAULT 'not submitted',
        [requirement_type] VARCHAR(20) NOT NULL,
        CONSTRAINT FK_tbl_StudentRequirements_tbl_Students FOREIGN KEY ([student_id]) REFERENCES [dbo].[tbl_Students]([student_id])
    );
    PRINT 'Table [tbl_StudentRequirements] created.';
END
GO

-- Employee tables (depend on tbl_Users)
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

-- STEP 2.5: ADD MISSING COLUMNS TO EXISTING TABLES (for existing databases)
-- Add date_registered column to tbl_Students if it doesn't exist
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Students' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Students') AND name = 'date_registered')
    BEGIN
        ALTER TABLE [dbo].[tbl_Students]
        ADD [date_registered] DATETIME NOT NULL DEFAULT GETDATE();
        PRINT 'Column [date_registered] added to existing [tbl_Students] table.';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Students') AND name = 'status')
    BEGIN
        ALTER TABLE [dbo].[tbl_Students]
        ADD [status] VARCHAR(20) NOT NULL DEFAULT 'Pending';
        PRINT 'Column [status] added to existing [tbl_Students] table.';
    END
END
GO

-- STEP 3: CREATE INDEXES (batch creation for performance)

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

-- STEP 3: CREATE STORED PROCEDURES

-- Stored Procedure: sp_CreateStudent - Generates 6-digit student ID and inserts student record
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_CreateStudent]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_CreateStudent];
GO

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
END;
GO

PRINT 'Stored procedure [sp_CreateStudent] created.';
GO

-- STEP 3.4: CREATE FINANCE TABLES

-- tbl_GradeLevel
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_GradeLevel' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_GradeLevel](
        [gradelevel_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [grade_level_name] VARCHAR(50) NOT NULL UNIQUE,
        [is_active] BIT NOT NULL DEFAULT 1
    );
    
    CREATE INDEX IX_tbl_GradeLevel_is_active ON [dbo].[tbl_GradeLevel]([is_active]);
    
    PRINT 'Table [tbl_GradeLevel] created.';
END
GO

-- Insert grade levels
IF NOT EXISTS (SELECT * FROM [dbo].[tbl_GradeLevel])
BEGIN
    INSERT INTO [dbo].[tbl_GradeLevel] ([grade_level_name]) VALUES
    ('Pre-School'),
    ('Kinder'),
    ('Grade 1'),
    ('Grade 2'),
    ('Grade 3'),
    ('Grade 4'),
    ('Grade 5'),
    ('Grade 6');
    
    PRINT 'Grade levels seeded.';
END
GO

-- tbl_Fees
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
    
    CREATE INDEX IX_tbl_Fees_gradelevel_ID ON [dbo].[tbl_Fees]([gradelevel_ID]);
    CREATE INDEX IX_tbl_Fees_is_active ON [dbo].[tbl_Fees]([is_active]);
    
    PRINT 'Table [tbl_Fees] created.';
END
GO

-- tbl_FeeBreakdown
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
    
    CREATE INDEX IX_tbl_FeeBreakdown_fee_ID ON [dbo].[tbl_FeeBreakdown]([fee_ID]);
    CREATE INDEX IX_tbl_FeeBreakdown_breakdown_type ON [dbo].[tbl_FeeBreakdown]([breakdown_type]);
    
    PRINT 'Table [tbl_FeeBreakdown] created.';
END
GO

-- STEP 3.5: CREATE VIEWS

-- View: vw_EmployeeData - Combines all employee-related data
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
LEFT JOIN [dbo].[tbl_salary_info] sal ON u.[user_ID] = sal.[user_ID] AND sal.[is_active] = 1;
GO

PRINT 'View [vw_EmployeeData] created.';
GO

-- View: vw_StudentData - Combines all student-related data
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
LEFT JOIN [dbo].[tbl_Guardians] g ON s.[guardian_id] = g.[guardian_id];
GO

PRINT 'View [vw_StudentData] created.';
GO

-- STEP 4: VERIFY SETUP
PRINT '';
PRINT '========================================';
PRINT 'Database Initialization Complete';
PRINT '========================================';
PRINT '';

-- Verify tables
DECLARE @TableCount INT = 0;
SELECT @TableCount = COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo');
PRINT 'Total tables created: ' + CAST(@TableCount AS VARCHAR(10));

-- Note: Admin user will be created automatically by the application seeder
PRINT '';
PRINT 'Next Steps:';
PRINT '1. Run the application - it will automatically seed the admin user';
PRINT '2. Login with: BDES-0001 / Admin123456';
PRINT '';
GO
