# Authentication System Guide

## Overview

This guide explains the authentication and authorization system in BrightEnroll_DES, including login flow, password security, and user management.

---

## Table of Contents

1. [Authentication Flow](#authentication-flow)
2. [Components](#components)
3. [Password Security](#password-security)
4. [User Roles](#user-roles)
5. [Session Management](#session-management)
6. [Functions Reference](#functions-reference)

---

## Authentication Flow

### Complete Login Process

```
User enters credentials
    ↓
Login.razor → HandleLogin()
    ↓
AuthService.LoginAsync(email, password)
    ↓
LoginService.ValidateUserCredentialsAsync(email, password)
    ↓
UserRepository.GetByEmailOrSystemIdAsync(email)
    ↓
DBConnection.ExecuteQueryAsync() → Database
    ↓
BCrypt.Verify(password, hashedPassword)
    ↓
[Success] → Set _currentUser, _isAuthenticated = true
    ↓
Navigate to /dashboard
```

### Step-by-Step

1. **User Input**: User enters email and password in login form
2. **Form Validation**: Fields validated (not empty, correct format)
3. **AuthService**: Calls LoginService to validate credentials
4. **LoginService**: Queries database for user by email
5. **Password Verification**: BCrypt compares plain password with hashed password
6. **Session Creation**: If valid, user object stored in AuthService
7. **Navigation**: User redirected to dashboard

---

## Components

### 1. LoginService

**Location**: `Services/AuthFunction/LoginService.cs`

**Purpose**: Validates user credentials against database

**Key Methods**:
- `ValidateUserCredentialsAsync(email, password)`: Validates email and password
- `GetUserByEmailAsync(email)`: Gets user by email only
- `UserExistsAsync(email)`: Checks if user exists

**How It Works**:
1. Queries database for user by email
2. Maps database row to User object
3. Verifies password using BCrypt
4. Returns User object if valid, null if invalid

### 2. AuthService

**Location**: `Services/AuthFunction/AuthService.cs`

**Purpose**: Manages authentication state and session

**Key Methods**:
- `LoginAsync(email, password)`: Authenticates user
- `Logout()`: Logs out current user
- `IsAuthenticated`: Property indicating if user is logged in
- `CurrentUser`: Property with current user object

**How It Works**:
1. Calls LoginService to validate credentials
2. If valid, stores user object and sets authenticated flag
3. Provides access to current user throughout application

### 3. DatabaseSeeder

**Location**: `Services/Seeders/DatabaseSeeder.cs`

**Purpose**: Creates initial admin account on startup

**Key Methods**:
- `SeedInitialAdminAsync()`: Creates admin user if it doesn't exist

**How It Works**:
1. Checks if BDES-0001 exists
2. If not, hashes password and inserts admin user
3. Called automatically on application startup

### 4. Login Component

**Location**: `Components/Pages/Auth/Login.razor`

**Purpose**: Login form UI and submission handling

**Key Methods**:
- `HandleLogin()`: Handles form submission
- `TogglePasswordVisibility()`: Shows/hides password

**How It Works**:
1. Displays login form
2. Validates input
3. Calls AuthService.LoginAsync()
4. Navigates to dashboard on success
5. Shows error on failure

---

## Password Security

### Password Hashing

All passwords are hashed using **BCrypt** before storage.

**Why BCrypt?**
- One-way hashing (cannot be reversed)
- Automatic salt generation
- Slow by design (prevents brute force attacks)
- Industry standard

### How It Works

**Registration/Seeding**:
```csharp
string plainPassword = "Admin123456";
string hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);
// Result: "$2a$11$..." (stored in database)
```

**Login Verification**:
```csharp
string plainPassword = userInput; // From form
string hashedPassword = user.password; // From database
bool isValid = BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
```

### Security Features

- ✅ **Passwords never stored in plain text**
- ✅ **Each password has unique salt**
- ✅ **Hashing is slow** (prevents brute force)
- ✅ **BCrypt handles salt automatically**

---

## User Roles

### Available Roles

- **Admin**: Full system access
- **System Admin**: System administration
- **HR**: Human resources management
- **Registrar**: Student registration
- **Cashier**: Payment processing
- **Teacher**: Teaching functions
- **Janitor**: Limited access
- **Other**: Custom role

### Role-Based Access

Roles determine:
- Which pages user can access
- Which functions are available
- System ID generation (for certain roles)
- Password requirements

### Role Storage

- Stored in `tbl_Users.user_role` column
- Accessed via `User.user_role` property
- Used for authorization checks

---

## Session Management

### Current User

The current logged-in user is stored in `AuthService`:

```csharp
// Get current user
var currentUser = AuthService.CurrentUser;

// Check if authenticated
if (AuthService.IsAuthenticated)
{
    // User is logged in
}
```

### Session Lifetime

- **Session starts**: When user successfully logs in
- **Session ends**: When user logs out or application closes
- **Storage**: In-memory (not persisted across app restarts)

### Logout

```csharp
AuthService.Logout();
Navigation.NavigateTo("/login", forceLoad: true);
```

---

## Functions Reference

### AuthService

| Method/Property | Purpose | Returns |
|----------------|---------|---------|
| `LoginAsync(email, password)` | Authenticates user | `bool` (success/failure) |
| `Logout()` | Logs out current user | `void` |
| `IsAuthenticated` | Checks if user is logged in | `bool` |
| `CurrentUser` | Gets current user object | `User?` |

### LoginService

| Method | Purpose | Parameters | Returns |
|--------|---------|------------|---------|
| `ValidateUserCredentialsAsync()` | Validates email and password | `email`, `password` | `User?` (null if invalid) |
| `GetUserByEmailAsync()` | Gets user by email only | `email` | `User?` |
| `UserExistsAsync()` | Checks if user exists | `email` | `bool` |

### DatabaseSeeder

| Method | Purpose | Returns |
|--------|---------|---------|
| `SeedInitialAdminAsync()` | Creates initial admin account | `Task` |

---

## Default Admin Account

### Credentials

- **Email**: joshvanderson01@gmail.com
- **System ID**: BDES-0001
- **Password**: Admin123456
- **Role**: Admin

### Creation

- Created automatically on first application startup
- Only created if BDES-0001 doesn't exist
- Password is hashed before storage

---

## Security Best Practices

### ✅ DO:

1. **Always hash passwords** before storage
2. **Use parameterized queries** for database operations
3. **Validate input** before processing
4. **Use BCrypt** for password hashing
5. **Check authentication** before accessing protected pages

### ❌ DON'T:

1. **Never store passwords in plain text**
2. **Never concatenate user input into SQL queries**
3. **Never trust user input** without validation
4. **Never expose password hashes** in error messages
5. **Never skip authentication checks**

---

## Summary

✅ **BCrypt** used for password hashing  
✅ **Parameterized queries** prevent SQL injection  
✅ **Session management** via AuthService  
✅ **Role-based access** control  
✅ **Automatic admin seeding** on startup  

**All authentication flows through AuthService and LoginService!**

