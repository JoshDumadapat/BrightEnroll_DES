namespace BrightEnroll_DES.Services.RoleBase
{
    // Defines role-to-permission mappings
    public interface IRolePermissionService
    {
        // Gets all permissions for a role
        List<string> GetPermissionsForRole(string roleName);

        // Checks if a role has a specific permission
        bool RoleHasPermission(string roleName, string permission);

        // Gets all roles that have a specific permission
        List<string> GetRolesWithPermission(string permission);
    }

    public class RolePermissionService : IRolePermissionService
    {
        private readonly Dictionary<string, List<string>> _rolePermissions;

        public RolePermissionService()
        {
            _rolePermissions = InitializeRolePermissions();
        }

        // Initializes role-to-permission mappings
        private Dictionary<string, List<string>> InitializeRolePermissions()
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                // SuperAdmin - Full system access to everything
                ["SuperAdmin"] = new List<string>
                {
                    Permissions.ViewDashboard,
                    Permissions.ViewEnrollment, Permissions.CreateEnrollment, Permissions.EditEnrollment, Permissions.DeleteEnrollment, Permissions.ProcessReEnrollment,
                    Permissions.ViewStudentRecord, Permissions.CreateStudentRecord, Permissions.EditStudentRecord, Permissions.DeleteStudentRecord, Permissions.ViewAcademicRecord,
                    Permissions.CreateStudentRegistration,
                    Permissions.ViewCurriculum, Permissions.CreateCurriculum, Permissions.EditCurriculum, Permissions.DeleteCurriculum, Permissions.ManageSections, Permissions.ManageSubjects, Permissions.ManageClassrooms, Permissions.AssignTeachers,
                    Permissions.ViewFinance, Permissions.CreateFee, Permissions.EditFee, Permissions.DeleteFee, Permissions.ProcessPayment, Permissions.ViewPaymentRecords, Permissions.ManageExpenses, Permissions.ViewFinancialReports,
                    Permissions.ViewHR, Permissions.CreateEmployee, Permissions.EditEmployee, Permissions.DeleteEmployee, Permissions.ViewEmployeeProfile, Permissions.ManageEmployeeData,
                    Permissions.ViewPayroll, Permissions.CreatePayroll, Permissions.EditPayroll, Permissions.DeletePayroll, Permissions.GeneratePayslip, Permissions.ManageRoles,
                    Permissions.ViewArchive, Permissions.ArchiveStudent, Permissions.ArchiveEmployee, Permissions.RestoreArchived,
                    Permissions.ViewAuditLog,
                    Permissions.ViewCloudManagement, Permissions.SyncData, Permissions.ManageCloudSettings,
                    Permissions.ViewSettings, Permissions.EditSettings, Permissions.ManageSystemSettings,
                    Permissions.ViewProfile, Permissions.EditProfile,
                    // Added: New module permissions (Attendance and Gradebook are Teacher-only)
                    Permissions.ViewReports, Permissions.GenerateReports, Permissions.ViewAnalytics, Permissions.ExportReports,
                    Permissions.ViewInventory, Permissions.CreateInventory, Permissions.EditInventory, Permissions.DeleteInventory, Permissions.ManageAssets
                },

                // Admin - Full system access (Attendance and Gradebook are Teacher-only)
                ["Admin"] = new List<string>
                {
                    Permissions.ViewDashboard,
                    Permissions.ViewEnrollment, Permissions.CreateEnrollment, Permissions.EditEnrollment, Permissions.DeleteEnrollment, Permissions.ProcessReEnrollment,
                    Permissions.ViewStudentRecord, Permissions.CreateStudentRecord, Permissions.EditStudentRecord, Permissions.DeleteStudentRecord, Permissions.ViewAcademicRecord,
                    Permissions.CreateStudentRegistration,
                    Permissions.ViewCurriculum, Permissions.CreateCurriculum, Permissions.EditCurriculum, Permissions.DeleteCurriculum, Permissions.ManageSections, Permissions.ManageSubjects, Permissions.ManageClassrooms, Permissions.AssignTeachers,
                    Permissions.ViewFinance, Permissions.CreateFee, Permissions.EditFee, Permissions.DeleteFee, Permissions.ProcessPayment, Permissions.ViewPaymentRecords, Permissions.ManageExpenses, Permissions.ViewFinancialReports,
                    Permissions.ViewHR, Permissions.CreateEmployee, Permissions.EditEmployee, Permissions.DeleteEmployee, Permissions.ViewEmployeeProfile, Permissions.ManageEmployeeData,
                    Permissions.ViewPayroll, Permissions.CreatePayroll, Permissions.EditPayroll, Permissions.DeletePayroll, Permissions.GeneratePayslip, Permissions.ManageRoles,
                    Permissions.ViewArchive, Permissions.ArchiveStudent, Permissions.ArchiveEmployee, Permissions.RestoreArchived,
                    Permissions.ViewAuditLog,
                    Permissions.ViewCloudManagement, Permissions.SyncData, Permissions.ManageCloudSettings,
                    Permissions.ViewSettings, Permissions.EditSettings, Permissions.ManageSystemSettings,
                    Permissions.ViewProfile, Permissions.EditProfile,
                    // Added: New module permissions (Attendance and Gradebook are Teacher-only)
                    Permissions.ViewReports, Permissions.GenerateReports, Permissions.ViewAnalytics, Permissions.ExportReports,
                    Permissions.ViewInventory, Permissions.CreateInventory, Permissions.EditInventory, Permissions.DeleteInventory, Permissions.ManageAssets
                },

                // HR - Human resources management
                ["HR"] = new List<string>
                {
                    Permissions.ViewDashboard,
                    Permissions.ViewHR, Permissions.CreateEmployee, Permissions.EditEmployee, Permissions.ViewEmployeeProfile, Permissions.ManageEmployeeData,
                    // Removed: DeleteEmployee - HR should only archive, not delete
                    Permissions.ViewPayroll, Permissions.GeneratePayslip,
                    Permissions.ViewArchive, Permissions.ArchiveEmployee, Permissions.RestoreArchived,
                    Permissions.ViewProfile, Permissions.EditProfile
                },

                // Registrar - Student registration and records
                ["Registrar"] = new List<string>
                {
                    Permissions.ViewDashboard,
                    Permissions.ViewEnrollment, Permissions.CreateEnrollment, Permissions.EditEnrollment, Permissions.ProcessReEnrollment,
                    // Removed: DeleteEnrollment - Registrar should only archive, not delete
                    Permissions.ViewStudentRecord, Permissions.CreateStudentRecord, Permissions.EditStudentRecord, Permissions.ViewAcademicRecord,
                    // Removed: DeleteStudentRecord - Registrar should only archive, not delete
                    Permissions.CreateStudentRegistration, // Student Registration in sidebar
                    Permissions.ViewCurriculum, // To see sections/subjects for enrollment (view only)
                    // Removed: ViewFinance - Registrar should not access finance module
                    Permissions.ViewArchive, // To view archived students
                    Permissions.ViewProfile, Permissions.EditProfile
                },

                // Cashier - Payment processing only (no fee setup or expenses)
                ["Cashier"] = new List<string>
                {
                    Permissions.ViewDashboard,
                    Permissions.ViewFinance, Permissions.ProcessPayment, Permissions.ViewPaymentRecords,
                    // Removed: CreateFee, EditFee, DeleteFee, ManageExpenses - Cashier should only process payments
                    Permissions.ViewEnrollment, // To see enrollment status for payment
                    Permissions.ViewStudentRecord, // To view student info for payment
                    Permissions.ViewProfile, Permissions.EditProfile
                },

                // Teacher - Teaching functions
                ["Teacher"] = new List<string>
                {
                    Permissions.ViewDashboard,
                    Permissions.ViewStudentRecord, Permissions.ViewAcademicRecord,
                    // Added: Edit access for academic records (limited to assigned students)
                    Permissions.ViewCurriculum, // To view assigned classes
                    // Added: Attendance and Grading permissions
                    Permissions.ViewAttendance, Permissions.RecordAttendance, Permissions.EditAttendance, Permissions.ViewAttendanceReports,
                    Permissions.ViewGradebook, Permissions.EnterGrades, Permissions.EditGrades, Permissions.ComputeGrades, Permissions.GenerateReportCard,
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

