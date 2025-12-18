using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace BrightEnroll_DES.Services.Seeders;

public class AdminUserSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminUserSeeder>? _logger;

    public AdminUserSeeder(AppDbContext context, ILogger<AdminUserSeeder>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task SeedAsync(int count = 50)
    {
        try
        {
            _logger?.LogInformation("=== STARTING SCHOOL ADMIN SEEDING ===");

            // Check if admin users already exist
            var existingCount = await _context.Users
                .Where(u => u.UserRole == "Admin")
                .CountAsync();

            if (existingCount >= count)
            {
                _logger?.LogInformation($"School admins already seeded ({existingCount} exist). Skipping.");
                return;
            }

            var adminsToCreate = count - existingCount;
            var random = new Random();
            var firstNames = new[] { "John", "Jane", "Michael", "Maria", "Robert", "Sarah", "David", "Lisa", "James", "Anna", "William", "Emily", "Richard", "Jessica", "Joseph", "Amanda", "Thomas", "Michelle", "Charles", "Kimberly" };
            var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee" };
            var genders = new[] { "Male", "Female" };

            var admins = new List<UserEntity>();

            for (int i = 0; i < adminsToCreate; i++)
            {
                var firstName = firstNames[random.Next(firstNames.Length)];
                var lastName = lastNames[random.Next(lastNames.Length)];
                var gender = genders[random.Next(genders.Length)];
                var birthdate = new DateTime(random.Next(1970, 1990), random.Next(1, 13), random.Next(1, 29));
                var age = (byte)(DateTime.Today.Year - birthdate.Year);
                if (birthdate.Date > DateTime.Today.AddYears(-age)) age--;

                var systemId = $"ADMIN{existingCount + i + 1:D4}";
                var email = $"{firstName.ToLower()}.{lastName.ToLower()}{existingCount + i + 1}@schooladmin.edu.ph";

                // Check if email already exists
                var emailExists = await _context.Users.AnyAsync(u => u.Email == email);
                if (emailExists)
                {
                    email = $"{firstName.ToLower()}.{lastName.ToLower()}{existingCount + i + 1}_{Guid.NewGuid().ToString().Substring(0, 4)}@schooladmin.edu.ph";
                }

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword("Admin123456");

                var admin = new UserEntity
                {
                    SystemId = systemId,
                    FirstName = firstName,
                    MidName = null,
                    LastName = lastName,
                    Suffix = null,
                    Birthdate = birthdate,
                    Age = age,
                    Gender = gender,
                    ContactNum = $"09{random.Next(100000000, 999999999)}",
                    UserRole = "Admin",
                    Email = email,
                    Password = hashedPassword,
                    DateHired = DateTime.Now.AddDays(-random.Next(1, 365)),
                    Status = "active"
                };

                admins.Add(admin);
            }

            _context.Users.AddRange(admins);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            _logger?.LogInformation($"=== SCHOOL ADMIN SEEDING COMPLETED: {adminsToCreate} admins created ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding school admins: {Message}", ex.Message);
            throw new Exception($"Failed to seed school admins: {ex.Message}", ex);
        }
    }
}
