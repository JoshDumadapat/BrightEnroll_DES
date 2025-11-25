# ORM-Like Security Implementation Guide

## Overview

This document explains how we've implemented an ORM-like security pattern in the BrightEnroll_DES application to prevent SQL injection attacks and provide a secure, maintainable data access layer.

---

## Table of Contents

1. [What is ORM-Like Security?](#what-is-orm-like-security)
2. [Architecture Overview](#architecture-overview)
3. [Security Layers](#security-layers)
4. [Implementation Details](#implementation-details)
5. [How SQL Injection is Prevented](#how-sql-injection-is-prevented)
6. [Code Examples](#code-examples)
7. [Best Practices](#best-practices)
8. [File Structure](#file-structure)

---

## What is ORM-Like Security?

**ORM (Object-Relational Mapping)** is a programming technique that converts data between incompatible type systems using object-oriented programming languages. While we're not using a full ORM framework like Entity Framework, we've implemented **ORM-like patterns** that provide similar security benefits:

- **Parameterized Queries**: All user input is passed as parameters, never concatenated into SQL strings
- **Input Validation**: Data is validated before reaching the database
- **Input Sanitization**: Strings are trimmed and length-limited
- **Type Safety**: Strong typing prevents invalid data types
- **Repository Pattern**: Encapsulates database operations in a consistent, secure way

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
│  (Blazor Components, Services like LoginService)            │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                    Repository Layer                          │
│  (UserRepository, BaseRepository)                           │
│  • Input Validation                                          │
│  • Input Sanitization                                        │
│  • Parameter Creation                                        │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                    Database Connection Layer                  │
│  (DBConnection)                                              │
│  • Query Validation                                          │
│  • Parameter Binding                                         │
│  • Connection Management                                      │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                    SQL Server Database                       │
│  (tbl_Users, etc.)                                           │
└─────────────────────────────────────────────────────────────┘
```

---

## Security Layers

Our implementation uses **multiple layers of security** to prevent SQL injection:

### Layer 1: Repository Pattern
- **Location**: `Services/Repositories/`
- **Purpose**: Encapsulates all database operations
- **Security Features**:
  - Input validation
  - Input sanitization
  - Parameter creation
  - Entity mapping

### Layer 2: BaseRepository
- **Location**: `Services/Repositories/BaseRepository.cs`
- **Purpose**: Provides common security functions
- **Security Features**:
  - Query validation (checks for dangerous patterns)
  - String sanitization
  - Email validation
  - Parameter creation with type safety

### Layer 3: DBConnection
- **Location**: `Services/DBConnections/DBConnection.cs`
- **Purpose**: Handles actual database communication
- **Security Features**:
  - Parameterized query execution
  - Connection management
  - Error handling

---

## Implementation Details

### 1. Repository Pattern

**File**: `Services/Repositories/UserRepository.cs`

The repository pattern encapsulates all database operations for a specific entity (User). This provides:

- **Abstraction**: Business logic doesn't need to know SQL syntax
- **Security**: All queries are parameterized
- **Maintainability**: Changes to database structure only affect the repository

**Example**:
```csharp
public async Task<User?> GetByEmailAsync(string email)
{
    // Email is validated before query
    if (!IsValidEmail(email))
    {
        throw new ArgumentException("Invalid email format", nameof(email));
    }

    // Query uses parameterized input
    const string query = @"
        SELECT [user_ID], [system_ID], [first_name], ...
        FROM [dbo].[tbl_Users] 
        WHERE [email] = @Email";

    // Input is sanitized and parameterized
    var parameters = new[]
    {
        CreateParameter("@Email", SanitizeString(email, 150), SqlDbType.VarChar)
    };

    // Query is executed safely
    var dataTable = await ExecuteQueryAsync(query, parameters);
    // ...
}
```

### 2. BaseRepository Security Functions

**File**: `Services/Repositories/BaseRepository.cs`

#### Query Validation
```csharp
private void ValidateQuery(string query)
{
    // Check for dangerous SQL patterns
    var dangerousPatterns = new[]
    {
        "; DROP", "; DELETE", "; TRUNCATE",
        "; EXEC", "; EXECUTE",
        "UNION SELECT", "--", "/*",
        "xp_", "sp_"
    };

    var upperQuery = query.ToUpperInvariant();
    foreach (var pattern in dangerousPatterns)
    {
        if (upperQuery.Contains(pattern))
        {
            throw new ArgumentException(
                $"Query contains potentially dangerous pattern: {pattern}", 
                nameof(query)
            );
        }
    }
}
```

**Why this matters**: Even though we use parameterized queries, this adds an extra layer of protection against malicious queries.

#### String Sanitization
```csharp
protected string SanitizeString(string? input, int maxLength = 255)
{
    if (string.IsNullOrWhiteSpace(input))
        return string.Empty;

    var sanitized = input.Trim();
    
    // Prevent buffer overflow attacks
    if (sanitized.Length > maxLength)
    {
        sanitized = sanitized.Substring(0, maxLength);
    }

    return sanitized;
}
```

**Why this matters**: 
- Removes leading/trailing whitespace
- Prevents buffer overflow attacks by limiting length
- Ensures data fits database column constraints

#### Email Validation
```csharp
protected bool IsValidEmail(string email)
{
    if (string.IsNullOrWhiteSpace(email))
        return false;

    try
    {
        var addr = new System.Net.Mail.MailAddress(email);
        return addr.Address == email;
    }
    catch
    {
        return false;
    }
}
```

**Why this matters**: Validates email format before database operations, preventing invalid data and potential issues.

#### Parameter Creation
```csharp
protected SqlParameter CreateParameter(string parameterName, object? value, SqlDbType? dbType = null)
{
    var parameter = new SqlParameter(parameterName, value ?? DBNull.Value);
    
    if (dbType.HasValue)
    {
        parameter.SqlDbType = dbType.Value;
    }
    
    return parameter;
}
```

**Why this matters**: 
- Ensures all parameters are properly typed
- Handles null values correctly (converts to DBNull)
- Provides consistent parameter creation

### 3. Parameterized Queries

**All database queries use parameters**, never string concatenation:

#### ❌ **INSECURE** (Never do this):
```csharp
// BAD: String concatenation - VULNERABLE TO SQL INJECTION
string query = $"SELECT * FROM tbl_Users WHERE email = '{email}'";
```

#### ✅ **SECURE** (What we do):
```csharp
// GOOD: Parameterized query - SAFE FROM SQL INJECTION
const string query = @"
    SELECT [user_ID], [system_ID], [first_name], ...
    FROM [dbo].[tbl_Users] 
    WHERE [email] = @Email";

var parameters = new[]
{
    CreateParameter("@Email", SanitizeString(email, 150), SqlDbType.VarChar)
};

var dataTable = await ExecuteQueryAsync(query, parameters);
```

**How it works**:
1. SQL Server receives the query with a placeholder (`@Email`)
2. The parameter value is sent separately
3. SQL Server treats the parameter as **data**, not SQL code
4. Even if malicious input is provided, it cannot execute as SQL

---

## How SQL Injection is Prevented

### Example Attack Scenario

**Attacker's Input**:
```
email = "admin@example.com' OR '1'='1"
```

#### ❌ **Without Protection** (Vulnerable):
```sql
-- Query becomes:
SELECT * FROM tbl_Users WHERE email = 'admin@example.com' OR '1'='1'
-- This returns ALL users because '1'='1' is always true!
```

#### ✅ **With Our Protection** (Safe):
```sql
-- Query sent to SQL Server:
SELECT * FROM tbl_Users WHERE email = @Email

-- Parameter sent separately:
@Email = "admin@example.com' OR '1'='1"

-- SQL Server treats the parameter as a STRING VALUE, not SQL code
-- Result: No users found (because no email matches that exact string)
```

### Multiple Protection Layers

1. **Input Validation**: Email format is validated before query
2. **Input Sanitization**: String is trimmed and length-limited
3. **Parameterization**: Value is passed as a parameter, not concatenated
4. **Query Validation**: BaseRepository checks for dangerous patterns
5. **Type Safety**: SqlParameter ensures correct data types

---

## Code Examples

### Example 1: Retrieving a User by Email

**Before (Direct DBConnection - Less Secure)**:
```csharp
// Old way - still uses parameters but no validation/sanitization
string query = "SELECT * FROM tbl_Users WHERE email = @Email";
var parameters = new[] { new SqlParameter("@Email", email) };
var dataTable = await _dbConnection.ExecuteQueryAsync(query, parameters);
```

**After (Repository Pattern - More Secure)**:
```csharp
// New way - with validation, sanitization, and abstraction
var user = await _userRepository.GetByEmailAsync(email);
// Repository handles:
// - Email format validation
// - String sanitization
// - Parameter creation
// - Error handling
```

### Example 2: Inserting a User

**Before**:
```csharp
string insertQuery = @"
    INSERT INTO tbl_Users (first_name, email, ...)
    VALUES (@FirstName, @Email, ...)";

var parameters = new[]
{
    new SqlParameter("@FirstName", user.first_name),
    new SqlParameter("@Email", user.email),
    // ... more parameters
};

await _dbConnection.ExecuteNonQueryAsync(insertQuery, parameters);
```

**After**:
```csharp
// Repository validates and sanitizes all fields automatically
await _userRepository.InsertAsync(user);
// Repository handles:
// - Field validation (required fields, email format, etc.)
// - String sanitization (trim, length limits)
// - Parameter creation with proper types
// - Null value handling
```

### Example 3: Using the Repository in Services

**LoginService.cs**:
```csharp
public class LoginService : ILoginService
{
    private readonly IUserRepository _userRepository;

    public LoginService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> ValidateUserCredentialsAsync(string emailOrSystemId, string password)
    {
        // Repository handles all security concerns
        var user = await _userRepository.GetByEmailOrSystemIdAsync(emailOrSystemId);
        
        if (user == null)
            return null;

        // Verify password
        if (BCrypt.Net.BCrypt.Verify(password, user.password))
            return user;

        return null;
    }
}
```

---

## Best Practices

### ✅ **DO**:

1. **Always use repositories** for database operations
   ```csharp
   // ✅ Good
   var user = await _userRepository.GetByEmailAsync(email);
   ```

2. **Let the repository handle validation**
   ```csharp
   // ✅ Good - repository validates email format
   var user = await _userRepository.GetByEmailAsync(email);
   ```

3. **Use const for SQL queries**
   ```csharp
   // ✅ Good - query is constant, can't be modified
   const string query = "SELECT * FROM tbl_Users WHERE email = @Email";
   ```

4. **Always use parameters**
   ```csharp
   // ✅ Good - parameterized
   var parameters = new[] { CreateParameter("@Email", email, SqlDbType.VarChar) };
   ```

### ❌ **DON'T**:

1. **Never concatenate user input into SQL**
   ```csharp
   // ❌ BAD - VULNERABLE TO SQL INJECTION
   string query = $"SELECT * FROM tbl_Users WHERE email = '{email}'";
   ```

2. **Never bypass the repository**
   ```csharp
   // ❌ BAD - bypasses validation and sanitization
   await _dbConnection.ExecuteQueryAsync(query, parameters);
   ```

3. **Never use dynamic SQL with user input**
   ```csharp
   // ❌ BAD - dangerous
   string query = "SELECT * FROM " + tableName + " WHERE id = @Id";
   ```

4. **Never trust user input**
   ```csharp
   // ❌ BAD - no validation
   var user = await _userRepository.GetByEmailAsync(userInput);
   // ✅ Good - validate first if needed
   if (IsValidEmail(userInput))
   {
       var user = await _userRepository.GetByEmailAsync(userInput);
   }
   ```

---

## File Structure

```
Services/
├── DBConnections/
│   └── DBConnection.cs              # Low-level database connection
│
├── Repositories/
│   ├── IRepository.cs               # Base repository interface
│   ├── BaseRepository.cs            # Base repository with security functions
│   └── UserRepository.cs            # User-specific repository
│
└── AuthFunction/
    ├── LoginService.cs              # Uses UserRepository
    ├── AuthService.cs               # Uses LoginService
    └── DatabaseSeeder.cs            # Uses UserRepository
```

### Dependency Flow

```
AuthService
    ↓ uses
LoginService
    ↓ uses
UserRepository (IUserRepository)
    ↓ inherits from
BaseRepository
    ↓ uses
DBConnection
    ↓ connects to
SQL Server Database
```

---

## Summary

### Key Security Features

1. ✅ **Parameterized Queries**: All user input is parameterized
2. ✅ **Input Validation**: Data is validated before database operations
3. ✅ **Input Sanitization**: Strings are trimmed and length-limited
4. ✅ **Query Validation**: Dangerous SQL patterns are detected
5. ✅ **Type Safety**: Strong typing prevents invalid data types
6. ✅ **Repository Pattern**: Encapsulates all database operations
7. ✅ **Error Handling**: Proper exception handling prevents information leakage

### Benefits

- **Security**: Multiple layers prevent SQL injection
- **Maintainability**: Changes to database structure only affect repositories
- **Testability**: Repositories can be easily mocked for testing
- **Consistency**: All database operations follow the same pattern
- **Type Safety**: Compile-time checks prevent many errors

### When Adding New Entities

1. Create a repository interface (e.g., `IStudentRepository`)
2. Create a repository class inheriting from `BaseRepository`
3. Implement CRUD operations using parameterized queries
4. Register the repository in `MauiProgram.cs`
5. Use the repository in your services

**Example**:
```csharp
// 1. Create interface
public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(int id);
    Task<int> InsertAsync(Student student);
    // ... more methods
}

// 2. Create repository
public class StudentRepository : BaseRepository, IStudentRepository
{
    public StudentRepository(DBConnection dbConnection) : base(dbConnection) { }
    
    public async Task<Student?> GetByIdAsync(int id)
    {
        const string query = "SELECT * FROM tbl_Students WHERE student_ID = @Id";
        var parameters = new[] { CreateParameter("@Id", id, SqlDbType.Int) };
        // ... implementation
    }
}

// 3. Register in MauiProgram.cs
builder.Services.AddSingleton<IStudentRepository, StudentRepository>();

// 4. Use in services
public class StudentService
{
    private readonly IStudentRepository _studentRepository;
    
    public StudentService(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }
    
    public async Task<Student?> GetStudentAsync(int id)
    {
        return await _studentRepository.GetByIdAsync(id);
    }
}
```

---

## Conclusion

This ORM-like security implementation provides enterprise-grade protection against SQL injection attacks while maintaining code clarity and maintainability. By following the repository pattern and using parameterized queries, we ensure that all database operations are secure by default.

**Remember**: Security is not optional. Always use repositories, always parameterize queries, and always validate input.

