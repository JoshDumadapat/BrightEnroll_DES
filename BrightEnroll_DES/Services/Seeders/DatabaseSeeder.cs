using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.Repositories;
using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;

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
                var exists = await _userRepository.ExistsBySystemIdAsync("BDES-0001");
                if (exists)
                {
                    // Admin exists, check for other test accounts
                    await SeedSuperAdminAsync();
                    await SeedTestTeacherAsync();
                    await SeedTestCashierAsync();
                    return;
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword("Admin123456");

                DateTime birthdate = new DateTime(2000, 1, 1);
                DateTime today = DateTime.Today;
                byte age = (byte)(today.Year - birthdate.Year);
                if (birthdate.Date > today.AddYears(-age)) age--;

                var adminUser = new User
                {
                    system_ID = "BDES-0001",
                    first_name = "Josh",
                    mid_name = null,
                    last_name = "Vanderson",
                    suffix = null,
                    birthdate = birthdate,
                    age = age,
                    gender = "male",
                    contact_num = "09366669571",
                    user_role = "Admin",
                    email = "joshvanderson01@gmail.com",
                    password = hashedPassword,
                    date_hired = DateTime.Now,
                    status = "active"
                };

                // InsertAsync now returns the generated UserId directly (EF Core)
                int userId = await _userRepository.InsertAsync(adminUser);

                // Create employee-related records in a transaction
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
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

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Failed to create admin employee records: {ex.Message}", ex);
                }

                // Seed all test accounts
                await SeedSuperAdminAsync();
                await SeedTestTeacherAsync();
                await SeedTestCashierAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error seeding initial admin: {ex.Message}", ex);
            }
        }

        private async Task SeedSuperAdminAsync()
        {
            try
            {
                // Check if Super Admin account already exists
                var exists = await _userRepository.ExistsBySystemIdAsync("BDES-SA-0001");
                if (exists)
                {
                    return;
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword("SuperAdmin123456");

                DateTime birthdate = new DateTime(1985, 3, 10);
                DateTime today = DateTime.Today;
                byte age = (byte)(today.Year - birthdate.Year);
                if (birthdate.Date > today.AddYears(-age)) age--;

                var superAdminUser = new User
                {
                    system_ID = "BDES-SA-0001",
                    first_name = "Alexander",
                    mid_name = "Cruz",
                    last_name = "Rodriguez",
                    suffix = null,
                    birthdate = birthdate,
                    age = age,
                    gender = "male",
                    contact_num = "09171111111",
                    user_role = "Super Admin",
                    email = "admin@brightenroll-erp.com",
                    password = hashedPassword,
                    date_hired = DateTime.Now.AddYears(-5), // Senior employee - 5 years
                    status = "active"
                };

                // InsertAsync now returns the generated UserId directly (EF Core)
                int userId = await _userRepository.InsertAsync(superAdminUser);

                // Create Super Admin-related records in a transaction
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Add employee address (Company HQ)
                    var address = new EmployeeAddress
                    {
                        UserId = userId,
                        HouseNo = "1",
                        StreetName = "BrightEnroll Tower, Ayala Avenue",
                        Province = "Metro Manila",
                        City = "Makati City",
                        Barangay = "Salcedo Village",
                        Country = "Philippines",
                        ZipCode = "1227"
                    };

                    _context.EmployeeAddresses.Add(address);
                    await _context.SaveChangesAsync();

                    // Add emergency contact info
                    var emergencyContact = new EmployeeEmergencyContact
                    {
                        UserId = userId,
                        FirstName = "Sofia",
                        MiddleName = "Luz",
                        LastName = "Rodriguez",
                        Suffix = null,
                        Relationship = "Spouse",
                        ContactNumber = "09172222222",
                        Address = "1 BrightEnroll Tower, Ayala Avenue, Makati City, Metro Manila"
                    };

                    _context.EmployeeEmergencyContacts.Add(emergencyContact);
                    await _context.SaveChangesAsync();

                    // Add salary information (Higher tier for Super Admin)
                    var salaryInfo = new SalaryInfo
                    {
                        UserId = userId,
                        BaseSalary = 120000.00m, // Higher base salary for Super Admin
                        Allowance = 15000.00m,   // Higher allowance
                        DateEffective = DateTime.Today.AddYears(-5),
                        IsActive = true
                    };

                    _context.SalaryInfos.Add(salaryInfo);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    Console.WriteLine("? Super Admin account created successfully!");
                    Console.WriteLine($"   Email: {superAdminUser.email}");
                    Console.WriteLine($"   Password: SuperAdmin123456");
                    Console.WriteLine($"   System ID: {superAdminUser.system_ID}");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Failed to create Super Admin employee records: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error seeding Super Admin: {ex.Message}", ex);
            }
        }

        private async Task SeedTestTeacherAsync()
        {
            try
            {
                // Check if teacher account already exists
                var exists = await _userRepository.ExistsBySystemIdAsync("BDES-1001");
                if (exists)
                {
                    return;
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword("Teacher123456");

                DateTime birthdate = new DateTime(1995, 5, 15);
                DateTime today = DateTime.Today;
                byte age = (byte)(today.Year - birthdate.Year);
                if (birthdate.Date > today.AddYears(-age)) age--;

                var teacherUser = new User
                {
                    system_ID = "BDES-1001",
                    first_name = "Maria",
                    mid_name = "Santos",
                    last_name = "Garcia",
                    suffix = null,
                    birthdate = birthdate,
                    age = age,
                    gender = "female",
                    contact_num = "09171234567",
                    user_role = "Teacher",
                    email = "maria.garcia@brightenroll.com",
                    password = hashedPassword,
                    date_hired = DateTime.Now.AddMonths(-6),
                    status = "active"
                };

                // InsertAsync now returns the generated UserId directly (EF Core)
                int userId = await _userRepository.InsertAsync(teacherUser);

                // Create teacher-related records in a transaction
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Add employee address
                    var address = new EmployeeAddress
                    {
                        UserId = userId,
                        HouseNo = "456",
                        StreetName = "School Avenue",
                        Province = "Davao del Sur",
                        City = "Davao City",
                        Barangay = "Ma-a",
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
                        LastName = "Garcia",
                        Suffix = null,
                        Relationship = "Spouse",
                        ContactNumber = "09189876543",
                        Address = "456 School Avenue, Ma-a, Davao City, Davao del Sur"
                    };

                    _context.EmployeeEmergencyContacts.Add(emergencyContact);
                    await _context.SaveChangesAsync();

                    // Add salary information
                    var salaryInfo = new SalaryInfo
                    {
                        UserId = userId,
                        BaseSalary = 35000.00m,
                        Allowance = 3000.00m,
                        DateEffective = DateTime.Today.AddMonths(-6),
                        IsActive = true
                    };

                    _context.SalaryInfos.Add(salaryInfo);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Failed to create teacher employee records: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error seeding test teacher: {ex.Message}", ex);
            }
        }

        private async Task SeedTestCashierAsync()
        {
            try
            {
                // Check if cashier account already exists
                var exists = await _userRepository.ExistsBySystemIdAsync("BDES-2001");
                if (exists)
                {
                    return;
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword("Cashier123456");

                DateTime birthdate = new DateTime(1992, 8, 20);
                DateTime today = DateTime.Today;
                byte age = (byte)(today.Year - birthdate.Year);
                if (birthdate.Date > today.AddYears(-age)) age--;

                var cashierUser = new User
                {
                    system_ID = "BDES-2001",
                    first_name = "Rosa",
                    mid_name = "Luz",
                    last_name = "Mendoza",
                    suffix = null,
                    birthdate = birthdate,
                    age = age,
                    gender = "female",
                    contact_num = "09198765432",
                    user_role = "Cashier",
                    email = "rosa.mendoza@brightenroll.com",
                    password = hashedPassword,
                    date_hired = DateTime.Now.AddYears(-1),
                    status = "active"
                };

                // InsertAsync now returns the generated UserId directly (EF Core)
                int userId = await _userRepository.InsertAsync(cashierUser);

                // Create cashier-related records in a transaction
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Add employee address
                    var address = new EmployeeAddress
                    {
                        UserId = userId,
                        HouseNo = "789",
                        StreetName = "Finance Street",
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
                        FirstName = "Roberto",
                        MiddleName = null,
                        LastName = "Mendoza",
                        Suffix = null,
                        Relationship = "Spouse",
                        ContactNumber = "09187654321",
                        Address = "789 Finance Street, Buhangin, Davao City, Davao del Sur"
                    };

                    _context.EmployeeEmergencyContacts.Add(emergencyContact);
                    await _context.SaveChangesAsync();

                    // Add salary information
                    var salaryInfo = new SalaryInfo
                    {
                        UserId = userId,
                        BaseSalary = 28000.00m,
                        Allowance = 2500.00m,
                        DateEffective = DateTime.Today.AddYears(-1),
                        IsActive = true
                    };

                    _context.SalaryInfos.Add(salaryInfo);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Failed to create cashier employee records: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error seeding test cashier: {ex.Message}", ex);
            }
        }
    }
}

