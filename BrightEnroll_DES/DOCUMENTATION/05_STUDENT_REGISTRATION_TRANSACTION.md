# Student Registration Transaction Flow

## Overview

This guide explains the complete flow of student registration, from form submission to database storage.

---

## Table of Contents

1. [Transaction Flow](#transaction-flow)
2. [Data Models](#data-models)
3. [Component Structure](#component-structure)
4. [Service Layer](#service-layer)
5. [Database Operations](#database-operations)
6. [Automatic Requirements](#automatic-requirements)
7. [Functions Reference](#functions-reference)
8. [Terminologies](#termiologies)

---

## Transaction Flow

### Complete Flow Diagram

```
User fills Student Registration form
    ↓
StudentRegistrationModel (View Model)
    ↓
Form Validation
    ↓
SubmitRegistration() in StudentRegistration.razor.cs
    ↓
Map to StudentRegistrationData (DTO)
    ↓
StudentService.RegisterStudentAsync()
    ↓
[EF Core Transaction Start]
    ↓
Create Guardian → tbl_Guardians
    ↓
Get guardian_id
    ↓
Create Student → tbl_Students (linked to guardian)
    ↓
Get student_id
    ↓
Create Requirements (based on student_type)
    ↓
[Transaction Commit]
    ↓
Success → Navigate to login page
```

### Step-by-Step Process

1. **User Input**: User fills out Student Registration form
2. **Form Validation**: All required fields validated
3. **DTO Mapping**: StudentRegistrationModel → StudentRegistrationData
4. **Transaction Start**: EF Core begins database transaction
5. **Guardian Creation**: Insert into tbl_Guardians
6. **Student Creation**: Insert into tbl_Students (linked to guardian)
7. **Requirements Creation**: Automatically create requirements based on student type
8. **Transaction Commit**: All data saved or rolled back
9. **Success**: Navigate to login page with success message

---

## Data Models

### 1. StudentRegistrationModel (View Model)

**Location**: `Models/StudentRegistrationModel.cs`

**Purpose**: Holds form input data with validation attributes

**Contains**:
- Personal information (name, birthdate, age, sex, contact, email)
- Current address information
- Permanent address information
- Guardian information
- Enrollment details (student type, LRN, school year, grade level)

**Validation**:
- Required fields marked with `[Required]`
- Email validation with `[EmailAddress]`
- Range validation for age
- Regular expressions for contact numbers
- Custom validation for addresses

### 2. StudentRegistrationData (DTO)

**Location**: `Services/StudentService.cs`

**Purpose**: Data Transfer Object that maps form data to database entities

**Contains**:
- All fields from StudentRegistrationModel
- Used to transfer data between UI and service layers
- Maps to multiple database tables (Guardian, Student, Requirements)

### 3. Entity Models

**Location**: `Data/Models/`

**Entities Used**:
- `Student` → `tbl_Students`
- `Guardian` → `tbl_Guardians`
- `StudentRequirement` → `tbl_StudentRequirements`

---

## Component Structure

### StudentRegistration.razor

**Location**: `Components/Pages/Auth/StudentRegistration.razor`

**Purpose**: UI markup for the Student Registration form

**Sections**:
1. Personal Information
2. Current Address
3. Permanent Address
4. Guardian Information
5. Enrollment Details

### StudentRegistration.razor.cs (Code-Behind)

**Location**: `Components/Pages/Auth/StudentRegistration.razor` (code section)

**Key Properties**:
- `registrationModel`: StudentRegistrationModel instance
- `editContext`: EditContext for form validation
- `currentAddressHandler`: Manages current address dropdowns
- `permanentAddressHandler`: Manages permanent address dropdowns

**Key Methods**:
- `OnInitialized()`: Sets up handlers and loads school years
- `HandleSubmitClick()`: Validates form and shows confirmation modal
- `SubmitRegistration()`: Maps data and calls StudentService
- `LoadSchoolYearsAsync()`: Loads available school years

### Address Handlers

**Location**: `Components/Pages/Auth/Handlers/`

**Files**:
- `CurrentAddressHandler.cs`: Manages current address dropdowns
- `PermanentAddressHandler.cs`: Manages permanent address dropdowns

**Purpose**: Handle address dropdown functionality (province, city, barangay)

**Features**:
- Dropdown open/close management
- Filter cities based on province
- Filter barangays based on city
- Auto-populate ZIP codes
- "Same as Current Address" logic

---

## Service Layer

### StudentService

**Location**: `Services/StudentService.cs`

**Purpose**: Business logic for student registration

### RegisterStudentAsync Method

**Flow**:

```csharp
public async Task<Student> RegisterStudentAsync(StudentRegistrationData studentData)
{
    // Start EF Core transaction
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // Step 1: Create Guardian
        var guardian = new Guardian { ... };
        _context.Guardians.Add(guardian);
        await _context.SaveChangesAsync();
        int guardianId = guardian.GuardianId;
        
        // Step 2: Create Student (linked to guardian)
        var student = new Student 
        { 
            ...,
            GuardianId = guardianId 
        };
        _context.Students.Add(student);
        await _context.SaveChangesAsync();
        int studentId = student.StudentId;
        
        // Step 3: Create Requirements (based on student_type)
        var requirements = GetDefaultRequirements(studentData.StudentType);
        foreach (var req in requirements)
        {
            var requirement = new StudentRequirement
            {
                StudentId = studentId,
                RequirementName = req,
                Status = "Not Submitted",
                RequirementType = DetermineRequirementType(req)
            };
            _context.StudentRequirements.Add(requirement);
        }
        await _context.SaveChangesAsync();
        
        // Commit transaction
        await transaction.CommitAsync();
        
        return student;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

---

## Database Operations

### 1. Guardian Creation

**Table**: `tbl_Guardians`

**Entity**: `Guardian`

**Fields Inserted**:
- first_name, mid_name, last_name, suffix
- relationship_to_student
- contact_number, email
- occupation, address

**Generated**: `guardian_id` (auto-increment)

### 2. Student Creation

**Table**: `tbl_Students`

**Entity**: `Student`

**Fields Inserted**:
- guardian_id (foreign key to tbl_Guardians)
- Personal information (name, birthdate, age, sex, contact, email)
- Current address fields
- Permanent address fields
- Enrollment details (student_type, LRN, school_year, grade_level)

**Generated**: `student_id` (auto-increment)

### 3. Requirements Creation

**Table**: `tbl_StudentRequirements`

**Entity**: `StudentRequirement`

**Automatic Creation Based on Student Type**:

#### New Student
- PSA Birth Certificate
- Baptismal Certificate
- Report Card

#### Transferee
- Form 138 (Report Card)
- Form 137 (Permanent Record)
- Good Moral Certificate
- Transfer Certificate

#### Returnee
- Updated Enrollment Form
- Clearance

**Fields Inserted**:
- student_id (foreign key to tbl_Students)
- requirement_name
- status (default: "Not Submitted")
- requirement_type

---

## Automatic Requirements

### How It Works

Requirements are automatically created based on the `student_type` field selected in the form.

### GetDefaultRequirements Method

```csharp
private List<string> GetDefaultRequirements(string studentType)
{
    return studentType.ToLower() switch
    {
        "new student" => new List<string>
        {
            "PSA Birth Certificate",
            "Baptismal Certificate",
            "Report Card"
        },
        "transferee" => new List<string>
        {
            "Form 138 (Report Card)",
            "Form 137 (Permanent Record)",
            "Good Moral Certificate",
            "Transfer Certificate"
        },
        "returnee" => new List<string>
        {
            "Updated Enrollment Form",
            "Clearance"
        },
        _ => new List<string>()
    };
}
```

### Requirement Status

All requirements are created with status **"Not Submitted"** by default. The status can be updated later when documents are submitted.

---

## Functions Reference

### StudentRegistration Component

| Method | Purpose | Returns |
|--------|---------|---------|
| `OnInitialized()` | Sets up handlers and loads data | `Task` |
| `HandleSubmitClick()` | Validates form and shows confirmation | `Task` |
| `SubmitRegistration()` | Maps data and calls service | `Task` |
| `LoadSchoolYearsAsync()` | Loads available school years | `Task` |

### StudentService

| Method | Purpose | Returns |
|--------|---------|---------|
| `RegisterStudentAsync()` | Registers new student | `Task<Student>` |
| `GetStudentByIdAsync()` | Gets student by ID | `Task<Student?>` |
| `GetAllStudentsAsync()` | Gets all students | `Task<List<Student>>` |
| `GetDefaultRequirements()` | Gets requirements for student type | `List<string>` |

### Address Handlers

| Method | Purpose | Returns |
|--------|---------|---------|
| `ToggleProvinceDropdown()` | Opens/closes province dropdown | `void` |
| `SelectProvince()` | Selects province and filters cities | `void` |
| `SelectCity()` | Selects city and filters barangays | `void` |
| `SelectBarangay()` | Selects barangay and updates ZIP | `void` |

---

## Terminologies

### Student Type
The classification of the student:
- **New Student**: First-time enrollee
- **Transferee**: Transferring from another school
- **Returnee**: Returning after absence

### LRN (Learner Reference Number)
A unique identifier for each student. Can be "Pending" if not yet assigned.

### School Year
The academic year for enrollment (e.g., "2024-2025").

### Grade Level
The grade the student is enrolling in (e.g., "Grade 7", "Grade 11").

### Requirements
Documents required for enrollment. Automatically created based on student type.

### Guardian
The parent or legal guardian of the student. One guardian can have multiple students.

### Current Address vs Permanent Address
- **Current Address**: Where the student currently lives
- **Permanent Address**: The student's permanent residence
- Can be the same (checkbox option)

### Transaction Safety
All database operations are wrapped in a transaction. If any step fails, the entire operation is rolled back, ensuring data consistency.

---

## Summary

✅ **Form data** collected in `StudentRegistrationModel`  
✅ **Mapped to DTO** (`StudentRegistrationData`)  
✅ **Guardian created** first (gets guardian_id)  
✅ **Student created** second (linked to guardian)  
✅ **Requirements auto-created** based on student type  
✅ **Transaction ensures** data integrity  

**All student data is saved atomically - either all succeed or all fail!**

