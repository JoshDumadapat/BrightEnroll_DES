# Role Permissions Summary - Quick Reference

## Role Access Matrix

| Module | SuperAdmin | Admin | Registrar | Cashier | Teacher | HR | Janitor | Other |
|--------|-----------|-------|-----------|---------|---------|----|---------|-------|
| **Dashboard** | ✅ Full | ✅ Full | ✅ View | ✅ View | ✅ View | ✅ View | ✅ View | ✅ View |
| **Enrollment** | ✅ Full | ✅ Full | ✅ Full* | ✅ View | ❌ | ❌ | ❌ | ❌ |
| **Student Record** | ✅ Full | ✅ Full | ✅ Full* | ✅ View | ✅ View** | ❌ | ❌ | ❌ |
| **Academic Record** | ✅ Full | ✅ Full | ✅ View | ❌ | ✅ Edit** | ❌ | ❌ | ❌ |
| **Curriculum** | ✅ Full | ✅ Full | ✅ View | ❌ | ✅ View** | ❌ | ❌ | ❌ |
| **Finance** | ✅ Full | ✅ Full | ✅ View | ✅ Payment*** | ❌ | ❌ | ❌ | ❌ |
| **HR** | ✅ Full | ✅ Full | ❌ | ❌ | ❌ | ✅ Full* | ❌ | ❌ |
| **Payroll** | ✅ Full | ✅ Full | ❌ | ❌ | ❌ | ✅ View | ❌ | ❌ |
| **Archive** | ✅ Full | ✅ Full | ✅ View | ❌ | ❌ | ✅ View | ❌ | ❌ |
| **Audit Log** | ✅ Full | ✅ Full | ✅ View | ❌ | ❌ | ❌ | ❌ | ❌ |
| **Cloud Management** | ✅ Full | ✅ Full | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **Settings** | ✅ Full | ✅ Full | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **Profile** | ✅ Full | ✅ Full | ✅ Edit | ✅ Edit | ✅ Edit | ✅ Edit | ✅ Edit | ✅ Edit |

### Legend:
- ✅ Full = Full access (view, create, edit, delete)
- ✅ View = View only access
- ✅ Edit = View and edit access
- ✅ Payment*** = Process payments and view records only (no fee setup)
- ✅ Full* = Full access except DELETE (only archive)
- ✅ View** = View access limited to assigned students/classes
- ✅ Edit** = Edit access limited to assigned students/classes
- ❌ = No access

---

## Detailed Role Descriptions

### 1. SUPERADMIN
**Full system access** - Can manage everything including system settings and cloud management.

### 2. ADMIN
**Full operational access** - Can manage all school operations except system-level configurations (may vary by policy).

### 3. REGISTRAR
**Student-focused access**:
- ✅ Manage enrollments (create, edit, process re-enrollment)
- ✅ Manage student records (create, edit, view academic records)
- ✅ View curriculum (for enrollment purposes)
- ✅ View finance (to check payment status)
- ✅ View archive and audit logs
- ❌ Cannot delete (only archive with authorization)
- ❌ No HR, Payroll, or Settings access

### 4. CASHIER
**Payment-focused access**:
- ✅ Process payments
- ✅ View payment records
- ✅ View student records (for payment verification)
- ✅ View enrollment status (for payment verification)
- ❌ Cannot setup fees or manage expenses
- ❌ No other module access

### 5. TEACHER
**Classroom-focused access**:
- ✅ View assigned students' records
- ✅ View and edit academic records (for assigned students only)
- ✅ View curriculum (assigned classes only)
- ❌ Cannot access enrollment, finance, HR, or administrative modules

### 6. HR
**Employee-focused access**:
- ✅ Manage employee records (create, edit, view)
- ✅ View payroll and generate payslips
- ✅ Archive employees (with authorization)
- ❌ Cannot delete employees (only archive)
- ❌ Cannot create/edit payroll records
- ❌ No student or finance access

### 7. JANITOR
**Minimal access**:
- ✅ View dashboard (limited)
- ✅ Manage own profile
- ❌ No other access

### 8. OTHER
**Custom role** - Minimal access by default, can be customized per school needs.

---

## Missing Modules (High Priority)

1. **Attendance Monitoring** - Teachers need to record daily attendance
2. **Gradebook** - Teachers need to enter and compute grades
3. **Reporting** - Comprehensive report generation needed
4. **Parent Portal** - Parents need access to student information

---

## Recommended Code Changes

### Update RolePermissionService.cs

**Registrar Role** - Remove delete permissions:
```csharp
["Registrar"] = new List<string>
{
    Permissions.ViewDashboard,
    Permissions.ViewEnrollment, 
    Permissions.CreateEnrollment, 
    Permissions.EditEnrollment, 
    // REMOVE: Permissions.DeleteEnrollment,
    Permissions.ProcessReEnrollment,
    Permissions.ViewStudentRecord, 
    Permissions.CreateStudentRecord, 
    Permissions.EditStudentRecord, 
    // REMOVE: Permissions.DeleteStudentRecord,
    Permissions.ViewAcademicRecord,
    Permissions.ViewCurriculum, 
    Permissions.ViewProfile, 
    Permissions.EditProfile
}
```

**Cashier Role** - Restrict to payments only:
```csharp
["Cashier"] = new List<string>
{
    Permissions.ViewDashboard,
    Permissions.ViewFinance, 
    Permissions.ProcessPayment, 
    Permissions.ViewPaymentRecords,
    // REMOVE: Permissions.CreateFee, EditFee, DeleteFee, ManageExpenses
    Permissions.ViewEnrollment, // To see enrollment status for payment
    Permissions.ViewStudentRecord, // To view student info for payment
    Permissions.ViewProfile, 
    Permissions.EditProfile
}
```

**HR Role** - Remove delete permission:
```csharp
["HR"] = new List<string>
{
    Permissions.ViewDashboard,
    Permissions.ViewHR, 
    Permissions.CreateEmployee, 
    Permissions.EditEmployee, 
    // REMOVE: Permissions.DeleteEmployee,
    Permissions.ViewEmployeeProfile, 
    Permissions.ManageEmployeeData,
    Permissions.ViewPayroll, 
    Permissions.GeneratePayslip,
    Permissions.ViewArchive, 
    Permissions.ArchiveEmployee, 
    Permissions.RestoreArchived,
    Permissions.ViewProfile, 
    Permissions.EditProfile
}
```

**Teacher Role** - Add grade and attendance permissions (when modules are implemented):
```csharp
["Teacher"] = new List<string>
{
    Permissions.ViewDashboard,
    Permissions.ViewStudentRecord, 
    Permissions.ViewAcademicRecord,
    // ADD: Permissions.EditAcademicRecord (for assigned students only)
    // ADD: Permissions.EnterGrades (when gradebook module is added)
    // ADD: Permissions.RecordAttendance (when attendance module is added)
    Permissions.ViewCurriculum, // To view assigned classes
    Permissions.ViewProfile, 
    Permissions.EditProfile
}
```

---

## Security Best Practices

1. **Principle of Least Privilege**: Each role should only have access to what they need
2. **Separation of Duties**: Financial operations (fee setup) should be separate from payment processing
3. **Audit Trail**: All sensitive operations should be logged
4. **Soft Delete**: Use archiving instead of hard deletes for important records
5. **Role Hierarchy**: SuperAdmin > Admin > Department Heads > Staff

---

**Last Updated**: 2024

