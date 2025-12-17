using BrightEnroll_DES.Services.RoleBase;
using RoleBasePermissions = BrightEnroll_DES.Services.RoleBase.Permissions;

namespace BrightEnroll_DES.Services.Business.SuperAdmin
{
    /// <summary>
    /// Defines module packages that group related features together
    /// </summary>
    public static class ModulePackages
    {
        public class ModulePackage
        {
            public string PackageId { get; set; } = string.Empty;
            public string PackageName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public List<string> Permissions { get; set; } = new();
            public bool IsRequired { get; set; } = false; // Some packages might be required
        }

        /// <summary>
        /// Enrollment Package - Complete enrollment system with curriculum, payment, and teacher portal
        /// Based on menu: Enrollment + Student Record + Student Registration + Curriculum + Finance (payment/records) + Teacher Portal + Reports
        /// </summary>
        public static ModulePackage EnrollmentPackage => new ModulePackage
        {
            PackageId = "enrollment",
            PackageName = "Enrollment & Academic Management",
            Description = "Complete enrollment system: Enrollment, Student Records, Student Registration, Curriculum, Payment & Records in Finance, Teacher Portal (My Classes, Gradebook), and Reports",
            Permissions = new List<string>
            {
                // Enrollment
                RoleBasePermissions.ViewEnrollment,
                RoleBasePermissions.CreateEnrollment,
                RoleBasePermissions.EditEnrollment,
                RoleBasePermissions.DeleteEnrollment,
                RoleBasePermissions.ProcessReEnrollment,
                
                // Student Records
                RoleBasePermissions.ViewStudentRecord,
                RoleBasePermissions.CreateStudentRecord,
                RoleBasePermissions.EditStudentRecord,
                RoleBasePermissions.DeleteStudentRecord,
                RoleBasePermissions.ViewAcademicRecord,
                
                // Student Registration
                RoleBasePermissions.CreateStudentRegistration,
                
                // Curriculum
                RoleBasePermissions.ViewCurriculum,
                RoleBasePermissions.CreateCurriculum,
                RoleBasePermissions.EditCurriculum,
                RoleBasePermissions.DeleteCurriculum,
                RoleBasePermissions.ManageSections,
                RoleBasePermissions.ManageSubjects,
                RoleBasePermissions.ManageClassrooms,
                RoleBasePermissions.AssignTeachers,
                
                // Teacher Portal (My Classes & Gradebook)
                RoleBasePermissions.ViewGradebook,
                RoleBasePermissions.EnterGrades,
                RoleBasePermissions.EditGrades,
                RoleBasePermissions.ComputeGrades,
                RoleBasePermissions.GenerateReportCard,
                
                // Finance - Payment & Records Only (not full finance management)
                RoleBasePermissions.ViewFinance,
                RoleBasePermissions.ProcessPayment,
                RoleBasePermissions.ViewPaymentRecords,
                
                // Reports (for enrollment-related)
                RoleBasePermissions.ViewReports,
                RoleBasePermissions.GenerateReports,
                RoleBasePermissions.ViewAnalytics,
                RoleBasePermissions.ExportReports
            },
            IsRequired = false
        };

        /// <summary>
        /// HR & Payroll Package - Complete HR and Payroll management (includes attendance as part of HR)
        /// Based on menu: Human Resource + Payroll
        /// </summary>
        public static ModulePackage HRPayrollPackage => new ModulePackage
        {
            PackageId = "hr_payroll",
            PackageName = "HR & Payroll Management",
            Description = "Complete HR and Payroll system: Employee management, Payroll processing, Payslip generation, Attendance (part of HR), and Reports",
            Permissions = new List<string>
            {
                // HR Management
                RoleBasePermissions.ViewHR,
                RoleBasePermissions.CreateEmployee,
                RoleBasePermissions.EditEmployee,
                RoleBasePermissions.DeleteEmployee,
                RoleBasePermissions.ViewEmployeeProfile,
                RoleBasePermissions.ManageEmployeeData,
                
                // Payroll
                RoleBasePermissions.ViewPayroll,
                RoleBasePermissions.CreatePayroll,
                RoleBasePermissions.EditPayroll,
                RoleBasePermissions.DeletePayroll,
                RoleBasePermissions.GeneratePayslip,
                RoleBasePermissions.ManageRoles,
                
                // Attendance (part of HR, not separate)
                RoleBasePermissions.ViewAttendance,
                RoleBasePermissions.RecordAttendance,
                RoleBasePermissions.EditAttendance,
                RoleBasePermissions.ViewAttendanceReports,
                
                // Reports
                RoleBasePermissions.ViewReports,
                RoleBasePermissions.GenerateReports,
                RoleBasePermissions.ExportReports
            },
            IsRequired = false
        };

        /// <summary>
        /// Finance Package - Complete financial management (full finance, not just payment records)
        /// Based on menu: Finance
        /// </summary>
        public static ModulePackage FinancePackage => new ModulePackage
        {
            PackageId = "finance",
            PackageName = "Finance Management",
            Description = "Complete financial management: Fee setup, Payment processing, Payment records, Expense management, and Financial reports",
            Permissions = new List<string>
            {
                RoleBasePermissions.ViewFinance,
                RoleBasePermissions.CreateFee,
                RoleBasePermissions.EditFee,
                RoleBasePermissions.DeleteFee,
                RoleBasePermissions.ProcessPayment,
                RoleBasePermissions.ViewPaymentRecords,
                RoleBasePermissions.ManageExpenses,
                RoleBasePermissions.ViewFinancialReports,
                RoleBasePermissions.ViewReports,
                RoleBasePermissions.GenerateReports,
                RoleBasePermissions.ExportReports
            },
            IsRequired = false
        };

        /// <summary>
        /// Inventory Package - Inventory and asset management
        /// Based on menu: Inventory
        /// </summary>
        public static ModulePackage InventoryPackage => new ModulePackage
        {
            PackageId = "inventory",
            PackageName = "Inventory & Asset Management",
            Description = "Manage school inventory, assets, and equipment with reporting",
            Permissions = new List<string>
            {
                RoleBasePermissions.ViewInventory,
                RoleBasePermissions.CreateInventory,
                RoleBasePermissions.EditInventory,
                RoleBasePermissions.DeleteInventory,
                RoleBasePermissions.ManageAssets,
                RoleBasePermissions.ViewReports,
                RoleBasePermissions.GenerateReports
            },
            IsRequired = false
        };

        /// <summary>
        /// Core Package - Basic features that should always be included
        /// Based on menu: Dashboard, Settings, Archive, Audit Log, Cloud Management
        /// </summary>
        public static ModulePackage CorePackage => new ModulePackage
        {
            PackageId = "core",
            PackageName = "Core Features",
            Description = "Essential system features: Dashboard, Settings, Archive, Audit Log, and Cloud Management",
            Permissions = new List<string>
            {
                RoleBasePermissions.ViewDashboard,
                RoleBasePermissions.ViewSettings,
                RoleBasePermissions.EditSettings,
                RoleBasePermissions.ManageSystemSettings,
                RoleBasePermissions.ViewProfile,
                RoleBasePermissions.EditProfile,
                RoleBasePermissions.ViewArchive,
                RoleBasePermissions.ArchiveStudent,
                RoleBasePermissions.ArchiveEmployee,
                RoleBasePermissions.RestoreArchived,
                RoleBasePermissions.ViewAuditLog,
                RoleBasePermissions.ViewCloudManagement,
                RoleBasePermissions.SyncData,
                RoleBasePermissions.ManageCloudSettings
            },
            IsRequired = true // Core package is always included
        };

        /// <summary>
        /// Get all available module packages
        /// </summary>
        public static List<ModulePackage> GetAllPackages()
        {
            return new List<ModulePackage>
            {
                CorePackage,
                EnrollmentPackage,
                HRPayrollPackage,
                FinancePackage,
                InventoryPackage
            };
        }

        /// <summary>
        /// Get a package by its ID
        /// </summary>
        public static ModulePackage? GetPackageById(string packageId)
        {
            return GetAllPackages().FirstOrDefault(p => p.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get all permissions from selected packages
        /// </summary>
        public static List<string> GetPermissionsFromPackages(List<string> selectedPackageIds)
        {
            var allPermissions = new HashSet<string>();
            
            // Always include core package
            allPermissions.UnionWith(CorePackage.Permissions);
            
            // Add permissions from selected packages
            foreach (var packageId in selectedPackageIds)
            {
                var package = GetPackageById(packageId);
                if (package != null)
                {
                    allPermissions.UnionWith(package.Permissions);
                }
            }
            
            return allPermissions.ToList();
        }
    }
}
