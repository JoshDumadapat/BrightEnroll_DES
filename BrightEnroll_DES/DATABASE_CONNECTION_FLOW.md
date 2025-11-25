# Database Connection and Login Flow - Step by Step Guide

## Overview
This document explains how the database connection and login authentication system works in the BrightEnroll_DES application.

---

## 📋 Table of Contents
1. [Application Startup Flow](#1-application-startup-flow)
2. [Database Connection Setup](#2-database-connection-setup)
3. [Database Seeding Flow](#3-database-seeding-flow)
4. [User Login Flow](#4-user-login-flow)
5. [Component Functions Reference](#5-component-functions-reference)

---

## 1. Application Startup Flow

### Step 1.1: Application Initialization
**File:** `MauiProgram.cs` → `CreateMauiApp()`

```
Application Starts
    ↓
MauiProgram.CreateMauiApp() is called
    ↓
MauiApp Builder is created
```

**What happens:**
- Creates the MAUI application builder
- Configures fonts and basic app settings
- Registers Blazor WebView

### Step 1.2: Service Registration
**File:** `MauiProgram.cs` (Lines 22-48)

```
Services are registered in dependency injection container:
    ↓
1. DBConnection (Singleton)
2. ILoginService → LoginService (Singleton)
3. IAuthService → AuthService (Singleton)
4. DatabaseSeeder (Singleton)
5. Other services...
```

**Registration Order:**
1. **DBConnection** - Registered first with connection string
2. **LoginService** - Depends on DBConnection (injected via constructor)
3. **AuthService** - Depends on ILoginService (injected via constructor)
4. **DatabaseSeeder** - Depends on DBConnection (injected via constructor)

**Code:**
```csharp
// DBConnection is registered with connection string
builder.Services.AddSingleton<DBConnection>(sp =>
{
    string connectionString = "Server=localhost;Database=YourDatabaseName;...";
    return new DBConnection(connectionString);
});

// LoginService receives DBConnection via dependency injection
builder.Services.AddSingleton<ILoginService, LoginService>();

// AuthService receives ILoginService via dependency injection
builder.Services.AddSingleton<IAuthService, AuthService>();
```

---

## 2. Database Connection Setup

### Step 2.1: DBConnection Class Initialization
**File:** `Services/DBConnections/DBConnection.cs`

**Constructor:**
```csharp
public DBConnection(string connectionString)
{
    _connectionString = connectionString; // Stores connection string
}
```

**What it does:**
- Stores the connection string in a private field
- Connection string is provided during service registration in `MauiProgram.cs`

### Step 2.2: Connection Methods Available

#### `GetConnection()`
**Purpose:** Creates a new SqlConnection object
```csharp
public SqlConnection GetConnection()
{
    return new SqlConnection(_connectionString);
}
```
- Returns a new connection object (doesn't open it yet)
- Connection is opened when needed

#### `TestConnectionAsync()`
**Purpose:** Tests if database connection works
```csharp
public async Task<bool> TestConnectionAsync()
{
    using var connection = GetConnection();
    await connection.OpenAsync(); // Opens connection
    return true; // Returns true if successful
}
```

#### `ExecuteQueryAsync(query, parameters)`
**Purpose:** Executes SELECT queries and returns DataTable
```csharp
public async Task<DataTable> ExecuteQueryAsync(string query, params SqlParameter[] parameters)
```
**Flow:**
1. Creates new SqlConnection
2. Opens connection
3. Creates SqlCommand with query and parameters
4. Executes query using SqlDataAdapter
5. Fills DataTable with results
6. Returns DataTable
7. Automatically closes connection (using statement)

**Used for:** SELECT statements that return data

#### `ExecuteNonQueryAsync(query, parameters)`
**Purpose:** Executes INSERT, UPDATE, DELETE queries
```csharp
public async Task<int> ExecuteNonQueryAsync(string query, params SqlParameter[] parameters)
```
**Flow:**
1. Creates new SqlConnection
2. Opens connection
3. Creates SqlCommand with query and parameters
4. Executes command
5. Returns number of affected rows
6. Automatically closes connection

**Used for:** INSERT, UPDATE, DELETE statements

#### `ExecuteScalarAsync(query, parameters)`
**Purpose:** Executes queries that return a single value
```csharp
public async Task<object?> ExecuteScalarAsync(string query, params SqlParameter[] parameters)
```
**Flow:**
1. Creates new SqlConnection
2. Opens connection
3. Creates SqlCommand with query and parameters
4. Executes scalar query (returns first row, first column)
5. Returns the value
6. Automatically closes connection

**Used for:** COUNT(*), MAX(), MIN(), or checking if record exists

---

## 3. Database Seeding Flow

### Step 3.1: Seeder Execution on Startup
**File:** `MauiProgram.cs` (Lines 57-67)

```
Application Builds
    ↓
app.Build() completes
    ↓
DatabaseSeeder is retrieved from service container
    ↓
SeedInitialAdminAsync() is called
```

**Code:**
```csharp
var app = builder.Build();

// Seed initial admin user on startup
var seeder = app.Services.GetRequiredService<DatabaseSeeder>();
Task.Run(async () => await seeder.SeedInitialAdminAsync()).Wait();
```

### Step 3.2: Seeder Logic
**File:** `Services/AuthFunction/DatabaseSeeder.cs` → `SeedInitialAdminAsync()`

**Step-by-step process:**

1. **Check if admin exists:**
   ```csharp
   string checkQuery = "SELECT COUNT(*) FROM tbl_Users WHERE system_ID = @SystemID";
   var exists = await _dbConnection.ExecuteScalarAsync(checkQuery, checkParams);
   ```
   - Uses `ExecuteScalarAsync` to count records
   - Checks if `system_ID = "BDES-0001"` exists
   - If count > 0, admin exists → **STOP** (skip seeding)

2. **Hash the password:**
   ```csharp
   string hashedPassword = BCrypt.Net.BCrypt.HashPassword("Admin123456");
   ```
   - Takes plain text password: `"Admin123456"`
   - Hashes it using BCrypt algorithm
   - Result: `"$2a$11$..."` (hashed string)

3. **Calculate age:**
   ```csharp
   DateTime birthdate = new DateTime(2000, 1, 1);
   byte age = (byte)(today.Year - birthdate.Year);
   ```
   - Calculates age from birthdate (January 1, 2000)
   - Adjusts if birthday hasn't occurred this year

4. **Insert admin user:**
   ```csharp
   string insertQuery = "INSERT INTO tbl_Users (...) VALUES (...)";
   await _dbConnection.ExecuteNonQueryAsync(insertQuery, parameters);
   ```
   - Uses `ExecuteNonQueryAsync` to insert record
   - Parameters include all user fields
   - Password is stored as hashed value

**Complete Flow:**
```
SeedInitialAdminAsync()
    ↓
Check if BDES-0001 exists → [No] → Continue
    ↓
Hash password "Admin123456" → "$2a$11$..."
    ↓
Calculate age from birthdate
    ↓
Insert user record with all fields
    ↓
Done! Admin account created
```

---

## 4. User Login Flow

### Step 4.1: User Enters Credentials
**File:** `Components/Pages/Auth/Login.razor`

```
User visits /login page
    ↓
User enters email and password
    ↓
User clicks "LOG IN" button
    ↓
HandleLogin() method is called
```

### Step 4.2: Form Validation
**File:** `Login.razor` → `HandleLogin()`

```csharp
private async Task HandleLogin()
{
    // 1. Reset all error flags
    emailError = false;
    passwordError = false;
    loginError = false;

    // 2. Validate fields are not empty
    if (string.IsNullOrWhiteSpace(_email))
        emailError = true;
    
    if (string.IsNullOrWhiteSpace(_password))
        passwordError = true;

    // 3. If validation fails, show errors and STOP
    if (!isValid) return;

    // 4. Call AuthService to authenticate
    var loginResult = await AuthService.LoginAsync(_email, _password);
}
```

### Step 4.3: AuthService Authentication
**File:** `Services/AuthFunction/AuthService.cs` → `LoginAsync()`

```csharp
public async Task<bool> LoginAsync(string username, string password)
{
    // 1. Call LoginService to validate credentials
    var user = await _loginService.ValidateUserCredentialsAsync(username, password);

    // 2. If user is found and password matches
    if (user != null)
    {
        _currentUser = user;        // Store user object
        _isAuthenticated = true;    // Set authenticated flag
        return true;                // Return success
    }

    // 3. If credentials invalid
    _isAuthenticated = false;
    _currentUser = null;
    return false;                   // Return failure
}
```

**Flow:**
```
LoginAsync(email, password)
    ↓
Calls LoginService.ValidateUserCredentialsAsync()
    ↓
[Wait for result]
    ↓
If user found → Set _currentUser and _isAuthenticated = true
    ↓
Return true/false
```

### Step 4.4: LoginService Database Query
**File:** `Services/AuthFunction/LoginService.cs` → `ValidateUserCredentialsAsync()`

**Step-by-step process:**

1. **Build SQL Query:**
   ```csharp
   string query = @"
       SELECT user_ID, system_ID, first_name, mid_name, last_name, suffix, 
              birthdate, age, gender, contact_num, user_role, email, password, date_hired
       FROM tbl_Users 
       WHERE email = @Email";
   ```
   - SELECT all user fields from `tbl_Users`
   - WHERE clause filters by email (parameterized to prevent SQL injection)

2. **Create SQL Parameter:**
   ```csharp
   var parameters = new[]
   {
       new SqlParameter("@Email", email)
   };
   ```
   - Creates parameterized query (safe from SQL injection)

3. **Execute Query:**
   ```csharp
   var dataTable = await _dbConnection.ExecuteQueryAsync(query, parameters);
   ```
   - Calls `DBConnection.ExecuteQueryAsync()`
   - Opens database connection
   - Executes SELECT query
   - Returns DataTable with results
   - Closes connection automatically

4. **Check if User Exists:**
   ```csharp
   if (dataTable.Rows.Count == 0)
   {
       return null; // User not found
   }
   ```

5. **Map Database Row to User Object:**
   ```csharp
   var row = dataTable.Rows[0];
   var user = MapDataRowToUser(row);
   ```
   - Extracts data from DataRow
   - Creates User model object
   - Handles NULL values for optional fields

6. **Verify Password:**
   ```csharp
   if (BCrypt.Net.BCrypt.Verify(password, user.password))
   {
       return user; // Password matches
   }
   return null; // Password doesn't match
   ```
   - Takes plain text password from user input
   - Takes hashed password from database
   - BCrypt.Verify() compares them
   - Returns true if passwords match

**Complete Flow:**
```
ValidateUserCredentialsAsync(email, password)
    ↓
Build SELECT query with @Email parameter
    ↓
ExecuteQueryAsync() → Opens DB connection
    ↓
Query executes: SELECT * FROM tbl_Users WHERE email = @Email
    ↓
DataTable returned with results
    ↓
Check if rows exist → [No] → Return null
    ↓
[Yes] → Map DataRow to User object
    ↓
BCrypt.Verify(plainPassword, hashedPassword)
    ↓
[Match] → Return User object
[No Match] → Return null
```

### Step 4.5: Login Result Handling
**File:** `Login.razor` → `HandleLogin()`

```csharp
if (loginResult)
{
    // SUCCESS: Redirect to dashboard
    Navigation.NavigateTo("/dashboard");
}
else
{
    // FAILURE: Show error message
    loginError = true;
    emailError = true;
    passwordError = true;
    StateHasChanged(); // Update UI
}
```

**Flow:**
```
LoginAsync returns true/false
    ↓
[True] → Navigate to /dashboard
    ↓
[False] → Show error message, highlight fields in red
```

---

## 5. Component Functions Reference

### 5.1 DBConnection Class
**Location:** `Services/DBConnections/DBConnection.cs`

| Function | Purpose | Returns | Used For |
|----------|---------|---------|----------|
| `GetConnection()` | Creates SqlConnection object | `SqlConnection` | Internal use |
| `TestConnectionAsync()` | Tests database connectivity | `bool` | Connection testing |
| `ExecuteQueryAsync()` | Executes SELECT queries | `DataTable` | Reading data |
| `ExecuteNonQueryAsync()` | Executes INSERT/UPDATE/DELETE | `int` (rows affected) | Modifying data |
| `ExecuteScalarAsync()` | Executes queries returning single value | `object?` | COUNT, EXISTS checks |

### 5.2 LoginService Class
**Location:** `Services/AuthFunction/LoginService.cs`

| Function | Purpose | Parameters | Returns |
|----------|---------|------------|---------|
| `ValidateUserCredentialsAsync()` | Validates email and password | `email`, `password` | `User?` (null if invalid) |
| `GetUserByEmailAsync()` | Gets user by email only | `email` | `User?` |
| `UserExistsAsync()` | Checks if user exists | `email` | `bool` |
| `MapDataRowToUser()` | Converts DataRow to User object | `DataRow` | `User` (private helper) |

### 5.3 AuthService Class
**Location:** `Services/AuthFunction/AuthService.cs`

| Function/Property | Purpose | Returns |
|------------------|---------|---------|
| `LoginAsync()` | Authenticates user credentials | `bool` (success/failure) |
| `IsAuthenticated` | Checks if user is logged in | `bool` |
| `CurrentUser` | Gets currently logged in user | `User?` |
| `Logout()` | Logs out current user | `void` |

### 5.4 DatabaseSeeder Class
**Location:** `Services/AuthFunction/DatabaseSeeder.cs`

| Function | Purpose | Returns |
|----------|---------|---------|
| `SeedInitialAdminAsync()` | Creates initial admin account if it doesn't exist | `Task` |

### 5.5 Login.razor Component
**Location:** `Components/Pages/Auth/Login.razor`

| Function | Purpose | Returns |
|----------|---------|---------|
| `HandleLogin()` | Handles form submission and calls AuthService | `Task` |
| `TogglePasswordVisibility()` | Shows/hides password | `void` |
| `NavigateToStudentRegistration()` | Navigates to registration page | `void` |

---

## 🔄 Complete Login Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    APPLICATION STARTUP                      │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  MauiProgram.CreateMauiApp()                                 │
│  1. Register DBConnection with connection string            │
│  2. Register LoginService (depends on DBConnection)         │
│  3. Register AuthService (depends on LoginService)          │
│  4. Register DatabaseSeeder                                 │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  DatabaseSeeder.SeedInitialAdminAsync()                     │
│  1. Check if BDES-0001 exists                               │
│  2. If not, hash password and insert admin user             │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                    USER LOGIN FLOW                           │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  Login.razor → HandleLogin()                                 │
│  1. Validate email and password fields                       │
│  2. Call AuthService.LoginAsync(email, password)            │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  AuthService.LoginAsync()                                    │
│  1. Call LoginService.ValidateUserCredentialsAsync()        │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  LoginService.ValidateUserCredentialsAsync()                │
│  1. Build SQL query: SELECT * FROM tbl_Users WHERE email=@  │
│  2. Call DBConnection.ExecuteQueryAsync()                   │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  DBConnection.ExecuteQueryAsync()                            │
│  1. Create SqlConnection                                     │
│  2. Open connection                                          │
│  3. Create SqlCommand with query and parameters              │
│  4. Execute query using SqlDataAdapter                       │
│  5. Fill DataTable with results                              │
│  6. Close connection (automatic)                             │
│  7. Return DataTable                                         │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  LoginService (continued)                                    │
│  1. Check if DataTable has rows                             │
│  2. Map DataRow to User object                               │
│  3. BCrypt.Verify(plainPassword, hashedPassword)           │
│  4. Return User object if valid, null if invalid             │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  AuthService (continued)                                     │
│  1. If user != null:                                         │
│     - Set _currentUser = user                               │
│     - Set _isAuthenticated = true                           │
│     - Return true                                            │
│  2. If user == null:                                         │
│     - Set _isAuthenticated = false                           │
│     - Return false                                           │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  Login.razor (continued)                                     │
│  1. If true: Navigate to /dashboard                          │
│  2. If false: Show error message                            │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔐 Security Features

### 1. **Parameterized Queries**
- All SQL queries use `@Parameter` syntax
- Prevents SQL injection attacks
- Example: `WHERE email = @Email` instead of `WHERE email = 'value'`

### 2. **Password Hashing**
- Passwords are hashed using BCrypt
- Plain passwords are never stored in database
- BCrypt automatically handles salt generation

### 3. **Connection Management**
- Connections are opened only when needed
- Automatically closed using `using` statements
- Prevents connection leaks

### 4. **Error Handling**
- Try-catch blocks around database operations
- Errors are caught and logged
- Application doesn't crash on database errors

---

## 📝 Key Points to Remember

1. **Dependency Injection:** Services are registered in `MauiProgram.cs` and automatically injected via constructors

2. **Connection String:** Must be updated in `MauiProgram.cs` line 28 with your actual database connection string

3. **Password Verification:** Uses BCrypt to compare plain text password with hashed password from database

4. **Service Lifetime:** All services are registered as Singletons (one instance for entire application lifetime)

5. **Async Operations:** All database operations are asynchronous to prevent UI blocking

6. **Data Mapping:** Database rows are mapped to User model objects using `MapDataRowToUser()` method

---

## 🛠️ Troubleshooting

### Connection Issues
- Verify connection string is correct
- Check SQL Server is running
- Verify database exists
- Check Windows Authentication or SQL Server credentials

### Login Not Working
- Verify admin account was seeded (check database)
- Check email matches exactly (case-sensitive)
- Verify password is correct: `Admin123456`
- Check BCrypt hashing is working

### Seeder Not Running
- Check database permissions
- Verify `tbl_Users` table exists
- Check connection string is valid
- Review error logs in Debug output

