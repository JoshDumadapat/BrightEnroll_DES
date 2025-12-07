-- =============================================
-- Add batch_timestamp column to tbl_payroll_transactions
-- This field groups transactions processed in the same batch
-- =============================================

USE [DB_BrightEnroll_DES];
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'batch_timestamp' AND object_id = OBJECT_ID('dbo.tbl_payroll_transactions'))
BEGIN
    ALTER TABLE [dbo].[tbl_payroll_transactions]
    ADD [batch_timestamp] DATETIME NULL;
    
    -- Create index for batch queries
    CREATE INDEX IX_tbl_payroll_transactions_batch_timestamp 
    ON [dbo].[tbl_payroll_transactions]([batch_timestamp]);
    
    PRINT 'Column [batch_timestamp] added to [tbl_payroll_transactions] successfully.';
END
ELSE
BEGIN
    PRINT 'Column [batch_timestamp] already exists in [tbl_payroll_transactions].';
END
GO

