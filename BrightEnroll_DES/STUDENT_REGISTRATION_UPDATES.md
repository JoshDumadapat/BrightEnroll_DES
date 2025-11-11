# Student Registration Updates - Logic Paths

This document outlines all the changes made to the Student Registration form and where the logic is located.

## Summary of Changes

### 1. Age Field ✅
- **Location**: `Components/Pages/Auth/StudentRegistration.razor` (Line 75-83)
- **Logic**: Auto-calculates from birth date
- **Method**: `CalculateAge()` in `@code` section (Line 720-729)
- **Required**: Yes

### 2. Updated Placeholders ✅
- **Location**: Throughout `Components/Pages/Auth/StudentRegistration.razor`
- **Changed**: All placeholders now show examples (e.g., "e.g. Juan" instead of "First Name")
- **Examples**:
  - First Name: "e.g. Juan"
  - Last Name: "e.g. Santos"
  - Place of Birth: "e.g. Davao City"
  - Contact Number: "e.g. 09123456789"
  - LRN: "e.g. 123456789012"

### 3. Address Autocomplete with Philippine Addresses ✅
- **Service Location**: `Services/AddressService.cs`
- **JavaScript Helper**: `wwwroot/js/address-autocomplete.js` (optional helper)
- **Main Logic**: C# service in `Services/AddressService.cs`
- **Features**:
  - Barangay autocomplete (filters based on city/province)
  - City autocomplete (filters based on province)
  - Province autocomplete
  - When city is selected, province auto-fills
  - When province changes, city and barangay reset
- **Methods in StudentRegistration.razor**:
  - `FilterBarangays()` (Line 830-842)
  - `FilterCities()` (Line 844-855)
  - `FilterProvinces()` (Line 857-867)
  - `SelectBarangay()`, `SelectCity()`, `SelectProvince()` (Line 869-896)

### 4. Requirements Based on Student Type ✅
- **Location**: `Components/Pages/Auth/StudentRegistration.razor` (Line 500-575)
- **Logic**: Conditional rendering based on `registrationModel.StudentType`
- **Student Types**:
  - **New Student**: PSA Birth Certificate, Baptismal Certificate, Report Card/Form 138
  - **Transferee**: PSA Birth Certificate, Baptismal Certificate, Form 138, Form 137, Good Moral Character, Transfer Certificate
  - **Returnee**: Form 138, Updated Enrollment Form, Clearance
- **Model Properties**: Added requirement checkboxes in model (Line 849-862)

### 5. LRN Field Modifications ✅
- **Location**: `Components/Pages/Auth/StudentRegistration.razor` (Line 453-465)
- **Features**:
  - Numbers only (onkeypress validation)
  - Maximum 12 characters (maxlength attribute)
  - Disabled when "No" is selected for "With LRN?"
  - Auto-sets to "Pending" when "No" is selected
- **Logic**: `HandleLRNChange()` method (Line 731-737)

### 6. School Year Dropdown with Add Modal ✅
- **Service Location**: `Services/SchoolYearService.cs`
- **Modal Location**: `Components/Pages/Auth/StudentRegistration.razor` (Line 622-660)
- **Features**:
  - Shows current school year + 2 future years
  - "+ Add School Year" option opens modal
  - Modal validates format (YYYY-YYYY)
  - Adds new school year to list
- **Methods**:
  - `LoadSchoolYears()` (Line 715-718)
  - `AddSchoolYear()` (Line 744-752)
  - `ValidateSchoolYearFormat()` (Line 739-742)
  - `OnSchoolYearChanged()` (Line 820-827)

### 7. Pre-school Option Added ✅
- **Location**: `Components/Pages/Auth/StudentRegistration.razor` (Line 485-497)
- **Change**: Added "Pre-school" as first option in Grade to Enroll dropdown

## Service Registration

All services are registered in `MauiProgram.cs`:
- `AddressService` - Line 25
- `SchoolYearService` - Line 28

## File Structure

```
BrightEnroll_DES/
├── Components/
│   └── Pages/
│       └── Auth/
│           └── StudentRegistration.razor (Main form with all UI and logic)
├── Services/
│   ├── AddressService.cs (Philippine address data and filtering logic)
│   └── SchoolYearService.cs (School year management logic)
├── wwwroot/
│   └── js/
│       └── address-autocomplete.js (Optional JavaScript helper)
└── MauiProgram.cs (Service registration)
```

## Key Logic Locations

1. **Address Autocomplete**: `Services/AddressService.cs` - Contains Philippine address data structure and filtering methods
2. **School Year Management**: `Services/SchoolYearService.cs` - Handles school year CRUD operations
3. **Form Logic**: `Components/Pages/Auth/StudentRegistration.razor` - Contains all form-specific logic in `@code` section
4. **Age Calculation**: Auto-calculated in `CalculateAge()` method when birth date changes

## Notes

- All address logic is in C# (AddressService.cs)
- School year logic is in C# (SchoolYearService.cs)
- JavaScript file (address-autocomplete.js) is optional and can be used for enhanced client-side functionality if needed
- All placeholders have been updated to show examples
- Requirements dynamically show/hide based on selected student type

