-- Script to create tbl_StudentPayments table if it doesn't exist
-- Run this in SQL Server Management Studio (SSMS) if the table is missing

USE [DB_BrightEnroll_DES];
GO

-- Check if table exists, if not create it
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_StudentPayments' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_StudentPayments](
        [payment_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [student_id] VARCHAR(6) NOT NULL,
        [amount] DECIMAL(18,2) NOT NULL,
        [payment_method] VARCHAR(50) NOT NULL,
        [or_number] VARCHAR(50) NOT NULL,
        [processed_by] VARCHAR(50) NULL,
        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_tbl_StudentPayments_tbl_Students FOREIGN KEY ([student_id]) REFERENCES [dbo].[tbl_Students]([student_id]) ON DELETE CASCADE
    );
    
    PRINT 'Table [tbl_StudentPayments] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [tbl_StudentPayments] already exists.';
END
GO

-- Create indexes if they don't exist
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_StudentPayments' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    -- Index on StudentId
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentPayments_StudentId' AND object_id = OBJECT_ID('dbo.tbl_StudentPayments'))
    BEGIN
        CREATE INDEX IX_tbl_StudentPayments_StudentId ON [dbo].[tbl_StudentPayments]([student_id]);
        PRINT 'Index [IX_tbl_StudentPayments_StudentId] created.';
    END
    
    -- Unique index on OR Number
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentPayments_OrNumber' AND object_id = OBJECT_ID('dbo.tbl_StudentPayments'))
    BEGIN
        CREATE UNIQUE INDEX IX_tbl_StudentPayments_OrNumber ON [dbo].[tbl_StudentPayments]([or_number]);
        PRINT 'Unique index [IX_tbl_StudentPayments_OrNumber] created.';
    END
    
    -- Index on CreatedAt
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_StudentPayments_CreatedAt' AND object_id = OBJECT_ID('dbo.tbl_StudentPayments'))
    BEGIN
        CREATE INDEX IX_tbl_StudentPayments_CreatedAt ON [dbo].[tbl_StudentPayments]([created_at]);
        PRINT 'Index [IX_tbl_StudentPayments_CreatedAt] created.';
    END
END
GO

PRINT 'Script completed.';
GO

