# Database Transfer and Setup Guide

## Overview

This guide explains how to transfer your BrightEnroll_DES project to any device and set up the database without manual configuration. The system uses dynamic connection strings and includes SQL scripts for easy database setup.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Project Structure](#project-structure)
3. [Database Setup Methods](#database-setup-methods)
4. [Connection String Configuration](#connection-string-configuration)
5. [Step-by-Step Transfer Process](#step-by-step-transfer-process)
6. [Troubleshooting](#troubleshooting)
7. [Backup and Restore](#backup-and-restore)

---

## Prerequisites

Before transferring the project, ensure the target device has:

- ✅ **Visual Studio 2022** (or later) with .NET MAUI workload
- ✅ **SQL Server** (one of the following):
  - SQL Server Express / Developer / Standard / Enterprise
  - SQL Server LocalDB (included with Visual Studio)
  - SQL Server on a remote server (accessible via network)

---

## Project Structure

### Database Scripts Location

```
BrightEnroll_DES/
├── Database_Scripts/
│   ├── Initialize_Database.sql          # Complete database setup script
│   ├── Export_Database.sql              # Export schema and structure
│   └── Backup_Database_Data.sql         # Backup existing data
├── appsettings.json                     # Connection string configuration
└── Services/
    └── DBConnections/
        └── DBConnection.cs             # Dynamic connection string handler
```

### Key Files

- **`Database_Scripts/Initialize_Database.sql`**: Run this on a new device to create the database
- **`appsettings.json`**: Contains connection string configuration
- **`Services/DBConnections/DBConnection.cs`**: Handles dynamic connection string resolution

---

## Database Setup Methods

### Method 1: Using SQL Server LocalDB (Recommended for Development)

**LocalDB** is included with Visual Studio and is perfect for development. It automatically creates databases in your user profile.

#### Steps:

1. **Open SQL Server Management Studio (SSMS)** or **Visual Studio SQL Server Object Explorer**

2. **Connect to LocalDB**:
   - Server name: `(localdb)\MSSQLLocalDB`
   - Authentication: Windows Authentication

3. **Run the initialization script**:
   - Open `Database_Scripts/Initialize_Database.sql` in SSMS
   - Execute the script (F5)
   - Verify the database `DB_BrightEnroll_DES` was created

4. **Run the application**:
   - The application will automatically seed the admin user
   - No additional configuration needed!

#### Connection String (Automatic):
```
Data Source=(localdb)\MSSQLLocalDB;Integrated Security=True;Initial Catalog=DB_BrightEnroll_DES;TrustServerCertificate=True;
```

---

### Method 2: Using SQL Server Express/Standard/Enterprise

If you have a full SQL Server installation:

#### Steps:

1. **Open SQL Server Management Studio (SSMS)**

2. **Connect to your SQL Server instance**:
   - Server name: `localhost` or `.\SQLEXPRESS` (for Express)
   - Authentication: Windows Authentication or SQL Server Authentication

3. **Run the initialization script**:
   - Open `Database_Scripts/Initialize_Database.sql` in SSMS
   - Modify the database name if needed (line 7)
   - Execute the script (F5)

4. **Update connection string** (see [Connection String Configuration](#connection-string-configuration))

---

### Method 3: Using Remote SQL Server

If you're using a remote SQL Server:

#### Steps:

1. **Ensure network access** to the SQL Server

2. **Run the initialization script** on the remote server:
   - Connect to the remote server via SSMS
   - Execute `Database_Scripts/Initialize_Database.sql`

3. **Update connection string** with remote server details:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Data Source=YOUR_SERVER_NAME;Initial Catalog=DB_BrightEnroll_DES;Integrated Security=True;TrustServerCertificate=True;"
     }
   }
   ```

---

## Connection String Configuration

The application uses a **dynamic connection string** that checks multiple sources in order of priority:

### Priority Order:

1. **Environment Variable** (Highest Priority)
2. **appsettings.json** file
3. **Default LocalDB** connection string

### Option 1: Using appsettings.json (Recommended)

**File**: `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=(localdb)\\MSSQLLocalDB;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Initial Catalog=DB_BrightEnroll_DES;"
  }
}
```

**For SQL Server Express**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=.\\SQLEXPRESS;Integrated Security=True;Initial Catalog=DB_BrightEnroll_DES;TrustServerCertificate=True;"
  }
}
```

**For Remote SQL Server**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=YOUR_SERVER_NAME;Initial Catalog=DB_BrightEnroll_DES;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

**For SQL Server Authentication**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=YOUR_SERVER_NAME;Initial Catalog=DB_BrightEnroll_DES;User ID=your_username;Password=your_password;TrustServerCertificate=True;"
  }
}
```

### Option 2: Using Environment Variables

Set environment variables on your system:

**Windows (PowerShell)**:
```powershell
$env:ConnectionStrings__DefaultConnection = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=DB_BrightEnroll_DES;Integrated Security=True;TrustServerCertificate=True;"
```

**Windows (Command Prompt)**:
```cmd
set ConnectionStrings__DefaultConnection=Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=DB_BrightEnroll_DES;Integrated Security=True;TrustServerCertificate=True;
```

**Linux/Mac (Bash)**:
```bash
export ConnectionStrings__DefaultConnection="Data Source=localhost;Initial Catalog=DB_BrightEnroll_DES;User ID=sa;Password=your_password;TrustServerCertificate=True;"
```

### Option 3: Individual Environment Variables

You can also set individual database parameters:

```powershell
$env:DB_SERVER = "(localdb)\MSSQLLocalDB"
$env:DB_DATABASE = "DB_BrightEnroll_DES"
$env:DB_INTEGRATED_SECURITY = "True"
```

---

## Step-by-Step Transfer Process

### On Source Device (Exporting)

1. **Backup your database** (if you have existing data):
   ```sql
   -- Run in SSMS
   USE [DB_BrightEnroll_DES];
   GO
   
   -- Generate INSERT statements for all data
   -- (Use Database_Scripts/Backup_Database_Data.sql)
   ```

2. **Copy the project folder** to the target device:
   - Copy the entire `BrightEnroll_DES` folder
   - Include all files, especially:
     - `Database_Scripts/` folder
     - `appsettings.json`
     - All `.cs` files

3. **Note your connection string** (if using custom settings):
   - Check `appsettings.json` for your current connection string
   - Or note your SQL Server instance name

### On Target Device (Importing)

#### Step 1: Install Prerequisites

1. **Install Visual Studio 2022** with .NET MAUI workload
2. **Verify SQL Server** is installed:
   - LocalDB (included with Visual Studio) ✅
   - Or install SQL Server Express/Standard

#### Step 2: Set Up Database

1. **Open SQL Server Management Studio (SSMS)**
   - If SSMS is not installed, download it from Microsoft
   - Or use Visual Studio's SQL Server Object Explorer

2. **Connect to SQL Server**:
   - For LocalDB: `(localdb)\MSSQLLocalDB`
   - For Express: `.\SQLEXPRESS`
   - For Standard: `localhost` or your server name

3. **Run the initialization script**:
   - Open `Database_Scripts/Initialize_Database.sql`
   - Execute the script (F5 or Execute button)
   - Verify success messages

4. **Verify database creation**:
   ```sql
   USE [DB_BrightEnroll_DES];
   GO
   SELECT * FROM [dbo].[tbl_Users];
   GO
   ```

#### Step 3: Configure Connection String

**Option A: Use Default (LocalDB - Easiest)**
- No configuration needed! The app will use LocalDB automatically.

**Option B: Update appsettings.json**
1. Open `appsettings.json` in the project root
2. Update the `DefaultConnection` string to match your SQL Server:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Data Source=YOUR_SERVER;Initial Catalog=DB_BrightEnroll_DES;Integrated Security=True;TrustServerCertificate=True;"
     }
   }
   ```

**Option C: Set Environment Variable**
- Set the environment variable before running the application (see [Connection String Configuration](#connection-string-configuration))

#### Step 4: Restore Data (If Applicable)

If you exported data from the source device:

1. **Open the backup SQL file** (if you created one)
2. **Run the INSERT statements** in SSMS
3. **Verify data**:
   ```sql
   SELECT COUNT(*) FROM [dbo].[tbl_Users];
   ```

#### Step 5: Run the Application

1. **Open the project** in Visual Studio
2. **Restore NuGet packages** (if needed):
   - Right-click solution → Restore NuGet Packages
3. **Build the project**:
   - Build → Build Solution (Ctrl+Shift+B)
4. **Run the application**:
   - Press F5 or click Run
5. **Verify database connection**:
   - The application will automatically seed the admin user
   - Check the database to confirm the admin user was created

#### Step 6: Test Login

1. **Open the login page**
2. **Login with admin credentials**:
   - Email/System ID: `joshvanderson01@gmail.com` or `BDES-0001`
   - Password: `Admin123456`
3. **Verify successful login**

---

## Troubleshooting

### Issue: "Cannot connect to database"

**Solutions**:

1. **Check SQL Server is running**:
   - Open Services (services.msc)
   - Find "SQL Server (MSSQLSERVER)" or "SQL Server (SQLEXPRESS)"
   - Ensure it's running

2. **Verify connection string**:
   - Check `appsettings.json` for correct server name
   - Test connection in SSMS first

3. **Check firewall**:
   - Ensure SQL Server port (1433) is not blocked
   - For LocalDB, this is usually not an issue

4. **Verify database exists**:
   ```sql
   -- Run in SSMS
   SELECT name FROM sys.databases WHERE name = 'DB_BrightEnroll_DES';
   ```

### Issue: "Invalid object name 'tbl_Users'"

**Solutions**:

1. **Run the initialization script**:
   - Execute `Database_Scripts/Initialize_Database.sql`

2. **Check database context**:
   ```sql
   USE [DB_BrightEnroll_DES];
   GO
   SELECT * FROM [dbo].[tbl_Users];
   ```

3. **Verify table exists**:
   ```sql
   SELECT * FROM sys.tables WHERE name = 'tbl_Users';
   ```

### Issue: "Login failed for user"

**Solutions**:

1. **Use Windows Authentication** (if possible):
   ```json
   "Integrated Security=True"
   ```

2. **Check SQL Server Authentication**:
   - Ensure SQL Server Authentication is enabled
   - Verify username and password

3. **Test connection in SSMS first**:
   - If SSMS can connect, the connection string should work

### Issue: "Database 'DB_BrightEnroll_DES' does not exist"

**Solutions**:

1. **Run the initialization script**:
   - Execute `Database_Scripts/Initialize_Database.sql`
   - This will create the database automatically

2. **Or create manually**:
   ```sql
   CREATE DATABASE [DB_BrightEnroll_DES];
   GO
   ```

### Issue: Connection string not being read

**Solutions**:

1. **Check file location**:
   - Ensure `appsettings.json` is in the project root
   - Set "Copy to Output Directory" to "Copy always" or "Copy if newer"

2. **Use environment variable**:
   - Set `ConnectionStrings__DefaultConnection` environment variable

3. **Check build output**:
   - Verify `appsettings.json` is copied to the output folder

---

## Backup and Restore

### Creating a Backup

#### Method 1: SQL Script Backup

1. **Run the export script**:
   - Execute `Database_Scripts/Export_Database.sql`
   - This creates the schema

2. **Export data**:
   - Run `Database_Scripts/Backup_Database_Data.sql`
   - Copy the generated INSERT statements
   - Save to a file (e.g., `backup_data.sql`)

#### Method 2: SQL Server Backup (Recommended for Production)

1. **Right-click database** in SSMS → Tasks → Back Up
2. **Choose backup location**
3. **Click OK** to create `.bak` file
4. **Transfer the `.bak` file** to the target device

### Restoring from Backup

#### Method 1: SQL Script Restore

1. **Run the initialization script** first:
   - Execute `Database_Scripts/Initialize_Database.sql`

2. **Run your backup SQL file**:
   - Execute the INSERT statements from your backup

#### Method 2: SQL Server Restore

1. **Right-click Databases** in SSMS → Restore Database
2. **Select "Device"** and browse to your `.bak` file
3. **Click OK** to restore

---

## Quick Reference

### Default Connection Strings

**LocalDB**:
```
Data Source=(localdb)\MSSQLLocalDB;Integrated Security=True;Initial Catalog=DB_BrightEnroll_DES;TrustServerCertificate=True;
```

**SQL Server Express**:
```
Data Source=.\SQLEXPRESS;Integrated Security=True;Initial Catalog=DB_BrightEnroll_DES;TrustServerCertificate=True;
```

**SQL Server Standard**:
```
Data Source=localhost;Integrated Security=True;Initial Catalog=DB_BrightEnroll_DES;TrustServerCertificate=True;
```

**Remote SQL Server**:
```
Data Source=YOUR_SERVER_NAME;Initial Catalog=DB_BrightEnroll_DES;Integrated Security=True;TrustServerCertificate=True;
```

### Default Admin Credentials

- **Email/System ID**: `joshvanderson01@gmail.com` or `BDES-0001`
- **Password**: `Admin123456`

*(Note: The application will automatically create this user on first run)*

---

## Summary

✅ **Database Setup**: Run `Database_Scripts/Initialize_Database.sql` on the target device

✅ **Connection String**: Configure in `appsettings.json` or use environment variables

✅ **Automatic Seeding**: The application automatically creates the admin user on first run

✅ **Dynamic Configuration**: Connection string is resolved automatically from multiple sources

✅ **Easy Transfer**: Copy the project folder and run the initialization script

---

## Next Steps

After setting up the database:

1. ✅ Run the application
2. ✅ Verify admin user is created automatically
3. ✅ Test login functionality
4. ✅ Start developing!

For questions or issues, refer to the troubleshooting section or check the application logs.

