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
            }
            catch (Exception ex)
            {
                throw new Exception($"Error seeding initial admin: {ex.Message}", ex);
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
