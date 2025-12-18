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

-- =============================================
-- TABLE 8: tbl_SuperAdminBIRFilings
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_SuperAdminBIRFilings' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_SuperAdminBIRFilings](
        [filing_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [filing_type] VARCHAR(50) NOT NULL,
        [period] VARCHAR(20) NOT NULL,
        [filing_date] DATETIME NOT NULL,
        [due_date] DATETIME NOT NULL,
        [status] VARCHAR(20) NOT NULL DEFAULT 'Pending',
        [amount] DECIMAL(18,2) NULL,
        [reference_number] VARCHAR(100) NULL,
        [notes] NVARCHAR(1000) NULL,
        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
        [created_by] INT NULL,
        [updated_at] DATETIME NULL,
        [updated_by] INT NULL
    );
    
    CREATE INDEX IX_SuperAdminBIRFilings_FilingType ON [dbo].[tbl_SuperAdminBIRFilings]([filing_type]);
    CREATE INDEX IX_SuperAdminBIRFilings_Period ON [dbo].[tbl_SuperAdminBIRFilings]([period]);
    CREATE INDEX IX_SuperAdminBIRFilings_Status ON [dbo].[tbl_SuperAdminBIRFilings]([status]);
    CREATE INDEX IX_SuperAdminBIRFilings_DueDate ON [dbo].[tbl_SuperAdminBIRFilings]([due_date]);
    CREATE INDEX IX_SuperAdminBIRFilings_FilingDate ON [dbo].[tbl_SuperAdminBIRFilings]([filing_date]);
    
    PRINT 'Table tbl_SuperAdminBIRFilings created.';
END
ELSE
BEGIN
    PRINT 'Table tbl_SuperAdminBIRFilings already exists.';
END
GO

-- =============================================
-- TABLE 9: tbl_SubscriptionPlans
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_SubscriptionPlans' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_SubscriptionPlans](
        [plan_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [plan_code] VARCHAR(50) NOT NULL UNIQUE,
        [plan_name] VARCHAR(100) NOT NULL,
        [description] NVARCHAR(500) NULL,
        [is_active] BIT NOT NULL DEFAULT 1,
        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
        [updated_at] DATETIME NULL
    );
    
    CREATE UNIQUE INDEX IX_tbl_SubscriptionPlans_PlanCode ON [dbo].[tbl_SubscriptionPlans]([plan_code]);
    CREATE INDEX IX_tbl_SubscriptionPlans_IsActive ON [dbo].[tbl_SubscriptionPlans]([is_active]);
    
    PRINT 'Table tbl_SubscriptionPlans created.';
END
ELSE
BEGIN
    PRINT 'Table tbl_SubscriptionPlans already exists.';
END
GO

-- =============================================
-- TABLE 10: tbl_PlanModules
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_PlanModules' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_PlanModules](
        [plan_module_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [plan_id] INT NOT NULL,
        [module_package_id] VARCHAR(50) NOT NULL,
        [granted_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_PlanModules_Plan FOREIGN KEY ([plan_id]) REFERENCES [dbo].[tbl_SubscriptionPlans]([plan_id]) ON DELETE CASCADE,
        CONSTRAINT UQ_PlanModules_Plan_Module UNIQUE ([plan_id], [module_package_id])
    );
    
    CREATE INDEX IX_tbl_PlanModules_PlanId ON [dbo].[tbl_PlanModules]([plan_id]);
    CREATE INDEX IX_tbl_PlanModules_ModulePackageId ON [dbo].[tbl_PlanModules]([module_package_id]);
    
    PRINT 'Table tbl_PlanModules created.';
END
ELSE
BEGIN
    PRINT 'Table tbl_PlanModules already exists.';
END
GO

-- =============================================
-- TABLE 11: tbl_CustomerSubscriptions
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_CustomerSubscriptions' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_CustomerSubscriptions](
        [subscription_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [customer_id] INT NOT NULL,
        [plan_id] INT NULL,
        [subscription_type] VARCHAR(20) NOT NULL DEFAULT 'predefined',
        [status] VARCHAR(20) NOT NULL DEFAULT 'Active',
        [start_date] DATE NOT NULL,
        [end_date] DATE NULL,
        [monthly_fee] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [auto_renewal] BIT NOT NULL DEFAULT 0,
        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
        [created_by] INT NULL,
        [updated_at] DATETIME NULL,
        [updated_by] INT NULL,
        CONSTRAINT FK_CustomerSubscriptions_Customer FOREIGN KEY ([customer_id]) REFERENCES [dbo].[tbl_Customers]([customer_id]) ON DELETE CASCADE,
        CONSTRAINT FK_CustomerSubscriptions_Plan FOREIGN KEY ([plan_id]) REFERENCES [dbo].[tbl_SubscriptionPlans]([plan_id]) ON DELETE SET NULL
    );
    
    CREATE INDEX IX_tbl_CustomerSubscriptions_CustomerId ON [dbo].[tbl_CustomerSubscriptions]([customer_id]);
    CREATE INDEX IX_tbl_CustomerSubscriptions_Status ON [dbo].[tbl_CustomerSubscriptions]([status]);
    CREATE INDEX IX_tbl_CustomerSubscriptions_StartDate ON [dbo].[tbl_CustomerSubscriptions]([start_date]);
    CREATE INDEX IX_tbl_CustomerSubscriptions_PlanId ON [dbo].[tbl_CustomerSubscriptions]([plan_id]);
    
    PRINT 'Table tbl_CustomerSubscriptions created.';
END
ELSE
BEGIN
    PRINT 'Table tbl_CustomerSubscriptions already exists.';
END
GO

-- =============================================
-- TABLE 12: tbl_CustomerSubscriptionModules
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_CustomerSubscriptionModules' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_CustomerSubscriptionModules](
        [customer_module_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [subscription_id] INT NOT NULL,
        [module_package_id] VARCHAR(50) NOT NULL,
        [granted_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [granted_by] INT NULL,
        [revoked_date] DATETIME NULL,
        [revoked_by] INT NULL,
        CONSTRAINT FK_CustomerSubscriptionModules_Subscription FOREIGN KEY ([subscription_id]) REFERENCES [dbo].[tbl_CustomerSubscriptions]([subscription_id]) ON DELETE CASCADE,
        CONSTRAINT UQ_CustomerSubscriptionModules_Subscription_Module UNIQUE ([subscription_id], [module_package_id])
    );
    
    CREATE INDEX IX_tbl_CustomerSubscriptionModules_SubscriptionId ON [dbo].[tbl_CustomerSubscriptionModules]([subscription_id]);
    CREATE INDEX IX_tbl_CustomerSubscriptionModules_ModulePackageId ON [dbo].[tbl_CustomerSubscriptionModules]([module_package_id]);
    
    PRINT 'Table tbl_CustomerSubscriptionModules created.';
END
ELSE
BEGIN
    PRINT 'Table tbl_CustomerSubscriptionModules already exists.';
END
GO

-- =============================================
-- TABLE 13: tbl_TenantModules
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_TenantModules' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_TenantModules](
        [tenant_module_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [customer_id] INT NOT NULL,
        [module_package_id] VARCHAR(50) NOT NULL,
        [subscription_id] INT NOT NULL,
        [granted_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [is_active] BIT NOT NULL DEFAULT 1,
        [last_updated] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_TenantModules_Customer FOREIGN KEY ([customer_id]) REFERENCES [dbo].[tbl_Customers]([customer_id]) ON DELETE CASCADE,
        CONSTRAINT FK_TenantModules_Subscription FOREIGN KEY ([subscription_id]) REFERENCES [dbo].[tbl_CustomerSubscriptions]([subscription_id]) ON DELETE CASCADE,
        CONSTRAINT UQ_TenantModules_Customer_Module UNIQUE ([customer_id], [module_package_id], [subscription_id])
    );
    
    CREATE INDEX IX_tbl_TenantModules_CustomerId ON [dbo].[tbl_TenantModules]([customer_id]);
    CREATE INDEX IX_tbl_TenantModules_ModulePackageId ON [dbo].[tbl_TenantModules]([module_package_id]);
    CREATE INDEX IX_tbl_TenantModules_IsActive ON [dbo].[tbl_TenantModules]([is_active]);
    
    PRINT 'Table tbl_TenantModules created.';
END
ELSE
BEGIN
    PRINT 'Table tbl_TenantModules already exists.';
END
GO

-- =============================================
-- TABLE 14: tbl_SuperAdminAuditLogs
-- =============================================
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
    
    CREATE INDEX IX_SuperAdminAuditLogs_Timestamp ON [dbo].[tbl_SuperAdminAuditLogs]([timestamp] DESC);
    CREATE INDEX IX_SuperAdminAuditLogs_UserId ON [dbo].[tbl_SuperAdminAuditLogs]([user_id]);
    CREATE INDEX IX_SuperAdminAuditLogs_Module ON [dbo].[tbl_SuperAdminAuditLogs]([module]);
    CREATE INDEX IX_SuperAdminAuditLogs_Action ON [dbo].[tbl_SuperAdminAuditLogs]([action]);
    CREATE INDEX IX_SuperAdminAuditLogs_Status ON [dbo].[tbl_SuperAdminAuditLogs]([status]);
    CREATE INDEX IX_SuperAdminAuditLogs_Severity ON [dbo].[tbl_SuperAdminAuditLogs]([severity]);
    
    PRINT 'Table tbl_SuperAdminAuditLogs created.';
END
ELSE
BEGIN
    PRINT 'Table tbl_SuperAdminAuditLogs already exists.';
END
GO

-- =============================================
-- TABLE 15: tbl_SuperAdminNotifications
-- =============================================
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
    
    CREATE INDEX IX_SuperAdminNotifications_IsRead ON [dbo].[tbl_SuperAdminNotifications]([is_read]);
    CREATE INDEX IX_SuperAdminNotifications_CreatedAt ON [dbo].[tbl_SuperAdminNotifications]([created_at] DESC);
    CREATE INDEX IX_SuperAdminNotifications_NotificationType ON [dbo].[tbl_SuperAdminNotifications]([notification_type]);
    CREATE INDEX IX_SuperAdminNotifications_ReferenceType ON [dbo].[tbl_SuperAdminNotifications]([reference_type]);
    CREATE INDEX IX_SuperAdminNotifications_ReferenceId ON [dbo].[tbl_SuperAdminNotifications]([reference_id]);
    
    PRINT 'Table tbl_SuperAdminNotifications created.';
END
ELSE
BEGIN
    PRINT 'Table tbl_SuperAdminNotifications already exists.';
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
PRINT '  8. tbl_SuperAdminBIRFilings';
PRINT '  9. tbl_SubscriptionPlans';
PRINT '  10. tbl_PlanModules';
PRINT '  11. tbl_CustomerSubscriptions';
PRINT '  12. tbl_CustomerSubscriptionModules';
PRINT '  13. tbl_TenantModules';
PRINT '  14. tbl_SuperAdminAuditLogs';
PRINT '  15. tbl_SuperAdminNotifications';
PRINT '';
PRINT 'NOT Created (by design):';
PRINT '  - tbl_Contracts (contract info stored in tbl_Customers)';
PRINT '  - tbl_SalesLeads (removed functionality)';
PRINT '';
PRINT 'Schema is ready for cloud deployment!';
PRINT '=============================================';
GO
