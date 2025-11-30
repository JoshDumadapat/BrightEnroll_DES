using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.Authentication;

namespace BrightEnroll_DES.Services.RoleBase
{
    /// <summary>
    /// Helper class providing static methods for easy role and permission checking.
    /// Can be used in Razor components and other parts of the application.
    /// </summary>
    public static class RoleAuthorizationHelper
    {
        /// <summary>
        /// Checks if a user has a specific permission.
        /// </summary>
        public static bool UserHasPermission(User? user, IRolePermissionService rolePermissionService, string permission)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.user_role) || rolePermissionService == null)
            {
                return false;
            }

            return rolePermissionService.RoleHasPermission(user.user_role, permission);
        }

        /// <summary>
        /// Checks if a user has a specific role.
        /// </summary>
        public static bool UserHasRole(User? user, string roleName)
        {
            if (user == null || string.IsNullOrWhiteSpace(roleName))
            {
                return false;
            }

            return string.Equals(user.user_role, roleName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if a user has any of the specified roles.
        /// </summary>
        public static bool UserHasAnyRole(User? user, params string[] roleNames)
        {
            if (user == null || roleNames == null || roleNames.Length == 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(user.user_role))
            {
                return false;
            }

            return roleNames.Any(role => string.Equals(role, user.user_role, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all permissions for a user.
        /// </summary>
        public static List<string> GetUserPermissions(User? user, IRolePermissionService rolePermissionService)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.user_role) || rolePermissionService == null)
            {
                return new List<string>();
            }

            return rolePermissionService.GetPermissionsForRole(user.user_role);
        }

        /// <summary>
        /// Checks if the current authenticated user has permission (requires IAuthService and IAuthorizationService).
        /// </summary>
        public static bool CurrentUserHasPermission(IAuthService authService, IAuthorizationService authorizationService, string permission)
        {
            if (authService == null || authorizationService == null)
            {
                return false;
            }

            return authorizationService.HasPermission(permission);
        }

        /// <summary>
        /// Checks if the current authenticated user has role (requires IAuthService and IAuthorizationService).
        /// </summary>
        public static bool CurrentUserHasRole(IAuthService authService, IAuthorizationService authorizationService, string roleName)
        {
            if (authService == null || authorizationService == null)
            {
                return false;
            }

            return authorizationService.HasRole(roleName);
        }

        /// <summary>
        /// Gets a display-friendly role name.
        /// </summary>
        public static string GetRoleDisplayName(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return "Unknown";
            }

            return roleName switch
            {
                "Admin" => "Administrator",
                "System Admin" => "System Administrator",
                "HR" => "Human Resources",
                "Registrar" => "Registrar",
                "Cashier" => "Cashier",
                "Teacher" => "Teacher",
                "Janitor" => "Janitor",
                "Other" => "Other",
                _ => roleName
            };
        }

        /// <summary>
        /// Gets all available roles in the system.
        /// </summary>
        public static List<string> GetAllRoles()
        {
            return new List<string>
            {
                "Admin",
                "System Admin",
                "HR",
                "Registrar",
                "Cashier",
                "Teacher",
                "Janitor",
                "Other"
            };
        }
    }
}

