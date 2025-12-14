using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.Business.Audit;
using BrightEnroll_DES.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        public AuthService(ILoginService loginService, AuditLogService? auditLogService = null, IServiceScopeFactory? serviceScopeFactory = null)
        {
            _loginService = loginService;
            _auditLogService = auditLogService;
            _serviceScopeFactory = serviceScopeFactory;
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
                // Validate credentials against database
                var user = await _loginService.ValidateUserCredentialsAsync(username, password);

                if (user != null)
                {
                    _currentUser = user;
                    _isAuthenticated = true;
                    
                    // Log successful login to audit trail - fetch from database after authentication
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // Small delay to ensure authentication is complete
                            await Task.Delay(100);
                            
                            if (_auditLogService != null && _serviceScopeFactory != null)
                            {
                                using var scope = _serviceScopeFactory.CreateScope();
                                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                                
                                // Fetch user from database to ensure we have complete information
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
                                    
                                    System.Diagnostics.Debug.WriteLine($"✓ Login audit log saved: {userName} ({dbUser.UserRole}) logged in at {loginTime:yyyy-MM-dd HH:mm:ss} from {ipAddress}");
                                }
                            }
                            else if (_auditLogService != null)
                            {
                                // Fallback if service scope factory is not available
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
                    
                    return true;
                }

                _isAuthenticated = false;
                _currentUser = null;
                
                // Log failed login attempt to audit trail with IP address
                if (_auditLogService != null)
                {
                    try
                    {
                        var ipAddress = GetLocalIpAddress();
                        await _auditLogService.CreateLogAsync(
                            action: "Failed Login Attempt",
                            module: "Authentication",
                            description: $"Failed login attempt with username: {username}",
                            userName: null,
                            userRole: null,
                            userId: null,
                            ipAddress: ipAddress,
                            status: "Failed",
                            severity: "Medium"
                        );
                    }
                    catch
                    {
                        // Don't break login if audit logging fails
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                // Log error in production
                _isAuthenticated = false;
                _currentUser = null;
                
                // Log login error to audit trail with IP address
                if (_auditLogService != null)
                {
                    try
                    {
                        var ipAddress = GetLocalIpAddress();
                        await _auditLogService.CreateLogAsync(
                            action: "Login Error",
                            module: "Authentication",
                            description: $"Login error for username: {username}. Error: {ex.Message}",
                            userName: null,
                            userRole: null,
                            userId: null,
                            ipAddress: ipAddress,
                            status: "Failed",
                            severity: "High"
                        );
                    }
                    catch
                    {
                        // Don't break login if audit logging fails
                    }
                }
                
                return false;
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

