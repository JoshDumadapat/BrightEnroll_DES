# Testing Re-Enrollment Functionality Guide

## Prerequisites

1. **Database Setup**: Ensure the database has been initialized (the `updated_at` column will be added automatically)
2. **Test Data**: You need at least one student who:
   - Has status = "Enrolled"
   - Has completed a school year (e.g., 2023-2024)
   - Has complete grades for Q1, Q2, Q3, Q4 for all subjects

---

## Testing Steps

### Step 1: Verify Database Column Was Added

**Check if `updated_at` column exists:**

1. Open SQL Server Management Studio (SSMS) or your database tool
2. Connect to your database: `DB_BrightEnroll_DES`
3. Run this query:
   ```sql
   SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
   FROM INFORMATION_SCHEMA.COLUMNS
   WHERE TABLE_NAME = 'tbl_Students'
   AND COLUMN_NAME = 'updated_at'
   ```
4. **Expected Result**: Should return one row showing `updated_at` column exists

---

### Step 2: Prepare Test Student Data

**Option A: Use Existing Student (Recommended)**
- Find a student who is currently "Enrolled"
- Ensure they have grades for Q1, Q2, Q3, Q4 for the current school year
- Note their Student ID, Grade Level, and School Year

**Option B: Create Test Data**
1. Register a new student through the registration form
2. Enroll them (status = "Enrolled")
3. Add grades for Q1, Q2, Q3, Q4 through the Gradebook module

---

### Step 3: Test Marking Students as Eligible

This simulates the end-of-school-year process.

**Method 1: Using Code (Recommended for Testing)**

1. Create a test page or add a button temporarily in the Enrollment page
2. Add this code to test the eligibility marking:

```csharp
@inject ReEnrollmentService ReEnrollmentService
@inject ILogger<TestPage> Logger

<button @onclick="TestMarkEligible">Test Mark Eligible</button>

@code {
    private async Task TestMarkEligible()
    {
        // Replace with the school year you want to test (e.g., "2023-2024")
        string completedSchoolYear = "2023-2024";
        
        var result = await ReEnrollmentService.MarkStudentsEligibleForReEnrollmentAsync(completedSchoolYear);
        
        Logger.LogInformation(
            "Mark Eligibility Result: Success={Success}, Eligible={Eligible}, Failed={Failed}, Incomplete={Incomplete}",
            result.Success, result.MarkedEligible, result.FailedStudents, result.IncompleteGrades);
    }
}
```

**Method 2: Direct Database Check**

After running the method, check the database:

```sql
-- Check if students were marked as Eligible
SELECT student_id, first_name, last_name, grade_level, school_yr, status
FROM tbl_Students
WHERE status = 'Eligible'
ORDER BY date_registered DESC
```

**Expected Results:**
- Students with complete Q1-Q4 grades and passing average (>= 75) should have status = "Eligible"
- Students with incomplete grades should remain "Enrolled"
- Students with failing grades should remain "Enrolled"

---

### Step 4: Test Re-Enrollment UI

1. **Navigate to Enrollment Page**
   - Go to `/enrollment` in your application
   - Click on the **"Re-enrollment"** tab

2. **Verify Eligible Students Appear**
   - You should see students with status = "Eligible" in the list
   - Check that their Grade Level and School Year are displayed correctly

3. **Test Re-Enroll Button**
   - Find a student with status = "Eligible"
   - Click the **"Re-Enroll"** button
   - **Expected Behavior:**
     - Button shows "Re-enrolling..." with spinner
     - After completion, student should disappear from Re-Enrollment tab
     - Student should appear in "Enrolled Student" tab
     - Toast notification should show success message

---

### Step 5: Verify Database Changes

After clicking "Re-Enroll", check the database:

```sql
-- Check student's updated information
SELECT 
    student_id,
    first_name,
    last_name,
    grade_level,           -- Should be promoted (e.g., G1 → G2)
    school_yr,             -- Should be next school year (e.g., 2023-2024 → 2024-2025)
    status,                -- Should be "Enrolled"
    updated_at             -- Should have current timestamp
FROM tbl_Students
WHERE student_id = 'YOUR_STUDENT_ID'
```

**Expected Results:**
- `grade_level`: Should be one level higher (G1 → G2, G2 → G3, etc.)
- `school_yr`: Should be next school year
- `status`: Should be "Enrolled"
- `updated_at`: Should have current timestamp

**Check Enrollment Record:**

```sql
-- Check if new enrollment record was created
SELECT 
    student_id,
    section_id,
    school_year,          -- Should be new school year
    status,               -- Should be "Enrolled"
    created_at
FROM tbl_StudentSectionEnrollments
WHERE student_id = 'YOUR_STUDENT_ID'
ORDER BY created_at DESC
```

**Expected Results:**
- New enrollment record for the new school year
- Status = "Enrolled"

---

### Step 6: Verify Audit Logs

Check that all actions were logged:

```sql
-- Check audit logs for re-enrollment actions
SELECT 
    timestamp,
    user_name,
    user_role,
    action,
    module,
    description,
    student_id,
    student_name,
    grade,
    student_status,
    status
FROM tbl_audit_logs
WHERE module = 'Re-Enrollment'
   OR action LIKE '%Re-Enrollment%'
   OR action LIKE '%Eligible%'
ORDER BY timestamp DESC
```

**Expected Results:**
- Log entries for "Mark Student Eligible for Re-Enrollment"
- Log entries for "Student Re-Enrollment"
- All entries should have user_name, user_role, and timestamps

**Check Status Logs:**

```sql
-- Check student status change history
SELECT 
    student_id,
    old_status,
    new_status,
    changed_by_name,
    created_at
FROM tbl_student_status_logs
WHERE student_id = 'YOUR_STUDENT_ID'
ORDER BY created_at DESC
```

**Expected Results:**
- Status change from "Enrolled" → "Eligible" (if marked eligible)
- Status change from "Eligible" → "Enrolled" (after re-enrollment)

---

### Step 7: Test Edge Cases

**Test 1: Student with Incomplete Grades**
- Student with only Q1, Q2, Q3 (missing Q4)
- **Expected**: Should NOT be marked as Eligible

**Test 2: Student with Failing Grades**
- Student with average < 75
- **Expected**: Should NOT be marked as Eligible

**Test 3: Student Already Eligible**
- Run MarkEligible twice on same student
- **Expected**: Should count as "AlreadyEligible" in result

**Test 4: Student Not Eligible for Re-Enrollment**
- Try to re-enroll a student with status = "Pending"
- **Expected**: Should fail with error message

**Test 5: Grade Level Promotion Limits**
- Try to re-enroll a G6 student
- **Expected**: Should fail (cannot promote beyond G6)

---

### Step 8: Verify UI Updates

1. **Re-Enrollment Tab**
   - After marking eligible: Students should appear
   - After re-enrolling: Student should disappear

2. **Enrolled Student Tab**
   - After re-enrolling: Student should appear with new grade level and school year

3. **Toast Notifications**
   - Should show success message after re-enrollment
   - Should show error message if re-enrollment fails

---

## Quick Test Checklist

- [ ] Database `updated_at` column exists
- [ ] `MarkStudentsEligibleForReEnrollmentAsync` runs without errors
- [ ] Students with complete passing grades are marked as "Eligible"
- [ ] Eligible students appear in Re-Enrollment tab
- [ ] "Re-Enroll" button is visible and clickable
- [ ] Re-enrollment promotes grade level (G1 → G2)
- [ ] Re-enrollment updates school year (2023-2024 → 2024-2025)
- [ ] Student status changes to "Enrolled" after re-enrollment
- [ ] New enrollment record is created
- [ ] Audit logs are created with correct information
- [ ] Status logs track all status changes
- [ ] UI refreshes after re-enrollment
- [ ] Toast notification shows success message

---

## Troubleshooting

### Issue: Students not appearing as Eligible

**Check:**
1. Do they have complete Q1-Q4 grades?
   ```sql
   SELECT student_id, grading_period, COUNT(*) as grade_count
   FROM tbl_Grades
   WHERE student_id = 'YOUR_STUDENT_ID'
   AND school_year = '2023-2024'
   GROUP BY student_id, grading_period
   ```
2. Is their average >= 75?
3. Is their current status "Enrolled"?

### Issue: Re-Enroll button not working

**Check:**
1. Is student status = "Eligible"?
2. Check browser console for JavaScript errors
3. Check server logs for exceptions
4. Verify ReEnrollmentService is registered in DI

### Issue: Grade level not promoting

**Check:**
1. Current grade level in database
2. Verify `GetNextGradeLevel()` logic
3. Check if grade level is null or invalid

### Issue: Audit logs not created

**Check:**
1. Is `AuditLogService` registered in DI?
2. Does `tbl_audit_logs` table exist?
3. Check server logs for audit log errors (they shouldn't break the main operation)

---

## Example Test Scenario: Mara's Journey

1. **Initial State:**
   - Mara is enrolled in G1 for 2023-2024
   - Status: "Enrolled"
   - Has Q1, Q2, Q3, Q4 grades (all passing)

2. **End of School Year:**
   - Run: `MarkStudentsEligibleForReEnrollmentAsync("2023-2024")`
   - Result: Mara's status → "Eligible"

3. **Re-Enrollment:**
   - Mara appears in Re-Enrollment tab
   - Click "Re-Enroll" button
   - Result:
     - Grade: G1 → G2
     - School Year: 2023-2024 → 2024-2025
     - Status: "Eligible" → "Enrolled"
     - New enrollment record created

4. **Verification:**
   - Check database: All fields updated correctly
   - Check audit logs: All actions logged
   - Check UI: Mara appears in "Enrolled Student" tab with G2

---

## Next Steps After Testing

Once verified working:
1. Create an admin page/button for "End School Year" process
2. Add bulk re-enrollment feature (select multiple students)
3. Add section assignment during re-enrollment
4. Add reports for eligible/retained students

