-- =============================================
-- VERIFY AND FIX: Payroll Status Constraint
-- Run this script to check and fix the constraint
-- =============================================

USE [DB_BrightEnroll_DES]
GO

-- Step 1: Check current constraint definition
PRINT '========================================';
PRINT 'STEP 1: Checking current constraint...';
PRINT '========================================';
GO

SELECT 
    cc.name AS ConstraintName,
    cc.definition AS ConstraintDefinition
FROM sys.check_constraints cc
WHERE cc.name = 'CK_tbl_payroll_transactions_Status'
    AND cc.parent_object_id = OBJECT_ID('dbo.tbl_payroll_transactions');
GO

-- Step 2: Drop the old constraint
PRINT '';
PRINT '========================================';
PRINT 'STEP 2: Dropping old constraint...';
PRINT '========================================';
GO

IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_tbl_payroll_transactions_Status' AND parent_object_id = OBJECT_ID('dbo.tbl_payroll_transactions'))
BEGIN
    ALTER TABLE [dbo].[tbl_payroll_transactions]
    DROP CONSTRAINT CK_tbl_payroll_transactions_Status;
    PRINT '✓ Old constraint dropped successfully.';
END
ELSE
BEGIN
    PRINT '⚠ Constraint does not exist (this is OK if it was already dropped).';
END
GO

-- Step 3: Create new constraint with all status values
PRINT '';
PRINT '========================================';
PRINT 'STEP 3: Creating new constraint...';
PRINT '========================================';
GO

ALTER TABLE [dbo].[tbl_payroll_transactions]
ADD CONSTRAINT CK_tbl_payroll_transactions_Status 
CHECK ([status] IN ('Pending', 'Pending Approval', 'Paid', 'Cancelled', 'Rejected'));
GO

PRINT '✓ New constraint created with status values:';
PRINT '  - Pending';
PRINT '  - Pending Approval (NEW)';
PRINT '  - Paid';
PRINT '  - Cancelled';
PRINT '  - Rejected (NEW)';
GO

-- Step 4: Update the filtered unique index
PRINT '';
PRINT '========================================';
PRINT 'STEP 4: Updating filtered unique index...';
PRINT '========================================';
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly' AND object_id = OBJECT_ID('dbo.tbl_payroll_transactions'))
BEGIN
    DROP INDEX UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly ON [dbo].[tbl_payroll_transactions];
    PRINT '✓ Old index dropped.';
END
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_tbl_payroll_transactions_UserPayPeriod_ActiveOnly
ON [dbo].[tbl_payroll_transactions]([user_id], [pay_period])
WHERE [status] IN ('Pending', 'Pending Approval', 'Paid');
GO

PRINT '✓ New index created with active statuses:';
PRINT '  - Pending';
PRINT '  - Pending Approval (NEW)';
PRINT '  - Paid';
GO

-- Step 5: Verify the fix
PRINT '';
PRINT '========================================';
PRINT 'STEP 5: Verifying the fix...';
PRINT '========================================';
GO

SELECT 
    cc.name AS ConstraintName,
    cc.definition AS ConstraintDefinition
FROM sys.check_constraints cc
WHERE cc.name = 'CK_tbl_payroll_transactions_Status'
    AND cc.parent_object_id = OBJECT_ID('dbo.tbl_payroll_transactions');
GO

PRINT '';
PRINT '========================================';
PRINT '✅ SUCCESS!';
PRINT '========================================';
PRINT 'The payroll status constraint has been updated.';
PRINT 'You can now process payroll and send it for approval.';
PRINT '';
PRINT 'Allowed status values:';
PRINT '  ✓ Pending';
PRINT '  ✓ Pending Approval';
PRINT '  ✓ Paid';
PRINT '  ✓ Cancelled';
PRINT '  ✓ Rejected';
PRINT '';
GO

