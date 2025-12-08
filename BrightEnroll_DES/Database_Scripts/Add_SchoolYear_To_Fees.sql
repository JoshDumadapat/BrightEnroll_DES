-- Add school_year column to tbl_Fees if it doesn't exist
-- This fixes the error: Invalid column name 'school_year'

IF NOT EXISTS (
    SELECT * 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.tbl_Fees') 
    AND name = 'school_year'
)
BEGIN
    ALTER TABLE [dbo].[tbl_Fees]
    ADD [school_year] VARCHAR(20) NULL;
    
    PRINT 'Column [school_year] added to [tbl_Fees] successfully.';
END
ELSE
BEGIN
    PRINT 'Column [school_year] already exists in [tbl_Fees].';
END
GO

-- Create indexes for school_year if they don't exist
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Fees_school_year' AND object_id = OBJECT_ID('dbo.tbl_Fees'))
BEGIN
    CREATE INDEX IX_tbl_Fees_school_year 
    ON [dbo].[tbl_Fees]([school_year])
    WHERE [school_year] IS NOT NULL;
    
    PRINT 'Index [IX_tbl_Fees_school_year] created successfully.';
END
ELSE
BEGIN
    PRINT 'Index [IX_tbl_Fees_school_year] already exists.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Fees_gradelevel_schoolyear' AND object_id = OBJECT_ID('dbo.tbl_Fees'))
BEGIN
    CREATE INDEX IX_tbl_Fees_gradelevel_schoolyear 
    ON [dbo].[tbl_Fees]([gradelevel_ID], [school_year])
    WHERE [school_year] IS NOT NULL;
    
    PRINT 'Index [IX_tbl_Fees_gradelevel_schoolyear] created successfully.';
END
ELSE
BEGIN
    PRINT 'Index [IX_tbl_Fees_gradelevel_schoolyear] already exists.';
END
GO

