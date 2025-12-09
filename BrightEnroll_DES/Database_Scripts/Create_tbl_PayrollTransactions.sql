-- =============================================
-- Create tbl_payroll_transactions table
-- Tracks payroll payment transactions and history
-- =============================================

USE [DB_BrightEnroll_DES];
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_payroll_transactions' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_payroll_transactions](
        [transaction_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [user_id] INT NOT NULL,
        [school_year] VARCHAR(20) NOT NULL,
        [pay_period] VARCHAR(20) NOT NULL, -- e.g., "2025-01" for January 2025
        [base_salary] DECIMAL(12,2) NOT NULL,
        [allowance] DECIMAL(12,2) NOT NULL,
        [gross_salary] DECIMAL(12,2) NOT NULL,
        
        -- Deductions
        [sss_deduction] DECIMAL(12,2) NOT NULL DEFAULT 0.00,
        [philhealth_deduction] DECIMAL(12,2) NOT NULL DEFAULT 0.00,
        [pagibig_deduction] DECIMAL(12,2) NOT NULL DEFAULT 0.00,
        [tax_deduction] DECIMAL(12,2) NOT NULL DEFAULT 0.00,
        [other_deductions] DECIMAL(12,2) NOT NULL DEFAULT 0.00,
        [total_deductions] DECIMAL(12,2) NOT NULL DEFAULT 0.00,
        
        [net_salary] DECIMAL(12,2) NOT NULL,
        [status] VARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Paid, Cancelled
        [payment_date] DATE NULL,
        [payment_method] VARCHAR(50) NULL, -- Cash, Bank Transfer, Check
        [reference_number] VARCHAR(100) NULL, -- Check number, transaction ID, etc.
        [processed_by] INT NOT NULL, -- Admin/Payroll user who processed the payment
        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
        [updated_at] DATETIME NULL,
        [notes] NVARCHAR(500) NULL,
        
        CONSTRAINT FK_tbl_payroll_transactions_tbl_Users FOREIGN KEY ([user_id]) 
            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
        CONSTRAINT FK_tbl_payroll_transactions_ProcessedBy FOREIGN KEY ([processed_by]) 
            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
        CONSTRAINT CK_tbl_payroll_transactions_Status CHECK ([status] IN ('Pending', 'Paid', 'Cancelled'))
    );
    
    -- Create indexes
    CREATE INDEX IX_tbl_payroll_transactions_user_id ON [dbo].[tbl_payroll_transactions]([user_id]);
    CREATE INDEX IX_tbl_payroll_transactions_school_year ON [dbo].[tbl_payroll_transactions]([school_year]);
    CREATE INDEX IX_tbl_payroll_transactions_pay_period ON [dbo].[tbl_payroll_transactions]([pay_period]);
    CREATE INDEX IX_tbl_payroll_transactions_status ON [dbo].[tbl_payroll_transactions]([status]);
    CREATE INDEX IX_tbl_payroll_transactions_processed_by ON [dbo].[tbl_payroll_transactions]([processed_by]);
    CREATE INDEX IX_tbl_payroll_transactions_created_at ON [dbo].[tbl_payroll_transactions]([created_at]);
    CREATE INDEX IX_tbl_payroll_transactions_user_schoolyear_payperiod ON [dbo].[tbl_payroll_transactions]([user_id], [school_year], [pay_period]);
    
    PRINT 'Table [tbl_payroll_transactions] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [tbl_payroll_transactions] already exists.';
END
GO

