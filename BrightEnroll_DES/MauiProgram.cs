using Microsoft.Extensions.Logging;
using BrightEnroll_DES.Services.Authentication;
using BrightEnroll_DES.Services.Business.Students;
using BrightEnroll_DES.Services.Business.Academic;
using BrightEnroll_DES.Services.Business.HR;
using BrightEnroll_DES.Services.Business.Finance;
using BrightEnroll_DES.Services.Database.Connections;
using BrightEnroll_DES.Services.Database.Initialization;
using BrightEnroll_DES.Services.Infrastructure;
using BrightEnroll_DES.Services.Seeders;
using BrightEnroll_DES.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestPDF.Infrastructure;

namespace BrightEnroll_DES
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {

            QuestPDF.Settings.License = LicenseType.Community;

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            // Register DBConnection with dynamic connection string resolution
            // Connection string is resolved from:
            // 1. Environment variable "ConnectionStrings__DefaultConnection"
            // 2. appsettings.json file
            // 3. Default LocalDB connection string
            builder.Services.AddSingleton<BrightEnroll_DES.Services.Database.Connections.DBConnection>(sp =>
            {
                // DBConnection constructor will automatically resolve connection string
                return new BrightEnroll_DES.Services.Database.Connections.DBConnection();
            });

            // Register Repositories (EF Core ORM with SQL injection protection)
            builder.Services.AddScoped<BrightEnroll_DES.Services.DataAccess.Repositories.IUserRepository, BrightEnroll_DES.Services.DataAccess.Repositories.UserRepository>();

            // Register LoginService
            builder.Services.AddSingleton<ILoginService, LoginService>();

            // Register AuthService
            builder.Services.AddSingleton<IAuthService, AuthService>();
            
            // Register Database Seeder (scoped for EF Core DbContext)
            builder.Services.AddScoped<DatabaseSeeder>();
            
            // Register Loading Service
            builder.Services.AddSingleton<ILoadingService, LoadingService>();
            
            // Register Address Service
            builder.Services.AddSingleton<AddressService>();
            
            // Register School Year Service
            builder.Services.AddSingleton<SchoolYearService>();

            // --- LOCAL DATABASE SETUP ---
            // Get connection strings
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var localConnectionString = configuration.GetConnectionString("DefaultConnection");
            
            // Fallback to default LocalDB connection if not found
            if (string.IsNullOrWhiteSpace(localConnectionString))
            {
                localConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Initial Catalog=DB_BrightEnroll_DES;";
            }

            // Register LOCAL LocalDbContext as primary (scoped) - always used for operations
            builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(localConnectionString);
            });

            // Register Connectivity Service
            builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();



            // Register StudentService (scoped for EF Core DbContext)
            builder.Services.AddScoped<StudentService>();
            
            // Register EmployeeService (scoped for EF Core DbContext)
            builder.Services.AddScoped<EmployeeService>();
            
            // Register FeeService (scoped for EF Core DbContext)
            builder.Services.AddScoped<FeeService>();
            // Register ExpenseService (scoped for EF Core DbContext)
            builder.Services.AddScoped<ExpenseService>();
            
            // Register CurriculumService (scoped for EF Core DbContext)
            builder.Services.AddScoped<CurriculumService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Initialize database and seed initial admin user on startup
            // Run in background task but wait for completion with timeout
            _ = Task.Run(async () =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[MauiProgram] ===== STARTING DATABASE INITIALIZATION =====");
                    
                    var dbConnection = app.Services.GetRequiredService<BrightEnroll_DES.Services.Database.Connections.DBConnection>();
                    
                    // Get connection string for initializer (use local)
                    var connection = dbConnection.GetConnection();
                    if (connection == null || string.IsNullOrWhiteSpace(connection.ConnectionString))
                    {
                        throw new Exception("Database connection string is null or empty.");
                    }
                    var connectionString = connection.ConnectionString;
                    System.Diagnostics.Debug.WriteLine($"[MauiProgram] Connection string: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");
                    
                    var initializer = new BrightEnroll_DES.Services.Database.Initialization.DatabaseInitializer(connectionString);
                    
                    // Automatically create database and tables if they don't exist
                    System.Diagnostics.Debug.WriteLine("[MauiProgram] Initializing database (creating tables if needed)...");
                    var initResult = await initializer.InitializeDatabaseAsync();
                    System.Diagnostics.Debug.WriteLine($"[MauiProgram] Database initialization result: {initResult}");
                    
                    // Small delay to ensure all database operations are committed
                    await Task.Delay(1500);
                    
                    // Seed initial admin user and deductions - create a scope for DbContext
                    System.Diagnostics.Debug.WriteLine("[MauiProgram] Starting admin user seeding...");
                    using (var scope = app.Services.CreateScope())
                    {
                        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                        
                        // Retry seeding up to 3 times if it fails
                        int maxRetries = 3;
                        bool seedingSuccess = false;
                        Exception? lastException = null;
                        
                        for (int attempt = 1; attempt <= maxRetries; attempt++)
                        {
                            try
                            {
                                System.Diagnostics.Debug.WriteLine($"[MauiProgram] Seeding attempt {attempt} of {maxRetries}...");
                                await seeder.SeedInitialAdminAsync();
                                seedingSuccess = true;
                                System.Diagnostics.Debug.WriteLine($"[MauiProgram] ✓ Seeding successful on attempt {attempt}");
                                break;
                            }
                            catch (Exception ex)
                            {
                                lastException = ex;
                                System.Diagnostics.Debug.WriteLine($"[MauiProgram] ✗ Seeding attempt {attempt} failed: {ex.Message}");
                                if (ex.InnerException != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[MauiProgram] Inner exception: {ex.InnerException.Message}");
                                }
                                if (attempt < maxRetries)
                                {
                                    int delayMs = 1000 * attempt;
                                    System.Diagnostics.Debug.WriteLine($"[MauiProgram] Retrying in {delayMs}ms...");
                                    await Task.Delay(delayMs); // Exponential backoff
                                }
                            }
                        }
                        
                        if (!seedingSuccess)
                        {
                            var errorMsg = $"Failed to seed admin user after {maxRetries} attempts";
                            System.Diagnostics.Debug.WriteLine($"[MauiProgram] ✗ FATAL: {errorMsg}");
                            if (lastException != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"[MauiProgram] Last exception: {lastException.Message}");
                            }
                            throw new Exception(errorMsg, lastException);
                        }
                        
                        System.Diagnostics.Debug.WriteLine("[MauiProgram] ✓ Admin user seeding completed");
                        
                        // Seed deductions
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("[MauiProgram] Starting deductions seeding...");
                            await seeder.SeedDeductionsAsync();
                            System.Diagnostics.Debug.WriteLine("[MauiProgram] ✓ Deductions seeding completed");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MauiProgram] ⚠ Warning: Deductions seeding failed: {ex.Message}");
                            // Don't throw - deductions seeding failure is not critical
                        }
                        
                        // Final verification: Try to retrieve the user
                        var userRepo = scope.ServiceProvider.GetRequiredService<BrightEnroll_DES.Services.DataAccess.Repositories.IUserRepository>();
                        var adminUser = await userRepo.GetBySystemIdAsync("BDES-0001");
                        if (adminUser == null)
                        {
                            var errorMsg = "CRITICAL: Admin user not found after seeding verification!";
                            System.Diagnostics.Debug.WriteLine($"[MauiProgram] ✗ FATAL: {errorMsg}");
                            throw new Exception(errorMsg);
                        }
                        System.Diagnostics.Debug.WriteLine($"[MauiProgram] ✓ Final verification successful - Admin user found with UserId: {adminUser.user_ID}");
                    }
                    System.Diagnostics.Debug.WriteLine("[MauiProgram] ===== DATABASE INITIALIZATION COMPLETED =====");
                }
                catch (Exception ex)
                {
                    // Log error but don't crash the app
                    var errorMsg = $"Error initializing database: {ex.Message}";
                    System.Diagnostics.Debug.WriteLine($"[MauiProgram] ✗ FATAL ERROR: {errorMsg}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MauiProgram] Inner exception: {ex.InnerException.Message}");
                    }
                    System.Diagnostics.Debug.WriteLine($"[MauiProgram] Stack trace: {ex.StackTrace}");
                    
                    var loggerFactory = app.Services.GetService<ILoggerFactory>();
                    var logger = loggerFactory?.CreateLogger("MauiProgram");
                    logger?.LogError(ex, "Error initializing database: {Message}", ex.Message);
                }
            });

            return app;
        }
    }
}
