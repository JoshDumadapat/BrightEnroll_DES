# Database Migrations and Backups Guide

This guide covers how to manage database migrations and create backups for the BrightEnroll_DES application.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Entity Framework Core Migrations](#entity-framework-core-migrations)
- [Creating Migrations](#creating-migrations)
- [Applying Migrations](#applying-migrations)
- [Database Backups](#database-backups)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

Before working with migrations, ensure you have:

1. **.NET 9.0 SDK** installed
2. **Entity Framework Core Tools** installed:
   ```bash
   dotnet tool install --global dotnet-ef
   ```
   Or update if already installed:
   ```bash
   dotnet tool update --global dotnet-ef
   ```
3. **SQL Server** (LocalDB, Express, or full SQL Server)
4. **sqlcmd** utility (for backups)
   - Windows: Included with SQL Server Management Studio or SQL Server tools
   - Linux/Mac: Install `mssql-tools` package

---

## Entity Framework Core Migrations

This project uses Entity Framework Core for database migrations. The `AppDbContextFactory` class allows EF Core tools to create migrations without running the application.

### Configuration

The migration tooling is configured in:
- `Data/AppDbContextFactory.cs` - Design-time factory for migrations
- `Data/AppDbContext.cs` - Database context with all entity configurations
- `appsettings.json` - Connection string configuration

### Connection String

The migration tools will use the connection string from `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=(localdb)\\MSSQLLocalDB;..."
  }
}
```

You can also override it using environment variables:
```bash
export ConnectionStrings__DefaultConnection="your-connection-string"
```

---

## Creating Migrations

### Create a New Migration

After making changes to your entity models in `Data/Models/`, create a migration:

```bash
dotnet ef migrations add <MigrationName> --project BrightEnroll_DES.csproj
```

**Example:**
```bash
dotnet ef migrations add AddStudentEmailColumn --project BrightEnroll_DES.csproj
```

This will:
- Create a new migration file in the `Migrations/` folder
- Generate the SQL scripts to apply your model changes
- Create a snapshot of your current model state

### Migration File Structure

Migrations are stored in the `Migrations/` folder with the following structure:
```
Migrations/
├── 20240101120000_InitialCreate.cs
├── 20240101120000_InitialCreate.Designer.cs
├── 20240102130000_AddStudentEmailColumn.cs
├── 20240102130000_AddStudentEmailColumn.Designer.cs
└── AppDbContextModelSnapshot.cs
```

### Best Practices

1. **Use descriptive names**: `AddStudentEmailColumn` instead of `Migration1`
2. **One logical change per migration**: Keep migrations focused and atomic
3. **Review generated SQL**: Check the migration file before applying
4. **Test locally first**: Always test migrations on a development database

---

## Applying Migrations

### Apply Pending Migrations

To apply all pending migrations to your database:

```bash
dotnet ef database update --project BrightEnroll_DES.csproj
```

This will:
- Check which migrations have been applied
- Apply any pending migrations in order
- Update the `__EFMigrationsHistory` table

### Apply to a Specific Migration

To apply migrations up to a specific point:

```bash
dotnet ef database update <MigrationName> --project BrightEnroll_DES.csproj
```

**Example:**
```bash
dotnet ef database update AddStudentEmailColumn --project BrightEnroll_DES.csproj
```

### Rollback a Migration

To rollback to a previous migration:

```bash
dotnet ef database update <PreviousMigrationName> --project BrightEnroll_DES.csproj
```

**Example:**
```bash
dotnet ef database update InitialCreate --project BrightEnroll_DES.csproj
```

### Generate SQL Script

To generate a SQL script without applying it:

```bash
dotnet ef migrations script --project BrightEnroll_DES.csproj --output migration.sql
```

Generate script from a specific migration:
```bash
dotnet ef migrations script <FromMigration> <ToMigration> --project BrightEnroll_DES.csproj --output migration.sql
```

---

## Database Backups

### Creating a Backup

Use the provided backup script to create database backups with metadata:

```bash
./scripts/backup_db.sh
```

Or on Windows (using Git Bash or WSL):
```bash
bash scripts/backup_db.sh
```

### Backup Script Features

The backup script (`scripts/backup_db.sh`) creates:
1. **Database backup file** (`.bak` format)
2. **Metadata file** (`.metadata.txt`) with:
   - Timestamp
   - Git SHA
   - Git branch
   - Database and server info
   - Backup size
3. **Log entry** in `backups/backup_log.txt`

### Configuration

You can configure the backup script using environment variables:

```bash
# Database connection
export DB_SERVER="localhost"
export DB_NAME="DB_BrightEnroll_DES"
export USE_INTEGRATED_SECURITY="true"

# Or use SQL authentication
export DB_USER="your_username"
export DB_PASSWORD="your_password"
export USE_INTEGRATED_SECURITY="false"

# Backup location
export BACKUP_DIR="./backups"

# Run backup
bash scripts/backup_db.sh
```

### Backup Output

The script creates files in the `backups/` directory:

```
backups/
├── DB_BrightEnroll_DES_20240101_120000.bak
├── DB_BrightEnroll_DES_20240101_120000.metadata.txt
└── backup_log.txt
```

### Metadata File Example

```
Database Backup Metadata
========================
Timestamp: 2024-01-01 12:00:00 UTC
Local Time: 2024-01-01 12:00:00 PST
Database: DB_BrightEnroll_DES
Server: localhost
Backup File: DB_BrightEnroll_DES_20240101_120000.bak
Backup Size: 15M
Git SHA: a1b2c3d
Git Branch: feature/add-migration-tooling
Created By: developer
Host: dev-machine
```

### Restoring from Backup

To restore a database backup:

```bash
# Using sqlcmd
sqlcmd -S localhost -E -Q "RESTORE DATABASE [DB_BrightEnroll_DES] FROM DISK = 'backups/DB_BrightEnroll_DES_20240101_120000.bak' WITH REPLACE;"
```

Or using SQL Server Management Studio:
1. Right-click on "Databases" → "Restore Database..."
2. Select "Device" and browse to your `.bak` file
3. Click "OK" to restore

---

## Workflow Examples

### Before Making Schema Changes

1. **Create a backup:**
   ```bash
   bash scripts/backup_db.sh
   ```

2. **Make your model changes** in `Data/Models/`

3. **Create a migration:**
   ```bash
   dotnet ef migrations add YourMigrationName --project BrightEnroll_DES.csproj
   ```

4. **Review the migration** in `Migrations/`

5. **Test the migration:**
   ```bash
   dotnet ef database update --project BrightEnroll_DES.csproj
   ```

### After Pulling Changes

1. **Check for pending migrations:**
   ```bash
   dotnet ef migrations list --project BrightEnroll_DES.csproj
   ```

2. **Create a backup** (safety first!):
   ```bash
   bash scripts/backup_db.sh
   ```

3. **Apply migrations:**
   ```bash
   dotnet ef database update --project BrightEnroll_DES.csproj
   ```

---

## Troubleshooting

### Migration Issues

**Problem:** `Unable to create an object of type 'AppDbContext'`

**Solution:** Ensure `AppDbContextFactory.cs` exists and is properly configured.

**Problem:** `No connection string named 'DefaultConnection' was found`

**Solution:** Check `appsettings.json` or set the connection string via environment variable:
```bash
export ConnectionStrings__DefaultConnection="your-connection-string"
```

**Problem:** Migration conflicts or out-of-sync state

**Solution:**
1. Create a backup
2. Check migration history: `dotnet ef migrations list`
3. If needed, remove problematic migrations and recreate them

### Backup Issues

**Problem:** `sqlcmd: command not found`

**Solution:** 
- Windows: Install SQL Server Management Studio or SQL Server tools
- Linux: `sudo apt-get install mssql-tools` (Ubuntu/Debian)
- Mac: Install via Homebrew or download from Microsoft

**Problem:** Permission denied when creating backup

**Solution:** Ensure the database user has BACKUP DATABASE permission:
```sql
GRANT BACKUP DATABASE TO [your_user];
```

**Problem:** Backup file is empty or corrupted

**Solution:**
- Check disk space
- Verify SQL Server service is running
- Check SQL Server error logs

### Connection Issues

**Problem:** Cannot connect to database

**Solution:**
1. Verify SQL Server is running
2. Check connection string in `appsettings.json`
3. Test connection: `sqlcmd -S localhost -E -Q "SELECT 1"`

---

## Additional Resources

- [EF Core Migrations Documentation](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [SQL Server Backup Documentation](https://learn.microsoft.com/en-us/sql/relational-databases/backup-restore/back-up-and-restore-of-sql-server-databases)
- [sqlcmd Utility Documentation](https://learn.microsoft.com/en-us/sql/tools/sqlcmd-utility)

---

## Notes

- **Always create a backup before applying migrations** to production databases
- **Test migrations on a development database first**
- **Keep migration files in version control** - they are part of your codebase
- **Never edit applied migrations** - create new migrations to fix issues
- **For cloud deployments**, ensure connection strings use environment variables or secure configuration

