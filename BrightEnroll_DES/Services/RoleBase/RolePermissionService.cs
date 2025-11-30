namespace BrightEnroll_DES.Services.RoleBase
{
    /// <summary>
    /// Service that defines role-to-permission mappings.
    /// Maps each role to its allowed permissions based on the project flow.
    /// </summary>
    public interface IRolePermissionService
    {
        /// <summary>
        /// Gets all permissions for a specific role.
        /// </summary>
        List<string> GetPermissionsForRole(string roleName);

        /// <summary>
        /// Checks if a role has a specific permission.
        /// </summary>
        bool RoleHasPermission(string roleName, string permission);

        /// <summary>
        /// Gets all roles that have a specific permission.
        /// </summary>
        List<string> GetRolesWithPermission(string permission);
    }

    public class RolePermissionService : IRolePermissionService
    {
        private readonly Dictionary<string, List<string>> _rolePermissions;

        public RolePermissionService()
        {
            _rolePermissions = InitializeRolePermissions();
        }

        /// <summary>
        /// Initializes role-to-permission mappings based on the project flow.
        /// </summary>
        private Dictionary<string, List<string>> InitializeRolePermissions()
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                // Admin - Full system access
                ["Admin"] = new List<string>
                {
                    Permissions.ViewDashboard,
                    Permissions.ViewEnrollment, Permissions.CreateEnrollment, Permissions.EditEnrollment, Permissions.DeleteEnrollment, Permissions.ProcessReEnrollment,
                    Permissions.ViewStudentRecord, Permissions.CreateStudentRecord, Permissions.EditStudentRecord, Permissions.DeleteStudentRecord, Permissions.ViewAcademicRecord,
                    Permissions.ViewCurriculum, Permissions.CreateCurriculum, Permissions.EditCurriculum, Permissions.DeleteCurriculum, Permissions.ManageSections, Permissions.ManageSubjects, Permissions.ManageClassrooms, Permissions.AssignTeachers,
                    Permissions.ViewFinance, Permissions.CreateFee, Permissions.EditFee, Permissions.DeleteFee, Permissions.ProcessPayment, Permissions.ViewPaymentRecords, Permissions.ManageExpenses, Permissions.ViewFinancialReports,
                    Permissions.ViewHR, Permissions.CreateEmployee, Permissions.EditEmployee, Permissions.DeleteEmployee, Permissions.ViewEmployeeProfile, Permissions.ManageEmployeeData,
                    Permissions.ViewPayroll, Permissions.CreatePayroll, Permissions.EditPayroll, Permissions.DeletePayroll, Permissions.GeneratePayslip, Permissions.ManageRoles,
                    Permissions.ViewArchive, Permissions.ArchiveStudent, Permissions.ArchiveEmployee, Permissions.RestoreArchived,
                    Permissions.ViewAuditLog,
                    Permissions.ViewCloudManagement, Permissions.SyncData, Permissions.ManageCloudSettings,
                    Permissions.ViewSettings, Permissions.EditSettings, Permissions.ManageSystemSettings,
                    Permissions.ViewProfile, Permissions.EditProfile
                },

                // System Admin - System administration and full access
                ["System Admin"] = new List<string>
                {
                    Permissions.ViewDashboard,
                    Permissions.ViewEnrollment, Permissions.CreateEnrollment, Permissions.EditEnrollment, Permissions.DeleteEnrollment, Permissions.ProcessReEnrollment,
                    Permissions.ViewStudentRecord, Permissions.CreateStudentRecord, Permissions.EditStudentRecord, Permissions.DeleteStudentRecord, Permissions.ViewAcademicRecord,
                    Permissions.ViewCurriculum, Permissions.CreateCurriculum, Permissions.EditCurriculum, Permissions.DeleteCurriculum, Permissions.ManageSections, Permissions.ManageSubjects, Permissions.ManageClassrooms, Permissions.AssignTeachers,
                    Permissions.ViewFinance, Permissions.CreateFee, Permissions.EditFee, Permissions.DeleteFee, Permissions.ProcessPayment, Permissions.ViewPaymentRecords, Permissions.ManageExpenses, Permissions.ViewFinancialReports,
                    Permissions.ViewHR, Permissions.CreateEmployee, Permissions.EditEmployee, Permissions.DeleteEmployee, Permissions.ViewEmployeeProfile, Permissions.ManageEmployeeData,
                    Permissions.ViewPayroll, Permissions.CreatePayroll, Permissions.EditPayroll, Permissions.DeletePayroll, Permissions.GeneratePayslip, Permissions.ManageRoles,
                    Permissions.ViewArchive, Permissions.ArchiveStudent, Permissions.ArchiveEmployee, Permissions.RestoreArchived,
                    Permissions.ViewAuditLog,
                    Permissions.ViewCloudManagement, Permissions.SyncData, Permissions.ManageCloudSettings,
                    Permissions.ViewSettings, Permissions.EditSettings, Permissions.ManageSystemSettings,
                    Permissions.ViewProfile, Permissions.EditProfile
                },

                // HR - Human resources management
                ["HR"] = new List<string>
                {
                    Permissions.ViewDashboard,
                    Permissions.ViewHR, Permissions.CreateEmployee, Permissions.EditEmployee, Permissions.ViewEmployeeProfile, Permissions.ManageEmployeeData,
                    Permissions.ViewPayroll, Permissions.GeneratePayslip,
                    Permissions.ViewArchive, Permissions.ArchiveEmployee, Permissions.RestoreArchived,
                    Permissions.ViewProfile, Permissions.EditProfile
                },

                // Registrar - Student registration and records
                ["Registrar"] = new List<string>
                {
                    Permissions.ViewDashboard,
                    Permissions.ViewEnrollment, Permissions.CreateEnrollment, Permissions.EditEnrollment, Permissions.ProcessReEnrollment,
                    Permissions.ViewStudentRecord, Permissions.CreateStudentRecord, Permissions.EditStudentRecord, Permissions.ViewAcademicRecord,
                    Permissions.ViewCurriculum, Permissions.ViewProfile, Permissions.EditProfile
                },

                // Cashier - Payment processing
                ["Cashier"] = new List<string>
                {
                    Permissions.ViewDashboard,
                    Permissions.ViewFinance, Permissions.ProcessPayment, Permissions.ViewPaymentRecords,
                    Permissions.ViewEnrollment, // To see enrollment status for payment
                    Permissions.ViewStudentRecord, // To view student info for payment
                    Permissions.ViewProfile, Permissions.EditProfile
                },

                // Teacher - Teaching functions
                ["Teacher"] = new List<string>
                {
                    Permissions.ViewDashboard,
                    Permissions.ViewStudentRecord, Permissions.ViewAcademicRecord,
                    Permissions.ViewCurriculum, // To view assigned classes
                    Permissions.ViewProfile, Permissions.EditProfile
                },

                // Janitor - Limited access
                ["Janitor"] = new List<string>
                {
                    Permissions.ViewDashboard,
                    Permissions.ViewProfile, Permissions.EditProfile
                },

                // Other - Custom role with minimal access
                ["Other"] = new List<string>
                {
                    Permissions.ViewDashboard,
                    Permissions.ViewProfile, Permissions.EditProfile
                }
            };
        }

        public List<string> GetPermissionsForRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return new List<string>();
            }

            return _rolePermissions.TryGetValue(roleName, out var permissions)
                ? new List<string>(permissions)
                : new List<string>();
        }

        public bool RoleHasPermission(string roleName, string permission)
        {
            if (string.IsNullOrWhiteSpace(roleName) || string.IsNullOrWhiteSpace(permission))
            {
                return false;
            }

            var permissions = GetPermissionsForRole(roleName);
            return permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
        }

        public List<string> GetRolesWithPermission(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
            {
                return new List<string>();
            }

            var roles = new List<string>();
            foreach (var rolePermission in _rolePermissions)
            {
                if (rolePermission.Value.Contains(permission, StringComparer.OrdinalIgnoreCase))
                {
                    roles.Add(rolePermission.Key);
                }
            }

            return roles;
        }
    }
}

