-- =============================================
-- Improved Student ID Generation Stored Procedure
-- This version includes automatic sequence table synchronization
-- to prevent primary key conflicts when sequence is out of sync
-- =============================================

USE [DB_BrightEnroll_DES];
GO

-- Drop procedure if it exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_CreateStudent]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[sp_CreateStudent];
GO

-- Create the improved stored procedure with sequence synchronization
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
    
    -- Maximum retry attempts for sequence synchronization
    DECLARE @MaxRetries INT = 3;
    DECLARE @RetryCount INT = 0;
    DECLARE @Success BIT = 0;
    
    WHILE @RetryCount < @MaxRetries AND @Success = 0
    BEGIN
        BEGIN TRY
            BEGIN TRANSACTION;
            
            DECLARE @NewID INT;
            DECLARE @FormattedID VARCHAR(6);
            DECLARE @MaxExistingID INT;
            
            -- STEP 1: Synchronize sequence table with highest existing student ID
            -- This prevents primary key conflicts when sequence is out of sync
            -- Only do this on first attempt or if we had a conflict
            IF @RetryCount = 0 OR @RetryCount > 0
            BEGIN
                -- Get the highest existing student ID (convert VARCHAR to INT)
                -- Handle non-numeric student IDs gracefully
                SELECT @MaxExistingID = ISNULL(MAX(
                    CASE 
                        WHEN ISNUMERIC([student_id]) = 1 THEN CAST([student_id] AS INT)
                        ELSE NULL
                    END
                ), 158021) -- Default to 158021 if no valid numeric IDs exist
                FROM [dbo].[tbl_Students]
                WHERE ISNUMERIC([student_id]) = 1 
                  AND LEN([student_id]) = 6;
                
                -- Get current sequence value
                DECLARE @CurrentSequence INT;
                SELECT @CurrentSequence = [LastStudentID]
                FROM [dbo].[tbl_StudentID_Sequence] WITH (UPDLOCK, HOLDLOCK);
                
                -- Update sequence if it's lower than the highest student ID
                -- Add a buffer of 1 to ensure we're always ahead
                IF @CurrentSequence < @MaxExistingID
                BEGIN
                    UPDATE [dbo].[tbl_StudentID_Sequence]
                    SET [LastStudentID] = @MaxExistingID;
                    
                    SET @CurrentSequence = @MaxExistingID;
                END
            END
            
            -- STEP 2: Lock the sequence table and get the next ID atomically
            SELECT @NewID = [LastStudentID]
            FROM [dbo].[tbl_StudentID_Sequence] WITH (UPDLOCK, HOLDLOCK);
            
            -- STEP 3: Check if we've exceeded the 6-digit limit
            IF @NewID >= 999999
            BEGIN
                ROLLBACK TRANSACTION;
                RAISERROR('Student ID limit reached (999999). Cannot generate more student IDs.', 16, 1);
                RETURN;
            END;
            
            -- STEP 4: Increment the ID
            SET @NewID = @NewID + 1;
            
            -- STEP 5: Format to 6 digits with leading zeros
            SET @FormattedID = RIGHT('000000' + CAST(@NewID AS VARCHAR(6)), 6);
            
            -- STEP 6: Verify the generated ID doesn't already exist
            -- This is an additional safety check before insertion
            IF EXISTS (SELECT 1 FROM [dbo].[tbl_Students] WHERE [student_id] = @FormattedID)
            BEGIN
                -- ID conflict detected - sync sequence and retry
                ROLLBACK TRANSACTION;
                
                -- Update sequence to highest existing ID + 1
                SELECT @MaxExistingID = ISNULL(MAX(
                    CASE 
                        WHEN ISNUMERIC([student_id]) = 1 THEN CAST([student_id] AS INT)
                        ELSE NULL
                    END
                ), 158021)
                FROM [dbo].[tbl_Students]
                WHERE ISNUMERIC([student_id]) = 1 
                  AND LEN([student_id]) = 6;
                
                UPDATE [dbo].[tbl_StudentID_Sequence]
                SET [LastStudentID] = @MaxExistingID;
                
                SET @RetryCount = @RetryCount + 1;
                CONTINUE; -- Retry with updated sequence
            END
            
            -- STEP 7: Update the sequence table
            UPDATE [dbo].[tbl_StudentID_Sequence]
            SET [LastStudentID] = @NewID;
            
            -- STEP 8: Insert the student record with the generated ID
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
            
            -- STEP 9: Set output parameter
            SET @student_id = @FormattedID;
            
            -- STEP 10: Mark as successful
            SET @Success = 1;
            
            COMMIT TRANSACTION;
        END TRY
        BEGIN CATCH
            -- Check if this is a primary key violation
            IF ERROR_NUMBER() = 2627 AND ERROR_MESSAGE() LIKE '%PRIMARY KEY%'
            BEGIN
                -- Primary key conflict - rollback and retry
                IF @@TRANCOUNT > 0
                    ROLLBACK TRANSACTION;
                
                -- Sync sequence table with highest existing ID
                SELECT @MaxExistingID = ISNULL(MAX(
                    CASE 
                        WHEN ISNUMERIC([student_id]) = 1 THEN CAST([student_id] AS INT)
                        ELSE NULL
                    END
                ), 158021)
                FROM [dbo].[tbl_Students]
                WHERE ISNUMERIC([student_id]) = 1 
                  AND LEN([student_id]) = 6;
                
                UPDATE [dbo].[tbl_StudentID_Sequence]
                SET [LastStudentID] = @MaxExistingID;
                
                SET @RetryCount = @RetryCount + 1;
                
                -- If we've exhausted retries, throw the error
                IF @RetryCount >= @MaxRetries
                BEGIN
                    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
                    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
                    DECLARE @ErrorState INT = ERROR_STATE();
                    
                    RAISERROR('Failed to generate unique student ID after %d attempts. Sequence table synchronization may be required. Original error: %s', 
                        @ErrorSeverity, @ErrorState, @MaxRetries, @ErrorMessage);
                    RETURN;
                END
                
                -- Continue to retry
                CONTINUE;
            END
            ELSE
            BEGIN
                -- Other error - rollback and throw
                IF @@TRANCOUNT > 0
                    ROLLBACK TRANSACTION;
                
                DECLARE @ErrMessage NVARCHAR(4000) = ERROR_MESSAGE();
                DECLARE @ErrSeverity INT = ERROR_SEVERITY();
                DECLARE @ErrState INT = ERROR_STATE();
                
                RAISERROR(@ErrMessage, @ErrSeverity, @ErrState);
                RETURN;
            END
        END CATCH
    END
    
    -- If we exit the loop without success, something went wrong
    IF @Success = 0
    BEGIN
        RAISERROR('Failed to create student after maximum retry attempts. Please contact administrator.', 16, 1);
        RETURN;
    END
END;
GO

PRINT 'Improved stored procedure [sp_CreateStudent] created successfully with sequence synchronization.';
GO

