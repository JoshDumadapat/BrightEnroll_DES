using Microsoft.Extensions.Logging;
using BrightEnroll_DES.Services.AuthFunction;
using BrightEnroll_DES.Services;
using BrightEnroll_DES.Services.HR;
using BrightEnroll_DES.Services.Finance;
using BrightEnroll_DES.Services.DBConnections;
using BrightEnroll_DES.Services.Seeders;
using BrightEnroll_DES.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BrightEnroll_DES
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
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
            builder.Services.AddSingleton<DBConnection>(sp =>
            {
                // DBConnection constructor will automatically resolve connection string
                return new DBConnection();
            });

            // Register Repositories (EF Core ORM with SQL injection protection)
            builder.Services.AddScoped<BrightEnroll_DES.Services.Repositories.IUserRepository, BrightEnroll_DES.Services.Repositories.UserRepository>();

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

            // Register EF Core DbContext
            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                // Get connection string from configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");
                
                // Fallback to default LocalDB connection if not found
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Initial Catalog=DB_BrightEnroll_DES;";
                }

                options.UseSqlServer(connectionString);
            });

            // Register StudentService (scoped for EF Core DbContext)
            builder.Services.AddScoped<StudentService>();
            
            // Register EmployeeService (scoped for EF Core DbContext)
            builder.Services.AddScoped<EmployeeService>();
            
            // Register FeeService (scoped for EF Core DbContext)
            builder.Services.AddScoped<FeeService>();
            
            // Register GradeService (scoped for EF Core DbContext)
            builder.Services.AddScoped<GradeService>();
            
            // Register ClassAssignmentService (scoped for EF Core DbContext)
            builder.Services.AddScoped<ClassAssignmentService>();
            
            // Register ReportService (scoped for EF Core DbContext)
            builder.Services.AddScoped<ReportService>();
            
            // Register TeacherDashboardService (scoped for EF Core DbContext)
            builder.Services.AddScoped<TeacherDashboardService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Initialize database and seed initial admin user on startup
            try
            {
                var dbConnection = app.Services.GetRequiredService<DBConnection>();
                
                // Get connection string for initializer
                var connectionString = dbConnection.GetConnection().ConnectionString;
                var initializer = new DatabaseInitializer(connectionString);
                
                // Automatically create database and tables if they don't exist
                Task.Run(async () => await initializer.InitializeDatabaseAsync()).Wait();
                
                // Seed initial admin user - create a scope for DbContext
                using (var scope = app.Services.CreateScope())
                {
                    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                    Task.Run(async () => await seeder.SeedInitialAdminAsync()).Wait();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash the app
                System.Diagnostics.Debug.WriteLine($"Error initializing database: {ex.Message}");
            }

            return app;
        }
    }
}
