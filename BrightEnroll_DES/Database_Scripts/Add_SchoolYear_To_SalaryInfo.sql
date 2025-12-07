-- =============================================
-- Add school_year column to tbl_salary_info
-- This script updates the existing table to include school year tracking
-- =============================================

USE [DB_BrightEnroll_DES];
GO

-- Check if school_year column exists, if not add it
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.tbl_salary_info') 
    AND name = 'school_year'
)
BEGIN
    ALTER TABLE [dbo].[tbl_salary_info]
    ADD [school_year] VARCHAR(20) NOT NULL DEFAULT '2024-2025';
    
    PRINT 'Column [school_year] added to [tbl_salary_info].';
    
    -- Update existing records with current school year (you may need to adjust this)
    UPDATE [dbo].[tbl_salary_info]
    SET [school_year] = (
        CASE 
            WHEN MONTH(GETDATE()) >= 6 THEN 
                CAST(YEAR(GETDATE()) AS VARCHAR(4)) + '-' + CAST(YEAR(GETDATE()) + 1 AS VARCHAR(4))
            ELSE 
                CAST(YEAR(GETDATE()) - 1 AS VARCHAR(4)) + '-' + CAST(YEAR(GETDATE()) AS VARCHAR(4))
        END
    )
    WHERE [school_year] = '2024-2025' OR [school_year] IS NULL;
    
    PRINT 'Existing records updated with current school year.';
END
ELSE
BEGIN
    PRINT 'Column [school_year] already exists in [tbl_salary_info].';
END
GO

