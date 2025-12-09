-- =============================================
-- Add Audit Trail and Company Contributions to tbl_payroll_transactions
-- =============================================

USE [DB_BrightEnroll_DES];
GO

-- Add Company Contribution columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_payroll_transactions') AND name = 'company_sss_contribution')
BEGIN
    ALTER TABLE [dbo].[tbl_payroll_transactions]
    ADD [company_sss_contribution] DECIMAL(12,2) NOT NULL DEFAULT 0.00;
    PRINT 'Column [company_sss_contribution] added to [tbl_payroll_transactions].';
END
ELSE
BEGIN
    PRINT 'Column [company_sss_contribution] already exists in [tbl_payroll_transactions].';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_payroll_transactions') AND name = 'company_philhealth_contribution')
BEGIN
    ALTER TABLE [dbo].[tbl_payroll_transactions]
    ADD [company_philhealth_contribution] DECIMAL(12,2) NOT NULL DEFAULT 0.00;
    PRINT 'Column [company_philhealth_contribution] added to [tbl_payroll_transactions].';
END
ELSE
BEGIN
    PRINT 'Column [company_philhealth_contribution] already exists in [tbl_payroll_transactions].';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_payroll_transactions') AND name = 'company_pagibig_contribution')
BEGIN
    ALTER TABLE [dbo].[tbl_payroll_transactions]
    ADD [company_pagibig_contribution] DECIMAL(12,2) NOT NULL DEFAULT 0.00;
    PRINT 'Column [company_pagibig_contribution] added to [tbl_payroll_transactions].';
END
ELSE
BEGIN
    PRINT 'Column [company_pagibig_contribution] already exists in [tbl_payroll_transactions].';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_payroll_transactions') AND name = 'total_company_contribution')
BEGIN
    ALTER TABLE [dbo].[tbl_payroll_transactions]
    ADD [total_company_contribution] DECIMAL(12,2) NOT NULL DEFAULT 0.00;
    PRINT 'Column [total_company_contribution] added to [tbl_payroll_transactions].';
END
ELSE
BEGIN
    PRINT 'Column [total_company_contribution] already exists in [tbl_payroll_transactions].';
END
GO

-- Add Audit Trail columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_payroll_transactions') AND name = 'created_by')
BEGIN
    ALTER TABLE [dbo].[tbl_payroll_transactions]
    ADD [created_by] INT NOT NULL DEFAULT 1;
    
    -- Update existing records to use processed_by as created_by
    UPDATE [dbo].[tbl_payroll_transactions]
    SET [created_by] = [processed_by]
    WHERE [created_by] = 1;
    
    -- Add foreign key constraint
    ALTER TABLE [dbo].[tbl_payroll_transactions]
    ADD CONSTRAINT FK_tbl_payroll_transactions_CreatedBy 
    FOREIGN KEY ([created_by]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION;
    
    PRINT 'Column [created_by] added to [tbl_payroll_transactions].';
END
ELSE
BEGIN
    PRINT 'Column [created_by] already exists in [tbl_payroll_transactions].';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_payroll_transactions') AND name = 'approved_by')
BEGIN
    ALTER TABLE [dbo].[tbl_payroll_transactions]
    ADD [approved_by] INT NULL;
    
    -- Add foreign key constraint
    ALTER TABLE [dbo].[tbl_payroll_transactions]
    ADD CONSTRAINT FK_tbl_payroll_transactions_ApprovedBy 
    FOREIGN KEY ([approved_by]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION;
    
    PRINT 'Column [approved_by] added to [tbl_payroll_transactions].';
END
ELSE
BEGIN
    PRINT 'Column [approved_by] already exists in [tbl_payroll_transactions].';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_payroll_transactions') AND name = 'approved_at')
BEGIN
    ALTER TABLE [dbo].[tbl_payroll_transactions]
    ADD [approved_at] DATETIME NULL;
    PRINT 'Column [approved_at] added to [tbl_payroll_transactions].';
END
ELSE
BEGIN
    PRINT 'Column [approved_at] already exists in [tbl_payroll_transactions].';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_payroll_transactions') AND name = 'cancelled_by')
BEGIN
    ALTER TABLE [dbo].[tbl_payroll_transactions]
    ADD [cancelled_by] INT NULL;
    
    -- Add foreign key constraint
    ALTER TABLE [dbo].[tbl_payroll_transactions]
    ADD CONSTRAINT FK_tbl_payroll_transactions_CancelledBy 
    FOREIGN KEY ([cancelled_by]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION;
    
    PRINT 'Column [cancelled_by] added to [tbl_payroll_transactions].';
END
ELSE
BEGIN
    PRINT 'Column [cancelled_by] already exists in [tbl_payroll_transactions].';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_payroll_transactions') AND name = 'cancelled_at')
BEGIN
    ALTER TABLE [dbo].[tbl_payroll_transactions]
    ADD [cancelled_at] DATETIME NULL;
    PRINT 'Column [cancelled_at] added to [tbl_payroll_transactions].';
END
ELSE
BEGIN
    PRINT 'Column [cancelled_at] already exists in [tbl_payroll_transactions].';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_payroll_transactions') AND name = 'cancellation_reason')
BEGIN
    ALTER TABLE [dbo].[tbl_payroll_transactions]
    ADD [cancellation_reason] NVARCHAR(500) NULL;
    PRINT 'Column [cancellation_reason] added to [tbl_payroll_transactions].';
END
ELSE
BEGIN
    PRINT 'Column [cancellation_reason] already exists in [tbl_payroll_transactions].';
END
GO

PRINT '';
PRINT 'Migration completed successfully!';
GO

