using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace BrightEnroll_DES.Services.Seeders;

public class ArchivedEmployeeSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<ArchivedEmployeeSeeder>? _logger;

    public ArchivedEmployeeSeeder(AppDbContext context, ILogger<ArchivedEmployeeSeeder>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task SeedAsync(int count = 50)
    {
        try
        {
            _logger?.LogInformation("=== STARTING ARCHIVED EMPLOYEE SEEDING ===");

            // Check if archived employees already exist (status = "Inactive")
            var existingCount = await _context.Users
                .Where(u => u.Status == "Inactive")
                .CountAsync();

            if (existingCount >= count)
            {
                _logger?.LogInformation($"Archived employees already seeded ({existingCount} exist). Skipping.");
                return;
            }

            var employeesToCreate = count - existingCount;
            var random = new Random();
            var firstNames = new[] { "Alfred", "Betty", "Cesar", "Dolores", "Ernesto", "Flora", "Gregorio", "Herminia", "Isidro", "Jocelyn", "Karlo", "Lourdes", "Mario", "Nenita", "Oscar", "Pilar", "Ramon", "Sofia", "Tomas", "Victoria" };
            var lastNames = new[] { "Aguilar", "Bautista", "Castro", "Dela Cruz", "Espiritu", "Fernandez", "Garcia", "Hernandez", "Ibarra", "Javier", "Kalaw", "Lopez", "Mendoza", "Nunez", "Ocampo", "Perez", "Quizon", "Ramos", "Santos", "Torres" };
            var genders = new[] { "Male", "Female" };
            var roles = new[] { "Teacher", "Registrar", "Cashier", "HR" };
            var inactiveReasons = new[] { "Resigned", "Retired", "Terminated", "End of Contract", "Transferred" };

            var employees = new List<UserEntity>();

            for (int i = 0; i < employeesToCreate; i++)
            {
                var firstName = firstNames[random.Next(firstNames.Length)];
                var lastName = lastNames[random.Next(lastNames.Length)];
                var gender = genders[random.Next(genders.Length)];
                var role = roles[random.Next(roles.Length)];

                var birthdate = new DateTime(random.Next(1960, 1990), random.Next(1, 13), random.Next(1, 29));
                var age = (byte)(DateTime.Today.Year - birthdate.Year);
                if (birthdate.Date > DateTime.Today.AddYears(-age)) age--;

                var systemId = $"ARCH{existingCount + i + 1:D4}";
                var email = $"{firstName.ToLower().Replace(" ", "")}.{lastName.ToLower().Replace(" ", "")}{existingCount + i + 1}@archived.edu.ph";

                // Check if email already exists
                var emailExists = await _context.Users.AnyAsync(u => u.Email == email);
                if (emailExists)
                {
                    email = $"{firstName.ToLower().Replace(" ", "")}.{lastName.ToLower().Replace(" ", "")}{existingCount + i + 1}_{Guid.NewGuid().ToString().Substring(0, 4)}@archived.edu.ph";
                }

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword("Employee123456");

                var employee = new UserEntity
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
                    UserRole = role,
                    Email = email,
                    Password = hashedPassword,
                    DateHired = DateTime.Now.AddDays(-random.Next(730, 3650)), // Hired 2-10 years ago
                    Status = "Inactive"
                };

                employees.Add(employee);
            }

            _context.Users.AddRange(employees);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            _logger?.LogInformation($"=== ARCHIVED EMPLOYEE SEEDING COMPLETED: {employeesToCreate} archived employees created ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding archived employees: {Message}", ex.Message);
            throw new Exception($"Failed to seed archived employees: {ex.Message}", ex);
        }
    }
}
