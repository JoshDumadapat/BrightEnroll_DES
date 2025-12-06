-- Script to create all missing database tables
-- Run this in SQL Server Management Studio (SSMS) to fix missing table errors

USE [DB_BrightEnroll_DES];
GO

PRINT '=== Creating Missing Tables ===';
GO

-- 1. Create tbl_roles table
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
    
    CREATE INDEX IX_tbl_roles_role_name ON [dbo].[tbl_roles]([role_name]);
    CREATE INDEX IX_tbl_roles_is_active ON [dbo].[tbl_roles]([is_active]);
    
    PRINT 'Table [tbl_roles] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [tbl_roles] already exists.';
END
GO

-- 2. Create tbl_deductions table
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
    
    CREATE INDEX IX_tbl_deductions_deduction_type ON [dbo].[tbl_deductions]([deduction_type]);
    CREATE INDEX IX_tbl_deductions_is_active ON [dbo].[tbl_deductions]([is_active]);
    
    PRINT 'Table [tbl_deductions] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [tbl_deductions] already exists.';
END
GO

-- 3. Create tbl_Expenses table
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
    
    CREATE INDEX IX_tbl_Expenses_ExpenseDate ON [dbo].[tbl_Expenses]([expense_date]);
    CREATE INDEX IX_tbl_Expenses_Category ON [dbo].[tbl_Expenses]([category]);
    CREATE INDEX IX_tbl_Expenses_Status ON [dbo].[tbl_Expenses]([status]);
    
    PRINT 'Table [tbl_Expenses] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [tbl_Expenses] already exists.';
END
GO

-- 4. Create tbl_ExpenseAttachments table (must be after tbl_Expenses)
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
    
    CREATE INDEX IX_tbl_ExpenseAttachments_ExpenseId ON [dbo].[tbl_ExpenseAttachments]([expense_ID]);
    
    PRINT 'Table [tbl_ExpenseAttachments] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [tbl_ExpenseAttachments] already exists.';
END
GO

-- 5. Create tbl_StudentPayments table (must be after tbl_Students)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_StudentPayments' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_StudentPayments](
        [payment_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [student_id] VARCHAR(6) NOT NULL,
        [amount] DECIMAL(18,2) NOT NULL,
        [payment_method] VARCHAR(50) NOT NULL,
        [or_number] VARCHAR(50) NOT NULL,
        [processed_by] VARCHAR(50) NULL,
        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_tbl_StudentPayments_tbl_Students FOREIGN KEY ([student_id]) REFERENCES [dbo].[tbl_Students]([student_id]) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_tbl_StudentPayments_StudentId ON [dbo].[tbl_StudentPayments]([student_id]);
    CREATE UNIQUE INDEX IX_tbl_StudentPayments_OrNumber ON [dbo].[tbl_StudentPayments]([or_number]);
    CREATE INDEX IX_tbl_StudentPayments_CreatedAt ON [dbo].[tbl_StudentPayments]([created_at]);
    
    PRINT 'Table [tbl_StudentPayments] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [tbl_StudentPayments] already exists.';
END
GO

-- 6. Create tbl_Grades table (must be after tbl_Students, tbl_Subjects, tbl_Sections, tbl_Users)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Grades' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_Grades](
        [grade_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [student_id] VARCHAR(6) NOT NULL,
        [subject_id] INT NOT NULL,
        [section_id] INT NOT NULL,
        [school_year] VARCHAR(20) NOT NULL,
        [grading_period] VARCHAR(10) NOT NULL,
        [quiz] DECIMAL(5,2) NULL,
        [exam] DECIMAL(5,2) NULL,
        [project] DECIMAL(5,2) NULL,
        [participation] DECIMAL(5,2) NULL,
        [final_grade] DECIMAL(5,2) NULL,
        [teacher_id] INT NOT NULL,
        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
        [updated_at] DATETIME NULL,
        CONSTRAINT FK_tbl_Grades_tbl_Students FOREIGN KEY ([student_id]) REFERENCES [dbo].[tbl_Students]([student_id]) ON DELETE CASCADE,
        CONSTRAINT FK_tbl_Grades_tbl_Subjects FOREIGN KEY ([subject_id]) REFERENCES [dbo].[tbl_Subjects]([SubjectID]) ON DELETE CASCADE,
        CONSTRAINT FK_tbl_Grades_tbl_Sections FOREIGN KEY ([section_id]) REFERENCES [dbo].[tbl_Sections]([SectionID]) ON DELETE CASCADE,
        CONSTRAINT FK_tbl_Grades_tbl_Users FOREIGN KEY ([teacher_id]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE RESTRICT
    );
    
    CREATE INDEX IX_tbl_Grades_StudentId ON [dbo].[tbl_Grades]([student_id]);
    CREATE INDEX IX_tbl_Grades_SubjectId ON [dbo].[tbl_Grades]([subject_id]);
    CREATE INDEX IX_tbl_Grades_SectionId ON [dbo].[tbl_Grades]([section_id]);
    CREATE INDEX IX_tbl_Grades_TeacherId ON [dbo].[tbl_Grades]([teacher_id]);
    CREATE INDEX IX_tbl_Grades_Student_Subject_Section_Period ON [dbo].[tbl_Grades]([student_id], [subject_id], [section_id], [grading_period], [school_year]);
    CREATE UNIQUE INDEX UX_tbl_Grades_Student_Subject_Section_Period_Year ON [dbo].[tbl_Grades]([student_id], [subject_id], [section_id], [grading_period], [school_year]);
    
    PRINT 'Table [tbl_Grades] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [tbl_Grades] already exists.';
END
GO

PRINT '';
PRINT '=== Script Completed ===';
PRINT 'All missing tables have been created.';
GO

