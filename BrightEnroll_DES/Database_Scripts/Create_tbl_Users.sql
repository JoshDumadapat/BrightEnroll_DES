-- =============================================
-- Script to create tbl_Users table
-- Database: DB_BrightEnroll_DES
-- =============================================

USE [DB_BrightEnroll_DES]
GO

-- Check if table exists, drop it if it does (optional - remove if you want to keep existing data)
IF OBJECT_ID('dbo.tbl_Users', 'U') IS NOT NULL
BEGIN
    PRINT 'Table tbl_Users already exists.'
    -- Uncomment the line below if you want to drop and recreate the table
    -- DROP TABLE [dbo].[tbl_Users]
END
GO

-- Create the tbl_Users table
CREATE TABLE [dbo].[tbl_Users] (
    [user_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [system_ID] VARCHAR(50) NOT NULL,
    [first_name] VARCHAR(50) NOT NULL,
    [mid_name] VARCHAR(50) NULL,
    [last_name] VARCHAR(50) NOT NULL,
    [suffix] VARCHAR(10) NULL,
    [birthdate] DATE NOT NULL,
    [age] TINYINT NOT NULL,
    [gender] VARCHAR(20) NOT NULL,
    [contact_num] VARCHAR(20) NOT NULL,
    [user_role] VARCHAR(50) NOT NULL,
    [email] VARCHAR(150) NOT NULL,
    [password] VARCHAR(255) NOT NULL,
    [date_hired] DATETIME NOT NULL
)
GO

-- Verify table creation
IF OBJECT_ID('dbo.tbl_Users', 'U') IS NOT NULL
BEGIN
    PRINT 'Table tbl_Users created successfully!'
END
ELSE
BEGIN
    PRINT 'ERROR: Table tbl_Users was not created!'
END
GO

-- Optional: Create a unique index on email to prevent duplicate emails
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Users_Email' AND object_id = OBJECT_ID('dbo.tbl_Users'))
BEGIN
    CREATE UNIQUE INDEX IX_tbl_Users_Email ON [dbo].[tbl_Users] ([email])
    PRINT 'Unique index on email created successfully!'
END
GO

