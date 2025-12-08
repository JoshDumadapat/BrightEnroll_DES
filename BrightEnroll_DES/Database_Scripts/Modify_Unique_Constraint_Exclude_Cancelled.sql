-- =============================================
-- Modify Unique Constraint to Exclude Cancelled Records
-- Allows multiple records for same employee and pay period if previous ones are cancelled
-- Each payroll generation creates a separate transaction record
-- =============================================

USE [DB_BrightEnroll_DES];
GO

-- Step 1: Drop the existing unique constraint
IF EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'UQ_tbl_payroll_transactions_UserPayPeriod' 
    AND object_id = OBJECT_ID('dbo.tbl_payroll_transactions')
)
BEGIN
    PRINT 'Dropping existing unique constraint UQ_tbl_payroll_transactions_UserPayPeriod...';
    
    ALTER TABLE [dbo].[tbl_payroll_transactions]
    DROP CONSTRAINT UQ_tbl_payroll_transactions_UserPayPeriod;
    
    PRINT '✓ Unique constraint dropped successfully.';
END
ELSE
BEGIN
    PRINT 'Unique constraint UQ_tbl_payroll_transactions_UserPayPeriod does not exist.';
END
GO

-- Step 2: Create a filtered unique index that only applies to non-cancelled records
-- This allows multiple cancelled records but only one active (Pending/Paid) record per user_id + pay_period
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly' 
    AND object_id = OBJECT_ID('dbo.tbl_payroll_transactions')
)
BEGIN
    PRINT 'Creating filtered unique index for non-cancelled records only...';
    
    CREATE UNIQUE NONCLUSTERED INDEX UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly
    ON [dbo].[tbl_payroll_transactions]([user_id], [pay_period])
    WHERE [status] IN ('Pending', 'Pending Approval', 'Paid');
    
    PRINT '✓ Filtered unique index created successfully.';
    PRINT '  This allows multiple cancelled records but only one active record per employee and pay period.';
    PRINT '  Each payroll generation creates a separate transaction record.';
END
ELSE
BEGIN
    PRINT '✓ Filtered unique index UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly already exists.';
END
GO

-- Step 3: Verify the index was created
SELECT 
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_unique AS IsUnique,
    i.filter_definition AS FilterDefinition
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('dbo.tbl_payroll_transactions')
    AND i.name = 'UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly';
GO

PRINT '';
PRINT '========================================';
PRINT 'Migration completed successfully!';
PRINT '========================================';
PRINT '';
PRINT 'The unique constraint has been modified to exclude cancelled records.';
PRINT 'This allows:';
PRINT '  - Multiple cancelled records for the same employee and pay period';
PRINT '  - Only one active (Pending/Paid) record per employee and pay period';
PRINT '  - Each payroll generation creates a separate transaction record';
PRINT '';

