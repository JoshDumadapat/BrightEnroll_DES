# Cloud Database Sync Setup Guide

## Overview
This guide explains how to set up and use the offline-to-online database synchronization feature in BrightEnroll_DES.

## Prerequisites
- ✅ Cloud database is set up and accessible
- ✅ Cloud database has all 30 tables created
- ✅ Network connectivity to cloud database server
- ✅ Valid credentials for cloud database

## Step 1: Configure Cloud Connection String

1. Open `appsettings.json` in the project root
2. Update the `CloudConnection` connection string with your cloud database details:

```json
{
  "ConnectionStrings": {
    "CloudConnection": "Data Source=YOUR_SERVER;Initial Catalog=DB_BrightEnroll_DES;User ID=YOUR_USERNAME;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True;"
  }
}
```

### Connection String Format for MonsterASP.net:
```
Data Source=YOUR_SERVER.monsterasp.net;Initial Catalog=db34128;User ID=YOUR_USERNAME;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True;
```

**Important:** Replace:
- `YOUR_SERVER` with your actual server name (e.g., `vmi1403053.monsterasp.net`)
- `YOUR_USERNAME` with your database username
- `YOUR_PASSWORD` with your database password
- `db34128` with your actual database name (if different)

## Step 2: Access Cloud Management Page

1. Log in to the system as an Administrator
2. Navigate to **Cloud Management** from the admin menu
3. The page will show:
   - **Connection Status**: Green dot = connected, Red dot = disconnected
   - **Sync Actions**: Four sync buttons

## Step 3: Test Cloud Connection

Before syncing, verify the connection:
- The **Connection Status** indicator should show "Connected" (green dot)
- If it shows "Disconnected", check:
  - Your internet connection
  - Cloud database server is accessible
  - Connection string is correct in `appsettings.json`

## Step 4: Sync Data

### Option 1: Full Sync (Recommended for First Time)
**Button:** "Sync Now" (Blue button)

This performs a **bidirectional sync**:
1. Pushes all local data to cloud
2. Pulls all cloud data to local
3. Updates both databases

**When to use:**
- First time setting up sync
- After being offline for a while
- When you want to ensure both databases are in sync

### Option 2: Push to Cloud
**Button:** "Push to Cloud" (Green button)

Uploads all local changes to the cloud database.

**When to use:**
- After creating/editing records locally
- When you want to backup local data to cloud
- Before going offline

### Option 3: Pull from Cloud
**Button:** "Pull from Cloud" (Purple button)

Downloads all cloud data to local database.

**When to use:**
- After syncing from another device
- When you need latest data from cloud
- After cloud database was updated

### Option 4: Sync Reference Data
**Button:** "Sync Reference Data" (Orange button)

Syncs only reference/master data (Grade Levels, Fees, etc.) from cloud to local.

**When to use:**
- When reference data was updated in cloud
- To refresh master data without syncing all records

## Step 5: Monitor Sync Status

After clicking a sync button:
1. A loading indicator will appear
2. The button will show "Syncing..." with a spinner
3. When complete, you'll see:
   - **Success message** with record counts
   - **Toast notification** confirming sync
   - **Last sync time** updated

## Sync Process Details

### Tables Synced (in order):
1. `tbl_Users` - User accounts
2. `tbl_Guardians` - Student guardians
3. `tbl_GradeLevel` - Grade levels
4. `tbl_Students` - Student records
5. `tbl_StudentRequirements` - Student requirements
6. `tbl_StudentPayments` - Payment records
7. `tbl_Fees` - Fee structures
8. `tbl_Expenses` - Expense records
9. `tbl_employee_address` - Employee addresses
10. `tbl_Sections` - Class sections
11. `tbl_Subjects` - Subject records

### Sync Behavior:
- **New records**: Inserted into target database
- **Existing records**: Updated in target database
- **Primary keys**: Used to identify existing records
- **Foreign keys**: Maintained during sync

## Troubleshooting

### Error: "Cannot connect to cloud database"
**Solution:**
1. Check internet connection
2. Verify connection string in `appsettings.json`
3. Test connection using SQL Server Management Studio
4. Check firewall settings
5. Verify database server is running

### Error: "CloudConnection string not found"
**Solution:**
1. Ensure `CloudConnection` exists in `appsettings.json`
2. Check for typos in connection string name
3. Restart the application

### Sync completes but no records synced
**Possible causes:**
1. Both databases already have the same data
2. Table structure mismatch between local and cloud
3. Primary key conflicts

**Solution:**
1. Check sync status message for details
2. Verify table structures match
3. Check for data in both databases

### Sync is slow
**Possible causes:**
1. Large amount of data
2. Slow network connection
3. Database server performance

**Solution:**
1. Be patient - large syncs can take time
2. Check network speed
3. Consider syncing during off-peak hours

## Best Practices

1. **Regular Syncs**: Sync regularly to keep data up-to-date
2. **Before Major Changes**: Always sync before making major data changes
3. **After Offline Work**: Sync immediately after working offline
4. **Backup First**: Consider backing up before first sync
5. **Monitor Errors**: Check sync status messages for any errors

## Security Notes

- ⚠️ **Never commit** `appsettings.json` with real credentials to version control
- ⚠️ Use secure passwords for database access
- ⚠️ Enable encryption in connection strings
- ⚠️ Regularly update database passwords

## Next Steps

After successful sync:
1. Verify data in both databases
2. Test creating new records locally
3. Sync again to verify changes propagate
4. Set up regular sync schedule (manual for now)

## Support

If you encounter issues:
1. Check the sync status message for error details
2. Review application logs
3. Verify database connectivity
4. Check table structures match between local and cloud

---

**Last Updated:** 2025-12-03
**Version:** 1.0

