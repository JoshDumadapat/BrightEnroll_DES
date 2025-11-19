-- =============================================
-- Create Student ID Generation Stored Procedure
-- Run this script in SSMS to create sp_CreateStudent
-- =============================================

USE [DB_BrightEnroll_DES];
GO

-- Drop procedure if it exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_CreateStudent]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_CreateStudent];
GO

-- Create the stored procedure
CREATE PROCEDURE [dbo].[sp_CreateStudent]
    @first_name VARCHAR(50),
    @middle_name VARCHAR(50),
    @last_name VARCHAR(50),
    @suffix VARCHAR(10) = NULL,
    @birthdate DATE,
    @age INT,
    @place_of_birth VARCHAR(100) = NULL,
    @sex VARCHAR(10),
    @mother_tongue VARCHAR(50) = NULL,
    @ip_comm BIT = 0,
    @ip_specify VARCHAR(50) = NULL,
    @four_ps BIT = 0,
    @four_ps_hseID VARCHAR(50) = NULL,
    @hse_no VARCHAR(20) = NULL,
    @street VARCHAR(100) = NULL,
    @brngy VARCHAR(50) = NULL,
    @province VARCHAR(50) = NULL,
    @city VARCHAR(50) = NULL,
    @country VARCHAR(50) = NULL,
    @zip_code VARCHAR(10) = NULL,
    @phse_no VARCHAR(20) = NULL,
    @pstreet VARCHAR(100) = NULL,
    @pbrngy VARCHAR(50) = NULL,
    @pprovince VARCHAR(50) = NULL,
    @pcity VARCHAR(50) = NULL,
    @pcountry VARCHAR(50) = NULL,
    @pzip_code VARCHAR(10) = NULL,
    @student_type VARCHAR(20),
    @LRN VARCHAR(20) = NULL,
    @school_yr VARCHAR(20) = NULL,
    @grade_level VARCHAR(10) = NULL,
    @guardian_id INT,
    @student_id VARCHAR(6) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        DECLARE @NewID INT;
        DECLARE @FormattedID VARCHAR(6);
        
        -- Lock the sequence table and get the next ID atomically
        SELECT @NewID = [LastStudentID]
        FROM [dbo].[tbl_StudentID_Sequence] WITH (UPDLOCK, HOLDLOCK);
        
        -- Check if we've exceeded the 6-digit limit
        IF @NewID >= 999999
        BEGIN
            ROLLBACK TRANSACTION;
            RAISERROR('Student ID limit reached (999999). Cannot generate more student IDs.', 16, 1);
            RETURN;
        END;
        
        -- Increment the ID
        SET @NewID = @NewID + 1;
        
        -- Format to 6 digits with leading zeros
        SET @FormattedID = RIGHT('000000' + CAST(@NewID AS VARCHAR(6)), 6);
        
        -- Update the sequence table
        UPDATE [dbo].[tbl_StudentID_Sequence]
        SET [LastStudentID] = @NewID;
        
        -- Insert the student record with the generated ID
        INSERT INTO [dbo].[tbl_Students] (
            [student_id], [first_name], [middle_name], [last_name], [suffix],
            [birthdate], [age], [place_of_birth], [sex], [mother_tongue],
            [ip_comm], [ip_specify], [four_ps], [four_ps_hseID],
            [hse_no], [street], [brngy], [province], [city], [country], [zip_code],
            [phse_no], [pstreet], [pbrngy], [pprovince], [pcity], [pcountry], [pzip_code],
            [student_type], [LRN], [school_yr], [grade_level], [guardian_id],
            [date_registered], [status]
        )
        VALUES (
            @FormattedID, @first_name, @middle_name, @last_name, @suffix,
            @birthdate, @age, @place_of_birth, @sex, @mother_tongue,
            @ip_comm, @ip_specify, @four_ps, @four_ps_hseID,
            @hse_no, @street, @brngy, @province, @city, @country, @zip_code,
            @phse_no, @pstreet, @pbrngy, @pprovince, @pcity, @pcountry, @pzip_code,
            @student_type, @LRN, @school_yr, @grade_level, @guardian_id,
            GETDATE(), 'Pending'
        );
        
        -- Set output parameter
        SET @student_id = @FormattedID;
        
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

PRINT 'Stored procedure [sp_CreateStudent] created successfully.';
GO

