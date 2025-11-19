# BrightEnroll_DES Documentation

Complete documentation for the BrightEnroll_DES application.

---

## Documentation Index

### 1. [Database Setup Guide](01_DATABASE_SETUP.md)
Complete guide for setting up the database, including automatic setup and manual configuration.

### 2. [ORM Setup Guide](02_ORM_SETUP.md)
Explanation of Entity Framework Core and Repository Pattern implementation.

### 3. [Authentication System Guide](03_AUTHENTICATION.md)
Complete authentication flow, password security, and user management.

### 4. [Add Employee Transaction Flow](04_ADD_EMPLOYEE_TRANSACTION.md)
Step-by-step guide for the employee registration process.

### 5. [Student Registration Transaction Flow](05_STUDENT_REGISTRATION_TRANSACTION.md)
Step-by-step guide for the student registration process.

---

## Quick Reference

### Key Concepts

- **Entity Framework Core**: ORM framework for database operations
- **Repository Pattern**: Security layer for database access
- **DTO (Data Transfer Object)**: Transfers data between layers
- **View Model**: Holds form data with validation
- **Entity Model**: Represents database tables as C# classes

### Key Files

- **MauiProgram.cs**: Application startup and service registration
- **AppDbContext.cs**: EF Core database context
- **Data/Models/**: Entity models (database table representations)
- **Services/**: Business logic and services
- **Components/Pages/**: UI components and forms

### Common Terminologies

- **System ID**: Unique identifier (BDES-XXXX format)
- **BCrypt**: Password hashing algorithm
- **Transaction**: All-or-nothing database operation
- **DTO**: Data Transfer Object
- **Entity**: Database table representation
- **Repository**: Database access layer

---

## Getting Started

1. Read [Database Setup Guide](01_DATABASE_SETUP.md) to set up your database
2. Review [ORM Setup Guide](02_ORM_SETUP.md) to understand the data layer
3. Check [Authentication System Guide](03_AUTHENTICATION.md) for login flow
4. Review transaction flows for [Employee](04_ADD_EMPLOYEE_TRANSACTION.md) and [Student](05_STUDENT_REGISTRATION_TRANSACTION.md) registration

---

## Support

For issues or questions, refer to the troubleshooting sections in each guide.

