@echo off
echo ============================================
echo HR & Payroll Database Update Script
echo ============================================
echo.
echo This script will update your database with:
echo 1. Add school_year column to tbl_salary_info
echo 2. Add threshold_percentage column to tbl_roles
echo 3. Create tbl_salary_change_requests table
echo 4. Create tbl_payroll_transactions table
echo.
echo Press any key to continue or Ctrl+C to cancel...
pause >nul
echo.
echo Running database update...
echo.

sqlcmd -S "(localdb)\MSSQLLocalDB" -d "DB_BrightEnroll_DES" -i "Update_HR_Payroll_Tables.sql"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ============================================
    echo Database update completed successfully!
    echo ============================================
) else (
    echo.
    echo ============================================
    echo ERROR: Database update failed!
    echo ============================================
    echo Please check the error messages above.
)

echo.
pause

