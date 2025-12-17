namespace BrightEnroll_DES.Services.RoleBase
{
    // Defines all available permissions in the system
    public static class Permissions
    {
        // Dashboard
        public const string ViewDashboard = "view_dashboard";

        // Enrollment
        public const string ViewEnrollment = "view_enrollment";
        public const string CreateEnrollment = "create_enrollment";
        public const string EditEnrollment = "edit_enrollment";
        public const string DeleteEnrollment = "delete_enrollment";
        public const string ProcessReEnrollment = "process_re_enrollment";

        // Student Records
        public const string ViewStudentRecord = "view_student_record";
        public const string CreateStudentRecord = "create_student_record";
        public const string EditStudentRecord = "edit_student_record";
        public const string DeleteStudentRecord = "delete_student_record";
        public const string ViewAcademicRecord = "view_academic_record";
        
        // Student Registration
        public const string CreateStudentRegistration = "create_student_registration";

        // Curriculum Management
        public const string ViewCurriculum = "view_curriculum";
        public const string CreateCurriculum = "create_curriculum";
        public const string EditCurriculum = "edit_curriculum";
        public const string DeleteCurriculum = "delete_curriculum";
        public const string ManageSections = "manage_sections";
        public const string ManageSubjects = "manage_subjects";
        public const string ManageClassrooms = "manage_classrooms";
        public const string AssignTeachers = "assign_teachers";

        // Finance
        public const string ViewFinance = "view_finance";
        public const string CreateFee = "create_fee";
        public const string EditFee = "edit_fee";
        public const string DeleteFee = "delete_fee";
        public const string ProcessPayment = "process_payment";
        public const string ViewPaymentRecords = "view_payment_records";
        public const string ManageExpenses = "manage_expenses";
        public const string ViewFinancialReports = "view_financial_reports";

        // Human Resource
        public const string ViewHR = "view_hr";
        public const string CreateEmployee = "create_employee";
        public const string EditEmployee = "edit_employee";
        public const string DeleteEmployee = "delete_employee";
        public const string ViewEmployeeProfile = "view_employee_profile";
        public const string ManageEmployeeData = "manage_employee_data";

        // Payroll
        public const string ViewPayroll = "view_payroll";
        public const string CreatePayroll = "create_payroll";
        public const string EditPayroll = "edit_payroll";
        public const string DeletePayroll = "delete_payroll";
        public const string GeneratePayslip = "generate_payslip";
        public const string ManageRoles = "manage_roles";

        // Archive
        public const string ViewArchive = "view_archive";
        public const string ArchiveStudent = "archive_student";
        public const string ArchiveEmployee = "archive_employee";
        public const string RestoreArchived = "restore_archived";

        // Audit Log
        public const string ViewAuditLog = "view_audit_log";

        // Cloud Management
        public const string ViewCloudManagement = "view_cloud_management";
        public const string SyncData = "sync_data";
        public const string ManageCloudSettings = "manage_cloud_settings";

        // Settings
        public const string ViewSettings = "view_settings";
        public const string EditSettings = "edit_settings";
        public const string ManageSystemSettings = "manage_system_settings";

        // Profile
        public const string ViewProfile = "view_profile";
        public const string EditProfile = "edit_profile";

        // Attendance Monitoring
        public const string ViewAttendance = "view_attendance";
        public const string RecordAttendance = "record_attendance";
        public const string EditAttendance = "edit_attendance";
        public const string ViewAttendanceReports = "view_attendance_reports";

        // Gradebook / Grading
        public const string ViewGradebook = "view_gradebook";
        public const string EnterGrades = "enter_grades";
        public const string EditGrades = "edit_grades";
        public const string ComputeGrades = "compute_grades";
        public const string GenerateReportCard = "generate_report_card";

        // Reporting / Analytics
        public const string ViewReports = "view_reports";
        public const string GenerateReports = "generate_reports";
        public const string ViewAnalytics = "view_analytics";
        public const string ExportReports = "export_reports";

        // Inventory / Asset Management
        public const string ViewInventory = "view_inventory";
        public const string CreateInventory = "create_inventory";
        public const string EditInventory = "edit_inventory";
        public const string DeleteInventory = "delete_inventory";
        public const string ManageAssets = "manage_assets";

        // Get all available permissions as a list
        public static List<string> GetAllPermissions()
        {
            return new List<string>
            {
                ViewDashboard,
                ViewEnrollment, CreateEnrollment, EditEnrollment, DeleteEnrollment, ProcessReEnrollment,
                ViewStudentRecord, CreateStudentRecord, EditStudentRecord, DeleteStudentRecord, ViewAcademicRecord,
                CreateStudentRegistration,
                ViewCurriculum, CreateCurriculum, EditCurriculum, DeleteCurriculum, ManageSections, ManageSubjects, ManageClassrooms, AssignTeachers,
                ViewFinance, CreateFee, EditFee, DeleteFee, ProcessPayment, ViewPaymentRecords, ManageExpenses, ViewFinancialReports,
                ViewHR, CreateEmployee, EditEmployee, DeleteEmployee, ViewEmployeeProfile, ManageEmployeeData,
                ViewPayroll, CreatePayroll, EditPayroll, DeletePayroll, GeneratePayslip, ManageRoles,
                ViewArchive, ArchiveStudent, ArchiveEmployee, RestoreArchived,
                ViewAuditLog,
                ViewCloudManagement, SyncData, ManageCloudSettings,
                ViewSettings, EditSettings, ManageSystemSettings,
                ViewProfile, EditProfile,
                ViewAttendance, RecordAttendance, EditAttendance, ViewAttendanceReports,
                ViewGradebook, EnterGrades, EditGrades, ComputeGrades, GenerateReportCard,
                ViewReports, GenerateReports, ViewAnalytics, ExportReports,
                ViewInventory, CreateInventory, EditInventory, DeleteInventory, ManageAssets
            };
        }
    }
}

