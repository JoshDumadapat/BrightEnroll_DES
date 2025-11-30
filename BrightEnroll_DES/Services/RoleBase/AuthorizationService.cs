using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.Authentication;

namespace BrightEnroll_DES.Services.RoleBase
{
    /// <summary>
    /// Service for checking user authorization based on roles and permissions.
    /// </summary>
    public interface IAuthorizationService
    {
        /// <summary>
        /// Checks if the current user has a specific permission.
        /// </summary>
        bool HasPermission(string permission);

        /// <summary>
        /// Checks if the current user has any of the specified permissions.
        /// </summary>
        bool HasAnyPermission(params string[] permissions);

        /// <summary>
        /// Checks if the current user has all of the specified permissions.
        /// </summary>
        bool HasAllPermissions(params string[] permissions);

        /// <summary>
        /// Checks if the current user has a specific role.
        /// </summary>
        bool HasRole(string roleName);

        /// <summary>
        /// Checks if the current user has any of the specified roles.
        /// </summary>
        bool HasAnyRole(params string[] roleNames);

        /// <summary>
        /// Gets all permissions for the current user.
        /// </summary>
        List<string> GetUserPermissions();

        /// <summary>
        /// Gets the current user's role.
        /// </summary>
        string? GetUserRole();

        /// <summary>
        /// Checks if a specific user has a permission (for checking other users).
        /// </summary>
        bool UserHasPermission(User user, string permission);

        /// <summary>
        /// Checks if a specific user has a role (for checking other users).
        /// </summary>
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

