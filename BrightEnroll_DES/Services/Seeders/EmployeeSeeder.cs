using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace BrightEnroll_DES.Services.Seeders;

public class EmployeeSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<EmployeeSeeder>? _logger;

    public EmployeeSeeder(AppDbContext context, ILogger<EmployeeSeeder>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    public async Task SeedAsync(int count = 50)
    {
        try
        {
            _logger?.LogInformation("=== STARTING EMPLOYEE SEEDING ===");

            // Check if employees already exist (excluding Admin role)
            var existingCount = await _context.Users
                .Where(u => u.UserRole != "Admin" && u.UserRole != "SuperAdmin")
                .CountAsync();

            if (existingCount >= count)
            {
                _logger?.LogInformation($"Employees already seeded ({existingCount} exist). Skipping.");
                return;
            }

            var employeesToCreate = count - existingCount;
            var random = new Random();
            var firstNames = new[] { "Juan", "Maria", "Jose", "Ana", "Carlos", "Rosa", "Pedro", "Carmen", "Miguel", "Elena", "Antonio", "Isabel", "Francisco", "Lucia", "Manuel", "Patricia", "Luis", "Monica", "Fernando", "Andrea", "Roberto", "Cristina", "Eduardo", "Gabriela", "Ricardo", "Sofia", "Daniel", "Valentina", "Alejandro", "Camila" };
            var lastNames = new[] { "Dela Cruz", "Garcia", "Reyes", "Ramos", "Mendoza", "Santos", "Cruz", "Torres", "Fernandez", "Gonzalez", "Lopez", "Martinez", "Rodriguez", "Perez", "Sanchez", "Rivera", "Morales", "Ortiz", "Gutierrez", "Chavez", "Villanueva", "Castillo", "Romero", "Diaz", "Moreno" };
            var genders = new[] { "Male", "Female" };
            
            // Role distribution: More teachers (80%), fewer other roles (20%)
            // For 50 employees: ~40 teachers, ~3 registrars, ~3 cashiers, ~4 HR
            var roleDistribution = new List<string>();
            
            // Calculate exact counts
            int teacherCount = (int)(employeesToCreate * 0.80); // 80% teachers
            int registrarCount = (int)(employeesToCreate * 0.06); // 6% registrars
            int cashierCount = (int)(employeesToCreate * 0.06); // 6% cashiers
            int hrCount = employeesToCreate - teacherCount - registrarCount - cashierCount; // Remaining for HR
            
            // Add roles to distribution list
            for (int i = 0; i < teacherCount; i++)
                roleDistribution.Add("Teacher");
            for (int i = 0; i < registrarCount; i++)
                roleDistribution.Add("Registrar");
            for (int i = 0; i < cashierCount; i++)
                roleDistribution.Add("Cashier");
            for (int i = 0; i < hrCount; i++)
                roleDistribution.Add("HR");
            
            // Shuffle the role distribution for randomness
            roleDistribution = roleDistribution.OrderBy(x => random.Next()).ToList();
            
            _logger?.LogInformation($"Role distribution: {teacherCount} Teachers, {registrarCount} Registrars, {cashierCount} Cashiers, {hrCount} HR");

            var employees = new List<UserEntity>();

            for (int i = 0; i < employeesToCreate; i++)
            {
                var firstName = firstNames[random.Next(firstNames.Length)];
                var lastName = lastNames[random.Next(lastNames.Length)];
                var gender = genders[random.Next(genders.Length)];
                var role = roleDistribution[i];
                var birthdate = new DateTime(random.Next(1980, 2000), random.Next(1, 13), random.Next(1, 29));
                var age = (byte)(DateTime.Today.Year - birthdate.Year);
                if (birthdate.Date > DateTime.Today.AddYears(-age)) age--;

                var systemId = $"EMP{existingCount + i + 1:D4}";
                var email = $"{firstName.ToLower().Replace(" ", "")}.{lastName.ToLower().Replace(" ", "")}{existingCount + i + 1}@school.edu.ph";

                // Check if email already exists
                var emailExists = await _context.Users.AnyAsync(u => u.Email == email);
                if (emailExists)
                {
                    email = $"{firstName.ToLower().Replace(" ", "")}.{lastName.ToLower().Replace(" ", "")}{existingCount + i + 1}_{Guid.NewGuid().ToString().Substring(0, 4)}@school.edu.ph";
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
                    DateHired = DateTime.Now.AddDays(-random.Next(1, 1095)),
                    Status = "active"
                };

                employees.Add(employee);
            }

            _context.Users.AddRange(employees);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            _logger?.LogInformation($"=== EMPLOYEE SEEDING COMPLETED: {employeesToCreate} employees created ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding employees: {Message}", ex.Message);
            throw new Exception($"Failed to seed employees: {ex.Message}", ex);
        }
    }
}
