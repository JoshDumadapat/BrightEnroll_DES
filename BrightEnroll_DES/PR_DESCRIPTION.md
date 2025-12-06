# PR: Add Database Migration Tooling and Backup Scripts

## Summary

This PR adds database migration tooling configuration and backup scripts to enable proper database schema management and backup procedures for the BrightEnroll_DES project.

## Changes Made

### 1. Entity Framework Core Migration Configuration

**File:** `Data/AppDbContextFactory.cs` (NEW)
- Created design-time factory for EF Core migrations
- Allows EF Core tools to create migrations without running the application
- Reads connection string from `appsettings.json` or uses default LocalDB connection

### 2. Database Backup Script

**File:** `scripts/backup_db.sh` (NEW)
- Bash script for creating SQL Server database backups
- Includes metadata tracking (timestamp, git SHA, git branch)
- Creates backup files in `.bak` format
- Generates metadata files with backup information
- Maintains backup log for tracking backup history
- Supports both Windows Authentication and SQL Authentication
- Configurable via environment variables

### 3. Migration Documentation

**File:** `README/dev-migrations.md` (NEW)
- Comprehensive guide for database migrations
- Instructions for creating and applying migrations
- Database backup procedures
- Troubleshooting guide
- Workflow examples

### 4. Updated .gitignore

**File:** `.gitignore` (MODIFIED)
- Added `backups/` directory to ignore list
- Added `*.bak` pattern to ignore backup files

## Files Changed

```
Data/
  └── AppDbContextFactory.cs          (NEW)

scripts/
  └── backup_db.sh                    (NEW)

README/
  └── dev-migrations.md               (NEW)

.gitignore                            (MODIFIED)
```

## Prerequisites

Before using the migration tools, install EF Core tools:

```bash
dotnet tool install --global dotnet-ef
```

Or update if already installed:

```bash
dotnet tool update --global dotnet-ef
```

## How to Use

### Creating Migrations

1. Make changes to entity models in `Data/Models/`
2. Create a migration:
   ```bash
   dotnet ef migrations add <MigrationName> --project BrightEnroll_DES.csproj
   ```
3. Apply the migration:
   ```bash
   dotnet ef database update --project BrightEnroll_DES.csproj
   ```

### Creating Backups

Run the backup script:

```bash
bash scripts/backup_db.sh
```

Or on Windows (using Git Bash or WSL):
```bash
bash scripts/backup_db.sh
```

Backups are saved to the `backups/` directory with metadata files.

## Testing

### Manual Test Checklist

- [ ] Install EF Core tools: `dotnet tool install --global dotnet-ef`
- [ ] Verify migration setup: `dotnet ef migrations list --project BrightEnroll_DES.csproj`
- [ ] Create a test migration: `dotnet ef migrations add TestMigration --project BrightEnroll_DES.csproj`
- [ ] Apply migration: `dotnet ef database update --project BrightEnroll_DES.csproj`
- [ ] Run backup script: `bash scripts/backup_db.sh`
- [ ] Verify backup file exists in `backups/` directory
- [ ] Verify metadata file exists with correct information
- [ ] Check backup log entry in `backups/backup_log.txt`

### Expected Output

**Migration Command:**
```
Build started...
Build succeeded.
Done. To undo this action, use 'ef migrations remove'
```

**Backup Script:**
```
[INFO] Starting database backup...
[INFO] Database: DB_BrightEnroll_DES
[INFO] Server: localhost
[INFO] Backup file: ./backups/DB_BrightEnroll_DES_20241206_092000.bak
[INFO] Creating database backup...
[INFO] Backup created successfully: ./backups/DB_BrightEnroll_DES_20241206_092000.bak
[INFO] Metadata saved to: ./backups/DB_BrightEnroll_DES_20241206_092000.metadata.txt
[INFO] Log entry added to: ./backups/backup_log.txt
[INFO] Backup completed successfully!
```

## Notes

- This PR only adds tooling and configuration; no schema changes are included
- The backup script requires `sqlcmd` utility (included with SQL Server tools)
- Migrations will be created in a `Migrations/` folder (created automatically on first migration)
- Backup files are excluded from version control via `.gitignore`

## Related Documentation

- See `README/dev-migrations.md` for detailed usage instructions
- See `Database_Scripts/README.md` for existing database scripts

