-- =============================================
-- Add effective_date column to tbl_salary_change_requests
-- This stores the date when the approved salary change should take effect
-- =============================================

USE [DB_BrightEnroll_DES];
GO

IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_salary_change_requests]') 
    AND name = 'effective_date'
)
BEGIN
    ALTER TABLE [dbo].[tbl_salary_change_requests]
    ADD [effective_date] DATE NULL;
    
    PRINT 'Column [effective_date] added to [tbl_salary_change_requests] successfully.';
END
ELSE
BEGIN
    PRINT 'Column [effective_date] already exists in [tbl_salary_change_requests].';
END
GO

