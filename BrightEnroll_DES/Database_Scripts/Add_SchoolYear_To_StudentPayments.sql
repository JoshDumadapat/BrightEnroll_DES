-- Add school_year column to tbl_StudentPayments if it doesn't exist
-- This fixes the error: Invalid column name 'school_year' in Finance module

IF NOT EXISTS (
    SELECT * 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.tbl_StudentPayments') 
    AND name = 'school_year'
)
BEGIN
    ALTER TABLE [dbo].[tbl_StudentPayments]
    ADD [school_year] VARCHAR(20) NULL;
    
    PRINT 'Column [school_year] added to [tbl_StudentPayments] successfully.';
END
ELSE
BEGIN
    PRINT 'Column [school_year] already exists in [tbl_StudentPayments].';
END
GO

-- Create index for school_year if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentPayments_school_year' AND object_id = OBJECT_ID('dbo.tbl_StudentPayments'))
BEGIN
    CREATE INDEX IX_tbl_StudentPayments_school_year 
    ON [dbo].[tbl_StudentPayments]([school_year])
    WHERE [school_year] IS NOT NULL;
    
    PRINT 'Index [IX_tbl_StudentPayments_school_year] created successfully.';
END
ELSE
BEGIN
    PRINT 'Index [IX_tbl_StudentPayments_school_year] already exists.';
END
GO

-- Backfill: Link existing payments to enrollment's school year
UPDATE p
SET p.school_year = e.school_yr
FROM [dbo].[tbl_StudentPayments] p
INNER JOIN [dbo].[tbl_StudentSectionEnrollment] e 
    ON p.student_id = e.student_id
    AND e.status = 'Enrolled'
WHERE p.school_year IS NULL;

PRINT 'Backfilled school_year for existing payments.';
GO

