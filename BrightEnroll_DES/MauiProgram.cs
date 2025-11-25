using Microsoft.Extensions.Logging;
using BrightEnroll_DES.Services.AuthFunction;
using BrightEnroll_DES.Services;
using BrightEnroll_DES.Services.DBConnections;

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

            // Register Repositories (ORM-like pattern with SQL injection protection)
            builder.Services.AddSingleton<BrightEnroll_DES.Services.Repositories.IUserRepository, BrightEnroll_DES.Services.Repositories.UserRepository>();
            builder.Services.AddSingleton<BrightEnroll_DES.Services.Repositories.IEmployeeRepository, BrightEnroll_DES.Services.Repositories.EmployeeRepository>();

            // Register LoginService
            builder.Services.AddSingleton<ILoginService, LoginService>();

            // Register AuthService
            builder.Services.AddSingleton<IAuthService, AuthService>();
            
            // Register Database Seeder
            builder.Services.AddSingleton<DatabaseSeeder>();
            
            // Register Loading Service
            builder.Services.AddSingleton<ILoadingService, LoadingService>();
            
            // Register Address Service
            builder.Services.AddSingleton<AddressService>();
            
            // Register School Year Service
            builder.Services.AddSingleton<SchoolYearService>();
            
            // Register Employee Service
            builder.Services.AddSingleton<BrightEnroll_DES.Services.IEmployeeService, BrightEnroll_DES.Services.EmployeeService>();

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
                
                // Seed initial admin user
                var seeder = app.Services.GetRequiredService<DatabaseSeeder>();
                Task.Run(async () => await seeder.SeedInitialAdminAsync()).Wait();
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
