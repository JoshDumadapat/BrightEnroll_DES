-- =============================================
-- Create tbl_SyncHistory and tbl_SyncLogs tables
-- For persisting cloud sync operations and logs
-- 
-- IMPORTANT NOTES:
-- - tbl_SyncHistory should only contain ONE record (the latest sync)
--   The application will UPDATE this record instead of creating new ones
-- - tbl_SyncLogs should contain maximum 25 records
--   Oldest logs are automatically deleted when the limit is exceeded
-- =============================================

USE [DB_BrightEnroll_DES];
GO

-- Create tbl_SyncHistory table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_SyncHistory' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_SyncHistory](
        [sync_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [sync_type] VARCHAR(50) NOT NULL,
        [sync_time] DATETIME NOT NULL DEFAULT GETDATE(),
        [status] VARCHAR(20) NOT NULL DEFAULT 'Success',
        [records_pushed] INT NOT NULL DEFAULT 0,
        [records_pulled] INT NOT NULL DEFAULT 0,
        [message] NVARCHAR(MAX) NULL,
        [error_details] NVARCHAR(MAX) NULL,
        [duration_seconds] INT NULL,
        [initiated_by] VARCHAR(100) NULL,
        [created_at] DATETIME NOT NULL DEFAULT GETDATE()
    );
    
    -- Create indexes
    CREATE INDEX IX_tbl_SyncHistory_SyncTime ON [dbo].[tbl_SyncHistory]([sync_time]);
    CREATE INDEX IX_tbl_SyncHistory_SyncType ON [dbo].[tbl_SyncHistory]([sync_type]);
    CREATE INDEX IX_tbl_SyncHistory_Status ON [dbo].[tbl_SyncHistory]([status]);
    CREATE INDEX IX_tbl_SyncHistory_SyncType_SyncTime ON [dbo].[tbl_SyncHistory]([sync_type], [sync_time]);
    
    PRINT 'Table [tbl_SyncHistory] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [tbl_SyncHistory] already exists.';
END
GO

-- Create tbl_SyncLogs table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_SyncLogs' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_SyncLogs](
        [log_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [sync_id] INT NULL,
        [log_type] VARCHAR(50) NOT NULL,
        [log_message] NVARCHAR(MAX) NOT NULL,
        [timestamp] DATETIME NOT NULL DEFAULT GETDATE(),
        [severity] VARCHAR(20) NULL,
        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
        
        -- Foreign key constraint
        CONSTRAINT FK_tbl_SyncLogs_tbl_SyncHistory FOREIGN KEY ([sync_id]) 
            REFERENCES [dbo].[tbl_SyncHistory]([sync_id]) ON DELETE SET NULL
    );
    
    -- Create indexes
    CREATE INDEX IX_tbl_SyncLogs_SyncId ON [dbo].[tbl_SyncLogs]([sync_id]);
    CREATE INDEX IX_tbl_SyncLogs_Timestamp ON [dbo].[tbl_SyncLogs]([timestamp]);
    CREATE INDEX IX_tbl_SyncLogs_LogType ON [dbo].[tbl_SyncLogs]([log_type]);
    CREATE INDEX IX_tbl_SyncLogs_SyncId_Timestamp ON [dbo].[tbl_SyncLogs]([sync_id], [timestamp]);
    
    PRINT 'Table [tbl_SyncLogs] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [tbl_SyncLogs] already exists.';
END
GO

PRINT 'Sync history and logs tables setup completed.';
GO

