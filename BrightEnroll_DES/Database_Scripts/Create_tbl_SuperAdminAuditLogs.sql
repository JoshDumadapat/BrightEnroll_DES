-- =============================================
-- Create tbl_SuperAdminAuditLogs table for Super Admin audit logging
-- Separate table specifically for Super Admin actions
-- =============================================

USE [DB_BrightEnroll_DES];
GO

-- Check if table exists, create if not
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_SuperAdminAuditLogs' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_SuperAdminAuditLogs](
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
        [entity_type] VARCHAR(100) NULL,
        [entity_id] VARCHAR(50) NULL,
        [old_values] NVARCHAR(MAX) NULL,
        [new_values] NVARCHAR(MAX) NULL,
        [customer_code] VARCHAR(50) NULL,
        [customer_name] VARCHAR(200) NULL
    );
    
    PRINT 'Table [tbl_SuperAdminAuditLogs] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [tbl_SuperAdminAuditLogs] already exists.';
END
GO

-- Create indexes for better query performance
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_SuperAdminAuditLogs' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    -- Index on timestamp for date range queries
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SuperAdminAuditLogs_Timestamp' AND object_id = OBJECT_ID('dbo.tbl_SuperAdminAuditLogs'))
    BEGIN
        CREATE INDEX IX_SuperAdminAuditLogs_Timestamp ON [dbo].[tbl_SuperAdminAuditLogs]([timestamp] DESC);
        PRINT 'Index IX_SuperAdminAuditLogs_Timestamp created.';
    END

    -- Index on user_id for user-specific queries
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SuperAdminAuditLogs_UserId' AND object_id = OBJECT_ID('dbo.tbl_SuperAdminAuditLogs'))
    BEGIN
        CREATE INDEX IX_SuperAdminAuditLogs_UserId ON [dbo].[tbl_SuperAdminAuditLogs]([user_id]);
        PRINT 'Index IX_SuperAdminAuditLogs_UserId created.';
    END

    -- Index on module for module-specific queries
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SuperAdminAuditLogs_Module' AND object_id = OBJECT_ID('dbo.tbl_SuperAdminAuditLogs'))
    BEGIN
        CREATE INDEX IX_SuperAdminAuditLogs_Module ON [dbo].[tbl_SuperAdminAuditLogs]([module]);
        PRINT 'Index IX_SuperAdminAuditLogs_Module created.';
    END

    -- Index on action for action-specific queries
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SuperAdminAuditLogs_Action' AND object_id = OBJECT_ID('dbo.tbl_SuperAdminAuditLogs'))
    BEGIN
        CREATE INDEX IX_SuperAdminAuditLogs_Action ON [dbo].[tbl_SuperAdminAuditLogs]([action]);
        PRINT 'Index IX_SuperAdminAuditLogs_Action created.';
    END

    -- Index on status for status filtering
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SuperAdminAuditLogs_Status' AND object_id = OBJECT_ID('dbo.tbl_SuperAdminAuditLogs'))
    BEGIN
        CREATE INDEX IX_SuperAdminAuditLogs_Status ON [dbo].[tbl_SuperAdminAuditLogs]([status]);
        PRINT 'Index IX_SuperAdminAuditLogs_Status created.';
    END

    -- Index on severity for severity filtering
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SuperAdminAuditLogs_Severity' AND object_id = OBJECT_ID('dbo.tbl_SuperAdminAuditLogs'))
    BEGIN
        CREATE INDEX IX_SuperAdminAuditLogs_Severity ON [dbo].[tbl_SuperAdminAuditLogs]([severity]);
        PRINT 'Index IX_SuperAdminAuditLogs_Severity created.';
    END

    -- Index on customer_code for customer-related queries
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SuperAdminAuditLogs_CustomerCode' AND object_id = OBJECT_ID('dbo.tbl_SuperAdminAuditLogs'))
    BEGIN
        CREATE INDEX IX_SuperAdminAuditLogs_CustomerCode ON [dbo].[tbl_SuperAdminAuditLogs]([customer_code]);
        PRINT 'Index IX_SuperAdminAuditLogs_CustomerCode created.';
    END
END
ELSE
BEGIN
    PRINT 'Table [tbl_SuperAdminAuditLogs] does not exist. Cannot create indexes.';
END
GO

PRINT 'Script execution completed.';
GO
