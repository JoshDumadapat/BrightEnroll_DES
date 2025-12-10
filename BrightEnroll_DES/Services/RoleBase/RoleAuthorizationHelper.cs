using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.Authentication;

namespace BrightEnroll_DES.Services.RoleBase
{
    // Helper class for role and permission checking
    public static class RoleAuthorizationHelper
    {
        // Checks if a user has a specific permission
        public static bool UserHasPermission(User? user, IRolePermissionService rolePermissionService, string permission)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.user_role) || rolePermissionService == null)
            {
                return false;
            }

            return rolePermissionService.RoleHasPermission(user.user_role, permission);
        }

        // Checks if a user has a specific role
        public static bool UserHasRole(User? user, string roleName)
        {
            if (user == null || string.IsNullOrWhiteSpace(roleName))
            {
                return false;
            }

            return string.Equals(user.user_role, roleName, StringComparison.OrdinalIgnoreCase);
        }

        // Checks if a user has any of the specified roles
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

        // Gets all permissions for a user
        public static List<string> GetUserPermissions(User? user, IRolePermissionService rolePermissionService)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.user_role) || rolePermissionService == null)
            {
                return new List<string>();
            }

            return rolePermissionService.GetPermissionsForRole(user.user_role);
        }

        // Checks if the current authenticated user has permission
        public static bool CurrentUserHasPermission(IAuthService authService, IAuthorizationService authorizationService, string permission)
        {
            if (authService == null || authorizationService == null)
            {
                return false;
            }

            return authorizationService.HasPermission(permission);
        }

        // Checks if the current authenticated user has role
        public static bool CurrentUserHasRole(IAuthService authService, IAuthorizationService authorizationService, string roleName)
        {
            if (authService == null || authorizationService == null)
            {
                return false;
            }

            return authorizationService.HasRole(roleName);
        }

        // Gets a display-friendly role name
        public static string GetRoleDisplayName(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return "Unknown";
            }

            return roleName switch
            {
                "SuperAdmin" => "Super Administrator",
                "Admin" => "Administrator",
                "HR" => "Human Resources",
                "Registrar" => "Registrar",
                "Cashier" => "Cashier",
                "Teacher" => "Teacher",
                "Janitor" => "Janitor",
                "Other" => "Other",
                _ => roleName
            };
        }

        // Gets all available roles in the system
        public static List<string> GetAllRoles()
        {
            return new List<string>
            {
                "SuperAdmin",
                "Admin",
                "HR",
                "Registrar",
                "Cashier",
                "Teacher"
            };
        }
    }
}

