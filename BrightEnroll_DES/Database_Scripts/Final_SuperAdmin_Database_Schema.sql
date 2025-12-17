-- =============================================
-- FINAL SUPERADMIN DATABASE SCHEMA
-- Database: DB_BrightEnroll_SuperAdmin
-- Run this script to create/update SuperAdmin database
-- =============================================

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'DB_BrightEnroll_SuperAdmin')
BEGIN
    CREATE DATABASE [DB_BrightEnroll_SuperAdmin];
    PRINT 'Database DB_BrightEnroll_SuperAdmin created.';
END
ELSE
BEGIN
    PRINT 'Database DB_BrightEnroll_SuperAdmin already exists.';
END
GO

USE [DB_BrightEnroll_SuperAdmin];
GO

-- =============================================
-- TABLE 1: tbl_Users (for SuperAdmin users - local dev/testing)
-- NOTE: In production, SuperAdmin should be in cloud database only
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Users' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_Users](
        [user_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [system_ID] VARCHAR(50) NOT NULL UNIQUE,
        [first_name] VARCHAR(50) NOT NULL,
        [mid_name] VARCHAR(50) NULL,
        [last_name] VARCHAR(50) NOT NULL,
        [suffix] VARCHAR(10) NULL,
        [birthdate] DATE NOT NULL,
        [age] TINYINT NOT NULL,
        [gender] VARCHAR(20) NOT NULL,
        [contact_num] VARCHAR(20) NOT NULL,
        [user_role] VARCHAR(50) NOT NULL,
        [email] VARCHAR(150) NOT NULL UNIQUE,
        [password] VARCHAR(255) NOT NULL,
        [date_hired] DATETIME NOT NULL DEFAULT GETDATE(),
        [status] VARCHAR(20) NOT NULL DEFAULT 'active'
    );
    
    CREATE UNIQUE INDEX IX_tbl_Users_SystemID ON [dbo].[tbl_Users]([system_ID]);
    CREATE UNIQUE INDEX IX_tbl_Users_Email ON [dbo].[tbl_Users]([email]);
    
    PRINT 'Table tbl_Users created in SuperAdmin database.';
END
ELSE
BEGIN
    PRINT 'Table tbl_Users already exists in SuperAdmin database.';
END
GO

-- =============================================
-- TABLE 2: tbl_Customers
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Customers' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_Customers](
        [customer_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [customer_code] VARCHAR(50) NOT NULL UNIQUE,
        [school_name] VARCHAR(200) NOT NULL,
        [school_type] VARCHAR(50) NULL,
        [address] VARCHAR(500) NULL,
        [contact_person] VARCHAR(200) NULL,
        [contact_position] VARCHAR(100) NULL,
        [contact_email] VARCHAR(150) NULL,
        [contact_phone] VARCHAR(20) NULL,
        [subscription_plan] VARCHAR(50) NULL,
        [monthly_fee] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [contract_start_date] DATE NULL,
        [contract_end_date] DATE NULL,
        [contract_duration_months] INT NULL,
        [student_count] INT NULL,
        [status] VARCHAR(20) NOT NULL DEFAULT 'Active',
        [notes] NVARCHAR(MAX) NULL,
        [date_registered] DATETIME NOT NULL DEFAULT GETDATE(),
        [created_by] INT NULL,
        [updated_at] DATETIME NULL,
        [database_name] NVARCHAR(200) NULL,
        [database_connection_string] NVARCHAR(MAX) NULL,
        [cloud_connection_string] NVARCHAR(MAX) NULL,
        [admin_username] NVARCHAR(100) NULL,
        [admin_password] NVARCHAR(255) NULL,
        [bir_tin] VARCHAR(50) NULL,
        [bir_business_name] VARCHAR(200) NULL,
        [bir_address] NVARCHAR(500) NULL,
        [bir_registration_type] VARCHAR(20) NULL,
        [is_vat_registered] BIT NOT NULL DEFAULT 0,
        [vat_rate] DECIMAL(5,2) NULL,
        [warranty_period] NVARCHAR(100) NULL,
        [maintenance_schedule] NVARCHAR(200) NULL,
        [free_services] NVARCHAR(500) NULL,
        [payment_terms] NVARCHAR(200) NULL,
        [termination_clause] NVARCHAR(500) NULL,
        [sla_details] NVARCHAR(500) NULL,
        [contract_terms_text] NVARCHAR(MAX) NULL,
        [auto_renewal] BIT NOT NULL DEFAULT 0
    );
    
    CREATE UNIQUE INDEX IX_tbl_Customers_CustomerCode ON [dbo].[tbl_Customers]([customer_code]);
    CREATE INDEX IX_tbl_Customers_SchoolName ON [dbo].[tbl_Customers]([school_name]);
    CREATE INDEX IX_tbl_Customers_Status ON [dbo].[tbl_Customers]([status]);
    CREATE INDEX IX_tbl_Customers_ContractEndDate ON [dbo].[tbl_Customers]([contract_end_date]);
    
    PRINT 'Table tbl_Customers created.';
END
ELSE
BEGIN
    -- Add cloud_connection_string column if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Customers') AND name = 'cloud_connection_string')
    BEGIN
        ALTER TABLE [dbo].[tbl_Customers] ADD [cloud_connection_string] NVARCHAR(MAX) NULL;
        PRINT 'Column cloud_connection_string added to tbl_Customers.';
    END
    ELSE
    BEGIN
        PRINT 'Table tbl_Customers already exists with cloud_connection_string column.';
    END
END
GO

-- =============================================
-- TABLE 3: tbl_SupportTickets
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_SupportTickets' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_SupportTickets](
        [ticket_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ticket_number] VARCHAR(50) NOT NULL UNIQUE,
        [customer_id] INT NULL,
        [subject] VARCHAR(200) NOT NULL,
        [description] NVARCHAR(MAX) NULL,
        [priority] VARCHAR(50) NOT NULL DEFAULT 'Medium',
        [status] VARCHAR(50) NOT NULL DEFAULT 'Open',
        [category] VARCHAR(50) NULL,
        [assigned_to] INT NULL,
        [resolved_at] DATETIME NULL,
        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
        [updated_at] DATETIME NULL,
        CONSTRAINT FK_SupportTickets_Customer FOREIGN KEY ([customer_id]) REFERENCES [dbo].[tbl_Customers]([customer_id])
    );
    
    CREATE UNIQUE INDEX IX_tbl_SupportTickets_TicketNumber ON [dbo].[tbl_SupportTickets]([ticket_number]);
    CREATE INDEX IX_tbl_SupportTickets_CustomerId ON [dbo].[tbl_SupportTickets]([customer_id]);
    CREATE INDEX IX_tbl_SupportTickets_Status ON [dbo].[tbl_SupportTickets]([status]);
    CREATE INDEX IX_tbl_SupportTickets_Priority ON [dbo].[tbl_SupportTickets]([priority]);
    CREATE INDEX IX_tbl_SupportTickets_CreatedAt ON [dbo].[tbl_SupportTickets]([created_at]);
    
    PRINT 'Table tbl_SupportTickets created.';
END
ELSE
BEGIN
    PRINT 'Table tbl_SupportTickets already exists.';
END
GO

-- =============================================
-- TABLE 4: tbl_CustomerInvoices
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_CustomerInvoices' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_CustomerInvoices](
        [invoice_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [customer_id] INT NOT NULL,
        [invoice_number] VARCHAR(50) NOT NULL UNIQUE,
        [invoice_date] DATE NOT NULL,
        [due_date] DATE NOT NULL,
        [billing_period_start] DATE NULL,
        [billing_period_end] DATE NULL,
        [subtotal] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [vat_amount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [total_amount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [amount_paid] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [balance] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [status] VARCHAR(20) NOT NULL DEFAULT 'Pending',
        [payment_terms] NVARCHAR(200) NULL,
        [notes] NVARCHAR(MAX) NULL,
        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
        [created_by] INT NULL,
        [paid_at] DATETIME NULL,
        CONSTRAINT FK_CustomerInvoices_Customer FOREIGN KEY ([customer_id]) REFERENCES [dbo].[tbl_Customers]([customer_id])
    );
    
    CREATE UNIQUE INDEX IX_tbl_CustomerInvoices_InvoiceNumber ON [dbo].[tbl_CustomerInvoices]([invoice_number]);
    CREATE INDEX IX_tbl_CustomerInvoices_CustomerId ON [dbo].[tbl_CustomerInvoices]([customer_id]);
    CREATE INDEX IX_tbl_CustomerInvoices_Status ON [dbo].[tbl_CustomerInvoices]([status]);
    CREATE INDEX IX_tbl_CustomerInvoices_DueDate ON [dbo].[tbl_CustomerInvoices]([due_date]);
    CREATE INDEX IX_tbl_CustomerInvoices_InvoiceDate ON [dbo].[tbl_CustomerInvoices]([invoice_date]);
    
    PRINT 'Table tbl_CustomerInvoices created.';
END
ELSE
BEGIN
    PRINT 'Table tbl_CustomerInvoices already exists.';
END
GO

-- =============================================
-- TABLE 5: tbl_CustomerPayments
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_CustomerPayments' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_CustomerPayments](
        [payment_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [invoice_id] INT NOT NULL,
        [customer_id] INT NOT NULL,
        [payment_reference] VARCHAR(50) NOT NULL,
        [payment_date] DATE NOT NULL,
        [amount] DECIMAL(18,2) NOT NULL,
        [payment_method] VARCHAR(50) NOT NULL DEFAULT 'Bank Transfer',
        [notes] NVARCHAR(MAX) NULL,
        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
        [created_by] INT NULL,
        CONSTRAINT FK_CustomerPayments_Invoice FOREIGN KEY ([invoice_id]) REFERENCES [dbo].[tbl_CustomerInvoices]([invoice_id]),
        CONSTRAINT FK_CustomerPayments_Customer FOREIGN KEY ([customer_id]) REFERENCES [dbo].[tbl_Customers]([customer_id])
    );
    
    CREATE INDEX IX_tbl_CustomerPayments_PaymentReference ON [dbo].[tbl_CustomerPayments]([payment_reference]);
    CREATE INDEX IX_tbl_CustomerPayments_InvoiceId ON [dbo].[tbl_CustomerPayments]([invoice_id]);
    CREATE INDEX IX_tbl_CustomerPayments_CustomerId ON [dbo].[tbl_CustomerPayments]([customer_id]);
    CREATE INDEX IX_tbl_CustomerPayments_PaymentDate ON [dbo].[tbl_CustomerPayments]([payment_date]);
    
    PRINT 'Table tbl_CustomerPayments created.';
END
ELSE
BEGIN
    PRINT 'Table tbl_CustomerPayments already exists.';
END
GO

-- =============================================
-- TABLE 6: tbl_SystemUpdates
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_SystemUpdates' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_SystemUpdates](
        [update_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [version_number] VARCHAR(50) NOT NULL UNIQUE,
        [title] VARCHAR(200) NOT NULL,
        [description] NVARCHAR(MAX) NULL,
        [update_type] VARCHAR(50) NOT NULL DEFAULT 'Feature',
        [release_date] DATE NOT NULL,
        [status] VARCHAR(20) NOT NULL DEFAULT 'Released',
        [is_major_update] BIT NOT NULL DEFAULT 0,
        [requires_action] BIT NOT NULL DEFAULT 0,
        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
        [created_by] INT NULL,
        [updated_at] DATETIME NULL
    );
    
    CREATE UNIQUE INDEX IX_tbl_SystemUpdates_VersionNumber ON [dbo].[tbl_SystemUpdates]([version_number]);
    CREATE INDEX IX_tbl_SystemUpdates_ReleaseDate ON [dbo].[tbl_SystemUpdates]([release_date]);
    CREATE INDEX IX_tbl_SystemUpdates_Status ON [dbo].[tbl_SystemUpdates]([status]);
    
    PRINT 'Table tbl_SystemUpdates created.';
END
ELSE
BEGIN
    PRINT 'Table tbl_SystemUpdates already exists.';
END
GO

-- =============================================
-- TABLE 7: tbl_SuperAdminBIRInfo
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_SuperAdminBIRInfo' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_SuperAdminBIRInfo](
        [bir_info_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [tin_number] VARCHAR(50) NULL,
        [business_name] VARCHAR(200) NOT NULL,
        [business_address] NVARCHAR(500) NULL,
        [registration_type] VARCHAR(20) NOT NULL DEFAULT 'VAT',
        [vat_rate] DECIMAL(5,2) NOT NULL DEFAULT 0.12,
        [is_vat_registered] BIT NOT NULL DEFAULT 1,
        [updated_at] DATETIME NULL,
        [updated_by] INT NULL
    );
    
    PRINT 'Table tbl_SuperAdminBIRInfo created.';
END
ELSE
BEGIN
    PRINT 'Table tbl_SuperAdminBIRInfo already exists.';
END
GO

PRINT '';
PRINT '=============================================';
PRINT 'SUPERADMIN DATABASE SCHEMA FINALIZED';
PRINT '=============================================';
PRINT 'Tables Created/Verified:';
PRINT '  1. tbl_Users (for SuperAdmin users - local dev/testing)';
PRINT '  2. tbl_Customers (with cloud_connection_string)';
PRINT '  3. tbl_SupportTickets';
PRINT '  4. tbl_CustomerInvoices';
PRINT '  5. tbl_CustomerPayments';
PRINT '  6. tbl_SystemUpdates';
PRINT '  7. tbl_SuperAdminBIRInfo';
PRINT '';
PRINT 'NOT Created (by design):';
PRINT '  - tbl_Contracts (contract info stored in tbl_Customers)';
PRINT '  - tbl_SalesLeads (removed functionality)';
PRINT '';
PRINT 'Schema is ready for cloud deployment!';
PRINT '=============================================';
GO
