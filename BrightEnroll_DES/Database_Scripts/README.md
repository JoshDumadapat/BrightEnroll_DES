# Database Scripts Guide

## Which Script Should I Use?

### ✅ **For New Device Setup (USE THIS ONE)**
**`Initialize_Database.sql`** - This is the **ONLY script you need** for setting up on a new device.
- Creates the database
- Creates all tables
- Creates indexes
- Ready to use!

### 📦 **Optional Scripts (For Advanced Users)**

1. **`Create_tbl_Users.sql`** - Legacy script (only creates table, assumes DB exists)
   - ⚠️ **Don't use this** - Use `Initialize_Database.sql` instead

2. **`Export_Database.sql`** - For exporting database schema
   - Use when you want to document your database structure

3. **`Backup_Database_Data.sql`** - For backing up existing data
   - Use when you want to transfer data from one device to another

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

