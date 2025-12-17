using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.Authentication;

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

        public AuthorizationService(IAuthService authService, IRolePermissionService rolePermissionService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _rolePermissionService = rolePermissionService ?? throw new ArgumentNullException(nameof(rolePermissionService));
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

            return UserHasPermission(_authService.CurrentUser, permission);
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

