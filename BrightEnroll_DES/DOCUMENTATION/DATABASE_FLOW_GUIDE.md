# Database Management Flow Guide

## Overview

This guide shows the simple flow of how the database is managed in BrightEnroll_DES, from creating models to using them in components.

---

## Database Initialization Flow

```
MauiProgram.cs
    ↓
Services/Database/Initialization/DatabaseInitializer.cs
    ↓
Services/Database/Definitions/TableDefinitions.cs
    ↓
SQL Server Database
```

**What happens:**
1. App starts → `MauiProgram.cs` calls `DatabaseInitializer`
2. `DatabaseInitializer` reads table definitions from `TableDefinitions.cs`
3. Creates tables in SQL Server if they don't exist

---

## Creating a New Database Table

### Step 1: Define Table Structure
**Location:** `Services/Database/Definitions/TableDefinitions.cs`

Add a new method:
```csharp
public static TableDefinition GetYourTableDefinition()
{
    return new TableDefinition
    {
        TableName = "tbl_YourTable",
        CreateTableScript = @"CREATE TABLE [dbo].[tbl_YourTable] (...)"
    };
}
```

Add it to `GetAllTableDefinitions()` list.

### Step 2: Create Entity Model
**Location:** `Data/Models/YourEntity.cs`

```csharp
public class YourEntity
{
    public int Id { get; set; }
    // ... properties
}
```

### Step 3: Register in AppDbContext
**Location:** `Data/AppDbContext.cs`

Add:
```csharp
public DbSet<YourEntity> YourEntities { get; set; }
```

### Step 4: Database Initialization
**Location:** `Services/Database/Initialization/DatabaseInitializer.cs`

The table is automatically created when the app starts (no code needed here).

---

## Data Flow: Model → Repository → Service → Component

### Complete Flow Diagram

```
Component (UI)
    ↓
Service (Business Logic)
    ↓
Repository (Data Access) OR AppDbContext (EF Core)
    ↓
Entity Model (Data/Models/)
    ↓
Database Table
```

---

## 1. Entity Models

**Location:** `Data/Models/`

**Purpose:** Represent database tables as C# classes

**Example:**
- `Data/Models/Student.cs` → `tbl_Students` table
- `Data/Models/UserEntity.cs` → `tbl_Users` table

**What to do:**
- Create a class that matches your database table structure
- Add properties that match table columns

---

## 2. AppDbContext

**Location:** `Data/AppDbContext.cs`

**Purpose:** Connects Entity Models to database tables

**What to do:**
- Add `DbSet<YourEntity> YourEntities { get; set; }` for each model
- This makes EF Core recognize your table

**Example:**
```csharp
public DbSet<Student> Students { get; set; }
public DbSet<Guardian> Guardians { get; set; }
```

---

## 3. Repositories (Optional - for User table)

**Location:** `Services/DataAccess/Repositories/`

**Purpose:** Direct SQL operations with security (used for `tbl_Users`)

**Files:**
- `BaseRepository.cs` - Base class with common methods
- `UserRepository.cs` - Specific to User operations

**When to use:**
- For `tbl_Users` table only
- When you need direct SQL control

**Flow:**
```
Service → UserRepository → DBConnection → SQL Server
```

---

## 4. Business Services

**Location:** `Services/Business/`

**Purpose:** Contains business logic and data operations

**Structure:**
```
Services/Business/
    ├── Students/
    │   └── StudentService.cs
    ├── HR/
    │   └── EmployeeService.cs
    ├── Finance/
    │   ├── FeeService.cs
    │   └── ExpenseService.cs
    └── Academic/
        ├── CurriculumService.cs
        └── SchoolYearService.cs
```

**What to do:**
- Inject `AppDbContext` in constructor
- Use `_context.YourEntities.Add()` to insert
- Use `_context.YourEntities.ToListAsync()` to read
- Use `await _context.SaveChangesAsync()` to save

**Example:**
```csharp
public class YourService
{
    private readonly AppDbContext _context;
    
    public YourService(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task CreateAsync(YourEntity entity)
    {
        _context.YourEntities.Add(entity);
        await _context.SaveChangesAsync();
    }
}
```

---

## 5. Register Service in MauiProgram

**Location:** `MauiProgram.cs`

**What to do:**
Add your service to dependency injection:

```csharp
builder.Services.AddScoped<YourService>();
```

---

## 6. Use in Component

**Location:** `Components/Pages/...`

**What to do:**
1. Inject service in component:
```csharp
@inject YourService YourService
```

2. Use in code:
```csharp
private async Task LoadData()
{
    var data = await YourService.GetAllAsync();
}
```

---

## Common Patterns

### Pattern 1: EF Core (Most Tables)

```
Component → Service → AppDbContext → Entity Model → Database
```

**Used for:**
- Students, Guardians, Employees, Fees, Expenses, Curriculum, etc.

### Pattern 2: Repository (User Table Only)

```
Component → Service → UserRepository → DBConnection → Database
```

**Used for:**
- `tbl_Users` table only

---

## Quick Reference: Where to Put What

| What You're Creating | Where It Goes |
|---------------------|---------------|
| Database table definition | `Services/Database/Definitions/TableDefinitions.cs` |
| Entity model (C# class) | `Data/Models/YourEntity.cs` |
| Register model in context | `Data/AppDbContext.cs` |
| Business logic | `Services/Business/YourDomain/YourService.cs` |
| Register service | `MauiProgram.cs` |
| Use in UI | `Components/Pages/...` |

---

## Example: Adding a New Feature

### Scenario: Add a "Courses" feature

1. **Table Definition:**
   - Go to `Services/Database/Definitions/TableDefinitions.cs`
   - Add `GetCoursesTableDefinition()` method
   - Add to `GetAllTableDefinitions()` list

2. **Entity Model:**
   - Create `Data/Models/Course.cs`
   - Define properties matching table columns

3. **AppDbContext:**
   - Add `public DbSet<Course> Courses { get; set; }` in `Data/AppDbContext.cs`

4. **Service:**
   - Create `Services/Business/Academic/CourseService.cs`
   - Inject `AppDbContext`
   - Add methods like `GetAllAsync()`, `CreateAsync()`, etc.

5. **Register Service:**
   - Add `builder.Services.AddScoped<CourseService>();` in `MauiProgram.cs`

6. **Use in Component:**
   - Inject `@inject CourseService CourseService`
   - Call `await CourseService.GetAllAsync()`

---

## Database Connection

**Location:** `Services/Database/Connections/DBConnection.cs`

**Purpose:** Manages SQL Server connection string

**Configuration:**
- Connection string in `appsettings.json`
- Or uses default LocalDB connection

---

## Database Initialization

**Location:** `Services/Database/Initialization/DatabaseInitializer.cs`

**What it does:**
- Creates database if it doesn't exist
- Creates all tables from `TableDefinitions.cs`
- Adds missing columns to existing tables
- Creates views and stored procedures
- Seeds initial data (grade levels, admin user, etc.)

**When it runs:**
- Automatically on app startup (in `MauiProgram.cs`)

---

## Summary

**Simple Flow:**
1. Define table → `TableDefinitions.cs`
2. Create model → `Data/Models/`
3. Register in context → `AppDbContext.cs`
4. Create service → `Services/Business/`
5. Register service → `MauiProgram.cs`
6. Use in component → `Components/Pages/`

That's it! The database is managed through this flow.

