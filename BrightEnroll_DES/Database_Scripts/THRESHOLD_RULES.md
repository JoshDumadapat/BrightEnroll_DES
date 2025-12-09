# Salary Threshold Rules

## Overview
The salary threshold system uses a **cumulative approach** to determine when salary changes require approval. The threshold is set per role in the Payroll module and is dynamically applied in HR operations.

## Rules

### Rule 1: Threshold = 0% (No Limit)
- **If threshold is 0.00%** → **NO approval required**
- Salary can be increased to any amount
- No request form is shown
- Direct update is allowed

### Rule 2: Threshold > 0% (Limited Increase)
- **If threshold > 0%** → Approval required **ONLY if exceeded**
- Salary can be increased up to: `Role Base Salary × (1 + Threshold%)`
- If new salary exceeds this limit → **Request form is shown**
- If new salary is within limit → **NO request form, direct update**

### Rule 3: Cumulative Approach
- **Always compares to role base salary**, not current salary
- **Threshold doesn't restart** on each change
- Previous increases count toward the threshold

## Examples

### Example 1: Threshold = 10%
- **Role Base Salary:** ₱100,000
- **Threshold:** 10%
- **Max Allowed:** ₱110,000 (100,000 × 1.10)

**Scenario A: New Employee**
- Requested salary: ₱105,000
- Check: 105,000 ≤ 110,000? **YES**
- Result: **NO request form** → Direct registration

**Scenario B: New Employee**
- Requested salary: ₱115,000
- Check: 115,000 > 110,000? **YES**
- Result: **Request form shown** → Approval required

### Example 2: Cumulative Approach
- **Role Base Salary:** ₱100,000
- **Threshold:** 10%
- **Max Allowed:** ₱110,000

**Step 1: First Increase**
- Current salary: ₱100,000 (base)
- New salary: ₱107,000 (7% increase)
- Check: 107,000 ≤ 110,000? **YES**
- Result: **NO request form** → Direct update
- **7% of threshold used, 3% remaining**

**Step 2: Second Increase (Cumulative)**
- Current salary: ₱107,000
- New salary: ₱109,000
- Check: 109,000 ≤ 110,000? **YES**
- Result: **NO request form** → Direct update
- **9% of threshold used, 1% remaining**

**Step 3: Third Increase (Cumulative)**
- Current salary: ₱109,000
- New salary: ₱112,000
- Check: 112,000 > 110,000? **YES**
- Result: **Request form shown** → Approval required
- **Exceeds 10% threshold from base**

### Example 3: Threshold = 0%
- **Role Base Salary:** ₱100,000
- **Threshold:** 0%
- **Max Allowed:** Unlimited

**Any Scenario:**
- Requested salary: Any amount (₱150,000, ₱200,000, etc.)
- Check: Threshold is 0%? **YES**
- Result: **NO request form** → Direct update/registration
- **No approval required regardless of amount**

## Implementation

### Add Employee Flow
1. Load role with `threshold_percentage` from database
2. If threshold = 0% → Register employee directly
3. If threshold > 0%:
   - Calculate: `maxAllowed = roleBase × (1 + threshold%)`
   - If `newSalary > maxAllowed` → Show approval modal
   - If `newSalary ≤ maxAllowed` → Register directly

### Edit Employee Flow
1. Load role with `threshold_percentage` from database
2. If threshold = 0% → Update salary directly
3. If threshold > 0%:
   - Calculate: `maxAllowed = roleBase × (1 + threshold%)`
   - If `newSalary > maxAllowed` → Show approval modal
   - If `newSalary ≤ maxAllowed` → Update directly

### EmployeeService Flow
1. Load role with `threshold_percentage` from database
2. If threshold = 0% → Update salary directly (no request)
3. If threshold > 0%:
   - Calculate: `maxAllowed = roleBase × (1 + threshold%)`
   - If `newSalary > maxAllowed` → Create salary change request
   - If `newSalary ≤ maxAllowed` → Update directly (no request)

## Key Points

✅ **Threshold = 0%** → No approval, no request form, direct update
✅ **Threshold > 0% and within limit** → No approval, no request form, direct update
✅ **Threshold > 0% and exceeded** → Approval required, request form shown
✅ **Cumulative** → Always compares to role base, not current salary
✅ **Independent checks** → Base salary and allowance checked separately

