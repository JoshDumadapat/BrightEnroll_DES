# Database Auto-Setup Guide

## ✅ Good News: Database is Now Automatically Created!

The application **automatically creates the database and tables** when you run it for the first time. You **don't need to run SQL scripts manually** anymore!

---

## How It Works

### Automatic Setup Process

When you run the application:

1. **Application starts** → `MauiProgram.cs` runs
2. **Database Initializer checks** → Does `DB_BrightEnroll_DES` database exist?
   - ❌ **No** → Creates the database automatically
   - ✅ **Yes** → Skips creation
3. **Table Initializer checks** → Does `tbl_Users` table exist?
   - ❌ **No** → Creates the table automatically
   - ✅ **Yes** → Skips creation
4. **Admin User Seeder runs** → Creates admin user if it doesn't exist
5. **Application is ready!** ✅

---

## What You Need to Do

### Option 1: Just Run the Application (Easiest) ✅

1. **Make sure SQL Server is installed**:
   - LocalDB (included with Visual Studio) ✅
   - Or SQL Server Express/Standard

2. **Run the application** (F5)
   - Database will be created automatically
   - Tables will be created automatically
   - Admin user will be seeded automatically

3. **That's it!** No SQL scripts needed!

### Option 2: Manual Setup (If Auto-Setup Fails)

If automatic setup doesn't work (rare), you can still use the SQL script:

1. Open **SQL Server Management Studio (SSMS)**
2. Connect to your SQL Server
3. Open `Database_Scripts/Initialize_Database.sql`
4. Execute the script (F5)

---

## Database Scripts Explained

### Why Are There 4 Scripts?

1. **`Initialize_Database.sql`** ✅
   - **Purpose**: Complete database setup
   - **When to use**: Manual setup (if auto-setup fails)
   - **Status**: You probably won't need this anymore!

2. **`Create_tbl_Users.sql`** ⚠️
   - **Purpose**: Legacy script (only creates table)
   - **When to use**: Never (use Initialize_Database.sql instead)
   - **Status**: Can be ignored

3. **`Export_Database.sql`** 📦
   - **Purpose**: Export database schema for documentation
   - **When to use**: When you want to document your database
   - **Status**: Optional utility

4. **`Backup_Database_Data.sql`** 📦
   - **Purpose**: Generate INSERT statements for backing up data
   - **When to use**: When transferring data between devices
   - **Status**: Optional utility

**Summary**: You only need `Initialize_Database.sql` if auto-setup fails. The others are optional utilities.

---

## Connection String Configuration

The application uses a **dynamic connection string** that works automatically:

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

---

## Troubleshooting

### Issue: "Cannot create database - insufficient permissions"

**Solution**: 
- Make sure you're running SQL Server with appropriate permissions
- For LocalDB, this usually works automatically
- For SQL Server Express/Standard, ensure your Windows user has database creation permissions

### Issue: "Database already exists" error

**Solution**: 
- This is normal! The application checks if the database exists before creating it
- If the database already exists, it will just use it
- No action needed

### Issue: Auto-setup not working

**Solution**:
1. Check SQL Server is running
2. Verify connection string in `appsettings.json`
3. Manually run `Database_Scripts/Initialize_Database.sql` as a fallback

---

## Summary

✅ **Database is automatically created** when you run the application  
✅ **Tables are automatically created** if they don't exist  
✅ **Admin user is automatically seeded** on first run  
✅ **No SQL scripts needed** for normal operation  
✅ **SQL scripts are available** as a backup option  

**Just run the application and it works!** 🎉

