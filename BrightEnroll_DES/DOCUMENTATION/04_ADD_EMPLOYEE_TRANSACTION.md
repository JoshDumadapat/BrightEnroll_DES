# Add Employee Transaction Flow

## Overview

This guide explains the complete flow of adding an employee to the system, from form submission to database storage.

---

## Table of Contents

1. [Transaction Flow](#transaction-flow)
2. [Data Models](#data-models)
3. [Component Structure](#component-structure)
4. [Service Layer](#service-layer)
5. [Database Operations](#database-operations)
6. [Functions Reference](#functions-reference)
7. [Terminologies](#terminologies)

---

## Transaction Flow

### Complete Flow Diagram

```
User fills Add Employee form
    ↓
EmployeeFormData (View Model)
    ↓
Form Validation
    ↓
SubmitEmployee() in AddEmployee.razor
    ↓
Map to EmployeeRegistrationData (DTO)
    ↓
EmployeeService.RegisterEmployeeAsync()
    ↓
[Transaction Start]
    ↓
UserRepository.InsertAsync() → tbl_Users
    ↓
Get user_ID from database
    ↓
EmployeeService (EF Core Transaction)
    ├── Create EmployeeAddress → tbl_employee_address
    ├── Create EmployeeEmergencyContact → tbl_employee_emergency_contact
    └── Create SalaryInfo → tbl_salary_info
    ↓
[Transaction Commit]
    ↓
Success → Navigate to HR page
```

### Step-by-Step Process

1. **User Input**: User fills out Add Employee form
2. **Form Validation**: All required fields validated
3. **DTO Mapping**: EmployeeFormData → EmployeeRegistrationData
4. **User Creation**: Insert into tbl_Users (via UserRepository)
5. **Get User ID**: Retrieve generated user_ID
6. **Employee Data Creation**: Insert address, emergency contact, salary (via EF Core)
7. **Transaction Commit**: All data saved or rolled back
8. **Success**: Navigate to HR page with success message

---

## Data Models

### 1. EmployeeFormData (View Model)

**Location**: `Components/Pages/Admin/HRComponents/EmployeeFormData.cs`

**Purpose**: Holds form input data with validation attributes

**Contains**:
- Personal information (name, birthdate, age, sex, contact, email)
- Address information (house no, street, province, city, barangay, etc.)
- Emergency contact information
- Role and account information (role, system ID, password)
- Salary information (base salary, allowance, total)

**Validation**:
- Required fields marked with `[Required]`
- Email validation with `[EmailAddress]`
- Range validation for age and salary
- Regular expressions for contact numbers

### 2. EmployeeRegistrationData (DTO)

**Location**: `Services/HR/EmployeeService.cs`

**Purpose**: Data Transfer Object that maps form data to database entities

**Contains**:
- All fields from EmployeeFormData
- Used to transfer data between UI and service layers
- Maps to multiple database tables

**Why DTO?**
- Separates UI concerns from database structure
- Allows mapping one form to multiple tables
- Cleaner separation of layers

### 3. Entity Models

**Location**: `Data/Models/`

**Entities Used**:
- `User` → `tbl_Users`
- `EmployeeAddress` → `tbl_employee_address`
- `EmployeeEmergencyContact` → `tbl_employee_emergency_contact`
- `SalaryInfo` → `tbl_salary_info`

---

## Component Structure

### AddEmployee.razor

**Location**: `Components/Pages/Admin/AddEmployee.razor`

**Purpose**: UI markup for the Add Employee form

**Sections**:
1. Personal Information
2. Address
3. Emergency Contact
4. Role and Account Information
5. Salary Information

### AddEmployee.razor.cs (Code-Behind)

**Location**: `Components/Pages/Admin/AddEmployee.razor` (code section)

**Key Properties**:
- `EmployeeData`: EmployeeFormData instance
- `editContext`: EditContext for form validation
- `addressHandler`: Manages address dropdowns

**Key Methods**:
- `HandleSubmitClick()`: Validates form and shows confirmation modal
- `SubmitEmployee()`: Maps data and calls EmployeeService
- `GenerateSystemIdAsync()`: Generates next system ID
- `OnRoleChangedAsync()`: Handles role selection changes

---

## Service Layer

### EmployeeService

**Location**: `Services/HR/EmployeeService.cs`

**Purpose**: Business logic for employee registration

### RegisterEmployeeAsync Method

**Flow**:

```csharp
public async Task<int> RegisterEmployeeAsync(EmployeeRegistrationData employeeData)
{
    // Step 1: Create User account (via UserRepository)
    var user = new User { ... };
    await _userRepository.InsertAsync(user);
    
    // Step 2: Get user_ID
    var insertedUser = await _userRepository.GetBySystemIdAsync(user.system_ID);
    int userId = insertedUser.user_ID;
    
    // Step 3: Create employee-related records (EF Core transaction)
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // Create address
        var address = new EmployeeAddress { ... };
        _context.EmployeeAddresses.Add(address);
        
        // Create emergency contact
        var emergencyContact = new EmployeeEmergencyContact { ... };
        _context.EmployeeEmergencyContacts.Add(emergencyContact);
        
        // Create salary info
        var salaryInfo = new SalaryInfo { ... };
        _context.SalaryInfos.Add(salaryInfo);
        
        // Save all changes
        await _context.SaveChangesAsync();
        
        // Commit transaction
        await transaction.CommitAsync();
        
        return userId;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### Why Two Different Approaches?

1. **User Creation (UserRepository)**:
   - Uses repository pattern with parameterized queries
   - Separate database connection
   - More control over SQL

2. **Employee Data (EF Core)**:
   - Uses Entity Framework Core
   - Transaction ensures all-or-nothing
   - Easier relationship management

---

## Database Operations

### 1. User Creation

**Table**: `tbl_Users`

**Method**: `UserRepository.InsertAsync()`

**Fields Inserted**:
- system_ID (auto-generated: BDES-0002, BDES-0003, etc.)
- first_name, mid_name, last_name, suffix
- birthdate, age, gender
- contact_num, email
- user_role
- password (BCrypt hashed)
- date_hired, status

**System ID Generation**:
- Queries database for highest existing BDES system ID
- Increments by 1
- Formats as BDES-XXXX (4 digits)
- Checks for duplicates before returning

### 2. Address Creation

**Table**: `tbl_employee_address`

**Entity**: `EmployeeAddress`

**Fields Inserted**:
- user_ID (foreign key to tbl_Users)
- house_no, street_name
- province, city, barangay
- country, zip_code

### 3. Emergency Contact Creation

**Table**: `tbl_employee_emergency_contact`

**Entity**: `EmployeeEmergencyContact`

**Fields Inserted**:
- user_ID (foreign key to tbl_Users)
- first_name, mid_name, last_name, suffix
- relationship (or relationship_other if "Other" selected)
- contact_number, address

### 4. Salary Information Creation

**Table**: `tbl_salary_info`

**Entity**: `SalaryInfo`

**Fields Inserted**:
- user_ID (foreign key to tbl_Users)
- base_salary, allowance
- total_salary (calculated: base + allowance)
- is_active (default: true)
- effective_date (default: current date)

---

## Functions Reference

### AddEmployee Component

| Method | Purpose | Returns |
|--------|---------|---------|
| `HandleSubmitClick()` | Validates form and shows confirmation | `Task` |
| `SubmitEmployee()` | Maps data and calls service | `Task` |
| `GenerateSystemIdAsync()` | Generates next system ID | `Task<string>` |
| `OnRoleChangedAsync()` | Handles role selection | `Task` |
| `OnRelationshipChanged()` | Handles relationship "Other" selection | `void` |
| `CalculateTotalSalary()` | Calculates total salary | `void` |

### EmployeeService

| Method | Purpose | Returns |
|--------|---------|---------|
| `RegisterEmployeeAsync()` | Registers new employee | `Task<int>` (user_ID) |
| `GetEmployeeByIdAsync()` | Gets employee by ID | `Task<(User, Address, Contact, Salary)>` |

### UserRepository

| Method | Purpose | Returns |
|--------|---------|---------|
| `InsertAsync()` | Inserts user into database | `Task<int>` |
| `GetBySystemIdAsync()` | Gets user by system ID | `Task<User?>` |
| `GetNextSystemIdAsync()` | Generates next system ID | `Task<string>` |

---

## Terminologies

### View Model
A class that holds data for the UI form. Contains validation attributes and represents the structure of form input.

**Example**: `EmployeeFormData` - holds all form field values

### DTO (Data Transfer Object)
A simple class that transfers data between layers (UI → Service → Database). Separates UI structure from database structure.

**Example**: `EmployeeRegistrationData` - transfers form data to service layer

### Entity Model
A C# class that represents a database table. Used by EF Core to map objects to database rows.

**Example**: `EmployeeAddress` - represents `tbl_employee_address` table

### Repository Pattern
A design pattern that encapsulates database operations. Provides abstraction and security.

**Example**: `UserRepository` - handles all user database operations

### Transaction
A database operation that ensures all-or-nothing execution. If any step fails, all changes are rolled back.

**Example**: Employee registration uses transaction to ensure all tables are updated together

### System ID
A unique identifier for each user in the format BDES-XXXX (e.g., BDES-0001, BDES-0002). Auto-generated sequentially.

### BCrypt
A password hashing algorithm. Converts plain text passwords into secure hashes that cannot be reversed.

---

## Summary

✅ **Form data** collected in `EmployeeFormData`  
✅ **Mapped to DTO** (`EmployeeRegistrationData`)  
✅ **User created** via `UserRepository`  
✅ **Employee data created** via EF Core transaction  
✅ **System ID auto-generated** from database  
✅ **Transaction ensures** data integrity  

**All employee data is saved atomically - either all succeed or all fail!**

