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
using BrightEnroll_DES.Data.Models.SuperAdmin;

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

        /// <summary>
        /// Gets the local machine's IP address for audit logging
        /// </summary>
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
                // STEP 1: Try local database first
                var user = await _loginService.ValidateUserCredentialsAsync(username, password);
                
                if (user != null)
                {
                    // Check if SuperAdmin (should NOT be in local DB for security)
                    var isSuperAdmin = user.user_role?.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) == true 
                                    || user.user_role?.Equals("Super Admin", StringComparison.OrdinalIgnoreCase) == true;
                    
                    if (isSuperAdmin)
                    {
                        // SuperAdmin found in local school DB - reject for security, try SuperAdmin DB or cloud instead
                        System.Diagnostics.Debug.WriteLine("WARNING: SuperAdmin found in local school DB. Authenticating from SuperAdmin DB or cloud instead.");
                        
                        // Try SuperAdmin database first (local dev/testing - most common scenario)
                        var superAdminDbUserFromDb = await _loginService.ValidateSuperAdminFromSuperAdminDatabaseAsync(username, password);
                        if (superAdminDbUserFromDb != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"SuperAdmin authenticated from SuperAdmin database: {username}");
                            _currentUser = superAdminDbUserFromDb;
                            _isAuthenticated = true;
                            LogSuccessfulLogin(superAdminDbUserFromDb);
                            return true;
                        }
                        
                        // Try cloud (production)
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
                    
                    // School admin found in local DB - proceed
                    _currentUser = user;
                    _isAuthenticated = true;
                    LogSuccessfulLogin(user);
                    return true;
                }
                
                // STEP 2: Not found in local - try SuperAdmin database first (for local dev/testing)
                // This allows SuperAdmin to login from SuperAdmin database even if not in cloud
                var superAdminDbUser = await _loginService.ValidateSuperAdminFromSuperAdminDatabaseAsync(username, password);
                if (superAdminDbUser != null)
                {
                    System.Diagnostics.Debug.WriteLine($"SuperAdmin authenticated from SuperAdmin database: {username}");
                    _currentUser = superAdminDbUser;
                    _isAuthenticated = true;
                    LogSuccessfulLogin(superAdminDbUser);
                    return true;
                }
                
                // STEP 2.5: Try cloud for SuperAdmin (production)
                var cloudUser = await _loginService.ValidateSuperAdminFromCloudAsync(username, password);
                if (cloudUser != null)
                {
                    System.Diagnostics.Debug.WriteLine($"SuperAdmin authenticated from cloud database: {username}");
                    _currentUser = cloudUser;
                    _isAuthenticated = true;
                    LogSuccessfulLogin(cloudUser);
                    return true;
                }
                
                // STEP 3: Not found anywhere - check if school admin (query SuperAdmin DB)
                if (_superAdminContext != null)
                {
                    var customer = await _loginService.GetCustomerByAdminEmailAsync(username);
                    if (customer != null && !string.IsNullOrWhiteSpace(customer.DatabaseConnectionString))
                    {
                        // FIRST: Validate credentials against SuperAdmin Customer table (source of truth)
                        if (string.IsNullOrWhiteSpace(customer.AdminPassword))
                        {
                            System.Diagnostics.Debug.WriteLine($"Customer {customer.SchoolName} has no AdminPassword stored.");
                            _isAuthenticated = false;
                            _currentUser = null;
                            LogFailedLogin(username, "No password stored in SuperAdmin database");
                            return false;
                        }
                        
                        // Validate password from SuperAdmin Customer table (case-sensitive comparison, trim whitespace)
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
                        
                        // Try to get user from school's database (or create if doesn't exist)
                        var schoolUser = await ValidateUserInSchoolDatabaseAsync(localConnectionString, username, password ?? string.Empty);
                        
                        // If user doesn't exist in school's database, create it from Customer table data
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
                            
                            // Clear and preload customer modules into cache (non-blocking, background task)
                            // This ensures module restrictions are enforced immediately with fresh data
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    if (_serviceScopeFactory != null && !string.IsNullOrWhiteSpace(schoolUser.email))
                                    {
                                        using var scope = _serviceScopeFactory.CreateScope();
                                        var customerModuleService = scope.ServiceProvider.GetService<BrightEnroll_DES.Services.Business.SuperAdmin.ICustomerModuleService>();
                                        if (customerModuleService != null)
                                        {
                                            // First, try to get customer ID to clear cache by ID (more efficient)
                                            var loginService = scope.ServiceProvider.GetService<ILoginService>();
                                            if (loginService != null)
                                            {
                                                var customer = await loginService.GetCustomerByAdminEmailAsync(schoolUser.email);
                                                if (customer != null)
                                                {
                                                    // Clear cache for this customer to ensure fresh data on login
                                                    customerModuleService.ClearCache(customer.CustomerId);
                                                    System.Diagnostics.Debug.WriteLine($"Cleared module cache for customer {customer.CustomerId} on login");
                                                }
                                            }
                                            
                                            // Preload modules to populate cache with fresh data
                                            await customerModuleService.GetAvailableModulePackageIdsAsync(schoolUser.email);
                                            System.Diagnostics.Debug.WriteLine($"Preloaded fresh modules for user: {schoolUser.email}");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error clearing/preloading modules: {ex.Message}");
                                }
                            });
                            
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
                
                var result = await checkCommand.ExecuteScalarAsync();
                var exists = result != null && result != DBNull.Value ? Convert.ToInt32(result) > 0 : false;
                
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
                // Don't throw - let the rest of the flow continue
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
                return true; // Assume empty if can't check
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
            // Simplified sync - just sync users table for now
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
                
                // Parse contact person name to get first/last name
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
                    user_ID = 1, // Will be set correctly when retrieved
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
                    
                    // Check if user is SuperAdmin
                    var isSuperAdmin = !string.IsNullOrWhiteSpace(user.user_role) && 
                                     (user.user_role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                                      user.user_role.Equals("Super Admin", StringComparison.OrdinalIgnoreCase));
                    
                    if (isSuperAdmin && _serviceScopeFactory != null)
                    {
                        // Log to SuperAdmin audit log
                        using var scope = _serviceScopeFactory.CreateScope();
                        var superAdminAuditLogService = scope.ServiceProvider.GetService<BrightEnroll_DES.Services.Business.SuperAdmin.SuperAdminAuditLogService>();
                        
                        if (superAdminAuditLogService != null)
                        {
                            var userName = $"{user.first_name} {user.last_name}".Trim();
                            if (string.IsNullOrWhiteSpace(userName))
                                userName = user.email ?? "SuperAdmin";
                            
                            var ipAddress = GetLocalIpAddress();
                            var loginTime = DateTime.Now;
                            
                            await superAdminAuditLogService.CreateLogAsync(
                                action: "SuperAdmin Login",
                                module: "Authentication",
                                description: $"SuperAdmin successfully logged into the system at {loginTime:yyyy-MM-dd HH:mm:ss}",
                                userName: userName,
                                userRole: user.user_role,
                                userId: user.user_ID,
                                ipAddress: ipAddress,
                                status: "Success",
                                severity: "Medium"
                            );
                            
                            System.Diagnostics.Debug.WriteLine($"✓ SuperAdmin login audit log saved: {userName} logged in at {loginTime:yyyy-MM-dd HH:mm:ss}");
                            return; // Exit early for SuperAdmin
                        }
                    }
                    
                    // Regular user logging (school admin)
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
            _ = Task.Run(async () =>
            {
                try
                {
                    // Check if this might be a SuperAdmin login attempt
                    // We'll log to both SuperAdmin and regular audit logs to be safe
                    var ipAddress = GetLocalIpAddress();
                    
                    if (_serviceScopeFactory != null)
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        
                        // Try to determine if this is a SuperAdmin login attempt
                        var superAdminAuditLogService = scope.ServiceProvider.GetService<BrightEnroll_DES.Services.Business.SuperAdmin.SuperAdminAuditLogService>();
                        var auditLogService = scope.ServiceProvider.GetService<AuditLogService>();
                        
                        // Check if username exists in SuperAdmin database
                        if (_superAdminContext != null && superAdminAuditLogService != null)
                        {
                            try
                            {
                                var superAdminUser = await _superAdminContext.Users
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(u => 
                                        u.Email != null && u.Email.Equals(username, StringComparison.OrdinalIgnoreCase));
                                
                                if (superAdminUser != null || username.Contains("admin", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Likely a SuperAdmin login attempt - log to SuperAdmin audit log
                                    await superAdminAuditLogService.CreateLogAsync(
                                        action: errorMessage == null ? "Failed SuperAdmin Login Attempt" : "SuperAdmin Login Error",
                                        module: "Authentication",
                                        description: errorMessage == null 
                                            ? $"Failed SuperAdmin login attempt with username: {username}"
                                            : $"SuperAdmin login error for username: {username}. Error: {errorMessage}",
                                        userName: username,
                                        userRole: "SuperAdmin",
                                        userId: null,
                                        ipAddress: ipAddress,
                                        status: "Failed",
                                        severity: errorMessage == null ? "High" : "Critical"
                                    );
                                }
                            }
                            catch
                            {
                                // If check fails, continue with regular logging
                            }
                        }
                        
                        // Also log to regular audit log for school admin attempts
                        if (auditLogService != null)
                        {
                            await auditLogService.CreateLogAsync(
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
                    }
                    else if (_auditLogService != null)
                    {
                        // Fallback to regular audit log
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
                }
                catch
                {
                    // Don't break login if audit logging fails
                }
            });
        }

        public void Logout()
        {
            // Capture user info before clearing
            var userId = _currentUser?.user_ID;
            var userName = _currentUser != null ? $"{_currentUser.first_name} {_currentUser.last_name}".Trim() : null;
            var userRole = _currentUser?.user_role;
            var userEmail = _currentUser?.email;
            
            // Check if user is SuperAdmin
            var isSuperAdmin = !string.IsNullOrWhiteSpace(userRole) && 
                             (userRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                              userRole.Equals("Super Admin", StringComparison.OrdinalIgnoreCase));
            
            _isAuthenticated = false;
            _currentUser = null;
            
            // Log logout to audit trail
            if (userId.HasValue || isSuperAdmin)
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
                            
                            if (isSuperAdmin)
                            {
                                // Log to SuperAdmin audit log
                                var superAdminAuditLogService = scope.ServiceProvider.GetService<BrightEnroll_DES.Services.Business.SuperAdmin.SuperAdminAuditLogService>();
                                
                                if (superAdminAuditLogService != null)
                                {
                                    var fullUserName = userName ?? userEmail ?? "SuperAdmin";
                                    var ipAddress = GetLocalIpAddress();
                                    var logoutTime = DateTime.Now;
                                    
                                    await superAdminAuditLogService.CreateLogAsync(
                                        action: "SuperAdmin Logout",
                                        module: "Authentication",
                                        description: $"SuperAdmin logged out from the system at {logoutTime:yyyy-MM-dd HH:mm:ss}",
                                        userName: fullUserName,
                                        userRole: userRole ?? "SuperAdmin",
                                        userId: userId,
                                        ipAddress: ipAddress,
                                        status: "Success",
                                        severity: "Low"
                                    );
                                    
                                    System.Diagnostics.Debug.WriteLine($"✓ SuperAdmin logout audit log saved: {fullUserName} logged out at {logoutTime:yyyy-MM-dd HH:mm:ss}");
                                    return; // Exit early for SuperAdmin
                                }
                            }
                            
                            // Regular user logout logging
                            if (_auditLogService != null)
                            {
                                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                                
                                // Fetch user from database to ensure we have complete information
                                if (userId.HasValue)
                                {
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
                                }
                            }
                            else if (userName != null && _auditLogService != null)
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
                        else if (userName != null && _auditLogService != null)
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

