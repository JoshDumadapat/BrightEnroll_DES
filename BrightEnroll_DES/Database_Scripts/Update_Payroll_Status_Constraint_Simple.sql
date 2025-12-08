-- =============================================
-- QUICK FIX: Update Payroll Status Constraint
-- Run this script in SQL Server Management Studio or sqlcmd
-- =============================================

USE [DB_BrightEnroll_DES]
GO

-- Step 1: Drop the old constraint
ALTER TABLE [dbo].[tbl_payroll_transactions]
DROP CONSTRAINT CK_tbl_payroll_transactions_Status;
GO

-- Step 2: Add the new constraint with all status values
ALTER TABLE [dbo].[tbl_payroll_transactions]
ADD CONSTRAINT CK_tbl_payroll_transactions_Status 
CHECK ([status] IN ('Pending', 'Pending Approval', 'Paid', 'Cancelled', 'Rejected'));
GO

-- Step 3: Update the filtered unique index (drop and recreate)
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly' AND object_id = OBJECT_ID('dbo.tbl_payroll_transactions'))
BEGIN
    DROP INDEX UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly ON [dbo].[tbl_payroll_transactions];
END
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly
ON [dbo].[tbl_payroll_transactions]([user_id], [pay_period])
WHERE [status] IN ('Pending', 'Pending Approval', 'Paid');
GO

PRINT 'SUCCESS: Payroll status constraint updated! You can now use "Pending Approval" status.';
GO

