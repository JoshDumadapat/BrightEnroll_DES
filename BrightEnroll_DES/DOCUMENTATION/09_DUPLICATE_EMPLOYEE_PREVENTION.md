# Duplicate Employee Prevention System

## Overview

This document describes the complete solution for preventing duplicate employee records in the Add Employee module. The system detects potential duplicates based on core uniqueness criteria and provides options to update existing records or create new ones.

## Enhanced Duplicate Detection Criteria

An employee is considered a **potential duplicate** using a comprehensive scoring system that checks multiple combinations:

### Core Required Matches:
1. **First Name + Last Name**: Must match exactly (case-insensitive)
2. **Date of Birth (DOB)**: Exact date match (date only, ignoring time)

### Name Combination Checks:
- **First Name + Last Name** (with or without middle name)
- **First Name + Middle Name + Last Name** (if middle name provided)
- **Suffix**: Included in matching (Jr., Sr., II, III, IV, etc.)

### Additional Matching Fields (Scoring System):
The system uses a scoring mechanism to detect duplicates more accurately:

- **Address Match** (10 points): Province + City + Barangay combination
- **Contact Number Match** (5 points): Normalized phone number comparison
- **Email Match** (5 points): Email address comparison
- **Emergency Contact Name Match** (5 points): Emergency contact first + last name
- **Emergency Contact Number Match** (5 points): Emergency contact phone number

### Duplicate Detection Rules:
An employee is flagged as duplicate if:
- **Core Match**: Name (10) + DOB (10) + Address (10) = 30+ points, OR
- **Strong Match**: Name (10) + DOB (10) + (Contact OR Email) = 25+ points with at least one additional match

This ensures that employees with the same name, DOB, and address are detected, as well as cases where name, DOB, and contact/email match even if address differs slightly.

## Implementation Details

### 1. Backend Service Methods (`EmployeeService.cs`)

#### `CheckForDuplicateEmployeeAsync(EmployeeRegistrationData employeeData)`

**Purpose**: Checks the database for potential duplicate employees based on core uniqueness criteria.

**Process**:
1. Validates required fields (FirstName, LastName, BirthDate)
2. Normalizes all fields for comparison:
   - Name fields: trim, lowercase (case-insensitive)
   - Contact numbers: remove spaces, dashes, parentheses
   - Email: lowercase, trim
   - Address components: normalize and combine
3. Queries database for employees matching:
   - First Name (case-insensitive)
   - Last Name (case-insensitive)
   - Birth Date (date only, ignoring time)
4. Filters results by name combinations:
   - Checks middle name match (if provided)
   - Checks suffix match (if provided)
   - Handles cases where middle name or suffix may be missing
5. For each potential match, calculates match score based on:
   - Name match (10 points - required)
   - DOB match (10 points - required)
   - Address match (10 points)
   - Contact number match (5 points)
   - Email match (5 points)
   - Emergency contact name match (5 points)
   - Emergency contact number match (5 points)
6. Flags as duplicate if score meets threshold (30+ for core match, 25+ for strong match)
7. Returns `EmployeeDuplicateCheckResult` with duplicate information if found

**Returns**:
- `EmployeeDuplicateCheckResult` with `IsDuplicate = true` if duplicate found
- `EmployeeDuplicateCheckResult` with `IsDuplicate = false` if no duplicate
- `null` if required fields are missing

#### `UpdateEmployeeContactInfoAsync(int employeeId, string? newContactNumber, string? newEmail)`

**Purpose**: Updates existing employee's contact number and/or email when duplicate is detected.

**Process**:
1. Retrieves existing employee by ID
2. Validates new email is not already used by another employee
3. Updates contact number if provided
4. Updates email if provided and valid
5. Saves changes within a transaction

**Returns**: `true` if successful, `false` if employee not found

### 2. Data Transfer Objects (DTOs)

#### `EmployeeDuplicateCheckResult`

Contains information about a detected duplicate:

```csharp
public class EmployeeDuplicateCheckResult
{
    public bool IsDuplicate { get; set; }
    public int? ExistingEmployeeId { get; set; }
    public string ExistingSystemId { get; set; } = string.Empty;
    public string ExistingFullName { get; set; } = string.Empty;
    public string ExistingEmail { get; set; } = string.Empty;
    public string ExistingContactNumber { get; set; } = string.Empty;
    public DateTime ExistingBirthDate { get; set; }
    public string ExistingAddress { get; set; } = string.Empty;
}
```

### 3. Frontend Component (`AddEmployee.razor`)

#### Duplicate Check Workflow

1. **User fills in Add Employee form**
2. **On Submit Click**:
   - Validates form fields
   - Checks for duplicate email (existing check)
   - **NEW**: Calls `CheckForDuplicateEmployee()` before showing confirmation modal
3. **If Duplicate Found**:
   - Shows duplicate warning modal with:
     - Existing employee details (System ID, Name, DOB, Address, Current Contact/Email)
     - New employee information (New Contact/Email)
     - Two action options:
       - **Update Existing Employee**: Updates contact/email of existing record
       - **Create New Employee**: Proceeds with new record creation
4. **If No Duplicate**:
   - Proceeds with normal confirmation modal and employee creation

#### Key Methods

- `CheckForDuplicateEmployee()`: Performs duplicate check before submission
- `UpdateExistingEmployee()`: Handles updating existing employee's contact/email
- `ProceedWithNewEmployee()`: Allows user to confirm and create new record
- `CloseDuplicateModal()`: Closes the duplicate warning modal

## User Interface Flow

### Duplicate Warning Modal

When a duplicate is detected, a warning modal appears with:

1. **Warning Header**: "⚠️ Potential Duplicate Employee Detected"
2. **Existing Employee Information Box** (Yellow background):
   - System ID
   - Full Name
   - Date of Birth
   - Address
   - Current Email
   - Current Contact Number
3. **New Employee Information Box** (Gray background):
   - New Email
   - New Contact Number
4. **Action Buttons**:
   - **Cancel**: Closes modal, returns to form
   - **Create New Employee**: Proceeds with new record (user confirms it's different person)
   - **Update Existing Employee**: Updates contact/email of existing record

## Database Schema

The duplicate check queries the following tables:

- `tbl_Users`: Contains employee personal information
  - `first_name`, `mid_name`, `last_name`, `suffix`
  - `birthdate`
  - `email`, `contact_num`
  - `system_ID`

- `tbl_employee_address`: Contains employee address information
  - `province`, `city`, `barangay`
  - `house_no`, `street_name`, `country`, `zip_code`

## Best Practices Implemented

1. **Case-Insensitive Comparison**: Names are normalized (lowercase, trimmed) for accurate matching
2. **Date-Only Comparison**: Birth dates compared by date only, ignoring time
3. **Address Normalization**: Address components are normalized and compared consistently
4. **Transaction Safety**: Updates are performed within database transactions
5. **Email Validation**: New email is validated to ensure it's not used by another employee
6. **User Choice**: System provides options rather than blocking, allowing legitimate cases
7. **Comprehensive Logging**: All operations are logged for audit purposes
8. **Error Handling**: Graceful error handling with user-friendly messages

## Example Scenarios

### Scenario 1: Duplicate Found - Update Contact

1. User enters: "Juan Santos", DOB: "01/15/1990", Address: "Manila, Metro Manila, Barangay 1"
2. System finds existing employee with same name, DOB, and address
3. Existing employee has: Email: "old@email.com", Contact: "09123456789"
4. New form has: Email: "new@email.com", Contact: "09987654321"
5. User clicks "Update Existing Employee"
6. System updates existing employee's email and contact number
7. No new record is created

### Scenario 2: Duplicate Found - Create New (Different Person)

1. User enters: "Juan Santos", DOB: "01/15/1990", Address: "Manila, Metro Manila, Barangay 1"
2. System finds existing employee with same name, DOB, and address
3. User reviews and confirms this is a different person (e.g., father and son with same name)
4. User clicks "Create New Employee"
5. System proceeds with normal employee creation
6. New record is created with new System ID

### Scenario 3: No Duplicate

1. User enters employee information
2. System checks database - no match found
3. Normal confirmation modal appears
4. Employee is created successfully

## Code Location

- **Service Methods**: `Services/HR/EmployeeService.cs`
- **DTOs**: `Services/HR/EmployeeService.cs` (at end of file)
- **Frontend Component**: `Components/Pages/Admin/AddEmployee.razor`
- **Database Models**: 
  - `Data/Models/UserEntity.cs`
  - `Data/Models/EmployeeAddress.cs`

## Testing Recommendations

1. **Test Duplicate Detection**:
   - Create employee with Name + DOB + Address
   - Try to add same employee again
   - Verify duplicate modal appears

2. **Test Update Existing**:
   - Detect duplicate
   - Click "Update Existing Employee"
   - Verify contact/email updated in database
   - Verify no new record created

3. **Test Create New**:
   - Detect duplicate
   - Click "Create New Employee"
   - Verify new record created with new System ID

4. **Test Edge Cases**:
   - Missing middle name (one has it, one doesn't)
   - Missing address components
   - Case variations in names
   - Same name, different DOB (should not be duplicate)
   - Same name and DOB, different address (should not be duplicate)

## Future Enhancements

Potential improvements:
1. Fuzzy matching for names (handling typos)
2. Address normalization (handling variations like "St." vs "Street")
3. Batch duplicate checking for bulk imports
4. Duplicate history/audit log
5. Automatic merge suggestions for similar records

