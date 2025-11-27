# Database Scripts Guide

## Main Database Initialization Script

### ✅ **For New Device Setup (USE THIS ONE)**
**`Initialize_Database_Complete.sql`** - Complete optimized database setup script (all-in-one)
- Creates the database
- Creates **ALL** tables including:
  - User management (Users, User Status Logs)
  - Student management (Students, Guardians, Requirements, Student ID Sequence)
  - Employee management (Address, Emergency Contact, Salary Info)
  - Finance (Grade Levels, Fees, Fee Breakdown, Expenses, Expense Attachments)
  - Curriculum (Buildings, Classrooms, Sections, Subjects, SubjectSchedule, SubjectSection, TeacherSectionAssignment, ClassSchedule)
  - Payroll (Roles, Deductions)
- Creates all indexes (optimized for performance)
- Creates stored procedures (sp_CreateStudent with sequence synchronization)
- Creates views (vw_EmployeeData, vw_StudentData)
- Handles existing databases (adds missing columns, updates column sizes)
- Ready to use!

**Note:** The application auto-creates the database on startup. This script is for manual setup or database migration only.

---

## Quick Start

1. **Open SQL Server Management Studio (SSMS)**
2. **Connect to your SQL Server** (LocalDB, Express, etc.)
3. **Open `Initialize_Database_Complete.sql`**
4. **Execute the script** (F5)
5. **Done!** The database is ready.

---

## Key Features

- **Optimized**: All tables, indexes, and constraints created in correct dependency order
- **Safe**: Checks for existing objects before creating (won't break existing databases)
- **Complete**: Includes all 24 tables, views, stored procedures, and indexes
- **Up-to-date**: Includes latest schema changes (is_verified column, status column size, nullable middle_name)

---

## Summary

- **Main Script**: `Initialize_Database_Complete.sql` ✅
- **All other scripts**: Removed (functionality merged into complete script)
