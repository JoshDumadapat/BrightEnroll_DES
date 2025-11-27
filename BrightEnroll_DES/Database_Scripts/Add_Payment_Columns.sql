-- Add payment tracking columns to tbl_Students table
-- Run this script to add the payment columns that are currently ignored in EF Core

USE DB_BrightEnroll_DES;
GO

-- Check if columns already exist before adding
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Students') AND name = 'amount_paid')
BEGIN
    ALTER TABLE dbo.tbl_Students
    ADD amount_paid DECIMAL(18,2) NOT NULL DEFAULT 0;
    PRINT 'Column amount_paid added successfully.';
END
ELSE
BEGIN
    PRINT 'Column amount_paid already exists.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Students') AND name = 'payment_status')
BEGIN
    ALTER TABLE dbo.tbl_Students
    ADD payment_status VARCHAR(20) NULL;
    PRINT 'Column payment_status added successfully.';
END
ELSE
BEGIN
    PRINT 'Column payment_status already exists.';
END
GO

PRINT 'Payment columns script completed.';
GO

