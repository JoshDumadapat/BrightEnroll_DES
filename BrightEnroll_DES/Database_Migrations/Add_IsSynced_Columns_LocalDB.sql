-- =============================================
-- SQL Script to Add is_synced Column to All Tables
-- Target: LocalDB (DB_BrightEnroll_DES)
-- =============================================
-- This script adds the is_synced BIT NOT NULL DEFAULT(0) column
-- to all tables that need offline-to-cloud synchronization.
-- It checks for column existence before adding to avoid errors.
-- =============================================

USE [DB_BrightEnroll_DES];
GO

-- User table
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_Users') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_Users ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_Users';
END
GO

-- Student tables
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_Students') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_Students ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_Students';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_Guardians') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_Guardians ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_Guardians';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_StudentRequirements') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_StudentRequirements ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_StudentRequirements';
END
GO

-- Employee tables
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_employee_address') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_employee_address ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_employee_address';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_employee_emergency_contact') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_employee_emergency_contact ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_employee_emergency_contact';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_salary_info') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_salary_info ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_salary_info';
END
GO

-- Finance tables
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_GradeLevel') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_GradeLevel ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_GradeLevel';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_Fees') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_Fees ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_Fees';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_FeeBreakdown') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_FeeBreakdown ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_FeeBreakdown';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_Expenses') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_Expenses ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_Expenses';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_ExpenseAttachments') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_ExpenseAttachments ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_ExpenseAttachments';
END
GO

-- User status logging
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_user_status_logs') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_user_status_logs ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_user_status_logs';
END
GO

-- Curriculum tables
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_Classrooms') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_Classrooms ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_Classrooms';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_Sections') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_Sections ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_Sections';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_Subjects') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_Subjects ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_Subjects';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_SubjectSection') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_SubjectSection ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_SubjectSection';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_SubjectSchedule') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_SubjectSchedule ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_SubjectSchedule';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_TeacherSectionAssignment') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_TeacherSectionAssignment ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_TeacherSectionAssignment';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_ClassSchedule') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_ClassSchedule ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_ClassSchedule';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_Buildings') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_Buildings ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_Buildings';
END
GO

-- Payroll tables
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_roles') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_roles ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_roles';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_deductions') AND name = 'is_synced')
BEGIN
    ALTER TABLE tbl_deductions ADD is_synced BIT NOT NULL DEFAULT(0);
    PRINT 'Added is_synced to tbl_deductions';
END
GO

PRINT '=============================================';
PRINT 'All is_synced columns added successfully!';
PRINT '=============================================';
GO

