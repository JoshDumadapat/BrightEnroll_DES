# How to Add New Tables to Automatic Database Setup

This guide explains how to add new database tables to the automatic initialization system so they're created automatically when the application runs.

---

## Quick Steps

1. **Create your table in SQL Server Management Studio (SSMS)** on your local device
2. **Copy the CREATE TABLE script** from SSMS
3. **Add the table definition** to `Services/DBConnections/TableDefinitions.cs`
4. **Register it** in the `GetAllTableDefinitions()` method
5. **Done!** The table will be created automatically on other devices

---

## Step-by-Step Guide

### Step 1: Create Table in SSMS

1. Open **SQL Server Management Studio (SSMS)**
2. Connect to your database
3. Create your new table (e.g., `tbl_Students`):

```sql
CREATE TABLE [dbo].[tbl_Students](
    [student_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [first_name] VARCHAR(50) NOT NULL,
    [last_name] VARCHAR(50) NOT NULL,
    [email] VARCHAR(150) NOT NULL UNIQUE,
    [created_date] DATETIME NOT NULL DEFAULT GETDATE()
);
```

4. **Right-click the table** → **Script Table as** → **CREATE To** → **New Query Editor Window**
5. **Copy the generated script**

### Step 2: Add Table Definition to Code

1. Open `Services/DBConnections/TableDefinitions.cs`

2. **Add a new method** for your table (follow the pattern):

```csharp
public static TableDefinition GetStudentsTableDefinition()
{
    return new TableDefinition
    {
        TableName = "tbl_Students",
        SchemaName = "dbo",
        CreateTableScript = @"
            CREATE TABLE [dbo].[tbl_Students](
                [student_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [first_name] VARCHAR(50) NOT NULL,
                [last_name] VARCHAR(50) NOT NULL,
                [email] VARCHAR(150) NOT NULL UNIQUE,
                [created_date] DATETIME NOT NULL DEFAULT GETDATE()
            )",
        CreateIndexesScripts = new List<string>
        {
            @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Students_Email' AND object_id = OBJECT_ID('dbo.tbl_Students'))
                CREATE INDEX IX_tbl_Students_Email ON [dbo].[tbl_Students]([email])"
        }
    };
}
```

3. **Register it** in the `GetAllTableDefinitions()` method:

```csharp
public static List<TableDefinition> GetAllTableDefinitions()
{
    return new List<TableDefinition>
    {
        GetUsersTableDefinition(),
        GetStudentsTableDefinition(), // ← Add your new table here
        // Add more tables here...
    };
}
```

### Step 3: Test It

1. **Delete the table** from your local database (to test auto-creation):
   ```sql
   DROP TABLE [dbo].[tbl_Students];
   ```

2. **Run the application** - the table should be created automatically!

3. **Verify** in SSMS that the table was created

---

## Complete Example

Here's a complete example of adding a `tbl_Students` table:

### 1. Table Definition Method

```csharp
public static TableDefinition GetStudentsTableDefinition()
{
    return new TableDefinition
    {
        TableName = "tbl_Students",
        SchemaName = "dbo",
        CreateTableScript = @"
            CREATE TABLE [dbo].[tbl_Students](
                [student_ID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [system_ID] VARCHAR(50) NOT NULL UNIQUE,
                [first_name] VARCHAR(50) NOT NULL,
                [mid_name] VARCHAR(50) NULL,
                [last_name] VARCHAR(50) NOT NULL,
                [email] VARCHAR(150) NOT NULL UNIQUE,
                [contact_num] VARCHAR(20) NULL,
                [birthdate] DATE NOT NULL,
                [gender] VARCHAR(20) NOT NULL,
                [enrollment_date] DATETIME NOT NULL DEFAULT GETDATE(),
                [status] VARCHAR(20) NOT NULL DEFAULT 'Active'
            )",
        CreateIndexesScripts = new List<string>
        {
            @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Students_Email' AND object_id = OBJECT_ID('dbo.tbl_Students'))
                CREATE INDEX IX_tbl_Students_Email ON [dbo].[tbl_Students]([email])",
            @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Students_SystemID' AND object_id = OBJECT_ID('dbo.tbl_Students'))
                CREATE INDEX IX_tbl_Students_SystemID ON [dbo].[tbl_Students]([system_ID])",
            @"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tbl_Students_Status' AND object_id = OBJECT_ID('dbo.tbl_Students'))
                CREATE INDEX IX_tbl_Students_Status ON [dbo].[tbl_Students]([status])"
        }
    };
}
```

### 2. Register in GetAllTableDefinitions()

```csharp
public static List<TableDefinition> GetAllTableDefinitions()
{
    return new List<TableDefinition>
    {
        GetUsersTableDefinition(),
        GetStudentsTableDefinition(), // ← Your new table
    };
}
```

---

## Tips and Best Practices

### ✅ DO:

1. **Use the exact table name** from your SSMS script
2. **Include all columns** exactly as they appear in SSMS
3. **Add indexes** for frequently queried columns (email, IDs, status, etc.)
4. **Test locally first** by dropping and recreating the table
5. **Use proper data types** matching your SSMS definition

### ❌ DON'T:

1. **Don't modify existing table definitions** without testing
2. **Don't forget to register** the new table in `GetAllTableDefinitions()`
3. **Don't use hardcoded values** in CREATE TABLE (use DEFAULT constraints instead)
4. **Don't forget indexes** - they improve query performance

---

## Adding Foreign Keys

If your table has foreign keys, add them in the `CreateIndexesScripts` section:

```csharp
CreateIndexesScripts = new List<string>
{
    // Indexes first
    @"CREATE INDEX IX_tbl_Students_Email ON [dbo].[tbl_Students]([email])",
    
    // Then foreign keys
    @"
        IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_tbl_Students_tbl_Users')
        ALTER TABLE [dbo].[tbl_Students]
        ADD CONSTRAINT FK_tbl_Students_tbl_Users 
        FOREIGN KEY ([user_ID]) REFERENCES [dbo].[tbl_Users]([user_ID])"
}
```

**Note**: Make sure the referenced table is created first (order matters in `GetAllTableDefinitions()`).

---

## Updating Existing Tables

If you need to **modify an existing table** (add columns, change types, etc.):

### Option 1: Manual Migration (Recommended for Production)

1. Create a migration script in `Database_Scripts/`:
   ```sql
   -- Database_Scripts/Migration_AddColumn_20240101.sql
   ALTER TABLE [dbo].[tbl_Users]
   ADD [new_column] VARCHAR(50) NULL;
   ```

2. Run it manually on each device

### Option 2: Update Table Definition (For New Deployments)

1. Update the `CreateTableScript` in `TableDefinitions.cs`
2. **Note**: This only affects **new** installations
3. Existing databases won't be modified automatically

---

## Verifying Your Changes

After adding a new table definition:

1. **Build the project** (Ctrl+Shift+B)
2. **Delete the table** from your local database (to test)
3. **Run the application**
4. **Check SSMS** - the table should be created automatically
5. **Verify indexes** were created correctly

---

## Troubleshooting

### Issue: Table not being created

**Check**:
- ✅ Is the table registered in `GetAllTableDefinitions()`?
- ✅ Is the SQL syntax correct in `CreateTableScript`?
- ✅ Are there any errors in the debug output?

**Solution**: Check the debug console for error messages

### Issue: Index creation fails

**Check**:
- ✅ Does the index name already exist?
- ✅ Are the column names correct?

**Solution**: The code uses `IF NOT EXISTS` so it should skip if the index exists

### Issue: Foreign key constraint fails

**Check**:
- ✅ Is the referenced table created first?
- ✅ Are the column names and types matching?

**Solution**: Reorder tables in `GetAllTableDefinitions()` so referenced tables come first

---

## Summary

1. ✅ Create table in SSMS
2. ✅ Copy CREATE TABLE script
3. ✅ Add `GetYourTableDefinition()` method to `TableDefinitions.cs`
4. ✅ Register it in `GetAllTableDefinitions()`
5. ✅ Test by deleting and running the app
6. ✅ Done! Table will be created automatically on all devices

---

## File Location

- **Table Definitions**: `Services/DBConnections/TableDefinitions.cs`
- **Database Initializer**: `Services/DBConnections/DatabaseInitializer.cs`
- **SQL Scripts** (backup): `Database_Scripts/`

---

## Quick Reference

```csharp
// 1. Add method
public static TableDefinition GetYourTableDefinition()
{
    return new TableDefinition
    {
        TableName = "tbl_YourTable",
        SchemaName = "dbo",
        CreateTableScript = @"CREATE TABLE ...",
        CreateIndexesScripts = new List<string> { "CREATE INDEX ..." }
    };
}

// 2. Register it
GetAllTableDefinitions() => new List<TableDefinition>
{
    GetUsersTableDefinition(),
    GetYourTableDefinition(), // ← Add here
};
```

That's it! Your table will be created automatically on any device that runs the application.

