# Database Scripts Guide

## Which Script Should I Use?

### ✅ **For New Device Setup (USE THIS ONE)**
**`Initialize_Database.sql`** - Complete database setup script (all-in-one)
- Creates the database
- Creates **ALL** tables including:
  - User management (Users, User Status Logs)
  - Student management (Students, Guardians, Requirements, Student ID Sequence)
  - Employee management (Address, Emergency Contact, Salary Info)
  - Finance (Grade Levels, Fees, Fee Breakdown)
  - Curriculum (Buildings, Classrooms, Sections, Subjects, SubjectSchedule, SubjectSection, TeacherSectionAssignment, ClassSchedule)
- Creates all indexes
- Creates stored procedures (sp_CreateStudent with sequence synchronization)
- Creates views (vw_EmployeeData, vw_StudentData)
- Seeds initial data (Grade Levels)
- Ready to use!

**Note:** The application auto-creates the database on startup. This script is for manual setup only.

### 📦 **Optional Scripts (For Advanced Users)**

1. **`Export_Database.sql`** - For exporting database schema
   - Use when you want to document your database structure

2. **`Backup_Database_Data.sql`** - For backing up existing data
   - Use when you want to transfer data from one device to another

3. **`sp_CreateStudent_Improved.sql`** - Upgrade script for existing databases
   - Use this if you have an existing database and want to upgrade to the improved stored procedure
   - The improved version includes automatic sequence synchronization to prevent ID conflicts
   - Run this to replace the basic sp_CreateStudent procedure

---

## Quick Start

1. **Open SQL Server Management Studio (SSMS)**
2. **Connect to your SQL Server** (LocalDB, Express, etc.)
3. **Open `Initialize_Database.sql`**
4. **Execute the script** (F5)
5. **Done!** The database is ready.

---

## Summary

- **Main Script**: `Initialize_Database.sql` ✅
- **Others**: Optional utilities (you can ignore them)

