# Database Connection Requirements for Employee Registration

## ✅ Quick Checklist

### 1. **SQL Server Installation** (REQUIRED)
- [ ] SQL Server is installed on your machine
- [ ] SQL Server service is **RUNNING**
- [ ] You can connect to SQL Server using SQL Server Management Studio (SSMS)

**Options:**
- **SQL Server LocalDB** (Recommended - comes with Visual Studio)
  - Server name: `(localdb)\MSSQLLocalDB`
- **SQL Server Express/Standard/Enterprise**
  - Server name: `localhost` or `.\SQLEXPRESS`
- **Remote SQL Server**
  - Server name: Your server's network name or IP address

### 2. **Database Setup** (AUTO-CREATED or MANUAL)

#### Option A: Automatic Setup (Easiest) ✅
- The application **automatically creates** the database and `tbl_Users` table on first run
- **No manual setup needed!**
- Just make sure SQL Server is running

#### Option B: Manual Setup (If auto-setup fails)
1. Open **SQL Server Management Studio (SSMS)**
2. Connect to your SQL Server
3. Run the script: `Database_Scripts/Initialize_Database.sql`
4. This creates:
   - Database: `DB_BrightEnroll_DES`
   - Table: `tbl_Users`

### 3. **Connection String Configuration** (REQUIRED)

#### Update `appsettings.json`:

**For LocalDB (Default - Works out of the box):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=(localdb)\\MSSQLLocalDB;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Initial Catalog=DB_BrightEnroll_DES;"
  }
}
```

**For SQL Server Express:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=.\\SQLEXPRESS;Integrated Security=True;Initial Catalog=DB_BrightEnroll_DES;TrustServerCertificate=True;"
  }
}
```

**For Remote SQL Server (Windows Authentication):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=YOUR_SERVER_NAME;Initial Catalog=DB_BrightEnroll_DES;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

**For SQL Server Authentication:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=YOUR_SERVER_NAME;Initial Catalog=DB_BrightEnroll_DES;User ID=your_username;Password=your_password;TrustServerCertificate=True;"
  }
}
```

### 4. **Database Table Structure** (AUTO-CREATED)

The `tbl_Users` table must have these columns:

| Column Name | Data Type | Required | Description |
|------------|-----------|----------|-------------|
| user_ID | INT | ✅ Yes | Primary Key, Auto-increment |
| system_ID | VARCHAR(50) | ✅ Yes | Unique identifier (e.g., BDES-0001) |
| first_name | VARCHAR(50) | ✅ Yes | First name |
| mid_name | VARCHAR(50) | ❌ No | Middle name |
| last_name | VARCHAR(50) | ✅ Yes | Last name |
| suffix | VARCHAR(10) | ❌ No | Suffix (Jr., Sr., etc.) |
| birthdate | DATE | ✅ Yes | Birth date |
| age | TINYINT | ✅ Yes | Age |
| gender | VARCHAR(20) | ✅ Yes | Gender |
| contact_num | VARCHAR(20) | ✅ Yes | Contact number |
| user_role | VARCHAR(50) | ✅ Yes | Role (Teacher, Registrar, etc.) |
| email | VARCHAR(150) | ✅ Yes | Email (must be unique) |
| password | VARCHAR(255) | ✅ Yes | Hashed password (BCrypt) |
| date_hired | DATETIME | ✅ Yes | Date hired |

### 5. **Permissions** (REQUIRED)

Your Windows user or SQL Server user must have:
- ✅ **CREATE DATABASE** permission (for auto-setup)
- ✅ **CREATE TABLE** permission
- ✅ **INSERT** permission on `tbl_Users`
- ✅ **SELECT** permission on `tbl_Users`
- ✅ **UPDATE** permission on `tbl_Users`
- ✅ **DELETE** permission on `tbl_Users`

**Note:** If using Windows Authentication, your Windows user usually has these permissions automatically.

---

## 🚀 Quick Start Guide

### Step 1: Verify SQL Server is Running
1. Open **SQL Server Configuration Manager** or **Services**
2. Find **SQL Server (MSSQLSERVER)** or **SQL Server (SQLEXPRESS)**
3. Make sure it's **Running**

### Step 2: Update Connection String
1. Open `appsettings.json` in your project
2. Update the `DefaultConnection` with your SQL Server details
3. Save the file

### Step 3: Run the Application
1. Build and run the application (F5)
2. The application will:
   - ✅ Automatically create the database if it doesn't exist
   - ✅ Automatically create the `tbl_Users` table if it doesn't exist
   - ✅ Seed the initial admin user

### Step 4: Test Employee Registration
1. Navigate to **Human Resource** → **Add Employee**
2. Fill out the form
3. Click **Add Employee**
4. Check the toast notification for success/error
5. Verify the employee appears in the Human Resource list

---

## 🔍 Verification Steps

### Check if Database Exists:
```sql
-- Run this in SSMS
SELECT name FROM sys.databases WHERE name = 'DB_BrightEnroll_DES';
```

### Check if Table Exists:
```sql
-- Run this in SSMS
USE DB_BrightEnroll_DES;
SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tbl_Users';
```

### Check if Employees are Being Saved:
```sql
-- Run this in SSMS
USE DB_BrightEnroll_DES;
SELECT * FROM tbl_Users;
```

---

## ⚠️ Common Issues & Solutions

### Issue 1: "Cannot open database"
**Solution:**
- Verify SQL Server is running
- Check connection string in `appsettings.json`
- Ensure database name is correct

### Issue 2: "Table 'tbl_Users' doesn't exist"
**Solution:**
- Restart the application (it will auto-create the table)
- Or manually run `Database_Scripts/Initialize_Database.sql`

### Issue 3: "Login failed for user"
**Solution:**
- For Windows Authentication: Ensure your Windows user has SQL Server access
- For SQL Authentication: Verify username and password in connection string

### Issue 4: "Insufficient permissions"
**Solution:**
- Contact your database administrator to grant permissions
- Or use a SQL Server account with proper permissions

### Issue 5: "Connection timeout"
**Solution:**
- Check if SQL Server is running
- Verify network connectivity (for remote servers)
- Check firewall settings

---

## 📝 Current Connection String (Check Your appsettings.json)

Your current `appsettings.json` shows:
- **Database:** `DB_BrightEnroll_DES_Test`
- **Server:** `(localdb)\MSSQLLocalDB`

**Make sure:**
1. The database name matches what you see in SQL Server Management Studio
2. The server name matches your SQL Server instance
3. SQL Server is running

---

## ✅ Final Checklist Before Testing

- [ ] SQL Server is installed and running
- [ ] Connection string in `appsettings.json` is correct
- [ ] Database `DB_BrightEnroll_DES` (or your database name) exists
- [ ] Table `tbl_Users` exists in the database
- [ ] You have INSERT permissions on `tbl_Users`
- [ ] Application builds without errors
- [ ] You can see debug output in Visual Studio Output window

---

## 🎯 Quick Test

After setup, try adding an employee:
1. Fill out the form completely
2. Click "Add Employee"
3. Check the **Debug Output** window in Visual Studio for:
   - `"InsertAsync: Inserted employee with result: 1"` ✅
   - `"GetAllAsync: Found X employees in database"` ✅
4. Check SQL Server Management Studio:
   ```sql
   SELECT * FROM tbl_Users;
   ```
   You should see your new employee!

---

## 📞 Need Help?

If employee registration still doesn't work:
1. Check the **Debug Output** window for error messages
2. Check the **toast notification** for user-friendly error messages
3. Verify the connection string matches your SQL Server setup
4. Ensure `tbl_Users` table exists and has the correct structure


