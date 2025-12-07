
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
using BrightEnroll_DES.Services.RoleBase;
using BrightEnroll_DES.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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

            // Role-based access control services
            builder.Services.AddSingleton<IRolePermissionService, RolePermissionService>();
            builder.Services.AddSingleton<IAuthorizationService, AuthorizationService>();

            // --- CONFIGURATION SETUP ---
            // Build and add configuration to service collection
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var configuration = configurationBuilder.Build();
            builder.Services.AddSingleton<IConfiguration>(configuration);

            // --- LOCAL DATABASE SETUP ---
            // Get connection strings
            var localConnectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(localConnectionString))
            {
                localConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Initial Catalog=DB_BrightEnroll_DES;";
            }

            builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(localConnectionString);

#if DEBUG
                options.EnableSensitiveDataLogging();
                options.LogTo(message => System.Diagnostics.Debug.WriteLine($"SQL: {message}"),
                    Microsoft.Extensions.Logging.LogLevel.Information);
#endif
            });

            builder.Services.AddSingleton<IConnectivityService>(sp =>
            {
                var jsRuntime = sp.GetService<IJSRuntime>();
                return new ConnectivityService(jsRuntime);
            });
            builder.Services.AddScoped<StudentService>();
            builder.Services.AddScoped<ArchiveService>();
            builder.Services.AddScoped<EnrollmentStatusService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.Audit.AuditLogService>();
            builder.Services.AddScoped<EmployeeService>();
            builder.Services.AddScoped<SalaryChangeRequestService>();
            builder.Services.AddScoped<FeeService>();
            builder.Services.AddScoped<ExpenseService>();
            builder.Services.AddScoped<PaymentService>();
            builder.Services.AddScoped<CurriculumService>();
            builder.Services.AddScoped<GradeService>();

            // Report services
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.Reports.EnrollmentReportService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.Reports.FinancialReportService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.Reports.AccountingReportService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.Reports.ExportService>();

            // QuestPDF services
            builder.Services.AddScoped<BrightEnroll_DES.Services.QuestPDF.EnrollmentStatisticsService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.QuestPDF.EnrollmentPdfGenerator>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.QuestPDF.Form1PdfGenerator>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.QuestPDF.PaymentReceiptPdfGenerator>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.QuestPDF.PayslipPdfGenerator>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.QuestPDF.PaymentSlipPdfGenerator>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.QuestPDF.SalaryChangeLogPdfGenerator>();

            // Inventory services
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.Inventory.AssetService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.Inventory.InventoryService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.Inventory.AssetAssignmentService>();

            // Database Sync Services
            builder.Services.AddScoped<BrightEnroll_DES.Services.Database.Sync.IDatabaseSyncService, BrightEnroll_DES.Services.Database.Sync.DatabaseSyncService>();
            builder.Services.AddSingleton<BrightEnroll_DES.Services.Database.Sync.ISyncStatusService, BrightEnroll_DES.Services.Database.Sync.SyncStatusService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Database.Sync.IOfflineQueueService, BrightEnroll_DES.Services.Database.Sync.OfflineQueueService>();

            // Auto Sync Scheduler (Background Service)
            builder.Services.AddHostedService<BrightEnroll_DES.Services.Database.Sync.AutoSyncScheduler>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

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
                        var loggerFactory = app.Services.GetService<ILoggerFactory>();
                        var logger = loggerFactory?.CreateLogger("MauiProgram");
                        int maxRetries = 3;
                        bool seedingSuccess = false;
                        Exception? lastException = null;

                        // First, seed all roles
                        try
                        {
                            await seeder.SeedAllRolesAsync();
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Role seeding failed: {Message}", ex.Message);
                        }

                        // Then seed admin user
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
                            logger?.LogWarning(lastException, "Failed to seed admin user after {MaxRetries} attempts: {Message}", maxRetries, lastException?.Message);
                        }

                        // Seed HR user
                        seedingSuccess = false; // Reset for HR user
                        lastException = null;
                        for (int attempt = 1; attempt <= maxRetries; attempt++)
                        {
                            try
                            {
                                await seeder.SeedInitialHRAsync();
                                seedingSuccess = true;
                                break;
                            }
                            catch (Exception ex)
                            {
                                lastException = ex;
                                logger?.LogWarning(ex, "HR seeding attempt {Attempt} failed: {Message}", attempt, ex.Message);
                                if (attempt < maxRetries)
                                {
                                    int delayMs = 1000 * attempt;
                                    await Task.Delay(delayMs);
                                }
                            }
                        }

                        if (!seedingSuccess)
                        {
                            logger?.LogWarning(lastException, "Failed to seed HR user after {MaxRetries} attempts: {Message}", maxRetries, lastException?.Message);
                        }

                        // Seed SuperAdmin user
                        try
                        {
                            await seeder.SeedInitialSuperAdminAsync();
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "SuperAdmin seeding failed: {Message}", ex.Message);
                        }

                        // Seed Teacher user
                        try
                        {
                            await seeder.SeedInitialTeacherAsync();
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Teacher seeding failed: {Message}", ex.Message);
                        }

                        // Seed Cashier user
                        try
                        {
                            await seeder.SeedInitialCashierAsync();
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Cashier seeding failed: {Message}", ex.Message);
                        }

                        // Seed Registrar user
                        try
                        {
                            await seeder.SeedInitialRegistrarAsync();
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Registrar seeding failed: {Message}", ex.Message);
                        }

                        try
                        {
                            await seeder.SeedDeductionsAsync();
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Deductions seeding failed: {Message}", ex.Message);
                        }

                        // Verify seeded users
                        var userRepo = scope.ServiceProvider.GetRequiredService<BrightEnroll_DES.Services.DataAccess.Repositories.IUserRepository>();
                        
                        var adminUser = await userRepo.GetBySystemIdAsync("BDES-0001");
                        if (adminUser == null)
                        {
                            logger?.LogWarning("Admin user (BDES-0001) not found after seeding verification.");
                        }
                        else
                        {
                            logger?.LogInformation("Admin user verified: {SystemId}", adminUser.system_ID);
                        }
                        
                        var hrUser = await userRepo.GetBySystemIdAsync("BDES-0002");
                        if (hrUser == null)
                        {
                            logger?.LogWarning("HR user (BDES-0002) not found after seeding verification.");
                        }
                        else
                        {
                            logger?.LogInformation("HR user verified: {SystemId}", hrUser.system_ID);
                        }
                        
                        var superAdminUser = await userRepo.GetBySystemIdAsync("BDES-SA-0001");
                        if (superAdminUser == null)
                        {
                            logger?.LogWarning("SuperAdmin user (BDES-SA-0001) not found after seeding verification.");
                        }
                        else
                        {
                            logger?.LogInformation("SuperAdmin user verified: {SystemId}", superAdminUser.system_ID);
                        }
                        
                        var teacherUser = await userRepo.GetBySystemIdAsync("BDES-TE-0001");
                        if (teacherUser == null)
                        {
                            logger?.LogWarning("Teacher user (BDES-TE-0001) not found after seeding verification.");
                        }
                        else
                        {
                            logger?.LogInformation("Teacher user verified: {SystemId}", teacherUser.system_ID);
                        }
                        
                        var cashierUser = await userRepo.GetBySystemIdAsync("BDES-CA-0001");
                        if (cashierUser == null)
                        {
                            logger?.LogWarning("Cashier user (BDES-CA-0001) not found after seeding verification.");
                        }
                        else
                        {
                            logger?.LogInformation("Cashier user verified: {SystemId}", cashierUser.system_ID);
                        }
                        
                        var registrarUser = await userRepo.GetBySystemIdAsync("BDES-RE-0001");
                        if (registrarUser == null)
                        {
                            logger?.LogWarning("Registrar user (BDES-RE-0001) not found after seeding verification.");
                        }
                        else
                        {
                            logger?.LogInformation("Registrar user verified: {SystemId}", registrarUser.system_ID);
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
