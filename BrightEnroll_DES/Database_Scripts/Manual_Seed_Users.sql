-- Manual Seeder Script for Admin and HR Users
-- Run this script in SSMS if the automatic seeder fails
-- Make sure you're connected to DB_BrightEnroll_DES database

USE [DB_BrightEnroll_DES];
GO

-- Check if users already exist
IF EXISTS (SELECT 1 FROM tbl_Users WHERE system_ID = 'BDES-0001')
BEGIN
    PRINT 'Admin user (BDES-0001) already exists. Skipping...';
END
ELSE
BEGIN
    -- Insert Admin User
    DECLARE @AdminUserId INT;
    
    INSERT INTO tbl_Users (
        system_ID, first_name, mid_name, last_name, suffix,
        birthdate, age, gender, contact_num, user_role,
        email, password, date_hired, status
    )
    VALUES (
        'BDES-0001', 'Josh', NULL, 'Vanderson', NULL,
        '2000-01-01', 24, 'male', '09366669571', 'Admin',
        'joshvanderson01@gmail.com', 
        '$2a$11$KIXqJXqJXqJXqJXqJXqJXe', -- This is a placeholder - you'll need to hash the password
        GETDATE(), 'active'
    );
    
    SET @AdminUserId = SCOPE_IDENTITY();
    PRINT 'Admin user inserted with UserId: ' + CAST(@AdminUserId AS VARCHAR(10));
    
    -- Insert Admin Employee Address
    INSERT INTO tbl_employee_address (user_id, house_no, street_name, province, city, barangay, country, zip_code)
    VALUES (@AdminUserId, '123', 'Main Street', 'Davao del Sur', 'Davao City', 'Poblacion', 'Philippines', '8000');
    
    -- Insert Admin Emergency Contact
    INSERT INTO tbl_employee_emergency_contact (user_id, first_name, middle_name, last_name, suffix, relationship, contact_number, address)
    VALUES (@AdminUserId, 'Jane', NULL, 'Vanderson', NULL, 'Spouse', '09123456789', '123 Main Street, Poblacion, Davao City, Davao del Sur');
    
    -- Insert Admin Salary Info
    INSERT INTO tbl_salary_info (user_id, base_salary, allowance, date_effective, is_active)
    VALUES (@AdminUserId, 50000.00, 5000.00, GETDATE(), 1);
    
    PRINT 'Admin user and related records inserted successfully!';
END
GO

-- Check if HR user already exists
IF EXISTS (SELECT 1 FROM tbl_Users WHERE system_ID = 'BDES-0002')
BEGIN
    PRINT 'HR user (BDES-0002) already exists. Skipping...';
END
ELSE
BEGIN
    -- Insert HR User
    DECLARE @HRUserId INT;
    
    INSERT INTO tbl_Users (
        system_ID, first_name, mid_name, last_name, suffix,
        birthdate, age, gender, contact_num, user_role,
        email, password, date_hired, status
    )
    VALUES (
        'BDES-0002', 'Maria', 'Cruz', 'Santos', NULL,
        '1995-05-15', 29, 'female', '09123456789', 'HR',
        'hr@brightenroll.com',
        '$2a$11$KIXqJXqJXqJXqJXqJXqJXe', -- This is a placeholder - you'll need to hash the password
        GETDATE(), 'active'
    );
    
    SET @HRUserId = SCOPE_IDENTITY();
    PRINT 'HR user inserted with UserId: ' + CAST(@HRUserId AS VARCHAR(10));
    
    -- Insert HR Employee Address
    INSERT INTO tbl_employee_address (user_id, house_no, street_name, province, city, barangay, country, zip_code)
    VALUES (@HRUserId, '456', 'HR Avenue', 'Davao del Sur', 'Davao City', 'Buhangin', 'Philippines', '8000');
    
    -- Insert HR Emergency Contact
    INSERT INTO tbl_employee_emergency_contact (user_id, first_name, middle_name, last_name, suffix, relationship, contact_number, address)
    VALUES (@HRUserId, 'Juan', NULL, 'Santos', NULL, 'Spouse', '09234567890', '456 HR Avenue, Buhangin, Davao City, Davao del Sur');
    
    -- Insert HR Salary Info
    INSERT INTO tbl_salary_info (user_id, base_salary, allowance, date_effective, is_active)
    VALUES (@HRUserId, 45000.00, 5000.00, GETDATE(), 1);
    
    PRINT 'HR user and related records inserted successfully!';
END
GO

PRINT 'Seeder script completed!';
PRINT 'Note: You need to update the password hashes with actual BCrypt hashes.';
PRINT 'Default passwords: Admin123456 (Admin), HR123456 (HR)';

