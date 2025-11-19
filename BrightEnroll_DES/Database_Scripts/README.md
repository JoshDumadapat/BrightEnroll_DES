# Database Scripts Guide

## Which Script Should I Use?

### ✅ **For New Device Setup (USE THIS ONE)**
**`Initialize_Database.sql`** - Complete database setup script (optimized)
- Creates the database
- Creates all tables (Users, Students, Guardians, Requirements, Employee tables)
- Creates all indexes
- Adds status column if missing
- Ready to use!

**Note:** The application auto-creates the database on startup. This script is for manual setup only.

### 📦 **Optional Scripts (For Advanced Users)**

1. **`Export_Database.sql`** - For exporting database schema
   - Use when you want to document your database structure

2. **`Backup_Database_Data.sql`** - For backing up existing data
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

