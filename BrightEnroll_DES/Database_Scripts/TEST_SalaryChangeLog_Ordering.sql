-- =============================================
-- Test Script: Salary Change Log Ordering Verification
-- Purpose: Verify that salary change log entries are ordered correctly
--          with latest changes appearing first
-- =============================================

-- Test 1: Verify Ordering by RequestedAt (Most Recent First)
-- Expected: Results should be ordered by requested_at DESC
SELECT 
    r.request_id,
    u.first_name + ' ' + u.last_name AS employee_name,
    r.requested_at,
    r.approved_at,
    r.effective_date,
    r.status,
    r.current_base_salary,
    r.requested_base_salary,
    CASE 
        WHEN r.current_base_salary > 0 
        THEN CAST(((r.requested_base_salary - r.current_base_salary) / r.current_base_salary * 100) AS DECIMAL(10,2))
        ELSE 0
    END AS change_percentage,
    'Order by RequestedAt' AS test_case
FROM tbl_salary_change_requests r
INNER JOIN tbl_Users u ON r.user_id = u.user_ID
ORDER BY 
    r.requested_at DESC,  -- Primary: Most recent requests first
    r.approved_at DESC,   -- Secondary: Approved changes
    r.effective_date DESC  -- Tertiary: Effective date
;

-- Test 2: Verify No 0% Changes When Salary Wasn't Edited
-- Expected: Should NOT find entries with 0% change and reason containing "Profile update (aut"
-- If found, these are bugs that need to be fixed
SELECT 
    r.request_id,
    u.first_name + ' ' + u.last_name AS employee_name,
    r.requested_at,
    r.status,
    r.current_base_salary,
    r.requested_base_salary,
    CASE 
        WHEN r.current_base_salary > 0 
        THEN CAST(((r.requested_base_salary - r.current_base_salary) / r.current_base_salary * 100) AS DECIMAL(10,2))
        ELSE 0
    END AS change_percentage,
    r.reason,
    '0% Change Detection' AS test_case
FROM tbl_salary_change_requests r
INNER JOIN tbl_Users u ON r.user_id = u.user_ID
WHERE 
    r.current_base_salary = r.requested_base_salary
    AND r.current_base_salary > 0  -- Exclude zero salaries
    AND (r.reason LIKE '%Profile update (aut%' OR r.reason LIKE '%auto-approved%')
ORDER BY r.requested_at DESC
;

-- Test 3: Verify Latest Changes for Specific Employee
-- Replace @EmployeeName with actual employee name to test
DECLARE @EmployeeName NVARCHAR(100) = 'Maria Santos'; -- Change this to test different employees

SELECT 
    r.request_id,
    u.first_name + ' ' + u.last_name AS employee_name,
    r.requested_at,
    r.approved_at,
    r.effective_date,
    r.status,
    r.current_base_salary,
    r.requested_base_salary,
    CASE 
        WHEN r.current_base_salary > 0 
        THEN CAST(((r.requested_base_salary - r.current_base_salary) / r.current_base_salary * 100) AS DECIMAL(10,2))
        ELSE 0
    END AS change_percentage,
    'Employee Specific Test' AS test_case
FROM tbl_salary_change_requests r
INNER JOIN tbl_Users u ON r.user_id = u.user_ID
WHERE u.first_name + ' ' + u.last_name = @EmployeeName
ORDER BY 
    r.requested_at DESC,
    r.approved_at DESC,
    r.effective_date DESC
;

-- Test 4: Count Entries by Status (for verification)
SELECT 
    status,
    COUNT(*) AS count,
    MAX(requested_at) AS latest_request,
    MIN(requested_at) AS oldest_request
FROM tbl_salary_change_requests
GROUP BY status
ORDER BY status
;

-- Test 5: Verify Ordering Matches Expected Pattern
-- This query shows the top 10 most recent entries
-- Verify they are in descending order by requested_at
SELECT TOP 10
    ROW_NUMBER() OVER (ORDER BY r.requested_at DESC, r.approved_at DESC, r.effective_date DESC) AS expected_rank,
    r.request_id,
    u.first_name + ' ' + u.last_name AS employee_name,
    r.requested_at,
    r.approved_at,
    r.effective_date,
    r.status,
    CASE 
        WHEN r.current_base_salary > 0 
        THEN CAST(((r.requested_base_salary - r.current_base_salary) / r.current_base_salary * 100) AS DECIMAL(10,2))
        ELSE 0
    END AS change_percentage,
    'Top 10 Verification' AS test_case
FROM tbl_salary_change_requests r
INNER JOIN tbl_Users u ON r.user_id = u.user_ID
ORDER BY 
    r.requested_at DESC,
    r.approved_at DESC,
    r.effective_date DESC
;

-- =============================================
-- Expected Results:
-- =============================================
-- Test 1: Should return all entries ordered by requested_at DESC (newest first)
-- Test 2: Should return 0 rows (no 0% changes from profile updates)
-- Test 3: Should return employee's entries ordered by requested_at DESC
-- Test 4: Should show count of entries by status
-- Test 5: Should show top 10 entries with expected_rank matching the order
-- =============================================

