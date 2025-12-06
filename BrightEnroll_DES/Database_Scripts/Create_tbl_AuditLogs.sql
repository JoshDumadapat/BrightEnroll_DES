-- =============================================
-- Create tbl_audit_logs table for enhanced audit logging
-- Supports detailed student registration logging with:
-- - Student ID, Student Name, Grade, Status, Registrar ID
-- =============================================

USE [DB_BrightEnroll_DES];
GO

-- Check if table exists, create if not
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_audit_logs' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_audit_logs](
        [log_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [timestamp] DATETIME NOT NULL DEFAULT GETDATE(),
        [user_name] VARCHAR(100) NULL,
        [user_role] VARCHAR(50) NULL,
        [user_id] INT NULL,
        [action] VARCHAR(100) NOT NULL,
        [module] VARCHAR(100) NULL,
        [description] NVARCHAR(MAX) NULL,
        [ip_address] VARCHAR(45) NULL,
        [status] VARCHAR(20) NULL,
        [severity] VARCHAR(20) NULL,
        
        -- Enhanced fields for student registration
        [student_id] VARCHAR(6) NULL,
        [student_name] VARCHAR(200) NULL,
        [grade] VARCHAR(10) NULL,
        [student_status] VARCHAR(20) NULL,
        [registrar_id] INT NULL,
        
        -- Foreign key constraints
        CONSTRAINT FK_tbl_audit_logs_tbl_Users FOREIGN KEY ([user_id]) 
            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE SET NULL,
        CONSTRAINT FK_tbl_audit_logs_tbl_Users_Registrar FOREIGN KEY ([registrar_id]) 
            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE SET NULL,
        CONSTRAINT FK_tbl_audit_logs_tbl_Students FOREIGN KEY ([student_id]) 
            REFERENCES [dbo].[tbl_Students]([student_id]) ON DELETE SET NULL
    );
    
    PRINT 'Table [tbl_audit_logs] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [tbl_audit_logs] already exists.';
END
GO

-- Create indexes for better query performance
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_audit_logs' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    -- Index on timestamp for date range queries
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_audit_logs_Timestamp' AND object_id = OBJECT_ID('dbo.tbl_audit_logs'))
    BEGIN
        CREATE INDEX IX_tbl_audit_logs_Timestamp ON [dbo].[tbl_audit_logs]([timestamp] DESC);
        PRINT 'Index [IX_tbl_audit_logs_Timestamp] created.';
    END
    
    -- Index on module for filtering by module
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_audit_logs_Module' AND object_id = OBJECT_ID('dbo.tbl_audit_logs'))
    BEGIN
        CREATE INDEX IX_tbl_audit_logs_Module ON [dbo].[tbl_audit_logs]([module]);
        PRINT 'Index [IX_tbl_audit_logs_Module] created.';
    END
    
    -- Index on action for filtering by action type
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_audit_logs_Action' AND object_id = OBJECT_ID('dbo.tbl_audit_logs'))
    BEGIN
        CREATE INDEX IX_tbl_audit_logs_Action ON [dbo].[tbl_audit_logs]([action]);
        PRINT 'Index [IX_tbl_audit_logs_Action] created.';
    END
    
    -- Index on student_id for student-related queries
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_audit_logs_StudentId' AND object_id = OBJECT_ID('dbo.tbl_audit_logs'))
    BEGIN
        CREATE INDEX IX_tbl_audit_logs_StudentId ON [dbo].[tbl_audit_logs]([student_id]);
        PRINT 'Index [IX_tbl_audit_logs_StudentId] created.';
    END
    
    -- Index on registrar_id for registrar activity queries
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_audit_logs_RegistrarId' AND object_id = OBJECT_ID('dbo.tbl_audit_logs'))
    BEGIN
        CREATE INDEX IX_tbl_audit_logs_RegistrarId ON [dbo].[tbl_audit_logs]([registrar_id]);
        PRINT 'Index [IX_tbl_audit_logs_RegistrarId] created.';
    END
    
    -- Composite index for common queries (module + timestamp)
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_audit_logs_Module_Timestamp' AND object_id = OBJECT_ID('dbo.tbl_audit_logs'))
    BEGIN
        CREATE INDEX IX_tbl_audit_logs_Module_Timestamp ON [dbo].[tbl_audit_logs]([module], [timestamp] DESC);
        PRINT 'Index [IX_tbl_audit_logs_Module_Timestamp] created.';
    END
END
GO

PRINT 'Audit log table setup completed.';
GO

