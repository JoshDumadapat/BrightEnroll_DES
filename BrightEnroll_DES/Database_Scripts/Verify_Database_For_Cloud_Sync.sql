-- =============================================
-- BrightEnroll_DES Database Cloud Sync Verification Script
-- Run this script on your LOCAL database to verify readiness for cloud sync
-- =============================================

USE [DB_BrightEnroll_DES];
GO

SET NOCOUNT ON;
GO

PRINT '========================================';
PRINT 'CLOUD SYNC READINESS VERIFICATION';
PRINT '========================================';
PRINT 'Database: DB_BrightEnroll_DES';
PRINT 'Date: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '';

-- =============================================
-- VERIFICATION 1: Check All 31 Tables Exist
-- =============================================
PRINT 'VERIFICATION 1: Table Completeness';
PRINT '----------------------------------------';

DECLARE @ExpectedTables TABLE (TableName VARCHAR(100), Category VARCHAR(50));
INSERT INTO @ExpectedTables VALUES
    ('tbl_Users', 'User Management'),
    ('tbl_user_status_logs', 'User Management'),
    ('tbl_Guardians', 'Student Management'),
    ('tbl_StudentID_Sequence', 'Student Management'),
    ('tbl_Students', 'Student Management'),
    ('tbl_StudentRequirements', 'Student Management'),
    ('tbl_StudentSectionEnrollment', 'Student Management'),
    ('tbl_StudentPayments', 'Student Management'),
    ('tbl_student_status_logs', 'Student Management'),
    ('tbl_employee_address', 'Employee Management'),
    ('tbl_employee_emergency_contact', 'Employee Management'),
    ('tbl_salary_info', 'Employee Management'),
    ('tbl_GradeLevel', 'Finance'),
    ('tbl_Fees', 'Finance'),
    ('tbl_FeeBreakdown', 'Finance'),
    ('tbl_Expenses', 'Finance'),
    ('tbl_ExpenseAttachments', 'Finance'),
    ('tbl_Buildings', 'Curriculum'),
    ('tbl_Classrooms', 'Curriculum'),
    ('tbl_Sections', 'Curriculum'),
    ('tbl_Subjects', 'Curriculum'),
    ('tbl_SubjectSection', 'Curriculum'),
    ('tbl_SubjectSchedule', 'Curriculum'),
    ('tbl_TeacherSectionAssignment', 'Curriculum'),
    ('tbl_ClassSchedule', 'Curriculum'),
    ('tbl_roles', 'Payroll'),
    ('tbl_deductions', 'Payroll'),
    ('tbl_Assets', 'Inventory'),
    ('tbl_InventoryItems', 'Inventory'),
    ('tbl_AssetAssignments', 'Inventory');

DECLARE @ExistingTables TABLE (TableName VARCHAR(100));
INSERT INTO @ExistingTables
SELECT t.name
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = 'dbo';

DECLARE @MissingTables TABLE (TableName VARCHAR(100), Category VARCHAR(50));
INSERT INTO @MissingTables
SELECT et.TableName, et.Category
FROM @ExpectedTables et
LEFT JOIN @ExistingTables ext ON et.TableName = ext.TableName
WHERE ext.TableName IS NULL;

DECLARE @TableCount INT;
SELECT @TableCount = COUNT(*) FROM @ExistingTables;

IF EXISTS (SELECT 1 FROM @MissingTables)
BEGIN
    PRINT '❌ MISSING TABLES:';
    SELECT TableName AS 'Missing Table', Category
    FROM @MissingTables
    ORDER BY Category, TableName;
    PRINT '';
    PRINT CONCAT('Status: ', @TableCount, '/31 tables found');
END
ELSE
BEGIN
    PRINT CONCAT('✅ All 31 expected tables exist (Found: ', @TableCount, ')');
    PRINT '';
END

-- =============================================
-- VERIFICATION 2: Check Sync Metadata Fields
-- =============================================
PRINT 'VERIFICATION 2: Sync Metadata Fields';
PRINT '----------------------------------------';

DECLARE @TablesWithSyncFields TABLE (TableName VARCHAR(100));
DECLARE @TablesNeedingSyncFields TABLE (TableName VARCHAR(100));

-- Check each table for sync fields
DECLARE @TableName VARCHAR(100);
DECLARE table_cursor CURSOR FOR
SELECT TableName FROM @ExpectedTables;

OPEN table_cursor;
FETCH NEXT FROM table_cursor INTO @TableName;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = @TableName)
    BEGIN
        DECLARE @HasLastSyncedAt BIT = 0;
        DECLARE @HasSyncStatus BIT = 0;
        DECLARE @HasSyncConflict BIT = 0;
        
        SELECT @HasLastSyncedAt = 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('dbo.' + @TableName) AND name = 'last_synced_at';
        
        SELECT @HasSyncStatus = 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('dbo.' + @TableName) AND name = 'sync_status';
        
        SELECT @HasSyncConflict = 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('dbo.' + @TableName) AND name = 'sync_conflict';
        
        IF @HasLastSyncedAt = 1 AND @HasSyncStatus = 1 AND @HasSyncConflict = 1
        BEGIN
            INSERT INTO @TablesWithSyncFields VALUES (@TableName);
        END
        ELSE
        BEGIN
            INSERT INTO @TablesNeedingSyncFields VALUES (@TableName);
        END
    END
    
    FETCH NEXT FROM table_cursor INTO @TableName;
END

CLOSE table_cursor;
DEALLOCATE table_cursor;

DECLARE @TablesWithSyncCount INT;
SELECT @TablesWithSyncCount = COUNT(*) FROM @TablesWithSyncFields;

IF EXISTS (SELECT 1 FROM @TablesNeedingSyncFields)
BEGIN
    PRINT '❌ TABLES MISSING SYNC FIELDS:';
    SELECT TableName AS 'Table Missing Sync Fields'
    FROM @TablesNeedingSyncFields
    ORDER BY TableName;
    PRINT '';
    PRINT CONCAT('Status: ', @TablesWithSyncCount, '/', @TableCount, ' tables have sync fields');
    PRINT '⚠️  CRITICAL: Sync fields required for cloud synchronization';
END
ELSE
BEGIN
    PRINT CONCAT('✅ All ', @TableCount, ' tables have sync metadata fields');
END
PRINT '';

-- =============================================
-- VERIFICATION 3: Check Temporary ID Support
-- =============================================
PRINT 'VERIFICATION 3: Temporary ID Support';
PRINT '----------------------------------------';

-- Check for ID mapping table
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_SyncIdMappings')
BEGIN
    PRINT '✅ ID Mapping table (tbl_SyncIdMappings) exists';
END
ELSE
BEGIN
    PRINT '❌ ID Mapping table (tbl_SyncIdMappings) is MISSING';
    PRINT '   Required for temporary ID support during offline sync';
END

-- Check for TempId fields in key tables
DECLARE @KeyTables TABLE (TableName VARCHAR(100));
INSERT INTO @KeyTables VALUES
    ('tbl_Students'),
    ('tbl_Users'),
    ('tbl_Guardians'),
    ('tbl_StudentRequirements'),
    ('tbl_StudentPayments');

DECLARE @TablesWithTempId TABLE (TableName VARCHAR(100));
DECLARE @TablesNeedingTempId TABLE (TableName VARCHAR(100));

DECLARE @KeyTableName VARCHAR(100);
DECLARE key_cursor CURSOR FOR SELECT TableName FROM @KeyTables;

OPEN key_cursor;
FETCH NEXT FROM key_cursor INTO @KeyTableName;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = @KeyTableName)
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.columns 
                   WHERE object_id = OBJECT_ID('dbo.' + @KeyTableName) 
                   AND (name = 'temp_id' OR name = 'TempId' OR name = 'temporary_id'))
        BEGIN
            INSERT INTO @TablesWithTempId VALUES (@KeyTableName);
        END
        ELSE
        BEGIN
            INSERT INTO @TablesNeedingTempId VALUES (@KeyTableName);
        END
    END
    
    FETCH NEXT FROM key_cursor INTO @KeyTableName;
END

CLOSE key_cursor;
DEALLOCATE key_cursor;

IF EXISTS (SELECT 1 FROM @TablesNeedingTempId)
BEGIN
    PRINT '❌ TABLES MISSING TEMP ID FIELDS:';
    SELECT TableName AS 'Table Missing TempId'
    FROM @TablesNeedingTempId;
    PRINT '⚠️  CRITICAL: TempId fields required for offline record creation';
END
ELSE
BEGIN
    PRINT '✅ Key tables have TempId fields';
END
PRINT '';

-- =============================================
-- VERIFICATION 4: Check Critical Columns
-- =============================================
PRINT 'VERIFICATION 4: Critical Columns';
PRINT '----------------------------------------';

-- Check tbl_Students
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_Students')
BEGIN
    DECLARE @HasArchiveReason BIT = 0;
    DECLARE @HasAmountPaid BIT = 0;
    DECLARE @HasPaymentStatus BIT = 0;
    
    SELECT @HasArchiveReason = 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.tbl_Students') AND name = 'archive_reason';
    
    SELECT @HasAmountPaid = 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.tbl_Students') AND name = 'amount_paid';
    
    SELECT @HasPaymentStatus = 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.tbl_Students') AND name = 'payment_status';
    
    IF @HasArchiveReason = 1 AND @HasAmountPaid = 1 AND @HasPaymentStatus = 1
    BEGIN
        PRINT '✅ tbl_Students has all payment columns';
    END
    ELSE
    BEGIN
        PRINT '⚠️  tbl_Students missing columns:';
        IF @HasArchiveReason = 0 PRINT '   - archive_reason';
        IF @HasAmountPaid = 0 PRINT '   - amount_paid';
        IF @HasPaymentStatus = 0 PRINT '   - payment_status';
    END
END

-- Check tbl_StudentRequirements
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_StudentRequirements')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns 
               WHERE object_id = OBJECT_ID('dbo.tbl_StudentRequirements') 
               AND name = 'is_verified')
    BEGIN
        PRINT '✅ tbl_StudentRequirements has is_verified column';
    END
    ELSE
    BEGIN
        PRINT '⚠️  tbl_StudentRequirements missing is_verified column';
    END
END
PRINT '';

-- =============================================
-- VERIFICATION 5: Check Foreign Keys
-- =============================================
PRINT 'VERIFICATION 5: Foreign Key Constraints';
PRINT '----------------------------------------';

DECLARE @FKCount INT;
SELECT @FKCount = COUNT(*)
FROM sys.foreign_keys fk
INNER JOIN sys.tables t ON fk.parent_object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = 'dbo';

PRINT CONCAT('Found ', @FKCount, ' foreign key constraints');

-- Check critical foreign keys
DECLARE @CriticalFKs TABLE (FKName VARCHAR(200), TableName VARCHAR(100), Status VARCHAR(20));
INSERT INTO @CriticalFKs VALUES
    ('FK_tbl_Students_tbl_Guardians', 'tbl_Students', 'Missing'),
    ('FK_tbl_StudentPayments_tbl_Students', 'tbl_StudentPayments', 'Missing'),
    ('FK_tbl_employee_address_tbl_Users', 'tbl_employee_address', 'Missing'),
    ('FK_tbl_Fees_GradeLevel', 'tbl_Fees', 'Missing'),
    ('FK_tbl_ExpenseAttachments_tbl_Expenses', 'tbl_ExpenseAttachments', 'Missing');

UPDATE cfk
SET Status = 'Exists'
FROM @CriticalFKs cfk
INNER JOIN sys.foreign_keys fk ON fk.name = cfk.FKName;

IF EXISTS (SELECT 1 FROM @CriticalFKs WHERE Status = 'Missing')
BEGIN
    PRINT '⚠️  WARNING: Missing critical foreign keys:';
    SELECT FKName AS 'Missing FK', TableName AS 'Table'
    FROM @CriticalFKs
    WHERE Status = 'Missing';
END
ELSE
BEGIN
    PRINT '✅ All critical foreign keys exist';
END
PRINT '';

-- =============================================
-- VERIFICATION 6: Check Views
-- =============================================
PRINT 'VERIFICATION 6: Database Views';
PRINT '----------------------------------------';

DECLARE @ExpectedViews TABLE (ViewName VARCHAR(100));
INSERT INTO @ExpectedViews VALUES
    ('vw_EmployeeData'),
    ('vw_StudentData'),
    ('tbl_FinalClasses');

DECLARE @ExistingViews TABLE (ViewName VARCHAR(100));
INSERT INTO @ExistingViews
SELECT v.name
FROM sys.views v
INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
WHERE s.name = 'dbo';

DECLARE @MissingViews TABLE (ViewName VARCHAR(100));
INSERT INTO @MissingViews
SELECT ev.ViewName
FROM @ExpectedViews ev
LEFT JOIN @ExistingViews exv ON ev.ViewName = exv.ViewName
WHERE exv.ViewName IS NULL;

DECLARE @ViewCount INT;
SELECT @ViewCount = COUNT(*) FROM @ExistingViews;

IF EXISTS (SELECT 1 FROM @MissingViews)
BEGIN
    PRINT '⚠️  WARNING: Missing views:';
    SELECT ViewName AS 'Missing View'
    FROM @MissingViews;
    PRINT CONCAT('Status: ', @ViewCount, '/3 views found');
END
ELSE
BEGIN
    PRINT CONCAT('✅ All 3 expected views exist (Found: ', @ViewCount, ')');
END
PRINT '';

-- =============================================
-- VERIFICATION 7: Check Stored Procedures
-- =============================================
PRINT 'VERIFICATION 7: Stored Procedures';
PRINT '----------------------------------------';

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_CreateStudent' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    PRINT '✅ Stored procedure sp_CreateStudent exists';
END
ELSE
BEGIN
    PRINT '⚠️  WARNING: Stored procedure sp_CreateStudent is missing';
END
PRINT '';

-- =============================================
-- VERIFICATION 8: Data Integrity Check
-- =============================================
PRINT 'VERIFICATION 8: Data Integrity';
PRINT '----------------------------------------';

DECLARE @OrphanedStudents INT = 0;
DECLARE @OrphanedPayments INT = 0;
DECLARE @OrphanedAddresses INT = 0;

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_Students')
BEGIN
    SELECT @OrphanedStudents = COUNT(*)
    FROM [dbo].[tbl_Students] s
    LEFT JOIN [dbo].[tbl_Guardians] g ON s.guardian_id = g.guardian_id
    WHERE g.guardian_id IS NULL;
END

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_StudentPayments')
BEGIN
    SELECT @OrphanedPayments = COUNT(*)
    FROM [dbo].[tbl_StudentPayments] sp
    LEFT JOIN [dbo].[tbl_Students] s ON sp.student_id = s.student_id
    WHERE s.student_id IS NULL;
END

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_employee_address')
BEGIN
    SELECT @OrphanedAddresses = COUNT(*)
    FROM [dbo].[tbl_employee_address] ea
    LEFT JOIN [dbo].[tbl_Users] u ON ea.user_ID = u.user_ID
    WHERE u.user_ID IS NULL;
END

IF @OrphanedStudents > 0 OR @OrphanedPayments > 0 OR @OrphanedAddresses > 0
BEGIN
    PRINT '⚠️  WARNING: Found orphaned records:';
    IF @OrphanedStudents > 0 PRINT CONCAT('   - ', @OrphanedStudents, ' students with invalid guardian_id');
    IF @OrphanedPayments > 0 PRINT CONCAT('   - ', @OrphanedPayments, ' payments with invalid student_id');
    IF @OrphanedAddresses > 0 PRINT CONCAT('   - ', @OrphanedAddresses, ' addresses with invalid user_ID');
    PRINT '   Fix these before exporting to cloud';
END
ELSE
BEGIN
    PRINT '✅ No orphaned records found';
END
PRINT '';

-- =============================================
-- FINAL SUMMARY
-- =============================================
PRINT '========================================';
PRINT 'VERIFICATION SUMMARY';
PRINT '========================================';
PRINT '';

DECLARE @IsStructurallyComplete BIT = 1;
DECLARE @IsSyncReady BIT = 1;

-- Check structural completeness
IF EXISTS (SELECT 1 FROM @MissingTables) SET @IsStructurallyComplete = 0;
IF EXISTS (SELECT 1 FROM @MissingViews) SET @IsStructurallyComplete = 0;
IF @OrphanedStudents > 0 OR @OrphanedPayments > 0 OR @OrphanedAddresses > 0 SET @IsStructurallyComplete = 0;

-- Check sync readiness
IF EXISTS (SELECT 1 FROM @TablesNeedingSyncFields) SET @IsSyncReady = 0;
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_SyncIdMappings') SET @IsSyncReady = 0;
IF EXISTS (SELECT 1 FROM @TablesNeedingTempId) SET @IsSyncReady = 0;

PRINT 'Database Status:';
PRINT CONCAT('  Tables: ', @TableCount, '/31');
PRINT CONCAT('  Views: ', @ViewCount, '/3');
PRINT CONCAT('  Foreign Keys: ', @FKCount);
PRINT CONCAT('  Tables with Sync Fields: ', @TablesWithSyncCount, '/', @TableCount);
PRINT '';

IF @IsStructurallyComplete = 1
BEGIN
    PRINT '✅ STRUCTURAL COMPLETENESS: PASSED';
    PRINT '   Database structure is complete and ready for export.';
END
ELSE
BEGIN
    PRINT '❌ STRUCTURAL COMPLETENESS: FAILED';
    PRINT '   Missing tables, views, or data integrity issues found.';
    PRINT '   Fix issues before exporting.';
END
PRINT '';

IF @IsSyncReady = 1
BEGIN
    PRINT '✅ SYNC READINESS: PASSED';
    PRINT '   Database has sync infrastructure and is ready for cloud synchronization.';
END
ELSE
BEGIN
    PRINT '❌ SYNC READINESS: FAILED';
    PRINT '   Missing sync metadata fields or temporary ID support.';
    PRINT '   Cannot perform cloud synchronization until fixed.';
    PRINT '';
    PRINT 'Required Actions:';
    IF EXISTS (SELECT 1 FROM @TablesNeedingSyncFields)
        PRINT '   1. Add sync fields (last_synced_at, sync_status, sync_conflict) to all tables';
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_SyncIdMappings')
        PRINT '   2. Create tbl_SyncIdMappings table for ID mapping';
    IF EXISTS (SELECT 1 FROM @TablesNeedingTempId)
        PRINT '   3. Add TempId fields to key tables';
END

PRINT '';
PRINT '========================================';
PRINT 'Verification completed.';
PRINT 'Review the detailed report: DATABASE_CLOUD_SYNC_READINESS_REPORT.md';
GO

