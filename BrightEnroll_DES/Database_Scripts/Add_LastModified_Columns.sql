-- Script to add LastModified columns to tables for incremental sync
-- Run this on both local and cloud databases

-- Add LastModified column to tables that don't have it
-- This enables incremental sync by tracking when records were last modified

-- Users table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('tbl_Users') AND name = 'LastModified')
BEGIN
    ALTER TABLE [tbl_Users] ADD [LastModified] DATETIME DEFAULT GETDATE();
    CREATE INDEX IX_Users_LastModified ON [tbl_Users]([LastModified]);
END
GO

-- Guardians table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('tbl_Guardians') AND name = 'LastModified')
BEGIN
    ALTER TABLE [tbl_Guardians] ADD [LastModified] DATETIME DEFAULT GETDATE();
    CREATE INDEX IX_Guardians_LastModified ON [tbl_Guardians]([LastModified]);
END
GO

-- GradeLevel table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('tbl_GradeLevel') AND name = 'LastModified')
BEGIN
    ALTER TABLE [tbl_GradeLevel] ADD [LastModified] DATETIME DEFAULT GETDATE();
    CREATE INDEX IX_GradeLevel_LastModified ON [tbl_GradeLevel]([LastModified]);
END
GO

-- Students table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('tbl_Students') AND name = 'LastModified')
BEGIN
    ALTER TABLE [tbl_Students] ADD [LastModified] DATETIME DEFAULT GETDATE();
    CREATE INDEX IX_Students_LastModified ON [tbl_Students]([LastModified]);
END
GO

-- StudentRequirements table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('tbl_StudentRequirements') AND name = 'LastModified')
BEGIN
    ALTER TABLE [tbl_StudentRequirements] ADD [LastModified] DATETIME DEFAULT GETDATE();
    CREATE INDEX IX_StudentRequirements_LastModified ON [tbl_StudentRequirements]([LastModified]);
END
GO

-- StudentPayments table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('tbl_StudentPayments') AND name = 'LastModified')
BEGIN
    ALTER TABLE [tbl_StudentPayments] ADD [LastModified] DATETIME DEFAULT GETDATE();
    CREATE INDEX IX_StudentPayments_LastModified ON [tbl_StudentPayments]([LastModified]);
END
GO

-- Fees table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('tbl_Fees') AND name = 'LastModified')
BEGIN
    ALTER TABLE [tbl_Fees] ADD [LastModified] DATETIME DEFAULT GETDATE();
    CREATE INDEX IX_Fees_LastModified ON [tbl_Fees]([LastModified]);
END
GO

-- Expenses table (already has updated_at, but we'll add LastModified for consistency)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('tbl_Expenses') AND name = 'LastModified')
BEGIN
    ALTER TABLE [tbl_Expenses] ADD [LastModified] DATETIME DEFAULT GETDATE();
    CREATE INDEX IX_Expenses_LastModified ON [tbl_Expenses]([LastModified]);
END
GO

-- EmployeeAddress table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('tbl_employee_address') AND name = 'LastModified')
BEGIN
    ALTER TABLE [tbl_employee_address] ADD [LastModified] DATETIME DEFAULT GETDATE();
    CREATE INDEX IX_EmployeeAddress_LastModified ON [tbl_employee_address]([LastModified]);
END
GO

-- Sections table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('tbl_Sections') AND name = 'LastModified')
BEGIN
    ALTER TABLE [tbl_Sections] ADD [LastModified] DATETIME DEFAULT GETDATE();
    CREATE INDEX IX_Sections_LastModified ON [tbl_Sections]([LastModified]);
END
GO

-- Subjects table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('tbl_Subjects') AND name = 'LastModified')
BEGIN
    ALTER TABLE [tbl_Subjects] ADD [LastModified] DATETIME DEFAULT GETDATE();
    CREATE INDEX IX_Subjects_LastModified ON [tbl_Subjects]([LastModified]);
END
GO

-- Create trigger to update LastModified on UPDATE for all tables
-- This ensures LastModified is automatically updated when records change

-- Users trigger
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_Users_UpdateLastModified')
    DROP TRIGGER [trg_Users_UpdateLastModified];
GO
CREATE TRIGGER [trg_Users_UpdateLastModified] ON [tbl_Users]
AFTER UPDATE
AS
BEGIN
    UPDATE [tbl_Users]
    SET [LastModified] = GETDATE()
    FROM [tbl_Users] u
    INNER JOIN inserted i ON u.[user_ID] = i.[user_ID];
END
GO

-- Students trigger
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_Students_UpdateLastModified')
    DROP TRIGGER [trg_Students_UpdateLastModified];
GO
CREATE TRIGGER [trg_Students_UpdateLastModified] ON [tbl_Students]
AFTER UPDATE
AS
BEGIN
    UPDATE [tbl_Students]
    SET [LastModified] = GETDATE()
    FROM [tbl_Students] s
    INNER JOIN inserted i ON s.[student_id] = i.[student_id];
END
GO

-- Guardians trigger
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_Guardians_UpdateLastModified')
    DROP TRIGGER [trg_Guardians_UpdateLastModified];
GO
CREATE TRIGGER [trg_Guardians_UpdateLastModified] ON [tbl_Guardians]
AFTER UPDATE
AS
BEGIN
    UPDATE [tbl_Guardians]
    SET [LastModified] = GETDATE()
    FROM [tbl_Guardians] g
    INNER JOIN inserted i ON g.[guardian_id] = i.[guardian_id];
END
GO

-- StudentPayments trigger
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_StudentPayments_UpdateLastModified')
    DROP TRIGGER [trg_StudentPayments_UpdateLastModified];
GO
CREATE TRIGGER [trg_StudentPayments_UpdateLastModified] ON [tbl_StudentPayments]
AFTER UPDATE
AS
BEGIN
    UPDATE [tbl_StudentPayments]
    SET [LastModified] = GETDATE()
    FROM [tbl_StudentPayments] p
    INNER JOIN inserted i ON p.[payment_id] = i.[payment_id];
END
GO

-- Expenses trigger (update both updated_at and LastModified)
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_Expenses_UpdateLastModified')
    DROP TRIGGER [trg_Expenses_UpdateLastModified];
GO
CREATE TRIGGER [trg_Expenses_UpdateLastModified] ON [tbl_Expenses]
AFTER UPDATE
AS
BEGIN
    UPDATE [tbl_Expenses]
    SET [LastModified] = GETDATE(),
        [updated_at] = GETDATE()
    FROM [tbl_Expenses] e
    INNER JOIN inserted i ON e.[expense_ID] = i.[expense_ID];
END
GO

PRINT 'LastModified columns and triggers created successfully!';
PRINT 'Run this script on both local and cloud databases.';

