using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.Authentication;
using BrightEnroll_DES.Services.Business.SuperAdmin;
using Microsoft.Extensions.DependencyInjection;

namespace BrightEnroll_DES.Services.RoleBase
{
    // Service for checking user authorization based on roles and permissions
    public interface IAuthorizationService
    {
        bool HasPermission(string permission);
        bool HasAnyPermission(params string[] permissions);
        bool HasAllPermissions(params string[] permissions);
        bool HasRole(string roleName);
        bool HasAnyRole(params string[] roleNames);
        List<string> GetUserPermissions();
        string? GetUserRole();
        bool UserHasPermission(User user, string permission);
        bool UserHasRole(User user, string roleName);
    }

    public class AuthorizationService : IAuthorizationService
    {
        private readonly IAuthService _authService;
        private readonly IRolePermissionService _rolePermissionService;
        private readonly IServiceScopeFactory? _serviceScopeFactory;

        public AuthorizationService(
            IAuthService authService, 
            IRolePermissionService rolePermissionService,
            IServiceScopeFactory? serviceScopeFactory = null)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _rolePermissionService = rolePermissionService ?? throw new ArgumentNullException(nameof(rolePermissionService));
            _serviceScopeFactory = serviceScopeFactory;
        }

        public bool HasPermission(string permission)
        {
            if (!_authService.IsAuthenticated || _authService.CurrentUser == null)
            {
                return false;
            }

            // SuperAdmin has access to everything
            var userRole = _authService.CurrentUser.user_role;
            if (!string.IsNullOrWhiteSpace(userRole) && 
                string.Equals(userRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // For Admin users: Check module-based permissions first (restriction based on purchased modules)
            // This ensures customers can only access modules they've purchased
            // EXCEPTION: Seeded admin account (development/testing) should have access to all modules
            if (IsAdmin() && _serviceScopeFactory != null)
            {
                var userEmail = _authService.CurrentUser.email;
                var systemId = _authService.CurrentUser.system_ID;
                
                // Check if this is the seeded admin account (bypass module restrictions)
                var isSeededAdmin = IsSeededAdminAccount(userEmail, systemId);
                
                if (isSeededAdmin)
                {
                    // Seeded admin account has access to all modules - bypass module check
                    // Continue to role-based permission check (all permissions allowed)
                }
                else if (!string.IsNullOrWhiteSpace(userEmail))
                {
                    try
                    {
                        // Create a scope to get CustomerModuleService (it's Scoped, but we're Singleton)
                        using var scope = _serviceScopeFactory.CreateScope();
                        var customerModuleService = scope.ServiceProvider.GetService<ICustomerModuleService>();
                        
                        if (customerModuleService != null)
                        {
                            // Check if permission comes from a purchased module
                            var hasModulePermission = customerModuleService.HasPermissionFromModule(permission, userEmail);
                            if (!hasModulePermission)
                            {
                                // Permission not available from purchased modules - deny access
                                return false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with role-based check as fallback
                        System.Diagnostics.Debug.WriteLine($"Error checking module permission: {ex.Message}");
                    }
                }
            }

            // For other roles or if module check passed: Check role-based permissions
            return UserHasPermission(_authService.CurrentUser, permission);
        }

        // Helper method to check if user is Admin (but not SuperAdmin)
        private bool IsAdmin()
        {
            var userRole = _authService.CurrentUser?.user_role;
            if (string.IsNullOrWhiteSpace(userRole))
            {
                return false;
            }
            
            var roleLower = userRole.ToLower().Trim().Replace(" ", "");
            return roleLower == "admin" && !IsSuperAdmin();
        }

        // Helper method to check if user is SuperAdmin
        private bool IsSuperAdmin()
        {
            var userRole = _authService.CurrentUser?.user_role;
            if (string.IsNullOrWhiteSpace(userRole))
            {
                return false;
            }
            
            var roleLower = userRole.ToLower().Trim().Replace(" ", "");
            return roleLower == "superadmin";
        }

        // Helper method to check if user is the seeded admin account (development/testing account)
        // Seeded admin should have access to all modules without subscription restrictions
        private bool IsSeededAdminAccount(string? email, string? systemId)
        {
            // Seeded admin account identifiers
            const string seededAdminEmail = "admin@brightenroll.com";
            const string seededAdminSystemId = "ADMIN001";
            
            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(systemId))
            {
                return false;
            }
            
            // Check by email or system ID
            var isSeededByEmail = !string.IsNullOrWhiteSpace(email) && 
                                  email.Equals(seededAdminEmail, StringComparison.OrdinalIgnoreCase);
            
            var isSeededBySystemId = !string.IsNullOrWhiteSpace(systemId) && 
                                     systemId.Equals(seededAdminSystemId, StringComparison.OrdinalIgnoreCase);
            
            return isSeededByEmail || isSeededBySystemId;
        }

        public bool HasAnyPermission(params string[] permissions)
        {
            if (!_authService.IsAuthenticated || _authService.CurrentUser == null)
            {
                return false;
            }

            if (permissions == null || permissions.Length == 0)
            {
                return false;
            }

            return permissions.Any(p => HasPermission(p));
        }

        public bool HasAllPermissions(params string[] permissions)
        {
            if (!_authService.IsAuthenticated || _authService.CurrentUser == null)
            {
                return false;
            }

            if (permissions == null || permissions.Length == 0)
            {
                return false;
            }

            return permissions.All(p => HasPermission(p));
        }

        public bool HasRole(string roleName)
        {
            if (!_authService.IsAuthenticated || _authService.CurrentUser == null)
            {
                return false;
            }

            return UserHasRole(_authService.CurrentUser, roleName);
        }

        public bool HasAnyRole(params string[] roleNames)
        {
            if (!_authService.IsAuthenticated || _authService.CurrentUser == null)
            {
                return false;
            }

            if (roleNames == null || roleNames.Length == 0)
            {
                return false;
            }

            var userRole = GetUserRole();
            if (string.IsNullOrWhiteSpace(userRole))
            {
                return false;
            }

            return roleNames.Any(role => string.Equals(role, userRole, StringComparison.OrdinalIgnoreCase));
        }

        public List<string> GetUserPermissions()
        {
            if (!_authService.IsAuthenticated || _authService.CurrentUser == null)
            {
                return new List<string>();
            }

            var userRole = _authService.CurrentUser.user_role;
            if (string.IsNullOrWhiteSpace(userRole))
            {
                return new List<string>();
            }

            return _rolePermissionService.GetPermissionsForRole(userRole);
        }

        public string? GetUserRole()
        {
            if (!_authService.IsAuthenticated || _authService.CurrentUser == null)
            {
                return null;
            }

            return _authService.CurrentUser.user_role;
        }

        public bool UserHasPermission(User user, string permission)
        {
            if (user == null || string.IsNullOrWhiteSpace(permission))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(user.user_role))
            {
                return false;
            }

            return _rolePermissionService.RoleHasPermission(user.user_role, permission);
        }

        public bool UserHasRole(User user, string roleName)
        {
            if (user == null || string.IsNullOrWhiteSpace(roleName))
            {
                return false;
            }

            return string.Equals(user.user_role, roleName, StringComparison.OrdinalIgnoreCase);
        }
    }
}

