-- =============================================
-- Create tbl_SuperAdminNotifications table for SuperAdmin notifications
-- Separate table specifically for SuperAdmin notifications
-- =============================================

USE [DB_BrightEnroll_DES];
GO

-- Check if table exists, create if not
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_SuperAdminNotifications' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_SuperAdminNotifications](
        [notification_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [notification_type] VARCHAR(50) NOT NULL,
        [title] VARCHAR(100) NOT NULL,
        [message] NVARCHAR(500) NULL,
        [reference_type] VARCHAR(50) NOT NULL,
        [reference_id] INT NULL,
        [is_read] BIT NOT NULL DEFAULT 0,
        [read_at] DATETIME NULL,
        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
        [action_url] VARCHAR(100) NULL,
        [priority] VARCHAR(50) NOT NULL DEFAULT 'Normal',
        [created_by] INT NULL,
        CONSTRAINT FK_tbl_SuperAdminNotifications_CreatedBy FOREIGN KEY ([created_by]) 
            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE SET NULL,
        CONSTRAINT CK_tbl_SuperAdminNotifications_Priority CHECK ([priority] IN ('Low', 'Normal', 'High', 'Urgent'))
    );
    
    PRINT 'Table [tbl_SuperAdminNotifications] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [tbl_SuperAdminNotifications] already exists.';
END
GO

-- Create indexes for better query performance
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_SuperAdminNotifications' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    -- Index on is_read for unread queries
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SuperAdminNotifications_IsRead' AND object_id = OBJECT_ID('dbo.tbl_SuperAdminNotifications'))
    BEGIN
        CREATE INDEX IX_SuperAdminNotifications_IsRead ON [dbo].[tbl_SuperAdminNotifications]([is_read]);
        PRINT 'Index IX_SuperAdminNotifications_IsRead created.';
    END

    -- Index on created_at for date sorting
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SuperAdminNotifications_CreatedAt' AND object_id = OBJECT_ID('dbo.tbl_SuperAdminNotifications'))
    BEGIN
        CREATE INDEX IX_SuperAdminNotifications_CreatedAt ON [dbo].[tbl_SuperAdminNotifications]([created_at] DESC);
        PRINT 'Index IX_SuperAdminNotifications_CreatedAt created.';
    END

    -- Index on notification_type for filtering
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SuperAdminNotifications_NotificationType' AND object_id = OBJECT_ID('dbo.tbl_SuperAdminNotifications'))
    BEGIN
        CREATE INDEX IX_SuperAdminNotifications_NotificationType ON [dbo].[tbl_SuperAdminNotifications]([notification_type]);
        PRINT 'Index IX_SuperAdminNotifications_NotificationType created.';
    END

    -- Index on reference_type and reference_id for entity lookups
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SuperAdminNotifications_ReferenceType' AND object_id = OBJECT_ID('dbo.tbl_SuperAdminNotifications'))
    BEGIN
        CREATE INDEX IX_SuperAdminNotifications_ReferenceType ON [dbo].[tbl_SuperAdminNotifications]([reference_type]);
        PRINT 'Index IX_SuperAdminNotifications_ReferenceType created.';
    END

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SuperAdminNotifications_ReferenceId' AND object_id = OBJECT_ID('dbo.tbl_SuperAdminNotifications'))
    BEGIN
        CREATE INDEX IX_SuperAdminNotifications_ReferenceId ON [dbo].[tbl_SuperAdminNotifications]([reference_id]);
        PRINT 'Index IX_SuperAdminNotifications_ReferenceId created.';
    END
END
GO
