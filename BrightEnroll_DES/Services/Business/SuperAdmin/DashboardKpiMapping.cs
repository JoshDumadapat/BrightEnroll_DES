using BrightEnroll_DES.Services.RoleBase;

namespace BrightEnroll_DES.Services.Business.SuperAdmin
{
    /// <summary>
    /// Maps dashboard KPIs to their required modules and permissions
    /// </summary>
    public static class DashboardKpiMapping
    {
        public class KpiDefinition
        {
            public string KpiId { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string RequiredModulePackageId { get; set; } = string.Empty;
            public List<string> RequiredPermissions { get; set; } = new();
            public bool IsCore { get; set; } = false; // Core KPIs are always shown
        }

        /// <summary>
        /// Get all KPI definitions
        /// </summary>
        public static List<KpiDefinition> GetAllKpis()
        {
            return new List<KpiDefinition>
            {
                // Core KPIs (always shown)
                new KpiDefinition
                {
                    KpiId = "dashboard_overview",
                    DisplayName = "Dashboard Overview",
                    RequiredModulePackageId = "core",
                    RequiredPermissions = new List<string> { BrightEnroll_DES.Services.RoleBase.Permissions.ViewDashboard },
                    IsCore = true
                },

                // Enrollment Module KPIs
                new KpiDefinition
                {
                    KpiId = "total_enrolled",
                    DisplayName = "Total Enrolled",
                    RequiredModulePackageId = "enrollment",
                    RequiredPermissions = new List<string> { BrightEnroll_DES.Services.RoleBase.Permissions.ViewEnrollment, BrightEnroll_DES.Services.RoleBase.Permissions.ViewStudentRecord }
                },
                new KpiDefinition
                {
                    KpiId = "pending_applications",
                    DisplayName = "Pending Applications",
                    RequiredModulePackageId = "enrollment",
                    RequiredPermissions = new List<string> { BrightEnroll_DES.Services.RoleBase.Permissions.ViewEnrollment, BrightEnroll_DES.Services.RoleBase.Permissions.CreateStudentRegistration }
                },
                new KpiDefinition
                {
                    KpiId = "student_type_breakdown",
                    DisplayName = "Student Type Breakdown",
                    RequiredModulePackageId = "enrollment",
                    RequiredPermissions = new List<string> { BrightEnroll_DES.Services.RoleBase.Permissions.ViewStudentRecord }
                },
                new KpiDefinition
                {
                    KpiId = "enrollment_by_grade",
                    DisplayName = "Enrollment by Grade",
                    RequiredModulePackageId = "enrollment",
                    RequiredPermissions = new List<string> { BrightEnroll_DES.Services.RoleBase.Permissions.ViewEnrollment, BrightEnroll_DES.Services.RoleBase.Permissions.ViewCurriculum }
                },

                // HR/Payroll Module KPIs
                new KpiDefinition
                {
                    KpiId = "total_employees",
                    DisplayName = "Total Employees",
                    RequiredModulePackageId = "hr-payroll",
                    RequiredPermissions = new List<string> { BrightEnroll_DES.Services.RoleBase.Permissions.ViewHR, BrightEnroll_DES.Services.RoleBase.Permissions.ViewEmployeeProfile }
                },
                new KpiDefinition
                {
                    KpiId = "payroll_summary",
                    DisplayName = "Payroll Summary",
                    RequiredModulePackageId = "hr-payroll",
                    RequiredPermissions = new List<string> { BrightEnroll_DES.Services.RoleBase.Permissions.ViewHR, BrightEnroll_DES.Services.RoleBase.Permissions.ViewPayroll }
                },

                // Finance Module KPIs
                new KpiDefinition
                {
                    KpiId = "total_tuition_collected",
                    DisplayName = "Total Tuition Collected",
                    RequiredModulePackageId = "finance",
                    RequiredPermissions = new List<string> { BrightEnroll_DES.Services.RoleBase.Permissions.ViewFinance, BrightEnroll_DES.Services.RoleBase.Permissions.ViewPaymentRecords }
                },
                new KpiDefinition
                {
                    KpiId = "total_income",
                    DisplayName = "Total Income",
                    RequiredModulePackageId = "finance",
                    RequiredPermissions = new List<string> { BrightEnroll_DES.Services.RoleBase.Permissions.ViewFinance, BrightEnroll_DES.Services.RoleBase.Permissions.ViewFinancialReports }
                },
                new KpiDefinition
                {
                    KpiId = "expenses",
                    DisplayName = "Expenses",
                    RequiredModulePackageId = "finance",
                    RequiredPermissions = new List<string> { BrightEnroll_DES.Services.RoleBase.Permissions.ViewFinance, BrightEnroll_DES.Services.RoleBase.Permissions.ManageExpenses }
                },
                new KpiDefinition
                {
                    KpiId = "tuition_fees_by_grade",
                    DisplayName = "Tuition Fees by Grade",
                    RequiredModulePackageId = "finance",
                    RequiredPermissions = new List<string> { BrightEnroll_DES.Services.RoleBase.Permissions.ViewFinance }
                }
            };
        }

        /// <summary>
        /// Get KPIs that should be shown based on tenant capabilities
        /// </summary>
        public static List<KpiDefinition> GetEnabledKpis(TenantCapabilities capabilities)
        {
            var allKpis = GetAllKpis();
            var enabledKpis = new List<KpiDefinition>();

            foreach (var kpi in allKpis)
            {
                // Core KPIs are always shown
                if (kpi.IsCore)
                {
                    enabledKpis.Add(kpi);
                    continue;
                }

                // Check if required module is enabled
                if (!capabilities.HasModule(kpi.RequiredModulePackageId))
                {
                    continue; // Skip this KPI
                }

                // Check if all required permissions are granted
                bool hasAllPermissions = true;
                foreach (var permission in kpi.RequiredPermissions)
                {
                    if (!capabilities.HasPermission(permission))
                    {
                        hasAllPermissions = false;
                        break;
                    }
                }

                if (hasAllPermissions)
                {
                    enabledKpis.Add(kpi);
                }
            }

            return enabledKpis;
        }

        /// <summary>
        /// Check if a specific KPI should be shown
        /// </summary>
        public static bool ShouldShowKpi(string kpiId, TenantCapabilities capabilities)
        {
            var kpi = GetAllKpis().FirstOrDefault(k => k.KpiId == kpiId);
            if (kpi == null)
            {
                return false; // Unknown KPI
            }

            // Core KPIs are always shown
            if (kpi.IsCore)
            {
                return true;
            }

            // Check module and permissions
            if (!capabilities.HasModule(kpi.RequiredModulePackageId))
            {
                return false;
            }

            foreach (var permission in kpi.RequiredPermissions)
            {
                if (!capabilities.HasPermission(permission))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
