-- Create tbl_StudentPayments table for payment tracking
-- This table logs all individual student payments with OR numbers for audit and receipt history

USE DB_BrightEnroll_DES;
GO

-- Check if table already exists before creating
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
        CONSTRAINT FK_tbl_StudentPayments_tbl_Students FOREIGN KEY ([student_id]) REFERENCES [dbo].[tbl_Students]([student_ID]) ON DELETE CASCADE
    );
    
    -- Create indexes for better query performance
    CREATE INDEX IX_tbl_StudentPayments_StudentId ON [dbo].[tbl_StudentPayments]([student_id]);
    CREATE UNIQUE INDEX IX_tbl_StudentPayments_OrNumber ON [dbo].[tbl_StudentPayments]([or_number]);
    CREATE INDEX IX_tbl_StudentPayments_CreatedAt ON [dbo].[tbl_StudentPayments]([created_at]);
    
    PRINT 'Table tbl_StudentPayments created successfully.';
END
ELSE
BEGIN
    PRINT 'Table tbl_StudentPayments already exists.';
END
GO

PRINT 'StudentPayments table script completed.';
GO

