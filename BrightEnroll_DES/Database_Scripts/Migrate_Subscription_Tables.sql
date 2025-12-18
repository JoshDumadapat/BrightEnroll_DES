-- =============================================
-- Migration Script: Subscription Tables Migration
-- Purpose: Migrate from Notes-based module parsing to proper relational tables
-- Date: [Current Date]
-- =============================================

USE [SuperAdmin_DB] -- Replace with your actual SuperAdmin database name
GO

BEGIN TRANSACTION;

PRINT 'Starting subscription tables migration...';

-- =============================================
-- STEP 1: Create new subscription tables
-- =============================================

-- Create tbl_SubscriptionPlans
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

-- Create tbl_PlanModules
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

-- Create tbl_CustomerSubscriptions
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
    
    CREATE INDEX IX_tbl_CustomerSubscriptions_Customer_Status ON [dbo].[tbl_CustomerSubscriptions]([customer_id], [status]);
    CREATE INDEX IX_tbl_CustomerSubscriptions_Active ON [dbo].[tbl_CustomerSubscriptions]([customer_id], [status]) WHERE [status] = 'Active';
    CREATE INDEX IX_tbl_CustomerSubscriptions_PlanId ON [dbo].[tbl_CustomerSubscriptions]([plan_id]);
    CREATE INDEX IX_tbl_CustomerSubscriptions_EndDate ON [dbo].[tbl_CustomerSubscriptions]([end_date]);
    
    PRINT 'Table tbl_CustomerSubscriptions created.';
END
ELSE
BEGIN
    PRINT 'Table tbl_CustomerSubscriptions already exists.';
END
GO

-- Create tbl_CustomerSubscriptionModules
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
    
    CREATE INDEX IX_tbl_CustomerSubscriptionModules_Subscription ON [dbo].[tbl_CustomerSubscriptionModules]([subscription_id], [revoked_date]);
    CREATE INDEX IX_tbl_CustomerSubscriptionModules_ModulePackageId ON [dbo].[tbl_CustomerSubscriptionModules]([module_package_id]);
    
    PRINT 'Table tbl_CustomerSubscriptionModules created.';
END
ELSE
BEGIN
    PRINT 'Table tbl_CustomerSubscriptionModules already exists.';
END
GO

-- Create tbl_TenantModules
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
    
    CREATE INDEX IX_tbl_TenantModules_Customer_Active ON [dbo].[tbl_TenantModules]([customer_id], [is_active]);
    CREATE INDEX IX_tbl_TenantModules_Subscription ON [dbo].[tbl_TenantModules]([subscription_id]);
    CREATE INDEX IX_tbl_TenantModules_ModulePackageId ON [dbo].[tbl_TenantModules]([module_package_id]);
    
    PRINT 'Table tbl_TenantModules created.';
END
ELSE
BEGIN
    PRINT 'Table tbl_TenantModules already exists.';
END
GO

-- =============================================
-- STEP 2: Seed predefined subscription plans
-- =============================================

-- Insert Basic Plan
IF NOT EXISTS (SELECT 1 FROM [dbo].[tbl_SubscriptionPlans] WHERE [plan_code] = 'basic')
BEGIN
    INSERT INTO [dbo].[tbl_SubscriptionPlans] ([plan_code], [plan_name], [description], [is_active])
    VALUES ('basic', 'Basic Plan', 'Includes Core and Enrollment modules', 1);
    
    DECLARE @BasicPlanId INT = SCOPE_IDENTITY();
    
    -- Add modules to Basic Plan
    INSERT INTO [dbo].[tbl_PlanModules] ([plan_id], [module_package_id])
    VALUES 
        (@BasicPlanId, 'core'),
        (@BasicPlanId, 'enrollment');
    
    PRINT 'Basic Plan seeded.';
END

-- Insert Standard Plan
IF NOT EXISTS (SELECT 1 FROM [dbo].[tbl_SubscriptionPlans] WHERE [plan_code] = 'standard')
BEGIN
    INSERT INTO [dbo].[tbl_SubscriptionPlans] ([plan_code], [plan_name], [description], [is_active])
    VALUES ('standard', 'Standard Plan', 'Includes Core, Enrollment, and Finance modules', 1);
    
    DECLARE @StandardPlanId INT = SCOPE_IDENTITY();
    
    -- Add modules to Standard Plan
    INSERT INTO [dbo].[tbl_PlanModules] ([plan_id], [module_package_id])
    VALUES 
        (@StandardPlanId, 'core'),
        (@StandardPlanId, 'enrollment'),
        (@StandardPlanId, 'finance');
    
    PRINT 'Standard Plan seeded.';
END

-- Insert Premium Plan
IF NOT EXISTS (SELECT 1 FROM [dbo].[tbl_SubscriptionPlans] WHERE [plan_code] = 'premium')
BEGIN
    INSERT INTO [dbo].[tbl_SubscriptionPlans] ([plan_code], [plan_name], [description], [is_active])
    VALUES ('premium', 'Premium Plan', 'Includes all modules: Core, Enrollment, Finance, and HR & Payroll', 1);
    
    DECLARE @PremiumPlanId INT = SCOPE_IDENTITY();
    
    -- Add modules to Premium Plan
    INSERT INTO [dbo].[tbl_PlanModules] ([plan_id], [module_package_id])
    VALUES 
        (@PremiumPlanId, 'core'),
        (@PremiumPlanId, 'enrollment'),
        (@PremiumPlanId, 'finance'),
        (@PremiumPlanId, 'hr_payroll');
    
    PRINT 'Premium Plan seeded.';
END
GO

-- =============================================
-- STEP 3: Migrate existing customer data
-- =============================================

PRINT 'Migrating existing customer subscriptions...';

-- Migrate customers with predefined plans
INSERT INTO [dbo].[tbl_CustomerSubscriptions] 
    ([customer_id], [plan_id], [subscription_type], [status], [start_date], [end_date], [monthly_fee], [auto_renewal], [created_at])
SELECT 
    c.[customer_id],
    sp.[plan_id],
    'predefined' AS [subscription_type],
    c.[status],
    ISNULL(c.[contract_start_date], c.[date_registered]) AS [start_date],
    c.[contract_end_date] AS [end_date],
    c.[monthly_fee],
    c.[auto_renewal],
    c.[date_registered] AS [created_at]
FROM [dbo].[tbl_Customers] c
INNER JOIN [dbo].[tbl_SubscriptionPlans] sp 
    ON LOWER(LTRIM(RTRIM(c.[subscription_plan]))) = LOWER(sp.[plan_code])
WHERE c.[subscription_plan] IS NOT NULL 
    AND LTRIM(RTRIM(c.[subscription_plan])) != ''
    AND NOT EXISTS (
        SELECT 1 FROM [dbo].[tbl_CustomerSubscriptions] cs 
        WHERE cs.[customer_id] = c.[customer_id] 
        AND cs.[status] = 'Active'
    );

PRINT 'Migrated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' predefined plan subscriptions.';

-- Migrate customers with custom modules (from Notes field)
-- This is a helper function to parse modules from Notes
-- Note: This migration preserves backward compatibility but new system will ignore Notes

DECLARE @CustomSubscriptionsMigrated INT = 0;

-- For customers without a subscription_plan but with Notes containing modules
-- We'll create custom subscriptions by parsing the Notes field (one-time migration)
-- Note: This is the LAST time we parse Notes - future operations will use new tables only

DECLARE customer_cursor CURSOR FOR
SELECT 
    [customer_id],
    [notes],
    [status],
    ISNULL([contract_start_date], [date_registered]) AS [start_date],
    [contract_end_date] AS [end_date],
    [monthly_fee],
    [auto_renewal],
    [date_registered]
FROM [dbo].[tbl_Customers]
WHERE ([subscription_plan] IS NULL OR LTRIM(RTRIM([subscription_plan])) = '')
    AND [notes] IS NOT NULL
    AND [notes] LIKE '%=== SELECTED MODULES ===%'
    AND NOT EXISTS (
        SELECT 1 FROM [dbo].[tbl_CustomerSubscriptions] cs 
        WHERE cs.[customer_id] = [tbl_Customers].[customer_id]
    );

DECLARE @CustomerId INT;
DECLARE @Notes NVARCHAR(MAX);
DECLARE @Status VARCHAR(20);
DECLARE @StartDate DATE;
DECLARE @EndDate DATE;
DECLARE @MonthlyFee DECIMAL(18,2);
DECLARE @AutoRenewal BIT;
DECLARE @CreatedAt DATETIME;

OPEN customer_cursor;
FETCH NEXT FROM customer_cursor INTO @CustomerId, @Notes, @Status, @StartDate, @EndDate, @MonthlyFee, @AutoRenewal, @CreatedAt;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Create custom subscription
    INSERT INTO [dbo].[tbl_CustomerSubscriptions] 
        ([customer_id], [plan_id], [subscription_type], [status], [start_date], [end_date], [monthly_fee], [auto_renewal], [created_at])
    VALUES 
        (@CustomerId, NULL, 'custom', @Status, @StartDate, @EndDate, @MonthlyFee, @AutoRenewal, @CreatedAt);
    
    DECLARE @SubscriptionId INT = SCOPE_IDENTITY();
    
    -- Parse modules from Notes (LAST TIME - for migration only)
    -- Extract module package IDs from Notes field
    -- Format: "=== SELECTED MODULES ===\nSelected Packages: Enrollment & Academic Management, Finance Management"
    
    DECLARE @ModuleList NVARCHAR(MAX);
    DECLARE @ModuleSectionIndex INT = CHARINDEX('=== SELECTED MODULES ===', @Notes);
    
    IF @ModuleSectionIndex > 0
    BEGIN
        SET @ModuleList = SUBSTRING(@Notes, @ModuleSectionIndex + LEN('=== SELECTED MODULES ==='), LEN(@Notes));
        
        -- Always add 'core' module
        INSERT INTO [dbo].[tbl_CustomerSubscriptionModules] ([subscription_id], [module_package_id], [granted_date])
        VALUES (@SubscriptionId, 'core', GETDATE());
        
        -- Parse and add other modules
        IF CHARINDEX('enrollment', LOWER(@ModuleList)) > 0 OR CHARINDEX('academic', LOWER(@ModuleList)) > 0
        BEGIN
            INSERT INTO [dbo].[tbl_CustomerSubscriptionModules] ([subscription_id], [module_package_id], [granted_date])
            VALUES (@SubscriptionId, 'enrollment', GETDATE());
        END
        
        IF CHARINDEX('finance', LOWER(@ModuleList)) > 0 OR CHARINDEX('financial', LOWER(@ModuleList)) > 0
        BEGIN
            INSERT INTO [dbo].[tbl_CustomerSubscriptionModules] ([subscription_id], [module_package_id], [granted_date])
            VALUES (@SubscriptionId, 'finance', GETDATE());
        END
        
        IF CHARINDEX('hr', LOWER(@ModuleList)) > 0 OR CHARINDEX('payroll', LOWER(@ModuleList)) > 0 OR CHARINDEX('human resource', LOWER(@ModuleList)) > 0
        BEGIN
            INSERT INTO [dbo].[tbl_CustomerSubscriptionModules] ([subscription_id], [module_package_id], [granted_date])
            VALUES (@SubscriptionId, 'hr_payroll', GETDATE());
        END
        
        SET @CustomSubscriptionsMigrated = @CustomSubscriptionsMigrated + 1;
    END
    
    FETCH NEXT FROM customer_cursor INTO @CustomerId, @Notes, @Status, @StartDate, @EndDate, @MonthlyFee, @AutoRenewal, @CreatedAt;
END

CLOSE customer_cursor;
DEALLOCATE customer_cursor;

PRINT 'Migrated ' + CAST(@CustomSubscriptionsMigrated AS VARCHAR) + ' custom subscriptions from Notes field.';

-- =============================================
-- STEP 4: Populate TenantModules (materialized cache)
-- =============================================

PRINT 'Populating TenantModules cache table...';

-- Clear existing tenant modules (if any)
DELETE FROM [dbo].[tbl_TenantModules];

-- Populate from predefined plan subscriptions
INSERT INTO [dbo].[tbl_TenantModules] 
    ([customer_id], [module_package_id], [subscription_id], [granted_date], [is_active], [last_updated])
SELECT 
    cs.[customer_id],
    pm.[module_package_id],
    cs.[subscription_id],
    cs.[created_at] AS [granted_date],
    CASE WHEN cs.[status] = 'Active' AND (cs.[end_date] IS NULL OR cs.[end_date] >= GETDATE()) THEN 1 ELSE 0 END AS [is_active],
    GETDATE() AS [last_updated]
FROM [dbo].[tbl_CustomerSubscriptions] cs
INNER JOIN [dbo].[tbl_PlanModules] pm ON cs.[plan_id] = pm.[plan_id]
WHERE cs.[subscription_type] = 'predefined'
    AND NOT EXISTS (
        SELECT 1 FROM [dbo].[tbl_TenantModules] tm 
        WHERE tm.[customer_id] = cs.[customer_id] 
        AND tm.[module_package_id] = pm.[module_package_id]
        AND tm.[subscription_id] = cs.[subscription_id]
    );

-- Populate from custom subscriptions
INSERT INTO [dbo].[tbl_TenantModules] 
    ([customer_id], [module_package_id], [subscription_id], [granted_date], [is_active], [last_updated])
SELECT 
    cs.[customer_id],
    csm.[module_package_id],
    cs.[subscription_id],
    csm.[granted_date],
    CASE WHEN cs.[status] = 'Active' AND (cs.[end_date] IS NULL OR cs.[end_date] >= GETDATE()) AND csm.[revoked_date] IS NULL THEN 1 ELSE 0 END AS [is_active],
    GETDATE() AS [last_updated]
FROM [dbo].[tbl_CustomerSubscriptions] cs
INNER JOIN [dbo].[tbl_CustomerSubscriptionModules] csm ON cs.[subscription_id] = csm.[subscription_id]
WHERE cs.[subscription_type] = 'custom'
    AND csm.[revoked_date] IS NULL
    AND NOT EXISTS (
        SELECT 1 FROM [dbo].[tbl_TenantModules] tm 
        WHERE tm.[customer_id] = cs.[customer_id] 
        AND tm.[module_package_id] = csm.[module_package_id]
        AND tm.[subscription_id] = cs.[subscription_id]
    );

PRINT 'TenantModules cache populated with ' + CAST(@@ROWCOUNT AS VARCHAR) + ' entries.';

-- =============================================
-- STEP 5: Create helper stored procedure for refreshing TenantModules
-- =============================================

IF EXISTS (SELECT * FROM sys.objects WHERE name = 'sp_RefreshTenantModules' AND type = 'P')
    DROP PROCEDURE [dbo].[sp_RefreshTenantModules];
GO

CREATE PROCEDURE [dbo].[sp_RefreshTenantModules]
    @CustomerId INT = NULL -- If NULL, refresh all customers
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Delete existing entries for customer(s)
    IF @CustomerId IS NOT NULL
    BEGIN
        DELETE FROM [dbo].[tbl_TenantModules] WHERE [customer_id] = @CustomerId;
    END
    ELSE
    BEGIN
        DELETE FROM [dbo].[tbl_TenantModules];
    END
    
    -- Re-populate from predefined plan subscriptions
    INSERT INTO [dbo].[tbl_TenantModules] 
        ([customer_id], [module_package_id], [subscription_id], [granted_date], [is_active], [last_updated])
    SELECT 
        cs.[customer_id],
        pm.[module_package_id],
        cs.[subscription_id],
        cs.[created_at] AS [granted_date],
        CASE WHEN cs.[status] = 'Active' AND (cs.[end_date] IS NULL OR cs.[end_date] >= GETDATE()) THEN 1 ELSE 0 END AS [is_active],
        GETDATE() AS [last_updated]
    FROM [dbo].[tbl_CustomerSubscriptions] cs
    INNER JOIN [dbo].[tbl_PlanModules] pm ON cs.[plan_id] = pm.[plan_id]
    WHERE cs.[subscription_type] = 'predefined'
        AND (@CustomerId IS NULL OR cs.[customer_id] = @CustomerId);
    
    -- Re-populate from custom subscriptions
    INSERT INTO [dbo].[tbl_TenantModules] 
        ([customer_id], [module_package_id], [subscription_id], [granted_date], [is_active], [last_updated])
    SELECT 
        cs.[customer_id],
        csm.[module_package_id],
        cs.[subscription_id],
        csm.[granted_date],
        CASE WHEN cs.[status] = 'Active' AND (cs.[end_date] IS NULL OR cs.[end_date] >= GETDATE()) AND csm.[revoked_date] IS NULL THEN 1 ELSE 0 END AS [is_active],
        GETDATE() AS [last_updated]
    FROM [dbo].[tbl_CustomerSubscriptions] cs
    INNER JOIN [dbo].[tbl_CustomerSubscriptionModules] csm ON cs.[subscription_id] = csm.[subscription_id]
    WHERE cs.[subscription_type] = 'custom'
        AND csm.[revoked_date] IS NULL
        AND (@CustomerId IS NULL OR cs.[customer_id] = @CustomerId);
END
GO

PRINT 'Stored procedure sp_RefreshTenantModules created.';

-- =============================================
-- Migration Complete
-- =============================================

PRINT '========================================';
PRINT 'Migration completed successfully!';
PRINT '========================================';
PRINT '';
PRINT 'IMPORTANT NOTES:';
PRINT '1. The Notes field in tbl_Customers is now IGNORED for module resolution.';
PRINT '2. All module entitlements are now stored in the new subscription tables.';
PRINT '3. Use sp_RefreshTenantModules to refresh the materialized cache when subscriptions change.';
PRINT '4. The subscription_plan field in tbl_Customers is kept for backward compatibility but should not be used for new subscriptions.';
PRINT '';

COMMIT TRANSACTION;

PRINT 'Transaction committed. Migration complete.';
