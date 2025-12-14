-- =============================================
-- Add transaction tracking columns to tbl_audit_logs
-- Adds: entity_type, entity_id, old_values, new_values
-- =============================================

USE [DB_BrightEnroll_DES];
GO

-- Add entity_type column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_audit_logs') AND name = 'entity_type')
BEGIN
    ALTER TABLE [dbo].[tbl_audit_logs] ADD [entity_type] VARCHAR(100) NULL;
    PRINT 'Column [entity_type] added to [tbl_audit_logs].';
END
ELSE
BEGIN
    PRINT 'Column [entity_type] already exists in [tbl_audit_logs].';
END
GO

-- Add entity_id column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_audit_logs') AND name = 'entity_id')
BEGIN
    ALTER TABLE [dbo].[tbl_audit_logs] ADD [entity_id] VARCHAR(50) NULL;
    PRINT 'Column [entity_id] added to [tbl_audit_logs].';
END
ELSE
BEGIN
    PRINT 'Column [entity_id] already exists in [tbl_audit_logs].';
END
GO

-- Add old_values column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_audit_logs') AND name = 'old_values')
BEGIN
    ALTER TABLE [dbo].[tbl_audit_logs] ADD [old_values] NVARCHAR(MAX) NULL;
    PRINT 'Column [old_values] added to [tbl_audit_logs].';
END
ELSE
BEGIN
    PRINT 'Column [old_values] already exists in [tbl_audit_logs].';
END
GO

-- Add new_values column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_audit_logs') AND name = 'new_values')
BEGIN
    ALTER TABLE [dbo].[tbl_audit_logs] ADD [new_values] NVARCHAR(MAX) NULL;
    PRINT 'Column [new_values] added to [tbl_audit_logs].';
END
ELSE
BEGIN
    PRINT 'Column [new_values] already exists in [tbl_audit_logs].';
END
GO

PRINT 'Transaction tracking columns added successfully to [tbl_audit_logs].';
GO

