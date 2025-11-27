# Cloud Database Connection Troubleshooting

## Current Error
- **Error**: "A network-related or instance-specific error occurred while establishing a connection to SQL Server"
- **Error Number**: 53 (The network path was not found)
- **Provider**: Named Pipes Provider (this is the problem - we need TCP/IP)

## Connection String Fixes Applied

### Current Connection String (Applied)
```
Data Source=db33580.databaseasp.net,1433; Initial Catalog=db33580; User ID=db33580; Password=6Hg%_n7BrW#3; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True; Connection Timeout=30;
```

## Alternative Connection Strings to Try

If the current connection string doesn't work, try these alternatives:

### Option 1: Force TCP/IP Protocol Explicitly
```
Server=db33580.databaseasp.net,1433; Database=db33580; User Id=db33580; Password=6Hg%_n7BrW#3; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True; Connection Timeout=30; Network Library=dbmssocn;
```

### Option 2: Try Without Port (Let SQL Server Auto-Detect)
```
Server=db33580.databaseasp.net; Database=db33580; User Id=db33580; Password=6Hg%_n7BrW#3; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True; Connection Timeout=30;
```

### Option 3: Try Different Port (Some databaseasp.net servers use 14333)
```
Data Source=db33580.databaseasp.net,14333; Initial Catalog=db33580; User ID=db33580; Password=6Hg%_n7BrW#3; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True; Connection Timeout=30;
```

### Option 4: Use IP Address Instead of Domain Name
If DNS resolution is the issue, you may need to:
1. Find the IP address of `db33580.databaseasp.net` using `nslookup` or `ping`
2. Use the IP address in the connection string:
```
Data Source=<IP_ADDRESS>,1433; Initial Catalog=db33580; User ID=db33580; Password=6Hg%_n7BrW#3; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True; Connection Timeout=30;
```

### Option 5: Try With Persist Security Info
```
Data Source=db33580.databaseasp.net,1433; Initial Catalog=db33580; User ID=db33580; Password=6Hg%_n7BrW#3; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True; Connection Timeout=30; Persist Security Info=True;
```

## How to Test Connection Strings

1. Update `appsettings.json` with the connection string you want to test
2. Restart the application
3. Go to Cloud Management page
4. Try to sync
5. Check the logs for connection errors

## Common Issues and Solutions

### Issue 1: Firewall Blocking Connection
- **Solution**: Ensure your firewall allows outbound connections on port 1433 (or the port your database uses)
- **Check**: Try connecting from SQL Server Management Studio (SSMS) from the same machine

### Issue 2: Server Name Not Resolving
- **Solution**: Try using the IP address instead of the domain name
- **Check**: Run `ping db33580.databaseasp.net` to see if DNS resolution works

### Issue 3: Wrong Port Number
- **Solution**: Contact your database host to confirm the correct port
- **Common Ports**: 1433 (default), 14333 (some hosts), 1434 (SQL Server Browser)

### Issue 4: Password Special Characters
- **Current Password**: `6Hg%_n7BrW#3` contains `%` and `#`
- **Note**: These should work in connection strings, but if issues persist, try URL-encoding:
  - `%` becomes `%25`
  - `#` becomes `%23`
- **Example**: `Password=6Hg%25_n7BrW%233;`

### Issue 5: SQL Server Not Allowing Remote Connections
- **Solution**: This is a server-side configuration issue. Contact your database host to ensure:
  - SQL Server is configured to accept remote connections
  - TCP/IP protocol is enabled
  - SQL Server Browser service is running (if using dynamic ports)

## Testing Connection from SQL Server Management Studio

To verify the connection string works, try connecting from SSMS:

1. Open SQL Server Management Studio
2. Click "Connect to Server"
3. Server name: `db33580.databaseasp.net,1433` (or try without port)
4. Authentication: SQL Server Authentication
5. Login: `db33580`
6. Password: `6Hg%_n7BrW#3`
7. Click "Connect"

If this works, the connection string format should work in the application too.

## Next Steps

1. Try the connection strings above in order
2. Test each one by updating `appsettings.json`
3. Check application logs for specific error messages
4. If none work, contact your database host to verify:
   - Server name and port
   - Authentication credentials
   - Firewall rules
   - SQL Server configuration

