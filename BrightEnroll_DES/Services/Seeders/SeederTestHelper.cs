using BrightEnroll_DES.Data;
using BrightEnroll_DES.Services.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Seeders;

// Test and run database seeding
public class SeederTestHelper
{
    private readonly DatabaseSeeder _seeder;
    private readonly AppDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<SeederTestHelper>? _logger;

    public SeederTestHelper(
        DatabaseSeeder seeder, 
        AppDbContext context, 
        IUserRepository userRepository,
        ILogger<SeederTestHelper>? logger = null)
    {
        _seeder = seeder;
        _context = context;
        _userRepository = userRepository;
        _logger = logger;
    }

    // Run seeder and return results
    public async Task<SeederTestResult> RunSeederManuallyAsync()
    {
        var result = new SeederTestResult();
        
        try
        {
            System.Diagnostics.Debug.WriteLine("=== MANUAL SEEDER TEST STARTED ===");
            
            // Test database connection
            result.DatabaseConnectionTest = await TestDatabaseConnectionAsync();
            if (!result.DatabaseConnectionTest)
            {
                result.ErrorMessage = "Database connection failed";
                return result;
            }
            
            // Test if tables exist
            result.TablesExist = await TestTablesExistAsync();
            if (!result.TablesExist)
            {
                result.ErrorMessage = "Required tables do not exist";
                return result;
            }
            
            // Seed roles
            try
            {
                await _seeder.SeedAllRolesAsync();
                result.RolesSeeded = true;
                System.Diagnostics.Debug.WriteLine("Roles seeded successfully");
            }
            catch (Exception ex)
            {
                result.RolesSeeded = false;
                result.RolesError = ex.Message;
                System.Diagnostics.Debug.WriteLine($"ERROR seeding roles: {ex.Message}");
            }
            
            // Users are created dynamically when customer is added
            result.AdminSeeded = false;
            result.HRSeeded = false;
            result.AdminUserExists = false;
            
            System.Diagnostics.Debug.WriteLine("NOTE: User seeding is now handled dynamically through the Add Customer feature.");
            System.Diagnostics.Debug.WriteLine("Users will be created in each school's database when a customer is added.");
            
            result.Success = result.RolesSeeded;
            
            System.Diagnostics.Debug.WriteLine("=== MANUAL SEEDER TEST COMPLETED ===");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"CRITICAL ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        return result;
    }

    private async Task<bool> TestDatabaseConnectionAsync()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            System.Diagnostics.Debug.WriteLine($"Database connection test: {canConnect}");
            return canConnect;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database connection test failed: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> TestTablesExistAsync()
    {
        try
        {
            // Check if users table exists
            var userCount = await _context.Users.CountAsync();
            System.Diagnostics.Debug.WriteLine($"Users table exists. Current user count: {userCount}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Tables existence test failed: {ex.Message}");
            return false;
        }
    }
}

public class SeederTestResult
{
    public bool Success { get; set; }
    public bool DatabaseConnectionTest { get; set; }
    public bool TablesExist { get; set; }
    public bool RolesSeeded { get; set; }
    public bool AdminSeeded { get; set; }
    public bool HRSeeded { get; set; }
    public bool AdminUserExists { get; set; }
    public int? AdminUserId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RolesError { get; set; }
    public string? AdminError { get; set; }
    public string? HRError { get; set; }
}

