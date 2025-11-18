# EF Core Implementation Guide

This document explains the Entity Framework Core implementation for the student registration system.

## 📁 Recommended Folder Structure

```
BrightEnroll_DES/
├── Data/
│   ├── Models/              # EF Core entity models
│   │   ├── Student.cs
│   │   ├── Guardian.cs
│   │   └── StudentRequirement.cs
│   └── AppDbContext.cs      # EF Core DbContext
│
├── Models/                  # View models / DTOs (existing)
│   └── StudentRegistrationModel.cs
│
├── Services/                # Business logic services
│   └── StudentService.cs   # Student registration service
│
├── Components/
│   └── Pages/
│       └── Auth/
│           └── StudentRegistration.razor.cs
│
└── MauiProgram.cs          # Dependency injection setup
```

## 🗄️ Database Models

### Student Model (`Data/Models/Student.cs`)
- Maps to `students_tbl` table
- Includes all student fields: personal info, addresses, enrollment details
- Navigation properties:
  - `Guardian` (one-to-many relationship)
  - `Requirements` (one-to-many relationship)

### Guardian Model (`Data/Models/Guardian.cs`)
- Maps to `guardians_tbl` table
- Navigation property: `Students` collection

### StudentRequirement Model (`Data/Models/StudentRequirement.cs`)
- Maps to `student_requirements_tbl` table
- Navigation property: `Student`

## 🔧 AppDbContext

Located in `Data/AppDbContext.cs`:
- Configured for SQL Server
- Includes all three `DbSet` properties
- Properly configured relationships:
  - Student → Guardian (many-to-one, restrict delete)
  - Student → Requirements (one-to-many, cascade delete)

## 🎯 StudentService

### RegisterStudentAsync Method

The `RegisterStudentAsync` method performs the following operations in a transaction:

1. **Inserts Guardian** first
   - Creates a new `Guardian` entity
   - Saves to database
   - Gets the generated `guardian_id`

2. **Inserts Student** linked to Guardian
   - Creates a new `Student` entity
   - Links to the guardian via `GuardianId`
   - Saves to database
   - Gets the generated `student_id`

3. **Automatically Inserts Requirements** based on `student_type`:
   - **New Student**: 
     - PSA Birth Certificate
     - Baptismal Certificate
     - Report Card
   - **Transferee**:
     - Form 138 (Report Card)
     - Form 137 (Permanent Record)
     - Good Moral Certificate
     - Transfer Certificate
   - **Returnee**:
     - Updated Enrollment Form
     - Clearance

All operations use **async/await** for database operations and are wrapped in a transaction for data integrity.

## 📝 Usage Example

### In Blazor Component (StudentRegistration.razor.cs)

```csharp
[Inject] private StudentService StudentService { get; set; } = null!;

private async Task SubmitRegistration()
{
    try
    {
        // Map from view model to data transfer object
        var studentData = new StudentRegistrationData
        {
            FirstName = registrationModel.FirstName,
            MiddleName = registrationModel.MiddleName,
            LastName = registrationModel.LastName,
            // ... map all other fields
            StudentType = registrationModel.StudentType,
            // ...
        };

        // Register student (creates guardian, student, and requirements)
        var registeredStudent = await StudentService.RegisterStudentAsync(studentData);
        
        // Success - student ID is available in registeredStudent.StudentId
        Navigation.NavigateTo("/login?toast=registration_submitted");
    }
    catch (Exception ex)
    {
        // Handle error
        toastMessage = $"Registration failed: {ex.Message}";
        showToast = true;
    }
}
```

## 🔌 Dependency Injection Setup

In `MauiProgram.cs`:

```csharp
// Register EF Core DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

// Register StudentService (scoped for EF Core DbContext)
builder.Services.AddScoped<StudentService>();
```

## 📦 Required NuGet Packages

The following packages have been added to `BrightEnroll_DES.csproj`:

- `Microsoft.EntityFrameworkCore` (9.0.0)
- `Microsoft.EntityFrameworkCore.SqlServer` (9.0.0)
- `Microsoft.EntityFrameworkCore.Design` (9.0.0)

## 🚀 Next Steps

1. **Run the application** - EF Core will use the existing connection string from `appsettings.json`

2. **Database Migration** (Optional):
   ```bash
   dotnet ef migrations add InitialStudentTables
   dotnet ef database update
   ```
   Note: Since tables already exist, you may need to create migrations manually or use `-IgnoreChanges` flag.

3. **Test Registration**:
   - Navigate to `/student-register`
   - Fill out the registration form
   - Submit and verify data is saved correctly

## 🔍 Additional Service Methods

The `StudentService` also includes:

- `GetStudentByIdAsync(int studentId)` - Retrieve a student with related data
- `GetAllStudentsAsync()` - Retrieve all students with related data

## ⚠️ Important Notes

1. **Transaction Safety**: All registration operations are wrapped in a database transaction. If any step fails, the entire operation is rolled back.

2. **Null Handling**: The service properly handles nullable fields and converts empty strings to null where appropriate.

3. **Student Type**: Requirements are automatically created based on the `student_type` field. Valid values are:
   - "New Student"
   - "Transferee"
   - "Returnee"

4. **LRN Handling**: If `LearnerReferenceNo` is "Pending" or empty, it's stored as `null` in the database.

5. **Service Lifetime**: `StudentService` is registered as **Scoped** because it uses `AppDbContext` which should be scoped in Blazor applications.

## 🐛 Troubleshooting

### Issue: "Cannot create database"
- Ensure SQL Server is running
- Verify connection string in `appsettings.json`
- Check database permissions

### Issue: "Foreign key constraint fails"
- Ensure guardian is created before student (handled automatically by service)
- Check that `GuardianId` is valid

### Issue: "Requirements not created"
- Verify `student_type` matches exactly: "New Student", "Transferee", or "Returnee"
- Check case sensitivity (service uses `ToLower()` for comparison)

