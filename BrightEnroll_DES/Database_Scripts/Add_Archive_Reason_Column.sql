IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Students]') 
    AND name = 'archive_reason'
)
BEGIN
    ALTER TABLE [dbo].[tbl_Students]
    ADD [archive_reason] TEXT NULL;
    
    PRINT 'Column [archive_reason] added to [tbl_Students] table.';
END
ELSE
BEGIN
    PRINT 'Column [archive_reason] already exists in [tbl_Students] table.';
END
GO

