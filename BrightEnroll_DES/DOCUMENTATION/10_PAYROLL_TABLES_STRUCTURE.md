# Payroll Tables Structure

This document describes the table structures for the payroll management system.

## Table Structures

### 1. tbl_roles

**Purpose:** Stores role-based salary configuration (base salary and allowance for each role).

**Columns:**
- `role_id` (INT, IDENTITY, PRIMARY KEY) - Unique identifier for each role
- `role_name` (VARCHAR(50), NOT NULL, UNIQUE) - Name of the role (e.g., "Admin", "Teacher", "HR")
- `base_salary` (DECIMAL(12,2), NOT NULL) - Base salary amount in Philippine Pesos
- `allowance` (DECIMAL(12,2), NOT NULL, DEFAULT 0.00) - Allowance amount in Philippine Pesos
- `is_active` (BIT, NOT NULL, DEFAULT 1) - Status flag to enable/disable the role configuration
- `created_date` (DATETIME, NOT NULL, DEFAULT GETDATE()) - Timestamp when the record was created
- `updated_date` (DATETIME, NULL) - Timestamp when the record was last updated

**Indexes:**
- `IX_tbl_roles_role_name` - Unique index on role_name for fast lookups
- `IX_tbl_roles_is_active` - Index on is_active for filtering active roles

**Notes:**
- This is a standalone table with no foreign key relationships
- Each role can have one active configuration
- Role names must be unique

---

### 2. tbl_deductions

**Purpose:** Stores deduction configuration values (rates, fixed amounts, and limits for SSS, PhilHealth, Pag-IBIG, Withholding Tax, etc.).

**Columns:**
- `deduction_id` (INT, IDENTITY, PRIMARY KEY) - Unique identifier for each deduction
- `deduction_type` (VARCHAR(50), NOT NULL, UNIQUE) - Type/code of deduction (e.g., "SSS", "PHILHEALTH", "PAGIBIG", "WITHHOLDING_TAX")
- `deduction_name` (VARCHAR(100), NOT NULL) - Display name of the deduction (e.g., "Social Security System", "Philippine Health Insurance")
- `rate_or_value` (DECIMAL(12,4), NOT NULL) - The rate (if percentage) or fixed value for the deduction
- `is_percentage` (BIT, NOT NULL, DEFAULT 1) - Flag indicating if rate_or_value is a percentage (1) or fixed amount (0)
- `max_amount` (DECIMAL(12,2), NULL) - Maximum deduction amount cap (e.g., Pag-IBIG max of ₱200)
- `min_amount` (DECIMAL(12,2), NULL) - Minimum deduction amount (if applicable)
- `description` (VARCHAR(500), NULL) - Optional description of the deduction
- `is_active` (BIT, NOT NULL, DEFAULT 1) - Status flag to enable/disable the deduction
- `created_date` (DATETIME, NOT NULL, DEFAULT GETDATE()) - Timestamp when the record was created
- `updated_date` (DATETIME, NULL) - Timestamp when the record was last updated

**Indexes:**
- `IX_tbl_deductions_deduction_type` - Unique index on deduction_type for fast lookups
- `IX_tbl_deductions_is_active` - Index on is_active for filtering active deductions

**Example Data:**
```
deduction_type: "SSS"
deduction_name: "Social Security System"
rate_or_value: 0.1100 (11%)
is_percentage: 1
max_amount: 2000.00
min_amount: NULL
description: "SSS contribution at 11% of base salary, capped at ₱2,000/month"

deduction_type: "PHILHEALTH"
deduction_name: "Philippine Health Insurance Corporation"
rate_or_value: 0.0300 (3%)
is_percentage: 1
max_amount: NULL
min_amount: NULL
description: "PhilHealth contribution at 3% of base salary"

deduction_type: "PAGIBIG"
deduction_name: "Home Development Mutual Fund"
rate_or_value: 0.0200 (2%)
is_percentage: 1
max_amount: 200.00
min_amount: NULL
description: "Pag-IBIG contribution at 2% of base salary, capped at ₱200/month"
```

**Notes:**
- This is a standalone table with no foreign key relationships
- Each deduction type must be unique
- Supports both percentage-based and fixed-amount deductions
- Can store maximum and minimum limits for deductions

---

## Implementation Details

### Models Created:
1. **Role.cs** - Located in `Data/Models/Role.cs`
2. **Deduction.cs** - Located in `Data/Models/Deduction.cs`

### Database Context:
- Both models have been added to `AppDbContext`
- Proper indexing and table mappings configured

### Table Definitions:
- Table creation scripts added to `Services/Database/Definitions/TableDefinitions.cs`
- Tables will be automatically created during database initialization

### Next Steps:
1. Create UI components to add/edit roles and deductions
2. Create service layer for CRUD operations
3. Seed initial deduction data (SSS, PhilHealth, Pag-IBIG rates)
4. Integrate with existing payroll calculation system

