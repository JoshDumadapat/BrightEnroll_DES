# ORM Setup Guide

## Overview

This guide explains the Object-Relational Mapping (ORM) implementation in BrightEnroll_DES. The system uses **Entity Framework Core** for database operations and an **ORM-like pattern** with repositories for security.

---

## Table of Contents

1. [What is ORM?](#what-is-orm)
2. [Entity Framework Core](#entity-framework-core)
3. [Repository Pattern (ORM-like)](#repository-pattern-orm-like)
4. [Entity Models](#entity-models)
5. [AppDbContext](#appdbcontext)
6. [Adding New Tables](#adding-new-tables)
7. [Security Features](#security-features)

---

## What is ORM?

**ORM (Object-Relational Mapping)** is a programming technique that converts data between incompatible type systems using object-oriented programming languages.

### Benefits

- **Type Safety**: Strong typing prevents errors
- **Code Reusability**: Write once, use everywhere
- **Maintainability**: Changes to database structure only affect entity models
- **Security**: Built-in protection against SQL injection

---

## Entity Framework Core

**Entity Framework Core (EF Core)** is Microsoft's official ORM framework for .NET.

### How It Works

1. **Entity Models** (C# classes) represent database tables
2. **AppDbContext** manages database connections and operations
3. **EF Core** automatically generates SQL queries
4. **Database** stores the data

### Example Flow

```csharp
// 1. Create entity
var student = new Student 
{ 
    FirstName = "Juan", 
    LastName = "Dela Cruz" 
};

// 2. Add to context
_context.Students.Add(student);

// 3. Save to database (EF Core generates SQL automatically)
await _context.SaveChangesAsync();
```

---

## Repository Pattern (ORM-like)

For additional security, we use a **Repository Pattern** that provides ORM-like security benefits without requiring EF Core for all operations.

### Architecture

```
Application Layer (Services)
    ↓
Repository Layer (UserRepository, etc.)
    ↓
Database Connection Layer (DBConnection)
    ↓
SQL Server Database
```

### Security Features

- **Parameterized Queries**: All user input is parameterized
- **Input Validation**: Data validated before database operations
- **Input Sanitization**: Strings trimmed and length-limited
- **Query Validation**: Dangerous SQL patterns detected
- **Type Safety**: Strong typing prevents invalid data

### Example

```csharp
// Repository handles all security
var user = await _userRepository.GetByEmailAsync(email);
// Repository automatically:
// - Validates email format
// - Sanitizes input
// - Uses parameterized queries
// - Handles errors
```

---

## Entity Models

Entity models are C# classes that represent database tables.

### Location

All entity models are located in: **`Data/Models/`**

### Current Entity Models

| Entity Model | Database Table | Purpose |
|--------------|----------------|---------|
| `User.cs` | `tbl_Users` | User accounts |
| `EmployeeAddress.cs` | `tbl_employee_address` | Employee addresses |
| `EmployeeEmergencyContact.cs` | `tbl_employee_emergency_contact` | Emergency contacts |
| `SalaryInfo.cs` | `tbl_salary_info` | Salary information |
| `Student.cs` | `tbl_Students` | Student records |
| `Guardian.cs` | `tbl_Guardians` | Guardian information |
| `StudentRequirement.cs` | `tbl_StudentRequirements` | Student requirements |

### Entity Model Structure

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("tbl_employee_address")]
public class EmployeeAddress
{
    [Key]
    [Column("address_ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AddressId { get; set; }

    [Required]
    [Column("user_ID")]
    public int UserId { get; set; }

    [MaxLength(50)]
    [Column("house_no")]
    public string? HouseNo { get; set; }
    
    // ... more properties
}
```

### Key Attributes

- **`[Table("table_name")]`**: Maps class to database table
- **`[Key]`**: Marks primary key property
- **`[Column("column_name")]`**: Maps property to database column
- **`[DatabaseGenerated]`**: Specifies auto-generated values (Identity)
- **`[Required]`**: Field cannot be null
- **`[MaxLength(n)]`**: Maximum string length

---

## AppDbContext

**AppDbContext** is the EF Core database context that manages all database operations.

### Location

**`Data/AppDbContext.cs`**

### Purpose

- Manages database connections
- Tracks entity changes
- Handles relationships between entities
- Maps C# entities to SQL Server tables

### Structure

```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) 
        : base(options) { }

    // DbSets represent database tables
    public DbSet<Student> Students { get; set; }
    public DbSet<Guardian> Guardians { get; set; }
    public DbSet<EmployeeAddress> EmployeeAddresses { get; set; }
    // ... more DbSets

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure relationships
        // Configure entity mappings
    }
}
```

### Registration

Registered in `MauiProgram.cs`:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});
```

---

## Adding New Tables

When adding a new table to the system:

### Step 1: Create Entity Model

Create a new file in `Data/Models/`:

```csharp
// Data/Models/YourEntity.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("tbl_your_table")]
public class YourEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;
}
```

### Step 2: Add to AppDbContext

Add DbSet to `Data/AppDbContext.cs`:

```csharp
public DbSet<YourEntity> YourEntities { get; set; }
```

### Step 3: Configure in OnModelCreating (if needed)

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Configure relationships, indexes, etc.
    modelBuilder.Entity<YourEntity>()
        .HasIndex(e => e.Name)
        .IsUnique();
}
```

### Step 4: Add Table Definition (for auto-creation)

Add to `Services/DBConnections/TableDefinitions.cs`:

```csharp
public static TableDefinition GetYourTableDefinition()
{
    return new TableDefinition
    {
        TableName = "tbl_your_table",
        SchemaName = "dbo",
        CreateTableScript = @"CREATE TABLE [dbo].[tbl_your_table](...)"
    };
}
```

Register in `GetAllTableDefinitions()`:

```csharp
public static List<TableDefinition> GetAllTableDefinitions()
{
    return new List<TableDefinition>
    {
        GetUsersTableDefinition(),
        GetYourTableDefinition(), // ← Add here
    };
}
```

---

## Security Features

### 1. Parameterized Queries

All queries use parameters, never string concatenation:

```csharp
// ✅ SECURE
const string query = "SELECT * FROM tbl_Users WHERE email = @Email";
var parameters = new[] { CreateParameter("@Email", email) };

// ❌ INSECURE (Never do this)
string query = $"SELECT * FROM tbl_Users WHERE email = '{email}'";
```

### 2. Input Validation

Data is validated before database operations:

```csharp
if (!IsValidEmail(email))
{
    throw new ArgumentException("Invalid email format");
}
```

### 3. Input Sanitization

Strings are trimmed and length-limited:

```csharp
protected string SanitizeString(string? input, int maxLength = 255)
{
    if (string.IsNullOrWhiteSpace(input))
        return string.Empty;
    
    var sanitized = input.Trim();
    if (sanitized.Length > maxLength)
        sanitized = sanitized.Substring(0, maxLength);
    
    return sanitized;
}
```

### 4. Query Validation

Dangerous SQL patterns are detected:

```csharp
private void ValidateQuery(string query)
{
    var dangerousPatterns = new[]
    {
        "; DROP", "; DELETE", "; TRUNCATE",
        "UNION SELECT", "--", "/*"
    };
    // Check for dangerous patterns...
}
```

---

## Summary

✅ **Entity Framework Core** handles ORM operations  
✅ **Repository Pattern** provides additional security  
✅ **Entity Models** in `Data/Models/` represent database tables  
✅ **AppDbContext** manages all EF Core operations  
✅ **Security** built-in with parameterized queries and validation  

**All table definitions go in `Data/Models/` as entity classes!**

