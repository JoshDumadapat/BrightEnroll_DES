-- =============================================
-- Create tbl_salary_change_requests table
-- Tracks salary change requests from HR that require Payroll/Admin approval
-- =============================================

USE [DB_BrightEnroll_DES];
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_salary_change_requests' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[tbl_salary_change_requests](
        [request_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [user_id] INT NOT NULL,
        [current_base_salary] DECIMAL(12,2) NOT NULL,
        [current_allowance] DECIMAL(12,2) NOT NULL,
        [requested_base_salary] DECIMAL(12,2) NOT NULL,
        [requested_allowance] DECIMAL(12,2) NOT NULL,
        [reason] NVARCHAR(500) NULL,
        [status] VARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Approved, Rejected
        [rejection_reason] NVARCHAR(500) NULL,
        [requested_by] INT NOT NULL, -- HR user who created the request
        [approved_by] INT NULL, -- Admin/Payroll user who approved/rejected
        [requested_at] DATETIME NOT NULL DEFAULT GETDATE(),
        [approved_at] DATETIME NULL,
        [effective_date] DATE NULL,
        [school_year] VARCHAR(20) NOT NULL,
        [is_initial_registration] BIT NOT NULL DEFAULT 0, -- True if from Add Employee, False if from Edit
        
        CONSTRAINT FK_tbl_salary_change_requests_tbl_Users FOREIGN KEY ([user_id]) 
            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
        CONSTRAINT FK_tbl_salary_change_requests_RequestedBy FOREIGN KEY ([requested_by]) 
            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
        CONSTRAINT FK_tbl_salary_change_requests_ApprovedBy FOREIGN KEY ([approved_by]) 
            REFERENCES [dbo].[tbl_Users]([user_ID]) ON DELETE NO ACTION,
        CONSTRAINT CK_tbl_salary_change_requests_Status CHECK ([status] IN ('Pending', 'Approved', 'Rejected'))
    );
    
    -- Create indexes
    CREATE INDEX IX_tbl_salary_change_requests_user_id ON [dbo].[tbl_salary_change_requests]([user_id]);
    CREATE INDEX IX_tbl_salary_change_requests_requested_by ON [dbo].[tbl_salary_change_requests]([requested_by]);
    CREATE INDEX IX_tbl_salary_change_requests_status ON [dbo].[tbl_salary_change_requests]([status]);
    CREATE INDEX IX_tbl_salary_change_requests_school_year ON [dbo].[tbl_salary_change_requests]([school_year]);
    CREATE INDEX IX_tbl_salary_change_requests_requested_at ON [dbo].[tbl_salary_change_requests]([requested_at]);
    
    PRINT 'Table [tbl_salary_change_requests] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [tbl_salary_change_requests] already exists.';
END
GO

