# Database Setup Guide

## Overview

This guide explains how to set up the database for BrightEnroll_DES. The system supports automatic database creation and manual setup options.

---

## Table of Contents

1. [Automatic Setup (Recommended)](#automatic-setup-recommended)
2. [Manual Setup](#manual-setup)
3. [Connection String Configuration](#connection-string-configuration)
4. [Database Structure](#database-structure)
5. [Initial Admin Account](#initial-admin-account)
6. [Troubleshooting](#troubleshooting)

---

## Automatic Setup (Recommended)

The application **automatically creates the database and tables** when you run it for the first time.

### How It Works

When the application starts:

1. **Application starts** → `MauiProgram.cs` runs
2. **Database Initializer checks** → Does `DB_BrightEnroll_DES` database exist?
   - ❌ **No** → Creates the database automatically
   - ✅ **Yes** → Skips creation
3. **Table Initializer checks** → Do tables exist?
   - ❌ **No** → Creates all tables automatically
   - ✅ **Yes** → Skips creation
4. **Admin User Seeder runs** → Creates admin user if it doesn't exist
5. **Application is ready!** ✅

### What You Need

1. **SQL Server installed**:
   - LocalDB (included with Visual Studio) ✅
   - Or SQL Server Express/Standard

2. **Run the application** (F5)
   - Database will be created automatically
   - Tables will be created automatically
   - Admin user will be seeded automatically

3. **That's it!** No SQL scripts needed!

---

## Manual Setup

If automatic setup doesn't work, you can use the SQL script:

### Steps

1. Open **SQL Server Management Studio (SSMS)**
2. Connect to your SQL Server
3. Open `Database_Scripts/Initialize_Database.sql`
4. Execute the script (F5)
5. Verify the database `DB_BrightEnroll_DES` was created

---

## Connection String Configuration

The application uses a **dynamic connection string** that works automatically.

### Default (LocalDB - Works Out of the Box)

- No configuration needed!
- Uses: `(localdb)\MSSQLLocalDB`
- Database: `DB_BrightEnroll_DES`

### Custom Configuration

If you need a different SQL Server, update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=YOUR_SERVER;Initial Catalog=DB_BrightEnroll_DES;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

### Connection String Priority

The system resolves connection strings in this order:

1. **Environment variable** `ConnectionStrings__DefaultConnection`
2. **appsettings.json** file
3. **Default LocalDB** connection string

---

## Database Structure

### Core Tables

| Table | Purpose | Entity Model |
|-------|---------|--------------|
| `tbl_Users` | User accounts (employees, admins) | `Models/User.cs` |
| `tbl_employee_address` | Employee addresses | `Data/Models/EmployeeAddress.cs` |
| `tbl_employee_emergency_contact` | Emergency contacts | `Data/Models/EmployeeEmergencyContact.cs` |
| `tbl_salary_info` | Salary information | `Data/Models/SalaryInfo.cs` |
| `tbl_Students` | Student records | `Data/Models/Student.cs` |
| `tbl_Guardians` | Guardian information | `Data/Models/Guardian.cs` |
| `tbl_StudentRequirements` | Student requirements | `Data/Models/StudentRequirement.cs` |

### Table Creation

Tables are automatically created by:
- **Automatic**: `Services/DBConnections/DatabaseInitializer.cs`
- **Manual**: `Database_Scripts/Initialize_Database.sql`

Table definitions are stored in:
- **Location**: `Services/DBConnections/TableDefinitions.cs`
- **Purpose**: Contains SQL scripts for creating all tables

---

## Initial Admin Account

The system automatically seeds the first admin account on startup:

- **System ID:** BDES-0001
- **Name:** Josh Vanderson
- **Email:** joshvanderson01@gmail.com
- **Password:** Admin123456
- **Role:** Admin
- **Contact:** 09366669571
- **Gender:** male
- **Birthdate:** January 1, 2000

**Note:** The password is hashed using BCrypt before being stored in the database.

### Seeder Location

- **File**: `Services/Seeders/DatabaseSeeder.cs`
- **Method**: `SeedInitialAdminAsync()`
- **Called**: On application startup in `MauiProgram.cs`

---

## Troubleshooting

### Connection Failed

**Symptoms:**
- Error: "Cannot open database"
- Error: "Login failed"

**Solutions:**
- Verify SQL Server is running
- Check the connection string is correct
- Ensure the database exists
- Verify Windows Authentication or SQL Server Authentication credentials

### Seeder Not Working

**Symptoms:**
- Admin account not created
- Error during startup

**Solutions:**
- Check that the `tbl_Users` table exists
- Verify the table structure matches the expected schema
- Check database permissions for INSERT operations
- Review error logs in Debug output

### Auto-Setup Not Working

**Symptoms:**
- Database not created automatically
- Tables not created

**Solutions:**
1. Check SQL Server is running
2. Verify connection string in `appsettings.json`
3. Manually run `Database_Scripts/Initialize_Database.sql` as a fallback
4. Check error logs in Debug output

### Login Not Working

**Symptoms:**
- Cannot login with admin credentials
- "Invalid credentials" error

**Solutions:**
- Verify the admin account was seeded successfully (check database)
- Check email matches exactly (case-sensitive)
- Verify password is correct: `Admin123456`
- Check BCrypt hashing is working correctly

---

## Summary

✅ **Database is automatically created** when you run the application  
✅ **Tables are automatically created** if they don't exist  
✅ **Admin user is automatically seeded** on first run  
✅ **No SQL scripts needed** for normal operation  
✅ **SQL scripts are available** as a backup option  

**Just run the application and it works!** 🎉

