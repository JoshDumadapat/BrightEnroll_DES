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

            // --- UNIFIED LOCAL + CLOUD DATABASE SETUP ---
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

            // Register LOCAL AppDbContext as primary (scoped) - always used for operations
            builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(localConnectionString);
            });


            // Cloud connection string (MonsterASP.net)
            string cloudConnectionString = "Server=db33104.public.databaseasp.net; Database=db33104; User Id=db33104; Password=Q_w93nD=+F7k; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

            // Register CLOUD AppDbContextFactory for cloud sync operations
            builder.Services.AddDbContextFactory<AppDbContext>(options =>
            {
                options.UseSqlServer(cloudConnectionString);
            }, ServiceLifetime.Singleton);

            // Register Connectivity Service
            builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();

            // Register Database Sync Service
            builder.Services.AddSingleton<IDatabaseSyncService, DatabaseSyncService>();



            // Register StudentService (scoped for EF Core DbContext)
            builder.Services.AddScoped<StudentService>();
            
            // Register EmployeeService (scoped for EF Core DbContext)
            builder.Services.AddScoped<EmployeeService>();
            
            // Register FeeService (scoped for EF Core DbContext)
            builder.Services.AddScoped<FeeService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Set service provider in AppDbContext for auto-sync functionality
            AppDbContext.SetServiceProvider(app.Services);

            // Initialize database and seed initial admin user on startup
            try
            {
                var dbConnection = app.Services.GetRequiredService<DBConnection>();
                
                // Get connection string for initializer (use local)
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

                // Start connectivity monitoring and auto-sync
                var connectivityService = app.Services.GetRequiredService<IConnectivityService>();
                var syncService = app.Services.GetRequiredService<IDatabaseSyncService>();
                
                connectivityService.StartMonitoring();
                
                // Subscribe to connectivity changes for bidirectional auto-sync
                connectivityService.ConnectivityChanged += (sender, isConnected) =>
                {
                    if (isConnected)
                    {
                        // When connection is restored, do bidirectional sync
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                // First, sync local changes to cloud
                                await syncService.TrySyncToCloudAsync();
                                
                                // Then, check if we need to pull from cloud (in case cloud has newer data)
                                var isLocalEmpty = await syncService.IsLocalDatabaseEmptyAsync();
                                if (isLocalEmpty)
                                {
                                    // If local is empty, pull from cloud
                                    await syncService.TrySyncFromCloudAsync();
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error syncing: {ex.Message}");
                            }
                        });
                    }
                };

                // Initial connectivity check and bidirectional sync if online
                _ = Task.Run(async () =>
                {
                    var isConnected = await connectivityService.CheckConnectivityAsync();
                    if (isConnected)
                    {
                        try
                        {
                            // Check if local database is empty (new device)
                            var isLocalEmpty = await syncService.IsLocalDatabaseEmptyAsync();
                            
                            if (isLocalEmpty)
                            {
                                // New device: Pull all data from cloud to local first
                                System.Diagnostics.Debug.WriteLine("Local database is empty, pulling data from cloud...");
                                await syncService.TrySyncFromCloudAsync();
                            }
                            else
                            {
                                // Existing device: Sync local changes to cloud
                                await syncService.TrySyncToCloudAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error in initial sync: {ex.Message}");
                        }
                    }
                });
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
