# Cloud Connection Verification Guide

This guide explains how to verify if your system can connect to the cloud database.

## Methods to Test Cloud Connection

### Method 1: Using the Cloud Management Page (Recommended)

1. **Start your application**
2. **Navigate to Cloud Management page** (`/cloud-management`)
3. **Click the "Test Connection" button** in the Connection Status card
4. **View the results:**
   - ✅ **Green indicator** = Cloud database is connected
   - ❌ **Red indicator** = Cloud database connection failed
   - **Error details** will be displayed below if connection fails

The page shows two connection statuses:
- **Internet**: Shows if you have internet connectivity
- **Cloud Database**: Shows if you can connect to the cloud SQL Server database

### Method 2: Using the Standalone Test Program

1. **Open a terminal/command prompt** in the project directory
2. **Run the test program:**
   ```bash
   dotnet run --project BrightEnroll_DES.csproj
   ```
   Or if you have a separate entry point:
   ```bash
   dotnet run TestCloudConnection.cs
   ```

3. **Review the output:**
   - ✅ **SUCCESS** = Connection is working
   - ❌ **FAILED** = Connection failed with detailed error information

### Method 3: Using SQL Server Management Studio (SSMS)

1. **Open SQL Server Management Studio**
2. **Connect to Server:**
   - Server name: `db33580.databaseasp.net,1433`
   - Authentication: **SQL Server Authentication**
   - Login: `db33580`
   - Password: `6Hg%_n7BrW#3`
3. **Click Connect**
4. If connection succeeds, your system can connect to the cloud database

## Connection String Configuration

The cloud connection string is configured in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "CloudConnection": "Server=db33580.databaseasp.net,1433; Database=db33580; User Id=db33580; Password=6Hg%_n7BrW#3; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True; Connection Timeout=30; Network Library=dbmssocn;"
  }
}
```

## Common Connection Issues

### Issue 1: Network Path Not Found (Error 53)
**Symptoms:**
- Cannot reach the server
- Timeout errors

**Solutions:**
- Check internet connection
- Verify firewall allows port 1433
- Try using IP address instead of domain name
- Contact network administrator

### Issue 2: Login Failed (Error 18456)
**Symptoms:**
- Authentication errors
- "Login failed for user" message

**Solutions:**
- Verify username and password in `appsettings.json`
- Check if SQL Server authentication is enabled
- Contact database administrator

### Issue 3: Server Not Found (Error 2)
**Symptoms:**
- Server name cannot be resolved
- DNS errors

**Solutions:**
- Verify server name is correct
- Check DNS resolution
- Try using IP address
- Contact database host

## Troubleshooting Steps

1. **Check Internet Connectivity**
   - Ensure you have an active internet connection
   - Try accessing other websites

2. **Verify Connection String**
   - Check `appsettings.json` for correct connection string
   - Ensure password special characters are properly escaped

3. **Test Port Connectivity**
   - Use `telnet db33580.databaseasp.net 1433` to test if port is accessible
   - If telnet fails, firewall may be blocking

4. **Check Firewall Settings**
   - Ensure outbound connections on port 1433 are allowed
   - Check Windows Firewall and any corporate firewall

5. **Verify Server Status**
   - Contact database host to confirm server is running
   - Check if server allows remote connections

## What Gets Tested

When you test the connection, the system:
1. ✅ Checks internet connectivity
2. ✅ Attempts to connect to the cloud SQL Server
3. ✅ Executes a test query (`SELECT 1`)
4. ✅ Retrieves database and server information
5. ✅ Reports detailed error information if connection fails

## Automatic Connection Testing

The system automatically tests the cloud connection:
- **Before each sync operation** - The sync service tests the connection before attempting to sync data
- **On internet reconnection** - When internet connectivity is restored, the system attempts to sync

## Next Steps

If the connection test fails:
1. Review the error message details
2. Check the troubleshooting steps above
3. Verify your connection string in `appsettings.json`
4. Contact your database administrator if issues persist

If the connection test succeeds:
- ✅ Your system is ready to sync data with the cloud database
- ✅ You can use the sync features in the Cloud Management page
- ✅ Data will automatically sync when internet is available

