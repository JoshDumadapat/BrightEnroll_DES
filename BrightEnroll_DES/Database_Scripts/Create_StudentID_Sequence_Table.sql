-- =============================================
-- Create Student ID Sequence Table
-- Run this script in SSMS to create tbl_StudentID_Sequence
-- =============================================

USE [DB_BrightEnroll_DES];
GO

-- Create sequence table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_StudentID_Sequence' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_StudentID_Sequence](
        [sequence_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [LastStudentID] INT NOT NULL DEFAULT 158021
    );
    
    -- Insert initial sequence value
    INSERT INTO [dbo].[tbl_StudentID_Sequence] ([LastStudentID]) VALUES (158021);
    
    PRINT 'Table [tbl_StudentID_Sequence] created successfully.';
END
ELSE
BEGIN
    -- If table exists but is empty, insert initial value
    IF NOT EXISTS (SELECT * FROM [dbo].[tbl_StudentID_Sequence])
    BEGIN
        INSERT INTO [dbo].[tbl_StudentID_Sequence] ([LastStudentID]) VALUES (158021);
        PRINT 'Initial sequence value inserted into existing [tbl_StudentID_Sequence] table.';
    END
    ELSE
    BEGIN
        PRINT 'Table [tbl_StudentID_Sequence] already exists with data.';
    END
END
GO

-- Verify the table and data
SELECT * FROM [dbo].[tbl_StudentID_Sequence];
GO

