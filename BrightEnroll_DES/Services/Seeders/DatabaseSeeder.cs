using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.DataAccess.Repositories;
using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

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
                // First, ensure Admin role exists in tbl_roles
                await SeedAdminRoleAsync();

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
            }
            catch (Exception ex)
            {
                throw new Exception($"Error seeding admin role: {ex.Message}", ex);
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
            }
            catch (Exception ex)
            {
                throw new Exception($"Error seeding all roles: {ex.Message}", ex);
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
            }
            catch (Exception ex)
            {
                throw new Exception($"Error seeding deductions: {ex.Message}", ex);
            }
        }
    }
}
