# QUICK FIX: Payroll Status Constraint Error

## Problem
You're getting this error when processing payroll:
```
The UPDATE statement conflicted with the CHECK constraint "CK_tbl_payroll_transactions_Status"
```

## Root Cause
The database CHECK constraint only allows these status values:
- `'Pending'`
- `'Paid'`
- `'Cancelled'`

But the code is trying to set status to `'Pending Approval'`, which is not allowed.

## Solution

### Option 1: Run the Simple Migration Script (RECOMMENDED)

1. Open **SQL Server Management Studio** (SSMS) or any SQL client
2. Connect to your database: `DB_BrightEnroll_DES`
3. Open the file: `Database_Scripts/Update_Payroll_Status_Constraint_Simple.sql`
4. Execute the entire script (F5 or Execute button)
5. You should see: `SUCCESS: Payroll status constraint updated!`

### Option 2: Run via Command Line

```bash
sqlcmd -S localhost -d DB_BrightEnroll_DES -E -i "Database_Scripts/Update_Payroll_Status_Constraint_Simple.sql"
```

### What the Script Does

1. **Drops** the old CHECK constraint
2. **Creates** a new CHECK constraint that allows:
   - `'Pending'`
   - `'Pending Approval'` ← NEW
   - `'Paid'`
   - `'Cancelled'`
   - `'Rejected'` ← NEW
3. **Updates** the filtered unique index to include `'Pending Approval'`

### After Running the Script

✅ You can now process payroll batches and individual transactions  
✅ They will be sent to Finance for approval with status `'Pending Approval'`  
✅ Finance can approve (→ `'Paid'`) or reject (→ `'Rejected'`)

## Verification

After running the script, verify it worked:

```sql
-- Check the constraint definition
SELECT 
    cc.name AS ConstraintName,
    cc.definition AS ConstraintDefinition
FROM sys.check_constraints cc
WHERE cc.name = 'CK_tbl_payroll_transactions_Status'
    AND cc.parent_object_id = OBJECT_ID('dbo.tbl_payroll_transactions');
```

You should see the constraint includes `'Pending Approval'` and `'Rejected'`.

## Still Having Issues?

1. Make sure you're connected to the correct database
2. Make sure you have ALTER TABLE permissions
3. Check if there are any active transactions that might be blocking the change
4. Try running the script during a maintenance window

