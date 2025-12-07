-- Add threshold_percentage column to tbl_roles table
-- This column stores the percentage threshold for salary changes that require approval

USE [DB_BrightEnroll_DES];
GO

-- Check if column exists, if not add it
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_roles]') 
    AND name = 'threshold_percentage'
)
BEGIN
    ALTER TABLE [dbo].[tbl_roles]
    ADD [threshold_percentage] DECIMAL(5,2) NOT NULL DEFAULT 10.00;
    
    PRINT 'Column [threshold_percentage] added to [tbl_roles] with default value 10.00%.';
END
ELSE
BEGIN
    PRINT 'Column [threshold_percentage] already exists in [tbl_roles].';
END
GO

