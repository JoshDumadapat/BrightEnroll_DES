using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Networking;
using BrightEnroll_DES.Services.Authentication;
using BrightEnroll_DES.Services.Business.Students;
using BrightEnroll_DES.Services.Business.Academic;
using BrightEnroll_DES.Services.Business.HR;
using BrightEnroll_DES.Services.Business.Finance;
using BrightEnroll_DES.Services.Business.Payroll;
using BrightEnroll_DES.Services.Database.Connections;
using BrightEnroll_DES.Services.Database.Initialization;
using BrightEnroll_DES.Services.Infrastructure;
using BrightEnroll_DES.Services.Seeders;
using BrightEnroll_DES.Services.Sync;
using BrightEnroll_DES.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
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

            builder.Services.AddSingleton<BrightEnroll_DES.Services.Database.Connections.DBConnection>(sp =>
            {
                return new BrightEnroll_DES.Services.Database.Connections.DBConnection();
            });

            builder.Services.AddScoped<BrightEnroll_DES.Services.DataAccess.Repositories.IUserRepository, BrightEnroll_DES.Services.DataAccess.Repositories.UserRepository>();
            builder.Services.AddSingleton<ILoginService, LoginService>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddScoped<DatabaseSeeder>();
            builder.Services.AddSingleton<ILoadingService, LoadingService>();
            builder.Services.AddSingleton<AddressService>();
            builder.Services.AddSingleton<SchoolYearService>();

            // --- DATABASE SETUP ---
            // Get connection strings
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // LOCAL CONNECTION STRING (LocalDB for offline mode)
            var localConnectionString = configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrWhiteSpace(localConnectionString))
            {
                localConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Initial Catalog=DB_BrightEnroll_DES;";
            }

            // CLOUD CONNECTION STRING (Monster SQL Server for online mode)
            var cloudConnectionString = configuration.GetConnectionString("CloudConnection");
            
            // Fallback to default cloud connection if not found
            if (string.IsNullOrWhiteSpace(cloudConnectionString))
            {
                // Use TCP/IP protocol with explicit port for remote SQL Server
                // Port 1433 is the default SQL Server TCP/IP port
                // Network Library=dbmssocn forces TCP/IP protocol (prevents Named Pipes)
                cloudConnectionString = "Server=db33580.public.databaseasp.net; Database=db33580; User Id=db33580; Password=6Hg%_n7BrW#3; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";
            }

            // Register LOCAL LocalDbContext (scoped) - used for offline operations
            builder.Services.AddDbContext<LocalDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(localConnectionString);
            });

            // Register CLOUD CloudDbContext (scoped) - used for online sync operations
            builder.Services.AddDbContext<CloudDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(cloudConnectionString);
            });

            // Register AppDbContext pointing to LocalDbContext (for backward compatibility)
            builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(localConnectionString);
            });

            // Register IDbContextFactory for proper DbContext lifetime management
            // This allows creating new DbContext instances per operation, preventing concurrency issues
            builder.Services.AddDbContextFactory<LocalDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(localConnectionString);
            });

            builder.Services.AddDbContextFactory<CloudDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(cloudConnectionString);
            });

            builder.Services.AddDbContextFactory<AppDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(localConnectionString);
            });

            // Register Connectivity Service
            builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();

            // Register Sync Service (scoped - uses IDbContextFactory internally)
            builder.Services.AddScoped<ISyncService, SyncService>();

            // Register Cloud Connection Tester (scoped - uses IDbContextFactory internally)
            builder.Services.AddScoped<CloudConnectionTester>();

            // Register Background Sync Service as singleton for MAUI Blazor Hybrid
            builder.Services.AddSingleton<SyncBackgroundService>();



            // Register StudentService (scoped for EF Core DbContext)
            builder.Services.AddScoped<StudentService>();
            builder.Services.AddScoped<ArchiveService>();
            builder.Services.AddScoped<EnrollmentStatusService>();
            builder.Services.AddScoped<EmployeeService>();
            builder.Services.AddScoped<FeeService>();
            builder.Services.AddScoped<ExpenseService>();
            builder.Services.AddScoped<CurriculumService>();

            // Register RoleService (uses IDbContextFactory for proper lifetime management)
            builder.Services.AddScoped<IRoleService, RoleService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Start the background sync service manually (MAUI doesn't auto-start hosted services)
            try
            {
                var syncBackgroundService = app.Services.GetRequiredService<SyncBackgroundService>();
                syncBackgroundService.Start();
                System.Diagnostics.Debug.WriteLine("[MauiProgram] Background sync service started");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MauiProgram] Error starting background sync service: {ex.Message}");
            }

            // Start connectivity monitoring service automatically
            try
            {
                var connectivityService = app.Services.GetRequiredService<IConnectivityService>();
                connectivityService.StartMonitoring();
                System.Diagnostics.Debug.WriteLine("[MauiProgram] Connectivity monitoring service started");
                
                // Subscribe to connectivity changes to trigger sync when going online
                connectivityService.ConnectivityChanged += async (sender, isConnected) =>
                {
                    if (isConnected)
                    {
                        System.Diagnostics.Debug.WriteLine("[MauiProgram] ConnectivityService: Internet connection detected, triggering sync...");
                        try
                        {
                            using var scope = app.Services.CreateScope();
                            var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();
                            var result = await syncService.SyncAsync();
                            System.Diagnostics.Debug.WriteLine($"[MauiProgram] Connectivity-triggered sync completed: {result}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MauiProgram] Error during connectivity-triggered sync: {ex.Message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[MauiProgram] ConnectivityService: Internet connection lost.");
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MauiProgram] Error starting connectivity monitoring: {ex.Message}");
            }

            // Initialize database and seed initial admin user on startup
            // Run in background task but wait for completion with timeout
            _ = Task.Run(async () =>
            {
                try
                {
                    var dbConnection = app.Services.GetRequiredService<BrightEnroll_DES.Services.Database.Connections.DBConnection>();
                    var connection = dbConnection.GetConnection();
                    if (connection == null || string.IsNullOrWhiteSpace(connection.ConnectionString))
                    {
                        throw new Exception("Database connection string is null or empty.");
                    }
                    var connectionString = connection.ConnectionString;
                    
                    var initializer = new BrightEnroll_DES.Services.Database.Initialization.DatabaseInitializer(connectionString);
                    await initializer.InitializeDatabaseAsync();
                    
                    await Task.Delay(1500);
                    
                    using (var scope = app.Services.CreateScope())
                    {
                        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                        int maxRetries = 3;
                        bool seedingSuccess = false;
                        Exception? lastException = null;
                        
                        for (int attempt = 1; attempt <= maxRetries; attempt++)
                        {
                            try
                            {
                                await seeder.SeedInitialAdminAsync();
                                seedingSuccess = true;
                                break;
                            }
                            catch (Exception ex)
                            {
                                lastException = ex;
                                if (attempt < maxRetries)
                                {
                                    int delayMs = 1000 * attempt;
                                    await Task.Delay(delayMs);
                                }
                            }
                        }
                        
                        if (!seedingSuccess)
                        {
                            var errorMsg = $"Failed to seed admin user after {maxRetries} attempts";
                            throw new Exception(errorMsg, lastException);
                        }
                        
                        try
                        {
                            await seeder.SeedDeductionsAsync();
                        }
                        catch (Exception ex)
                        {
                            var loggerFactory = app.Services.GetService<ILoggerFactory>();
                            var logger = loggerFactory?.CreateLogger("MauiProgram");
                            logger?.LogWarning(ex, "Deductions seeding failed: {Message}", ex.Message);
                        }
                        
                        var userRepo = scope.ServiceProvider.GetRequiredService<BrightEnroll_DES.Services.DataAccess.Repositories.IUserRepository>();
                        var adminUser = await userRepo.GetBySystemIdAsync("BDES-0001");
                        if (adminUser == null)
                        {
                            var errorMsg = "CRITICAL: Admin user not found after seeding verification!";
                            throw new Exception(errorMsg);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var loggerFactory = app.Services.GetService<ILoggerFactory>();
                    var logger = loggerFactory?.CreateLogger("MauiProgram");
                    logger?.LogError(ex, "Error initializing database: {Message}", ex.Message);
                }
            });


            return app;
        }
    }
}
