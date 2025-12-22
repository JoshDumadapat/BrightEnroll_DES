using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.Business.Audit;
using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Services.Database.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Net.NetworkInformation;

namespace BrightEnroll_DES.Services.Authentication
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string username, string password);
        bool IsAuthenticated { get; }
        User? CurrentUser { get; }
        void Logout();
    }

    public class AuthService : IAuthService
    {
        private bool _isAuthenticated = false;
        private User? _currentUser = null;
        private readonly ILoginService _loginService;
        private readonly AuditLogService? _auditLogService;
        private readonly IServiceScopeFactory? _serviceScopeFactory;
        private readonly IConfiguration? _configuration;
        private readonly SuperAdminDbContext? _superAdminContext;
        private readonly IDatabaseSyncService? _syncService;

        public AuthService(
            ILoginService loginService, 
            AuditLogService? auditLogService = null, 
            IServiceScopeFactory? serviceScopeFactory = null,
            IConfiguration? configuration = null,
            SuperAdminDbContext? superAdminContext = null,
            IDatabaseSyncService? syncService = null)
        {
            _loginService = loginService;
            _auditLogService = auditLogService;
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
            _superAdminContext = superAdminContext;
            _syncService = syncService;
        }

        public bool IsAuthenticated => _isAuthenticated;
        public User? CurrentUser => _currentUser;
        private string GetLocalIpAddress()
        {
            try
            {
                // Try to get the first non-loopback IPv4 address
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch
            {
                // Fallback if DNS lookup fails
            }

            // Fallback to localhost
            return "127.0.0.1";
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                var user = await _loginService.ValidateUserCredentialsAsync(username, password);
                
                if (user != null)
                {
                    var isSuperAdmin = user.user_role?.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) == true 
                                    || user.user_role?.Equals("Super Admin", StringComparison.OrdinalIgnoreCase) == true;
                    
                    if (isSuperAdmin)
                    {
                        System.Diagnostics.Debug.WriteLine("WARNING: SuperAdmin found in local school DB. Authenticating from SuperAdmin DB or cloud instead.");
                        
                        var superAdminDbUserFromDb = await _loginService.ValidateSuperAdminFromSuperAdminDatabaseAsync(username, password);
                        if (superAdminDbUserFromDb != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"SuperAdmin authenticated from SuperAdmin database: {username}");
                            _currentUser = superAdminDbUserFromDb;
                            _isAuthenticated = true;
                            LogSuccessfulLogin(superAdminDbUserFromDb);
                            return true;
                        }
                        
                        var cloudUserFromCloud = await _loginService.ValidateSuperAdminFromCloudAsync(username, password);
                        if (cloudUserFromCloud != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"SuperAdmin authenticated from cloud database: {username}");
                            _currentUser = cloudUserFromCloud;
                            _isAuthenticated = true;
                            LogSuccessfulLogin(cloudUserFromCloud);
                            return true;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"SuperAdmin login failed: {username} not found in SuperAdmin DB or cloud");
                        _isAuthenticated = false;
                        _currentUser = null;
                        LogFailedLogin(username);
                        return false;
                    }
                    
                    _currentUser = user;
                    _isAuthenticated = true;
                    LogSuccessfulLogin(user);
                    return true;
                }

                var superAdminDbUser = await _loginService.ValidateSuperAdminFromSuperAdminDatabaseAsync(username, password);
                if (superAdminDbUser != null)
                {
                    System.Diagnostics.Debug.WriteLine($"SuperAdmin authenticated from SuperAdmin database: {username}");
                    _currentUser = superAdminDbUser;
                    _isAuthenticated = true;
                    LogSuccessfulLogin(superAdminDbUser);
                    return true;
                }
                
                var cloudUser = await _loginService.ValidateSuperAdminFromCloudAsync(username, password);
                if (cloudUser != null)
                {
                    System.Diagnostics.Debug.WriteLine($"SuperAdmin authenticated from cloud database: {username}");
                    _currentUser = cloudUser;
                    _isAuthenticated = true;
                    LogSuccessfulLogin(cloudUser);
                    return true;
                }
                
                if (_superAdminContext != null)
                {
                    var customer = await _loginService.GetCustomerByAdminEmailAsync(username);
                    if (customer != null && !string.IsNullOrWhiteSpace(customer.DatabaseConnectionString))
                    {
                        if (string.IsNullOrWhiteSpace(customer.AdminPassword))
                        {
                            System.Diagnostics.Debug.WriteLine($"Customer {customer.SchoolName} has no AdminPassword stored.");
                            _isAuthenticated = false;
                            _currentUser = null;
                            LogFailedLogin(username, "No password stored in SuperAdmin database");
                            return false;
                        }
                        
                        // Validate password from SuperAdmin Customer table
                        var storedPassword = customer.AdminPassword?.Trim() ?? string.Empty;
                        var providedPassword = password?.Trim() ?? string.Empty;
                        
                        if (!string.Equals(providedPassword, storedPassword, StringComparison.Ordinal))
                        {
                            System.Diagnostics.Debug.WriteLine($"Password mismatch for customer {customer.SchoolName}. Provided: '{providedPassword}', Stored: '{storedPassword}'");
                            _isAuthenticated = false;
                            _currentUser = null;
                            LogFailedLogin(username, "Invalid password");
                            return false;
                        }
                        
                        // Password is valid - now connect to school's database
                        System.Diagnostics.Debug.WriteLine($"Credentials validated from SuperAdmin Customer table for {customer.SchoolName}. Connecting to school database...");
                        
                        // Auto-generate local database connection string if not set (uses LocalDB pattern)
                        var localConnectionString = customer.DatabaseConnectionString;
                        if (string.IsNullOrWhiteSpace(localConnectionString))
                        {
                            var localDbName = $"DB_{customer.CustomerCode}";
                            localConnectionString = $"Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog={localDbName};Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;";
                            System.Diagnostics.Debug.WriteLine($"Auto-generated local database connection string: {localDbName}");
                        }
                        
                        // Create local database if it doesn't exist
                        await EnsureLocalDatabaseExistsAsync(localConnectionString);
                        
                        // Check if local database is empty
                        var isLocalDbEmpty = await IsLocalDatabaseEmptyAsync(localConnectionString);
                        
                        if (isLocalDbEmpty && !string.IsNullOrWhiteSpace(customer.CloudConnectionString))
                        {
                            // Sync from cloud first
                            System.Diagnostics.Debug.WriteLine("Local database is empty. Syncing from cloud...");
                            await SyncFromCloudForSchoolAsync(customer.CloudConnectionString, localConnectionString);
                        }
                        
                        var schoolUser = await ValidateUserInSchoolDatabaseAsync(localConnectionString, username, password ?? string.Empty);
                        
                        if (schoolUser == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"User not found in school database. Creating from Customer table...");
                            schoolUser = await CreateUserFromCustomerAsync(customer, localConnectionString);
                        }
                        
                        if (schoolUser != null)
                        {
                            _currentUser = schoolUser;
                            _isAuthenticated = true;
                            LogSuccessfulLogin(schoolUser);
                            return true;
                        }
                    }
                }
                
                _isAuthenticated = false;
                _currentUser = null;
                LogFailedLogin(username);
                return false;
            }
            catch (Exception ex)
            {
                _isAuthenticated = false;
                _currentUser = null;
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                LogFailedLogin(username, ex.Message);
                return false;
            }
        }

        private async Task EnsureLocalDatabaseExistsAsync(string connectionString)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                var databaseName = builder.InitialCatalog;
                
                if (string.IsNullOrWhiteSpace(databaseName))
                {
                    System.Diagnostics.Debug.WriteLine("Cannot determine database name from connection string");
                    return;
                }
                
                // Connect to master database to create the target database
                builder.InitialCatalog = "master";
                var masterConnectionString = builder.ConnectionString;
                
                using var connection = new SqlConnection(masterConnectionString);
                await connection.OpenAsync();
                
                // Check if database exists
                var checkDbQuery = "SELECT COUNT(*) FROM sys.databases WHERE name = @DatabaseName";
                using var checkCommand = new SqlCommand(checkDbQuery, connection);
                checkCommand.Parameters.AddWithValue("@DatabaseName", databaseName);
                
                var exists = (int)await checkCommand.ExecuteScalarAsync() > 0;
                
                if (!exists)
                {
                    System.Diagnostics.Debug.WriteLine($"Creating local database: {databaseName}");
                    var createDbQuery = $"CREATE DATABASE [{databaseName}]";
                    using var createCommand = new SqlCommand(createDbQuery, connection);
                    await createCommand.ExecuteNonQueryAsync();
                    
                    // Initialize tables in the new database
                    System.Diagnostics.Debug.WriteLine($"Initializing tables in local database: {databaseName}");
                    var tablesInitialized = await BrightEnroll_DES.Services.Database.Initialization.DatabaseInitializer.InitializeTablesOnlyAsync(connectionString);
                    if (tablesInitialized)
                    {
                        System.Diagnostics.Debug.WriteLine($"Tables initialized successfully in {databaseName}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Local database already exists: {databaseName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error ensuring local database exists: {ex.Message}");
            }
        }

        private async Task<bool> IsLocalDatabaseEmptyAsync(string connectionString)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                var query = "SELECT COUNT(*) FROM tbl_Users";
                using var command = new SqlCommand(query, connection);
                var result = await command.ExecuteScalarAsync();
                var count = result != null ? (int)result : 0;
                
                return count == 0;
            }
            catch
            {
                return true;
            }
        }

        private async Task<User?> ValidateUserInSchoolDatabaseAsync(string connectionString, string email, string password)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                var query = @"
                    SELECT user_ID, system_ID, first_name, last_name, email, 
                           password, user_role, status
                    FROM tbl_Users 
                    WHERE (email = @Email OR system_ID = @Email)";
                
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Email", email);
                
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var hashedPassword = reader["password"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(hashedPassword) && 
                        BCrypt.Net.BCrypt.Verify(password, hashedPassword))
                    {
                        return new User
                        {
                            user_ID = (int)reader["user_ID"],
                            system_ID = reader["system_ID"]?.ToString() ?? "",
                            first_name = reader["first_name"]?.ToString() ?? "",
                            last_name = reader["last_name"]?.ToString() ?? "",
                            email = reader["email"]?.ToString() ?? "",
                            user_role = reader["user_role"]?.ToString() ?? "",
                            password = hashedPassword
                        };
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task SyncFromCloudForSchoolAsync(string cloudConnectionString, string localConnectionString)
        {
            try
            {
                using var cloudConnection = new SqlConnection(cloudConnectionString);
                await cloudConnection.OpenAsync();
                
                using var localConnection = new SqlConnection(localConnectionString);
                await localConnection.OpenAsync();
                
                // Get users from cloud
                var cloudQuery = "SELECT * FROM tbl_Users";
                using var cloudCommand = new SqlCommand(cloudQuery, cloudConnection);
                using var reader = await cloudCommand.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    // Insert into local database
                    var insertQuery = @"
                        IF NOT EXISTS (SELECT 1 FROM tbl_Users WHERE email = @Email)
                        INSERT INTO tbl_Users (system_ID, first_name, last_name, email, password, user_role, status, date_hired)
                        VALUES (@SystemId, @FirstName, @LastName, @Email, @Password, @UserRole, @Status, @DateHired)";
                    
                    using var insertCommand = new SqlCommand(insertQuery, localConnection);
                    insertCommand.Parameters.AddWithValue("@SystemId", reader["system_ID"] ?? DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@FirstName", reader["first_name"] ?? DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@LastName", reader["last_name"] ?? DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@Email", reader["email"] ?? DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@Password", reader["password"] ?? DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@UserRole", reader["user_role"] ?? DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@Status", reader["status"] ?? "active");
                    insertCommand.Parameters.AddWithValue("@DateHired", reader["date_hired"] ?? DateTime.Now);
                    
                    await insertCommand.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sync error: {ex.Message}");
            }
        }

        private async Task<User?> CreateUserFromCustomerAsync(Customer customer, string connectionString)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                var nameParts = customer.ContactPerson?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? new[] { "Admin", "User" };
                var firstName = nameParts.Length > 0 ? nameParts[0] : "Admin";
                var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "User";
                
                // Generate system ID
                var systemId = $"{customer.CustomerCode}-0001";
                
                // Hash the password for storage in school's database
                if (string.IsNullOrWhiteSpace(customer.AdminPassword))
                {
                    System.Diagnostics.Debug.WriteLine($"Cannot create user: AdminPassword is null or empty for customer {customer.SchoolName}");
                    return null;
                }
                
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(customer.AdminPassword);
                
                // Check if user already exists
                var checkQuery = "SELECT COUNT(*) FROM tbl_Users WHERE email = @Email";
                using var checkCommand = new SqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@Email", customer.AdminUsername);
                var result = await checkCommand.ExecuteScalarAsync();
                var exists = result != null && result != DBNull.Value && (int)result > 0;
                
                if (!exists)
                {
                    // Insert user into school's database
                    var insertQuery = @"
                        INSERT INTO tbl_Users (
                            system_ID, first_name, last_name, email, password, 
                            user_role, status, date_hired, contact_num, gender, age, birthdate
                        )
                        VALUES (
                            @SystemId, @FirstName, @LastName, @Email, @Password,
                            @UserRole, @Status, @DateHired, @ContactNum, @Gender, @Age, @Birthdate
                        )";
                    
                    using var insertCommand = new SqlCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@SystemId", systemId);
                    insertCommand.Parameters.AddWithValue("@FirstName", firstName);
                    insertCommand.Parameters.AddWithValue("@LastName", lastName);
                    insertCommand.Parameters.AddWithValue("@Email", customer.AdminUsername);
                    insertCommand.Parameters.AddWithValue("@Password", hashedPassword);
                    insertCommand.Parameters.AddWithValue("@UserRole", "Admin");
                    insertCommand.Parameters.AddWithValue("@Status", "active");
                    insertCommand.Parameters.AddWithValue("@DateHired", DateTime.Now);
                    insertCommand.Parameters.AddWithValue("@ContactNum", customer.ContactPhone ?? "00000000000");
                    insertCommand.Parameters.AddWithValue("@Gender", "male");
                    var birthdate = new DateTime(DateTime.Now.Year - 30, 1, 1);
                    insertCommand.Parameters.AddWithValue("@Birthdate", birthdate);
                    insertCommand.Parameters.AddWithValue("@Age", (byte)30);
                    
                    await insertCommand.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine($"User created in school database: {customer.AdminUsername}");
                }
                
                // Return user object
                return new User
                {
                    user_ID = 1, 
                    system_ID = systemId,
                    first_name = firstName,
                    last_name = lastName,
                    email = customer.AdminUsername ?? "",
                    user_role = "Admin",
                    password = hashedPassword
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating user from Customer: {ex.Message}");
                return null;
            }
        }

        private void LogSuccessfulLogin(User user)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(100);
                    
                    if (_auditLogService != null && _serviceScopeFactory != null)
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        
                        var dbUser = await context.Users
                            .AsNoTracking()
                            .FirstOrDefaultAsync(u => u.UserId == user.user_ID);
                        
                        if (dbUser != null)
                        {
                            var userName = $"{dbUser.FirstName} {dbUser.LastName}".Trim();
                            var ipAddress = GetLocalIpAddress();
                            var loginTime = DateTime.Now;
                            
                            await _auditLogService.CreateLogAsync(
                                action: "User Login",
                                module: "Authentication",
                                description: $"User successfully logged into the system at {loginTime:yyyy-MM-dd HH:mm:ss}",
                                userName: userName,
                                userRole: dbUser.UserRole,
                                userId: dbUser.UserId,
                                ipAddress: ipAddress,
                                status: "Success",
                                severity: "Low"
                            );
                        }
                    }
                    else if (_auditLogService != null)
                    {
                        var userName = $"{user.first_name} {user.last_name}".Trim();
                        var ipAddress = GetLocalIpAddress();
                        var loginTime = DateTime.Now;
                        
                        await _auditLogService.CreateLogAsync(
                            action: "User Login",
                            module: "Authentication",
                            description: $"User successfully logged into the system at {loginTime:yyyy-MM-dd HH:mm:ss}",
                            userName: userName,
                            userRole: user.user_role,
                            userId: user.user_ID,
                            ipAddress: ipAddress,
                            status: "Success",
                            severity: "Low"
                        );
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"✗ Failed to create login audit log: {ex.Message}");
                }
            });
        }

        private void LogFailedLogin(string username, string? errorMessage = null)
        {
            if (_auditLogService != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var ipAddress = GetLocalIpAddress();
                        await _auditLogService.CreateLogAsync(
                            action: errorMessage == null ? "Failed Login Attempt" : "Login Error",
                            module: "Authentication",
                            description: errorMessage == null 
                                ? $"Failed login attempt with username: {username}"
                                : $"Login error for username: {username}. Error: {errorMessage}",
                            userName: null,
                            userRole: null,
                            userId: null,
                            ipAddress: ipAddress,
                            status: "Failed",
                            severity: errorMessage == null ? "Medium" : "High"
                        );
                    }
                    catch
                    {
                        // Don't break login if audit logging fails
                    }
                });
            }
        }

        public void Logout()
        {
            // Capture user info before clearing
            var userId = _currentUser?.user_ID;
            var userName = _currentUser != null ? $"{_currentUser.first_name} {_currentUser.last_name}".Trim() : null;
            var userRole = _currentUser?.user_role;
            
            _isAuthenticated = false;
            _currentUser = null;
            
            // Log logout to audit trail - fetch from database after logout
            if (_auditLogService != null && userId.HasValue)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Small delay to ensure logout is complete
                        await Task.Delay(100);
                        
                        if (_serviceScopeFactory != null)
                        {
                            using var scope = _serviceScopeFactory.CreateScope();
                            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                            
                            // Fetch user from database to ensure we have complete information
                            var dbUser = await context.Users
                                .AsNoTracking()
                                .FirstOrDefaultAsync(u => u.UserId == userId.Value);
                            
                            if (dbUser != null)
                            {
                                var fullUserName = $"{dbUser.FirstName} {dbUser.LastName}".Trim();
                                var ipAddress = GetLocalIpAddress();
                                var logoutTime = DateTime.Now;
                                
                                await _auditLogService.CreateLogAsync(
                                    action: "User Logout",
                                    module: "Authentication",
                                    description: $"User logged out from the system at {logoutTime:yyyy-MM-dd HH:mm:ss}",
                                    userName: fullUserName,
                                    userRole: dbUser.UserRole,
                                    userId: dbUser.UserId,
                                    ipAddress: ipAddress,
                                    status: "Success",
                                    severity: "Low"
                                );
                                
                                System.Diagnostics.Debug.WriteLine($"✓ Logout audit log saved: {fullUserName} ({dbUser.UserRole}) logged out at {logoutTime:yyyy-MM-dd HH:mm:ss} from {ipAddress}");
                            }
                            else if (userName != null)
                            {
                                // Fallback if user not found in database
                                var ipAddress = GetLocalIpAddress();
                                var logoutTime = DateTime.Now;
                                
                                await _auditLogService.CreateLogAsync(
                                    action: "User Logout",
                                    module: "Authentication",
                                    description: $"User logged out from the system at {logoutTime:yyyy-MM-dd HH:mm:ss}",
                                    userName: userName,
                                    userRole: userRole,
                                    userId: userId,
                                    ipAddress: ipAddress,
                                    status: "Success",
                                    severity: "Low"
                                );
                            }
                        }
                        else if (userName != null)
                        {
                            // Fallback if service scope factory is not available
                            var ipAddress = GetLocalIpAddress();
                            var logoutTime = DateTime.Now;
                            
                            await _auditLogService.CreateLogAsync(
                                action: "User Logout",
                                module: "Authentication",
                                description: $"User logged out from the system at {logoutTime:yyyy-MM-dd HH:mm:ss}",
                                userName: userName,
                                userRole: userRole,
                                userId: userId,
                                ipAddress: ipAddress,
                                status: "Success",
                                severity: "Low"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"✗ Failed to create logout audit log: {ex.Message}");
                    }
                });
            }
        }
    }
}

