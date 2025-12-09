-- Migration Script: Update Payroll Transactions Status Constraint
-- This script updates the CHECK constraint to allow 'Pending Approval' and 'Rejected' status values
-- Run this script on existing databases to support the new payroll approval workflow

USE [DB_BrightEnroll_DES]
GO

-- Drop existing CHECK constraint
IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_tbl_payroll_transactions_Status' AND parent_object_id = OBJECT_ID('dbo.tbl_payroll_transactions'))
BEGIN
    ALTER TABLE [dbo].[tbl_payroll_transactions]
    DROP CONSTRAINT CK_tbl_payroll_transactions_Status;
    PRINT 'Dropped existing CHECK constraint CK_tbl_payroll_transactions_Status.';
END
ELSE
BEGIN
    PRINT 'CHECK constraint CK_tbl_payroll_transactions_Status does not exist.';
END
GO

-- Create new CHECK constraint with additional status values
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_tbl_payroll_transactions_Status' AND parent_object_id = OBJECT_ID('dbo.tbl_payroll_transactions'))
BEGIN
    ALTER TABLE [dbo].[tbl_payroll_transactions]
    ADD CONSTRAINT CK_tbl_payroll_transactions_Status 
    CHECK ([status] IN ('Pending', 'Pending Approval', 'Paid', 'Cancelled', 'Rejected'));
    PRINT 'Created new CHECK constraint CK_tbl_payroll_transactions_Status with status values: Pending, Pending Approval, Paid, Cancelled, Rejected.';
END
ELSE
BEGIN
    PRINT 'CHECK constraint CK_tbl_payroll_transactions_Status already exists.';
END
GO

-- Update the filtered unique index to include 'Pending Approval' in active statuses
-- First, check if the index exists and needs updating
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly' AND object_id = OBJECT_ID('dbo.tbl_payroll_transactions'))
BEGIN
    -- Drop the existing filtered index to recreate it with updated status values
    DROP INDEX UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly ON [dbo].[tbl_payroll_transactions];
    PRINT 'Dropped existing filtered unique index UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly.';
    
    -- Recreate the filtered unique index with 'Pending Approval' included
    CREATE UNIQUE NONCLUSTERED INDEX UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly
    ON [dbo].[tbl_payroll_transactions]([user_id], [pay_period])
    WHERE [status] IN ('Pending', 'Pending Approval', 'Paid');
    PRINT 'Recreated filtered unique index UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly with status values: Pending, Pending Approval, Paid.';
END
ELSE
BEGIN
    -- Create the index if it doesn't exist
    CREATE UNIQUE NONCLUSTERED INDEX UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly
    ON [dbo].[tbl_payroll_transactions]([user_id], [pay_period])
    WHERE [status] IN ('Pending', 'Pending Approval', 'Paid');
    PRINT 'Created filtered unique index UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly with status values: Pending, Pending Approval, Paid.';
END
GO

PRINT 'Migration completed successfully. Payroll transactions now support Pending Approval and Rejected statuses.';
GO

