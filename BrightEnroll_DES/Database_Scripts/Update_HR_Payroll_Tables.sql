-- =============================================
-- Complete HR & Payroll Database Update Script
-- This script:
-- 1. Adds school_year to tbl_salary_info
-- 2. Adds threshold_percentage to tbl_roles
-- 3. Creates tbl_salary_change_requests
-- 4. Creates tbl_payroll_transactions
-- Run this script to update your database with the new HR/Payroll features
-- =============================================

USE [DB_BrightEnroll_DES];
GO

SET NOCOUNT ON;
GO

PRINT '=============================================';
PRINT 'Starting HR & Payroll Database Update';
PRINT '=============================================';
PRINT '';

-- =============================================
-- STEP 1: Add school_year to tbl_salary_info
-- =============================================
PRINT 'STEP 1: Updating tbl_salary_info...';

IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.tbl_salary_info') 
    AND name = 'school_year'
)
BEGIN
    ALTER TABLE [dbo].[tbl_salary_info]
    ADD [school_year] VARCHAR(20) NOT NULL DEFAULT '2024-2025';
    
    PRINT '  ✓ Column [school_year] added to [tbl_salary_info].';
END
ELSE
BEGIN
    PRINT '  ✓ Column [school_year] already exists in [tbl_salary_info].';
END
GO

-- Update existing records with current school year (separate batch after column is added)
IF EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.tbl_salary_info') 
    AND name = 'school_year'
)
BEGIN
    DECLARE @CurrentSchoolYear VARCHAR(20);
    SET @CurrentSchoolYear = (
        CASE 
            WHEN MONTH(GETDATE()) >= 6 THEN 
                CAST(YEAR(GETDATE()) AS VARCHAR(4)) + '-' + CAST(YEAR(GETDATE()) + 1 AS VARCHAR(4))
            ELSE 
                CAST(YEAR(GETDATE()) - 1 AS VARCHAR(4)) + '-' + CAST(YEAR(GETDATE()) AS VARCHAR(4))
        END
    );
    
    UPDATE [dbo].[tbl_salary_info]
    SET [school_year] = @CurrentSchoolYear
    WHERE [school_year] = '2024-2025';
    
    PRINT '  ✓ Existing records updated with current school year: ' + @CurrentSchoolYear;
END
GO

-- =============================================
-- STEP 2: Add threshold_percentage to tbl_roles
-- =============================================
PRINT '';
PRINT 'STEP 2: Updating tbl_roles...';

IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_roles]') 
    AND name = 'threshold_percentage'
)
BEGIN
    ALTER TABLE [dbo].[tbl_roles]
    ADD [threshold_percentage] DECIMAL(5,2) NOT NULL DEFAULT 10.00;
    
    PRINT '  ✓ Column [threshold_percentage] added to [tbl_roles] with default value 10.00%.';
END
ELSE
BEGIN
    PRINT '  ✓ Column [threshold_percentage] already exists in [tbl_roles].';
END
GO

-- =============================================
-- STEP 3: Create tbl_salary_change_requests
-- =============================================
PRINT '';
PRINT 'STEP 2: Creating tbl_salary_change_requests...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_salary_change_requests' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
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
        [school_year] VARCHAR(20) NOT NULL,
        [is_initial_registration] BIT NOT NULL DEFAULT 0,
        
        CONSTRAINT FK_tbl_salary_change_requests_tbl_Users FOREIGN KEY ([user_id]) 
            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
        CONSTRAINT FK_tbl_salary_change_requests_RequestedBy FOREIGN KEY ([requested_by]) 
            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
        CONSTRAINT FK_tbl_salary_change_requests_ApprovedBy FOREIGN KEY ([approved_by]) 
            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
        CONSTRAINT CK_tbl_salary_change_requests_Status CHECK ([status] IN ('Pending', 'Approved', 'Rejected'))
    );
    
    CREATE INDEX IX_tbl_salary_change_requests_user_id ON [dbo].[tbl_salary_change_requests]([user_id]);
    CREATE INDEX IX_tbl_salary_change_requests_requested_by ON [dbo].[tbl_salary_change_requests]([requested_by]);
    CREATE INDEX IX_tbl_salary_change_requests_status ON [dbo].[tbl_salary_change_requests]([status]);
    CREATE INDEX IX_tbl_salary_change_requests_school_year ON [dbo].[tbl_salary_change_requests]([school_year]);
    CREATE INDEX IX_tbl_salary_change_requests_requested_at ON [dbo].[tbl_salary_change_requests]([requested_at]);
    
    PRINT '  ✓ Table [tbl_salary_change_requests] created successfully.';
END
ELSE
BEGIN
    PRINT '  ✓ Table [tbl_salary_change_requests] already exists.';
END
GO

-- =============================================
-- STEP 4: Create tbl_payroll_transactions
-- =============================================
PRINT '';
PRINT 'STEP 3: Creating tbl_payroll_transactions...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_payroll_transactions' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
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
        [notes] NVARCHAR(500) NULL,
        
        CONSTRAINT FK_tbl_payroll_transactions_tbl_Users FOREIGN KEY ([user_id]) 
            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
        CONSTRAINT FK_tbl_payroll_transactions_ProcessedBy FOREIGN KEY ([processed_by]) 
            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
        CONSTRAINT CK_tbl_payroll_transactions_Status CHECK ([status] IN ('Pending', 'Paid', 'Cancelled'))
    );
    
    CREATE INDEX IX_tbl_payroll_transactions_user_id ON [dbo].[tbl_payroll_transactions]([user_id]);
    CREATE INDEX IX_tbl_payroll_transactions_school_year ON [dbo].[tbl_payroll_transactions]([school_year]);
    CREATE INDEX IX_tbl_payroll_transactions_pay_period ON [dbo].[tbl_payroll_transactions]([pay_period]);
    CREATE INDEX IX_tbl_payroll_transactions_status ON [dbo].[tbl_payroll_transactions]([status]);
    CREATE INDEX IX_tbl_payroll_transactions_processed_by ON [dbo].[tbl_payroll_transactions]([processed_by]);
    CREATE INDEX IX_tbl_payroll_transactions_created_at ON [dbo].[tbl_payroll_transactions]([created_at]);
    CREATE INDEX IX_tbl_payroll_transactions_user_schoolyear_payperiod ON [dbo].[tbl_payroll_transactions]([user_id], [school_year], [pay_period]);
    
    PRINT '  ✓ Table [tbl_payroll_transactions] created successfully.';
END
ELSE
BEGIN
    PRINT '  ✓ Table [tbl_payroll_transactions] already exists.';
END
GO

PRINT '';
PRINT '=============================================';
PRINT 'HR & Payroll Database Update Completed!';
PRINT '=============================================';
GO

