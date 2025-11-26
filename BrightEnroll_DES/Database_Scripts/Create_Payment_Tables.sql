-- =============================================
-- BrightEnroll_DES Payment Tables Creation Script
-- Creates tables for cashier payment operations
-- =============================================

USE [DB_BrightEnroll_DES];
GO

-- STEP 1: CREATE STUDENT ACCOUNTS TABLE
-- Tracks student fee assessments and payment balances
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_StudentAccounts' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_StudentAccounts](
        [account_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [student_id] VARCHAR(6) NOT NULL,
        [school_year] VARCHAR(20) NULL,
        [grade_level] VARCHAR(10) NULL,
        [assessment_amount] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        [amount_paid] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        [balance] AS ([assessment_amount] - [amount_paid]) PERSISTED,
        [payment_status] VARCHAR(20) NOT NULL DEFAULT 'Unpaid',
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [updated_date] DATETIME NULL,
        [created_by] VARCHAR(50) NULL,
        [updated_by] VARCHAR(50) NULL,
        [is_active] BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_tbl_StudentAccounts_tbl_Students FOREIGN KEY ([student_id]) REFERENCES [dbo].[tbl_Students]([student_id]) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_tbl_StudentAccounts_student_id ON [dbo].[tbl_StudentAccounts]([student_id]);
    CREATE INDEX IX_tbl_StudentAccounts_payment_status ON [dbo].[tbl_StudentAccounts]([payment_status]);
    CREATE INDEX IX_tbl_StudentAccounts_school_year ON [dbo].[tbl_StudentAccounts]([school_year]);
    CREATE INDEX IX_tbl_StudentAccounts_is_active ON [dbo].[tbl_StudentAccounts]([is_active]);
    
    PRINT 'Table [tbl_StudentAccounts] created.';
END
GO

-- STEP 2: CREATE PAYMENTS TABLE
-- Records all payment transactions
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Payments' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_Payments](
        [payment_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [or_number] VARCHAR(50) NOT NULL UNIQUE,
        [student_id] VARCHAR(6) NOT NULL,
        [account_id] INT NULL,
        [payment_type] VARCHAR(50) NOT NULL,
        [payment_method] VARCHAR(50) NOT NULL,
        [amount] DECIMAL(18,2) NOT NULL,
        [reference_number] VARCHAR(100) NULL,
        [remarks] VARCHAR(500) NULL,
        [transaction_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [processed_by] INT NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [created_by] VARCHAR(50) NULL,
        [is_void] BIT NOT NULL DEFAULT 0,
        CONSTRAINT FK_tbl_Payments_tbl_Students FOREIGN KEY ([student_id]) REFERENCES [dbo].[tbl_Students]([student_id]) ON DELETE CASCADE,
        CONSTRAINT FK_tbl_Payments_tbl_StudentAccounts FOREIGN KEY ([account_id]) REFERENCES [dbo].[tbl_StudentAccounts]([account_ID]) ON DELETE SET NULL,
        CONSTRAINT FK_tbl_Payments_tbl_Users FOREIGN KEY ([processed_by]) REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE SET NULL
    );
    
    CREATE INDEX IX_tbl_Payments_student_id ON [dbo].[tbl_Payments]([student_id]);
    CREATE INDEX IX_tbl_Payments_or_number ON [dbo].[tbl_Payments]([or_number]);
    CREATE INDEX IX_tbl_Payments_transaction_date ON [dbo].[tbl_Payments]([transaction_date]);
    CREATE INDEX IX_tbl_Payments_payment_type ON [dbo].[tbl_Payments]([payment_type]);
    CREATE INDEX IX_tbl_Payments_processed_by ON [dbo].[tbl_Payments]([processed_by]);
    CREATE INDEX IX_tbl_Payments_is_void ON [dbo].[tbl_Payments]([is_void]);
    
    PRINT 'Table [tbl_Payments] created.';
END
GO

-- STEP 3: CREATE OR NUMBER SEQUENCE TABLE
-- Tracks the last OR number used for generating new OR numbers
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_ORNumber_Sequence' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_ORNumber_Sequence](
        [sequence_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [LastORNumber] INT NOT NULL DEFAULT 1234,
        [year_prefix] INT NOT NULL DEFAULT YEAR(GETDATE())
    );
    
    -- Insert initial sequence value
    INSERT INTO [dbo].[tbl_ORNumber_Sequence] ([LastORNumber], [year_prefix]) 
    VALUES (1234, YEAR(GETDATE()));
    
    PRINT 'Table [tbl_ORNumber_Sequence] created.';
END
GO

-- STEP 4: CREATE STORED PROCEDURE FOR OR NUMBER GENERATION
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GenerateORNumber]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_GenerateORNumber];
GO

CREATE PROCEDURE [dbo].[sp_GenerateORNumber]
    @or_number VARCHAR(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        DECLARE @CurrentYear INT = YEAR(GETDATE());
        DECLARE @NewNumber INT;
        DECLARE @LastNumber INT;
        DECLARE @LastYear INT;
        
        -- Lock the sequence table and get the current values
        SELECT @LastNumber = [LastORNumber], @LastYear = [year_prefix]
        FROM [dbo].[tbl_ORNumber_Sequence] WITH (UPDLOCK, HOLDLOCK);
        
        -- If year changed, reset the sequence
        IF @LastYear <> @CurrentYear
        BEGIN
            SET @NewNumber = 1;
            UPDATE [dbo].[tbl_ORNumber_Sequence]
            SET [LastORNumber] = @NewNumber, [year_prefix] = @CurrentYear;
        END
        ELSE
        BEGIN
            SET @NewNumber = @LastNumber + 1;
            UPDATE [dbo].[tbl_ORNumber_Sequence]
            SET [LastORNumber] = @NewNumber;
        END
        
        -- Format: YYYY-XXXXXX (e.g., 2024-001234)
        SET @or_number = CAST(@CurrentYear AS VARCHAR(4)) + '-' + RIGHT('000000' + CAST(@NewNumber AS VARCHAR(6)), 6);
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH;
END;
GO

PRINT 'Stored procedure [sp_GenerateORNumber] created.';
GO

-- STEP 5: CREATE TRIGGER TO UPDATE STUDENT ACCOUNT BALANCE
-- Automatically updates account balance when payment is made
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[trg_UpdateAccountBalance]') AND type = 'TR')
    DROP TRIGGER [dbo].[trg_UpdateAccountBalance];
GO

CREATE TRIGGER [dbo].[trg_UpdateAccountBalance]
ON [dbo].[tbl_Payments]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Update student account balance when payment is inserted
    UPDATE sa
    SET sa.[amount_paid] = sa.[amount_paid] + i.[amount],
        sa.[updated_date] = GETDATE(),
        sa.[payment_status] = CASE 
            WHEN (sa.[amount_paid] + i.[amount]) >= sa.[assessment_amount] THEN 'Fully Paid'
            WHEN (sa.[amount_paid] + i.[amount]) > 0 THEN 'Partial'
            ELSE 'Unpaid'
        END
    FROM [dbo].[tbl_StudentAccounts] sa
    INNER JOIN inserted i ON sa.[account_ID] = i.[account_id]
    WHERE i.[is_void] = 0;
END;
GO

PRINT 'Trigger [trg_UpdateAccountBalance] created.';
GO

PRINT '';
PRINT '========================================';
PRINT 'Payment Tables Creation Complete';
PRINT '========================================';
PRINT '';
GO

