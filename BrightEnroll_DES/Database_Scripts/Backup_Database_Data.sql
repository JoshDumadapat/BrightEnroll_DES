-- =============================================
-- BrightEnroll_DES Database Data Backup Script
-- This script exports all data from the database
-- Run this AFTER Export_Database.sql to backup existing data
-- =============================================

USE [DB_BrightEnroll_DES];
GO

-- =============================================
-- BACKUP tbl_Users DATA
-- =============================================

-- Delete existing data (optional - comment out if you want to append)
-- DELETE FROM [dbo].[tbl_Users];
-- GO

-- Insert all users from the database
-- This will generate INSERT statements for all existing data
-- Note: Replace the password hash with actual values from your database

PRINT '========================================';
PRINT 'Data Backup for tbl_Users';
PRINT '========================================';
PRINT '';

-- Generate INSERT statements for all users
-- Run this query and copy the results to use as INSERT statements
SELECT 
    'INSERT INTO [dbo].[tbl_Users] ([system_ID], [first_name], [mid_name], [last_name], [suffix], ' +
    '[birthdate], [age], [gender], [contact_num], [user_role], [email], [password], [date_hired]) ' +
    'VALUES (''' + [system_ID] + ''', ''' + [first_name] + ''', ' +
    CASE WHEN [mid_name] IS NULL THEN 'NULL' ELSE '''' + [mid_name] + '''' END + ', ''' + [last_name] + ''', ' +
    CASE WHEN [suffix] IS NULL THEN 'NULL' ELSE '''' + [suffix] + '''' END + ', ''' + 
    CONVERT(VARCHAR(10), [birthdate], 120) + ''', ' + CAST([age] AS VARCHAR(3)) + ', ''' + [gender] + ''', ''' + 
    [contact_num] + ''', ''' + [user_role] + ''', ''' + [email] + ''', ''' + [password] + ''', ''' + 
    CONVERT(VARCHAR(23), [date_hired], 121) + ''');' AS InsertStatement
FROM [dbo].[tbl_Users]
ORDER BY [user_ID];

GO

PRINT '';
PRINT 'Copy the INSERT statements above and add them to your database initialization script.';
PRINT 'Note: Make sure to handle password hashes correctly - they should match your BCrypt hashes.';
GO

