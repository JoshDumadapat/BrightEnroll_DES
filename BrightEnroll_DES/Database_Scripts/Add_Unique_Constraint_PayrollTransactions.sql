-- =============================================
-- Add Unique Constraint to tbl_payroll_transactions
-- Prevents duplicate payroll transactions for same employee and pay period
-- =============================================

USE [DB_BrightEnroll_DES];
GO

-- Check if constraint already exists
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'UQ_tbl_payroll_transactions_UserPayPeriod' 
    AND object_id = OBJECT_ID('dbo.tbl_payroll_transactions')
)
BEGIN
    -- First, check for existing duplicates and remove them (keep the most recent one)
    PRINT 'Checking for duplicate payroll transactions...';
    
    WITH DuplicateTransactions AS (
        SELECT 
            transaction_id,
            user_id,
            pay_period,
            ROW_NUMBER() OVER (
                PARTITION BY user_id, pay_period 
                ORDER BY created_at DESC, transaction_id DESC
            ) AS RowNum
        FROM [dbo].[tbl_payroll_transactions]
    )
    DELETE FROM [dbo].[tbl_payroll_transactions]
    WHERE transaction_id IN (
        SELECT transaction_id 
        FROM DuplicateTransactions 
        WHERE RowNum > 1
    );
    
    PRINT 'Duplicate transactions removed (kept most recent for each employee-pay period combination).';
    
    -- Add unique constraint
    ALTER TABLE [dbo].[tbl_payroll_transactions]
    ADD CONSTRAINT UQ_tbl_payroll_transactions_UserPayPeriod 
    UNIQUE ([user_id], [pay_period]);
    
    PRINT '✓ Unique constraint UQ_tbl_payroll_transactions_UserPayPeriod added successfully.';
    PRINT '  This prevents duplicate payroll transactions for the same employee and pay period.';
END
ELSE
BEGIN
    PRINT '✓ Unique constraint UQ_tbl_payroll_transactions_UserPayPeriod already exists.';
END
GO

