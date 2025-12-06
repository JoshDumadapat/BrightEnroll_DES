using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.DataAccess.Repositories;
using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using BrightEnroll_DES.Services.Database.Definitions;
using Microsoft.Data.SqlClient;

namespace BrightEnroll_DES.Services.Seeders
{
    public class DatabaseSeeder
    {
        private readonly IUserRepository _userRepository;
        private readonly AppDbContext _context;

        public DatabaseSeeder(IUserRepository userRepository, AppDbContext context)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task SeedInitialAdminAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== SEEDING ADMIN USER ===");
                
                // Try to seed admin role (non-critical - will skip if table doesn't exist)
                try
                {
                    await SeedAdminRoleAsync();
                    System.Diagnostics.Debug.WriteLine("Admin role check completed.");
                }
                catch (Exception roleEx)
                {
                    System.Diagnostics.Debug.WriteLine($"WARNING: Could not seed admin role: {roleEx.Message}. Continuing with user creation...");
                }

                var exists = await _context.Users.AnyAsync(u => u.SystemId == "BDES-0001");
                if (exists)
                {
                    System.Diagnostics.Debug.WriteLine("Admin user (BDES-0001) already exists, skipping seed.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Starting to seed admin user (BDES-0001)...");
                
                // Ensure database is connected
                if (!await _context.Database.CanConnectAsync())
                {
                    throw new Exception("Cannot connect to database!");
                }
                System.Diagnostics.Debug.WriteLine("Database connection verified.");

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword("Admin123456");

                DateTime birthdate = new DateTime(2000, 1, 1);
                DateTime today = DateTime.Today;
                byte age = (byte)(today.Year - birthdate.Year);
                if (birthdate.Date > today.AddYears(-age)) age--;

                // Use a single transaction for everything
                System.Diagnostics.Debug.WriteLine("Starting transaction for admin user and employee records...");
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create user entity directly using the same context
                    var adminUserEntity = new BrightEnroll_DES.Data.Models.UserEntity
                    {
                        SystemId = "BDES-0001",
                        FirstName = "Josh",
                        MidName = null,
                        LastName = "Vanderson",
                        Suffix = null,
                        Birthdate = birthdate,
                        Age = age,
                        Gender = "male",
                        ContactNum = "09366669571",
                        UserRole = "Admin",
                        Email = "joshvanderson01@gmail.com",
                        Password = hashedPassword,
                        DateHired = DateTime.Now,
                        Status = "active"
                    };

                    System.Diagnostics.Debug.WriteLine("Adding admin user to context...");
                    _context.Users.Add(adminUserEntity);
                    
                    // Save changes to get the UserId
                    int saveResult = await _context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"SaveChangesAsync returned: {saveResult} rows affected");
                    
                    // Reload to get the generated ID
                    await _context.Entry(adminUserEntity).ReloadAsync();
                    int userId = adminUserEntity.UserId;
                    
                    if (userId <= 0)
                    {
                        throw new Exception($"CRITICAL: UserId was not generated! UserId={userId}");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Admin user inserted successfully with UserId: {userId}");

                    // Create employee-related records in the same transaction
                    System.Diagnostics.Debug.WriteLine("Creating employee-related records (address, emergency contact, salary)...");
                    // Add employee address
                    var address = new EmployeeAddress
                    {
                        UserId = userId,
                        HouseNo = "123",
                        StreetName = "Main Street",
                        Province = "Davao del Sur",
                        City = "Davao City",
                        Barangay = "Poblacion",
                        Country = "Philippines",
                        ZipCode = "8000"
                    };

                    _context.EmployeeAddresses.Add(address);
                    await _context.SaveChangesAsync();

                    // Add emergency contact info
                    var emergencyContact = new EmployeeEmergencyContact
                    {
                        UserId = userId,
                        FirstName = "Jane",
                        MiddleName = null,
                        LastName = "Vanderson",
                        Suffix = null,
                        Relationship = "Spouse",
                        ContactNumber = "09123456789",
                        Address = "123 Main Street, Poblacion, Davao City, Davao del Sur"
                    };

                    _context.EmployeeEmergencyContacts.Add(emergencyContact);
                    await _context.SaveChangesAsync();

                    // Add salary information
                    var salaryInfo = new SalaryInfo
                    {
                        UserId = userId,
                        BaseSalary = 50000.00m,
                        Allowance = 5000.00m,
                        DateEffective = DateTime.Today,
                        IsActive = true
                    };

                    _context.SalaryInfos.Add(salaryInfo);
                    await _context.SaveChangesAsync();

                    System.Diagnostics.Debug.WriteLine("Committing transaction...");
                    await transaction.CommitAsync();
                    System.Diagnostics.Debug.WriteLine("Transaction committed successfully!");
                    
                    // Force context to accept changes
                    _context.ChangeTracker.AcceptAllChanges();
                    
                    // Verify the user was actually saved - use a fresh query
                    System.Diagnostics.Debug.WriteLine("Verifying user was saved...");
                    var verifyUser = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.SystemId == "BDES-0001");
                    if (verifyUser == null)
                    {
                        throw new Exception("CRITICAL: User was not saved to database after transaction commit!");
                    }
                    System.Diagnostics.Debug.WriteLine($"Admin user verified in database: UserId={verifyUser.UserId}, SystemId={verifyUser.SystemId}");
                    System.Diagnostics.Debug.WriteLine("Admin user seeding completed successfully!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"ERROR: Failed to create admin employee records: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                        System.Diagnostics.Debug.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
                    }
                    throw new Exception($"Failed to create admin employee records: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== CRITICAL ERROR SEEDING ADMIN ===");
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
                }
                throw new Exception($"Error seeding initial admin: {ex.Message}", ex);
            }
        }

        public async Task SeedInitialHRAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== SEEDING HR USER ===");
                
                // First, ensure HR role exists in tbl_roles
                await SeedAllRolesAsync();

                var exists = await _context.Users.AnyAsync(u => u.SystemId == "BDES-0002");
                if (exists)
                {
                    System.Diagnostics.Debug.WriteLine("HR user (BDES-0002) already exists, skipping seed.");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("Starting to seed HR user (BDES-0002)...");
                System.Diagnostics.Debug.WriteLine("Database connection verified.");

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword("HR123456");

                DateTime birthdate = new DateTime(1995, 5, 15);
                DateTime today = DateTime.Today;
                byte age = (byte)(today.Year - birthdate.Year);
                if (birthdate.Date > today.AddYears(-age)) age--;

                // Use a single transaction for everything
                System.Diagnostics.Debug.WriteLine("Starting transaction for HR user and employee records...");
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create user entity directly using the same context
                    var hrUserEntity = new BrightEnroll_DES.Data.Models.UserEntity
                    {
                        SystemId = "BDES-0002",
                        FirstName = "Maria",
                        MidName = "Cruz",
                        LastName = "Santos",
                        Suffix = null,
                        Birthdate = birthdate,
                        Age = age,
                        Gender = "female",
                        ContactNum = "09123456789",
                        UserRole = "HR",
                        Email = "hr@brightenroll.com",
                        Password = hashedPassword,
                        DateHired = DateTime.Now,
                        Status = "active"
                    };

                    System.Diagnostics.Debug.WriteLine("Adding HR user to context...");
                    _context.Users.Add(hrUserEntity);
                    
                    // Save changes to get the UserId
                    int saveResult = await _context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"SaveChangesAsync returned: {saveResult} rows affected");
                    
                    // Reload to get the generated ID
                    await _context.Entry(hrUserEntity).ReloadAsync();
                    int userId = hrUserEntity.UserId;
                    
                    if (userId <= 0)
                    {
                        throw new Exception($"CRITICAL: UserId was not generated! UserId={userId}");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"HR user inserted successfully with UserId: {userId}");

                    // Create employee-related records in the same transaction
                    // Add employee address
                    var address = new EmployeeAddress
                    {
                        UserId = userId,
                        HouseNo = "456",
                        StreetName = "HR Avenue",
                        Province = "Davao del Sur",
                        City = "Davao City",
                        Barangay = "Buhangin",
                        Country = "Philippines",
                        ZipCode = "8000"
                    };

                    _context.EmployeeAddresses.Add(address);
                    await _context.SaveChangesAsync();

                    // Add emergency contact info
                    var emergencyContact = new EmployeeEmergencyContact
                    {
                        UserId = userId,
                        FirstName = "Juan",
                        MiddleName = null,
                        LastName = "Santos",
                        Suffix = null,
                        Relationship = "Spouse",
                        ContactNumber = "09234567890",
                        Address = "456 HR Avenue, Buhangin, Davao City, Davao del Sur"
                    };

                    _context.EmployeeEmergencyContacts.Add(emergencyContact);
                    await _context.SaveChangesAsync();

                    // Add salary information (matching HR role salary: 45000 base + 5000 allowance)
                    var salaryInfo = new SalaryInfo
                    {
                        UserId = userId,
                        BaseSalary = 45000.00m,
                        Allowance = 5000.00m,
                        DateEffective = DateTime.Today,
                        IsActive = true
                    };

                    _context.SalaryInfos.Add(salaryInfo);
                    await _context.SaveChangesAsync();

                    System.Diagnostics.Debug.WriteLine("Committing HR transaction...");
                    await transaction.CommitAsync();
                    System.Diagnostics.Debug.WriteLine("HR transaction committed successfully!");
                    
                    // Force context to accept changes
                    _context.ChangeTracker.AcceptAllChanges();
                    
                    // Verify user was saved
                    System.Diagnostics.Debug.WriteLine("Verifying HR user was saved...");
                    var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.SystemId == "BDES-0002");
                    if (savedUser != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"HR user verified in database: UserId={savedUser.UserId}, SystemId={savedUser.SystemId}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("WARNING: HR user not found after commit!");
                    }
                    
                    System.Diagnostics.Debug.WriteLine("HR user seeding completed successfully!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"ERROR: HR transaction rolled back: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    throw new Exception($"Failed to create HR employee records: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: Error seeding initial HR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new Exception($"Error seeding initial HR: {ex.Message}", ex);
            }
        }

        public async Task SeedInitialSuperAdminAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== SEEDING SUPERADMIN USER ===");
                
                // First, ensure SuperAdmin role exists
                await SeedAllRolesAsync();

                var exists = await _context.Users.AnyAsync(u => u.SystemId == "BDES-SA-0001");
                if (exists)
                {
                    System.Diagnostics.Debug.WriteLine("SuperAdmin user (BDES-SA-0001) already exists, skipping seed.");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("Starting to seed SuperAdmin user (BDES-SA-0001)...");
                System.Diagnostics.Debug.WriteLine("Database connection verified.");

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword("SuperAdmin123456");

                DateTime birthdate = new DateTime(1990, 1, 1);
                DateTime today = DateTime.Today;
                byte age = (byte)(today.Year - birthdate.Year);
                if (birthdate.Date > today.AddYears(-age)) age--;

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var superAdminEntity = new BrightEnroll_DES.Data.Models.UserEntity
                    {
                        SystemId = "BDES-SA-0001",
                        FirstName = "Super",
                        MidName = null,
                        LastName = "Administrator",
                        Suffix = null,
                        Birthdate = birthdate,
                        Age = age,
                        Gender = "male",
                        ContactNum = "09111111111",
                        UserRole = "SuperAdmin",
                        Email = "superadmin@brightenroll.com",
                        Password = hashedPassword,
                        DateHired = DateTime.Now,
                        Status = "active"
                    };

                    System.Diagnostics.Debug.WriteLine("Adding SuperAdmin user to context...");
                    _context.Users.Add(superAdminEntity);
                    await _context.SaveChangesAsync();
                    await _context.Entry(superAdminEntity).ReloadAsync();
                    int userId = superAdminEntity.UserId;
                    
                    if (userId <= 0)
                    {
                        throw new Exception($"CRITICAL: UserId was not generated! UserId={userId}");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"SuperAdmin user inserted successfully with UserId: {userId}");

                    // Employee address
                    var address = new EmployeeAddress
                    {
                        UserId = userId,
                        HouseNo = "1",
                        StreetName = "Super Admin Street",
                        Province = "Davao del Sur",
                        City = "Davao City",
                        Barangay = "Centro",
                        Country = "Philippines",
                        ZipCode = "8000"
                    };
                    _context.EmployeeAddresses.Add(address);
                    await _context.SaveChangesAsync();

                    // Emergency contact
                    var emergencyContact = new EmployeeEmergencyContact
                    {
                        UserId = userId,
                        FirstName = "Admin",
                        MiddleName = null,
                        LastName = "Contact",
                        Suffix = null,
                        Relationship = "Spouse",
                        ContactNumber = "09222222222",
                        Address = "1 Super Admin Street, Centro, Davao City, Davao del Sur"
                    };
                    _context.EmployeeEmergencyContacts.Add(emergencyContact);
                    await _context.SaveChangesAsync();

                    // Salary info (60000 base + 10000 allowance)
                    var salaryInfo = new SalaryInfo
                    {
                        UserId = userId,
                        BaseSalary = 60000.00m,
                        Allowance = 10000.00m,
                        DateEffective = DateTime.Today,
                        IsActive = true
                    };
                    _context.SalaryInfos.Add(salaryInfo);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    System.Diagnostics.Debug.WriteLine("SuperAdmin transaction committed successfully!");
                    _context.ChangeTracker.AcceptAllChanges();
                    
                    var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.SystemId == "BDES-SA-0001");
                    if (savedUser != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"SuperAdmin user verified in database: UserId={savedUser.UserId}, SystemId={savedUser.SystemId}");
                    }
                    System.Diagnostics.Debug.WriteLine("SuperAdmin user seeding completed successfully!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"ERROR: SuperAdmin transaction rolled back: {ex.Message}");
                    throw new Exception($"Failed to create SuperAdmin employee records: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: Error seeding initial SuperAdmin: {ex.Message}");
                throw new Exception($"Error seeding initial SuperAdmin: {ex.Message}", ex);
            }
        }

        public async Task SeedInitialTeacherAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== SEEDING TEACHER USER ===");
                
                await SeedAllRolesAsync();

                var exists = await _context.Users.AnyAsync(u => u.SystemId == "BDES-TE-0001");
                if (exists)
                {
                    System.Diagnostics.Debug.WriteLine("Teacher user (BDES-TE-0001) already exists, skipping seed.");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("Starting to seed Teacher user (BDES-TE-0001)...");

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword("Teacher123456");

                DateTime birthdate = new DateTime(1992, 3, 20);
                DateTime today = DateTime.Today;
                byte age = (byte)(today.Year - birthdate.Year);
                if (birthdate.Date > today.AddYears(-age)) age--;

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var teacherEntity = new BrightEnroll_DES.Data.Models.UserEntity
                    {
                        SystemId = "BDES-TE-0001",
                        FirstName = "John",
                        MidName = "Doe",
                        LastName = "Smith",
                        Suffix = null,
                        Birthdate = birthdate,
                        Age = age,
                        Gender = "male",
                        ContactNum = "09333333333",
                        UserRole = "Teacher",
                        Email = "teacher@brightenroll.com",
                        Password = hashedPassword,
                        DateHired = DateTime.Now,
                        Status = "active"
                    };

                    _context.Users.Add(teacherEntity);
                    await _context.SaveChangesAsync();
                    await _context.Entry(teacherEntity).ReloadAsync();
                    int userId = teacherEntity.UserId;
                    
                    System.Diagnostics.Debug.WriteLine($"Teacher user inserted successfully with UserId: {userId}");

                    var address = new EmployeeAddress
                    {
                        UserId = userId,
                        HouseNo = "789",
                        StreetName = "Education Avenue",
                        Province = "Davao del Sur",
                        City = "Davao City",
                        Barangay = "Matina",
                        Country = "Philippines",
                        ZipCode = "8000"
                    };
                    _context.EmployeeAddresses.Add(address);
                    await _context.SaveChangesAsync();

                    var emergencyContact = new EmployeeEmergencyContact
                    {
                        UserId = userId,
                        FirstName = "Jane",
                        MiddleName = null,
                        LastName = "Smith",
                        Suffix = null,
                        Relationship = "Spouse",
                        ContactNumber = "09444444444",
                        Address = "789 Education Avenue, Matina, Davao City, Davao del Sur"
                    };
                    _context.EmployeeEmergencyContacts.Add(emergencyContact);
                    await _context.SaveChangesAsync();

                    var salaryInfo = new SalaryInfo
                    {
                        UserId = userId,
                        BaseSalary = 40000.00m,
                        Allowance = 4000.00m,
                        DateEffective = DateTime.Today,
                        IsActive = true
                    };
                    _context.SalaryInfos.Add(salaryInfo);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    System.Diagnostics.Debug.WriteLine("Teacher transaction committed successfully!");
                    _context.ChangeTracker.AcceptAllChanges();
                    
                    var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.SystemId == "BDES-TE-0001");
                    if (savedUser != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Teacher user verified in database: UserId={savedUser.UserId}, SystemId={savedUser.SystemId}");
                    }
                    System.Diagnostics.Debug.WriteLine("Teacher user seeding completed successfully!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"ERROR: Teacher transaction rolled back: {ex.Message}");
                    throw new Exception($"Failed to create Teacher employee records: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: Error seeding initial Teacher: {ex.Message}");
                throw new Exception($"Error seeding initial Teacher: {ex.Message}", ex);
            }
        }

        public async Task SeedInitialCashierAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== SEEDING CASHIER USER ===");
                
                await SeedAllRolesAsync();

                var exists = await _context.Users.AnyAsync(u => u.SystemId == "BDES-CA-0001");
                if (exists)
                {
                    System.Diagnostics.Debug.WriteLine("Cashier user (BDES-CA-0001) already exists, skipping seed.");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("Starting to seed Cashier user (BDES-CA-0001)...");

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword("Cashier123456");

                DateTime birthdate = new DateTime(1993, 6, 15);
                DateTime today = DateTime.Today;
                byte age = (byte)(today.Year - birthdate.Year);
                if (birthdate.Date > today.AddYears(-age)) age--;

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var cashierEntity = new BrightEnroll_DES.Data.Models.UserEntity
                    {
                        SystemId = "BDES-CA-0001",
                        FirstName = "Maria",
                        MidName = "Garcia",
                        LastName = "Lopez",
                        Suffix = null,
                        Birthdate = birthdate,
                        Age = age,
                        Gender = "female",
                        ContactNum = "09555555555",
                        UserRole = "Cashier",
                        Email = "cashier@brightenroll.com",
                        Password = hashedPassword,
                        DateHired = DateTime.Now,
                        Status = "active"
                    };

                    _context.Users.Add(cashierEntity);
                    await _context.SaveChangesAsync();
                    await _context.Entry(cashierEntity).ReloadAsync();
                    int userId = cashierEntity.UserId;
                    
                    System.Diagnostics.Debug.WriteLine($"Cashier user inserted successfully with UserId: {userId}");

                    var address = new EmployeeAddress
                    {
                        UserId = userId,
                        HouseNo = "321",
                        StreetName = "Finance Street",
                        Province = "Davao del Sur",
                        City = "Davao City",
                        Barangay = "Toril",
                        Country = "Philippines",
                        ZipCode = "8000"
                    };
                    _context.EmployeeAddresses.Add(address);
                    await _context.SaveChangesAsync();

                    var emergencyContact = new EmployeeEmergencyContact
                    {
                        UserId = userId,
                        FirstName = "Carlos",
                        MiddleName = null,
                        LastName = "Lopez",
                        Suffix = null,
                        Relationship = "Spouse",
                        ContactNumber = "09666666666",
                        Address = "321 Finance Street, Toril, Davao City, Davao del Sur"
                    };
                    _context.EmployeeEmergencyContacts.Add(emergencyContact);
                    await _context.SaveChangesAsync();

                    var salaryInfo = new SalaryInfo
                    {
                        UserId = userId,
                        BaseSalary = 30000.00m,
                        Allowance = 2000.00m,
                        DateEffective = DateTime.Today,
                        IsActive = true
                    };
                    _context.SalaryInfos.Add(salaryInfo);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    System.Diagnostics.Debug.WriteLine("Cashier transaction committed successfully!");
                    _context.ChangeTracker.AcceptAllChanges();
                    
                    var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.SystemId == "BDES-CA-0001");
                    if (savedUser != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Cashier user verified in database: UserId={savedUser.UserId}, SystemId={savedUser.SystemId}");
                    }
                    System.Diagnostics.Debug.WriteLine("Cashier user seeding completed successfully!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"ERROR: Cashier transaction rolled back: {ex.Message}");
                    throw new Exception($"Failed to create Cashier employee records: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: Error seeding initial Cashier: {ex.Message}");
                throw new Exception($"Error seeding initial Cashier: {ex.Message}", ex);
            }
        }

        public async Task SeedInitialRegistrarAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== SEEDING REGISTRAR USER ===");
                
                await SeedAllRolesAsync();

                var exists = await _context.Users.AnyAsync(u => u.SystemId == "BDES-RE-0001");
                if (exists)
                {
                    System.Diagnostics.Debug.WriteLine("Registrar user (BDES-RE-0001) already exists, skipping seed.");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("Starting to seed Registrar user (BDES-RE-0001)...");

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword("Registrar123456");

                DateTime birthdate = new DateTime(1991, 8, 10);
                DateTime today = DateTime.Today;
                byte age = (byte)(today.Year - birthdate.Year);
                if (birthdate.Date > today.AddYears(-age)) age--;

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var registrarEntity = new BrightEnroll_DES.Data.Models.UserEntity
                    {
                        SystemId = "BDES-RE-0001",
                        FirstName = "Anna",
                        MidName = "Rose",
                        LastName = "Rivera",
                        Suffix = null,
                        Birthdate = birthdate,
                        Age = age,
                        Gender = "female",
                        ContactNum = "09777777777",
                        UserRole = "Registrar",
                        Email = "registrar@brightenroll.com",
                        Password = hashedPassword,
                        DateHired = DateTime.Now,
                        Status = "active"
                    };

                    _context.Users.Add(registrarEntity);
                    await _context.SaveChangesAsync();
                    await _context.Entry(registrarEntity).ReloadAsync();
                    int userId = registrarEntity.UserId;
                    
                    System.Diagnostics.Debug.WriteLine($"Registrar user inserted successfully with UserId: {userId}");

                    var address = new EmployeeAddress
                    {
                        UserId = userId,
                        HouseNo = "654",
                        StreetName = "Registration Road",
                        Province = "Davao del Sur",
                        City = "Davao City",
                        Barangay = "Bajada",
                        Country = "Philippines",
                        ZipCode = "8000"
                    };
                    _context.EmployeeAddresses.Add(address);
                    await _context.SaveChangesAsync();

                    var emergencyContact = new EmployeeEmergencyContact
                    {
                        UserId = userId,
                        FirstName = "Roberto",
                        MiddleName = null,
                        LastName = "Rivera",
                        Suffix = null,
                        Relationship = "Spouse",
                        ContactNumber = "09888888888",
                        Address = "654 Registration Road, Bajada, Davao City, Davao del Sur"
                    };
                    _context.EmployeeEmergencyContacts.Add(emergencyContact);
                    await _context.SaveChangesAsync();

                    var salaryInfo = new SalaryInfo
                    {
                        UserId = userId,
                        BaseSalary = 35000.00m,
                        Allowance = 3000.00m,
                        DateEffective = DateTime.Today,
                        IsActive = true
                    };
                    _context.SalaryInfos.Add(salaryInfo);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    System.Diagnostics.Debug.WriteLine("Registrar transaction committed successfully!");
                    _context.ChangeTracker.AcceptAllChanges();
                    
                    var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.SystemId == "BDES-RE-0001");
                    if (savedUser != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Registrar user verified in database: UserId={savedUser.UserId}, SystemId={savedUser.SystemId}");
                    }
                    System.Diagnostics.Debug.WriteLine("Registrar user seeding completed successfully!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"ERROR: Registrar transaction rolled back: {ex.Message}");
                    throw new Exception($"Failed to create Registrar employee records: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: Error seeding initial Registrar: {ex.Message}");
                throw new Exception($"Error seeding initial Registrar: {ex.Message}", ex);
            }
        }

        public async Task SeedAdminRoleAsync()
        {
            try
            {
                // Check if Admin role already exists
                var existingRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleName == "Admin");
                
                if (existingRole != null)
                {
                    System.Diagnostics.Debug.WriteLine("Admin role already exists.");
                    return;
                }

                // Create Admin role with salary configuration matching the admin user
                var adminRole = new Role
                {
                    RoleName = "Admin",
                    BaseSalary = 50000.00m,
                    Allowance = 5000.00m,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.Roles.Add(adminRole);
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Admin role created successfully.");
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
            {
                System.Diagnostics.Debug.WriteLine("WARNING: tbl_roles table does not exist. Skipping role seeding.");
                // Don't throw - allow user creation to continue
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WARNING: Error seeding admin role: {ex.Message}. Continuing without role...");
                // Don't throw - allow user creation to continue
            }
        }

        /// <summary>
        /// Seeds all required roles in the system.
        /// </summary>
        public async Task SeedAllRolesAsync()
        {
            try
            {
                var requiredRoles = new[]
                {
                    new { Name = "SuperAdmin", BaseSalary = 60000.00m, Allowance = 10000.00m },
                    new { Name = "Admin", BaseSalary = 50000.00m, Allowance = 5000.00m },
                    new { Name = "Registrar", BaseSalary = 35000.00m, Allowance = 3000.00m },
                    new { Name = "Cashier", BaseSalary = 30000.00m, Allowance = 2000.00m },
                    new { Name = "Teacher", BaseSalary = 40000.00m, Allowance = 4000.00m },
                    new { Name = "HR", BaseSalary = 45000.00m, Allowance = 5000.00m }
                };

                foreach (var roleInfo in requiredRoles)
                {
                    var existingRole = await _context.Roles
                        .FirstOrDefaultAsync(r => r.RoleName == roleInfo.Name);
                    
                    if (existingRole == null)
                    {
                        var role = new Role
                        {
                            RoleName = roleInfo.Name,
                            BaseSalary = roleInfo.BaseSalary,
                            Allowance = roleInfo.Allowance,
                            IsActive = true,
                            CreatedDate = DateTime.Now
                        };

                        _context.Roles.Add(role);
                    }
                    else
                    {
                        // Update existing role to ensure it's active
                        existingRole.IsActive = true;
                        if (existingRole.UpdatedDate == null)
                        {
                            existingRole.UpdatedDate = DateTime.Now;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("All roles seeded successfully.");
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
            {
                System.Diagnostics.Debug.WriteLine("WARNING: tbl_roles table does not exist. Skipping role seeding.");
                // Don't throw - allow user seeding to continue
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WARNING: Error seeding all roles: {ex.Message}. This is non-critical.");
                // Don't throw - allow user seeding to continue
            }
        }

        public async Task SeedDeductionsAsync()
        {
            try
            {
                // Check if deductions already exist
                var existingDeductions = await _context.Deductions.AnyAsync();
                if (existingDeductions)
                {
                    System.Diagnostics.Debug.WriteLine("Deductions already exist. Skipping seeding.");
                    return;
                }

                var deductions = new List<Deduction>
                {
                    // SSS Contribution
                    new Deduction
                    {
                        DeductionType = "SSS",
                        DeductionName = "Social Security System",
                        RateOrValue = 0.11m, // 11%
                        IsPercentage = true,
                        MaxAmount = 2000.00m, // Capped at ₱2,000/month
                        MinAmount = null,
                        Description = "SSS contribution at 11% of base salary, capped at ₱2,000/month. Based on Republic Act No. 11199 (Social Security Act of 2018).",
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    },
                    // PhilHealth Contribution
                    new Deduction
                    {
                        DeductionType = "PHILHEALTH",
                        DeductionName = "Philippine Health Insurance Corporation",
                        RateOrValue = 0.03m, // 3%
                        IsPercentage = true,
                        MaxAmount = null,
                        MinAmount = null,
                        Description = "PhilHealth contribution at 3% of base salary. Based on Republic Act No. 11223 (Universal Health Care Act).",
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    },
                    // Pag-IBIG Contribution
                    new Deduction
                    {
                        DeductionType = "PAGIBIG",
                        DeductionName = "Home Development Mutual Fund",
                        RateOrValue = 0.02m, // 2%
                        IsPercentage = true,
                        MaxAmount = 200.00m, // Capped at ₱200/month
                        MinAmount = null,
                        Description = "Pag-IBIG contribution at 2% of base salary, capped at ₱200/month. Based on Republic Act No. 9679 (Pag-IBIG Fund Law of 2009).",
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    },
                    // Withholding Tax (Note: This uses progressive brackets, stored as base rate for reference)
                    new Deduction
                    {
                        DeductionType = "WITHHOLDING_TAX",
                        DeductionName = "Withholding Tax (Income Tax)",
                        RateOrValue = 0.00m, // Progressive brackets - calculated separately
                        IsPercentage = false, // Not a simple percentage
                        MaxAmount = null,
                        MinAmount = null,
                        Description = "Withholding tax based on TRAIN Law (RA 10963) progressive tax brackets. Taxable income = Gross Pay - (SSS + PhilHealth + Pag-IBIG). No tax if taxable income ≤ ₱20,833.33/month.",
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    }
                };

                _context.Deductions.AddRange(deductions);
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Deductions seeded successfully.");
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
            {
                System.Diagnostics.Debug.WriteLine("WARNING: tbl_deductions table does not exist. Skipping deductions seeding.");
                // Don't throw - allow seeding to continue
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WARNING: Error seeding deductions: {ex.Message}. This is non-critical.");
                // Don't throw - allow seeding to continue
            }
        }

        /// <summary>
        /// Verifies and creates finance tables if they don't exist.
        /// This method ensures all finance-related tables are present in the database.
        /// </summary>
        public async Task EnsureFinanceTablesExistAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== CHECKING FINANCE TABLES ===");
                
                // Get connection string from context
                var dbConnection = _context.Database.GetDbConnection();
                var connectionString = dbConnection.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new Exception("Cannot get connection string from database context.");
                }

                // Build connection string to target database
                var builder = new SqlConnectionStringBuilder(connectionString);
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();
                
                // List of finance tables that should exist
                var financeTables = new[]
                {
                    "tbl_GradeLevel",
                    "tbl_Fees",
                    "tbl_FeeBreakdown",
                    "tbl_Expenses",
                    "tbl_ExpenseAttachments",
                    "tbl_StudentPayments"
                };

                // Check which tables exist
                string checkAllTablesQuery = @"
                    SELECT t.name
                    FROM sys.tables t
                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE s.name = 'dbo' AND t.name IN ('tbl_GradeLevel', 'tbl_Fees', 'tbl_FeeBreakdown', 'tbl_Expenses', 'tbl_ExpenseAttachments', 'tbl_StudentPayments')";
                
                var existingTables = new HashSet<string>();
                using var checkCommand = new SqlCommand(checkAllTablesQuery, connection);
                using var reader = await checkCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    existingTables.Add(reader["name"].ToString() ?? string.Empty);
                }
                await reader.CloseAsync();

                // Get table definitions for finance tables
                var tableDefinitions = TableDefinitions.GetAllTableDefinitions();
                var financeTableDefinitions = new Dictionary<string, TableDefinition>();
                
                foreach (var tableDef in tableDefinitions)
                {
                    if (financeTables.Contains(tableDef.TableName))
                    {
                        financeTableDefinitions[tableDef.TableName] = tableDef;
                    }
                }

                bool anyTableCreated = false;

                // Create missing tables
                foreach (var tableName in financeTables)
                {
                    if (!existingTables.Contains(tableName))
                    {
                        System.Diagnostics.Debug.WriteLine($"Creating missing finance table: {tableName}");
                        
                        if (financeTableDefinitions.TryGetValue(tableName, out var tableDef))
                        {
                            // Create table
                            using var createCommand = new SqlCommand(tableDef.CreateTableScript, connection);
                            await createCommand.ExecuteNonQueryAsync();
                            
                            // Create indexes
                            foreach (var indexScript in tableDef.CreateIndexesScripts)
                            {
                                if (!string.IsNullOrWhiteSpace(indexScript))
                                {
                                    using var indexCommand = new SqlCommand(indexScript, connection);
                                    await indexCommand.ExecuteNonQueryAsync();
                                }
                            }
                            
                            anyTableCreated = true;
                            System.Diagnostics.Debug.WriteLine($"Successfully created table: {tableName}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"WARNING: Table definition not found for: {tableName}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Table already exists: {tableName}");
                    }
                }

                if (anyTableCreated)
                {
                    System.Diagnostics.Debug.WriteLine("=== FINANCE TABLES CREATED SUCCESSFULLY ===");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("=== ALL FINANCE TABLES ALREADY EXIST ===");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: Failed to ensure finance tables exist: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new Exception($"Failed to ensure finance tables exist: {ex.Message}", ex);
            }
        }
    }
}
