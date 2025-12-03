-- Add is_verified column to tbl_StudentRequirements table
-- This column tracks whether a requirement has been verified by staff

USE DB_BrightEnroll_DES;
GO

IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_StudentRequirements]') 
    AND name = 'is_verified'
)
BEGIN
    ALTER TABLE [dbo].[tbl_StudentRequirements]
    ADD [is_verified] BIT NOT NULL DEFAULT 0;
    
    PRINT 'Column [is_verified] added to [tbl_StudentRequirements] table.';
END
ELSE
BEGIN
    PRINT 'Column [is_verified] already exists in [tbl_StudentRequirements] table.';
END
GO

