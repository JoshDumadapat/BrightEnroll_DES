# Dynamic Threshold Implementation Summary

## Overview
The salary change threshold is now **fully dynamic** and connected to the Payroll module. The threshold percentage is stored in `tbl_roles.threshold_percentage` and is used throughout the HR module for both Add and Edit Employee operations.

## How It Works

### 1. **Threshold Storage**
- **Location:** `tbl_roles.threshold_percentage` (DECIMAL(5,2))
- **Default Value:** 10.00% (can be changed per role in Payroll module)
- **Set In:** Payroll > Manage Roles > Edit Role > Threshold Percentage

### 2. **Dynamic Threshold Usage**

#### **Add Employee Flow** (`AddEmployee.razor`)
1. Roles are loaded from database with `threshold_percentage` included
2. When submitting employee, system checks:
   - `newBaseSalary > roleBaseSalary * (1 + threshold_percentage/100)`
   - `newAllowance > roleAllowance * (1 + threshold_percentage/100)`
3. If threshold exceeded → Shows approval modal with dynamic threshold value
4. If within threshold → Employee created immediately with active salary

#### **Edit Employee Flow** (`EmployeeInfo.razor`)
1. When editing salary, system fetches role from database
2. Gets `threshold_percentage` for the role
3. Compares new salary against role base salary + threshold
4. If threshold exceeded → Shows approval modal with dynamic threshold value
5. If within threshold → Salary updated directly

#### **EmployeeService** (`EmployeeService.cs`)
- Uses dynamic threshold when updating employee info
- Logs threshold checks for debugging
- Creates salary change request if threshold exceeded

### 3. **Approval Modal Display**
- Shows the **actual threshold percentage** from Payroll module (not hardcoded)
- Example: "exceeds the role's monthly salary by more than **15.5%** (threshold set in Payroll module)"
- Displays role base salary, requested salary, and difference

## Files Modified

1. **Components/Pages/Admin/HumanResource/AddEmployee.razor**
   - Added `roleThresholdPercentage` field to store threshold for modal display
   - Updated `LoadRolesFromDatabase()` to handle missing `threshold_percentage` column
   - Updated threshold check to use dynamic value and log results
   - Updated approval modal to show dynamic threshold percentage

2. **Components/Pages/Admin/HumanResource/HRComponents/EmployeeInfo.razor**
   - Added `roleThresholdPercentage` field to store threshold for modal display
   - Updated threshold check to use dynamic value with better logging
   - Updated approval modal to show dynamic threshold percentage

3. **Services/Business/HR/EmployeeService.cs**
   - Enhanced threshold check with detailed logging
   - Uses dynamic `threshold_percentage` from database
   - Logs both base salary and allowance threshold checks

## Threshold Calculation Logic

### Key Rules:
1. **If threshold is 0%** → No approval needed (no limit on salary increase)
2. **If threshold > 0%** → Approval required only if new salary exceeds threshold
3. **CUMULATIVE APPROACH** → Always compares to role base salary, not current salary

### Calculation:

```csharp
// Get role from database (includes threshold_percentage)
var role = await DbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == employeeRole);

// Rule 1: If threshold is 0%, no approval needed
if (role.ThresholdPercentage == 0.00m)
{
    // Allow direct update - no approval required
    requiresApproval = false;
}
else
{
    // Rule 2: Calculate threshold amounts (CUMULATIVE)
    decimal thresholdPercentage = role.ThresholdPercentage / 100m;
    var thresholdBaseSalary = role.BaseSalary * (1 + thresholdPercentage);
    var thresholdAllowance = role.Allowance * (1 + thresholdPercentage);
    
    // Rule 3: CUMULATIVE CHECK - Compare new salary to role base + threshold
    // This ensures threshold doesn't restart
    // Example: If role base is 100,000 and threshold is 10%:
    //   - Max allowed is 110,000 (100,000 * 1.10)
    //   - If current salary is 107,000 (7% increase), remaining threshold is 3%
    //   - New salary must be <= 110,000, otherwise approval needed
    bool requiresApproval = (newBaseSalary > thresholdBaseSalary) || 
                            (newAllowance > thresholdAllowance);
}
```

### Cumulative Approach Example:
- **Role Base Salary:** ₱100,000
- **Threshold:** 10% (max allowed: ₱110,000)
- **Scenario 1:** Current salary is ₱107,000 (7% increase from base)
  - Remaining threshold: 3% (₱3,000)
  - New salary ≤ ₱110,000 → **No approval needed**
  - New salary > ₱110,000 → **Approval needed**
- **Scenario 2:** Current salary is ₱110,000 (10% increase from base)
  - Remaining threshold: 0%
  - New salary > ₱110,000 → **Approval needed**
  - New salary ≤ ₱110,000 → **No approval needed**

## Benefits

1. **Centralized Control:** Threshold is set once in Payroll module, used everywhere
2. **Per-Role Flexibility:** Each role can have different threshold percentage
3. **Easy Updates:** Change threshold in Payroll, immediately affects HR operations
4. **Transparency:** Approval modals show the actual threshold being used
5. **Audit Trail:** All threshold checks are logged for debugging

## Testing Checklist

- [ ] Add employee with salary within threshold → Should create immediately
- [ ] Add employee with salary exceeding threshold → Should show approval modal with correct threshold %
- [ ] Edit employee salary within threshold → Should update immediately
- [ ] Edit employee salary exceeding threshold → Should show approval modal with correct threshold %
- [ ] Change threshold in Payroll module → Should reflect in HR operations
- [ ] Different roles with different thresholds → Should use correct threshold per role
- [ ] Missing threshold_percentage column → Should default to 0% and log error

## Notes

### Threshold Rules:
1. **If `threshold_percentage` is 0.00%** → No approval required (salary can be any amount)
2. **If `threshold_percentage` is NULL or missing** → Defaults to 0.00% (no approval required)
3. **If `threshold_percentage` > 0%** → Approval required only if new salary exceeds threshold

### Cumulative Approach:
- **Always compares to role base salary**, not current salary
- **Threshold doesn't restart** - if previous increase used 7% of 10%, remaining threshold is still 3%
- **Example:** Role base ₱100,000, threshold 10% (max ₱110,000)
  - Current salary ₱107,000 (7% used) → Remaining 3% (₱3,000)
  - New salary ≤ ₱110,000 → No approval
  - New salary > ₱110,000 → Approval needed

### Independent Checks:
- **Base salary** and **allowance** are checked independently
- If either exceeds threshold, approval is required
- Both must be within threshold for direct update

