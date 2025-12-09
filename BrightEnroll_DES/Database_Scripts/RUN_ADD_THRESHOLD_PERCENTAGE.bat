@echo off
echo ============================================
echo Add threshold_percentage Column to tbl_roles
echo ============================================
echo.
echo This script will add the threshold_percentage column to tbl_roles table.
echo.
echo Press any key to continue or Ctrl+C to cancel...
pause >nul
echo.
echo Running database update...
echo.

sqlcmd -S "(localdb)\MSSQLLocalDB" -d "DB_BrightEnroll_DES" -i "Add_Threshold_Percentage_To_Roles.sql"

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

