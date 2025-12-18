using System.IO;
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
using BrightEnroll_DES.Services.Business.Audit;
using BrightEnroll_DES.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;
using QuestPDF.Infrastructure;
using Microsoft.Data.SqlClient;
using System.Runtime.Versioning;

namespace BrightEnroll_DES
{
    public static class MauiProgram
    {
        [SupportedOSPlatform("windows10.0.17763.0")]
        public static MauiApp CreateMauiApp()
        {
#pragma warning disable CA1416 // Validate platform compatibility
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
            
            // LoginService with dependencies for cloud SuperAdmin authentication
            builder.Services.AddSingleton<ILoginService>(sp =>
            {
                var userRepo = sp.GetRequiredService<BrightEnroll_DES.Services.DataAccess.Repositories.IUserRepository>();
                var config = sp.GetService<IConfiguration>();
                var superAdminContext = sp.GetService<SuperAdminDbContext>();
                return new LoginService(userRepo, config, superAdminContext);
            });
            
            // AuthService with dependencies for dynamic login and cloud sync
            builder.Services.AddSingleton<IAuthService>(sp =>
            {
                var loginService = sp.GetRequiredService<ILoginService>();
                var auditLog = sp.GetService<AuditLogService>();
                var scopeFactory = sp.GetService<IServiceScopeFactory>();
                var config = sp.GetService<IConfiguration>();
                var superAdminContext = sp.GetService<SuperAdminDbContext>();
                var syncService = sp.GetService<BrightEnroll_DES.Services.Database.Sync.IDatabaseSyncService>();
                return new AuthService(loginService, auditLog, scopeFactory, config, superAdminContext, syncService);
            });
            builder.Services.AddScoped<DatabaseSeeder>();
            builder.Services.AddScoped<CurriculumSeeder>();
            builder.Services.AddScoped<AdminUserSeeder>();
            builder.Services.AddScoped<EmployeeSeeder>();
            builder.Services.AddScoped<StudentForEnrollmentSeeder>();
            builder.Services.AddScoped<EnrolledStudentSeeder>();
            builder.Services.AddScoped<ArchivedStudentSeeder>();
            builder.Services.AddScoped<ArchivedEmployeeSeeder>();
            builder.Services.AddScoped<TeacherAssignmentSeeder>();
            builder.Services.AddScoped<FeeSetupSeeder>();
            builder.Services.AddScoped<DiscountSeeder>();
            builder.Services.AddScoped<PaymentSeeder>();
            builder.Services.AddScoped<SuperAdminSeeder>();
            builder.Services.AddSingleton<ILoadingService, LoadingService>();
            builder.Services.AddSingleton<AddressService>();
            builder.Services.AddScoped<SchoolYearService>();

            // Role-based access control services
            builder.Services.AddSingleton<IRolePermissionService, RolePermissionService>();
            
            // Subscription and Module Management Services
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.SuperAdmin.ISubscriptionService, BrightEnroll_DES.Services.Business.SuperAdmin.SubscriptionService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.SuperAdmin.ICustomerModuleService, BrightEnroll_DES.Services.Business.SuperAdmin.CustomerModuleService>();
            
            // AuthorizationService with IServiceScopeFactory for accessing scoped CustomerModuleService
            builder.Services.AddSingleton<IAuthorizationService>(sp =>
            {
                var authService = sp.GetRequiredService<IAuthService>();
                var rolePermissionService = sp.GetRequiredService<IRolePermissionService>();
                var serviceScopeFactory = sp.GetService<IServiceScopeFactory>();
                return new AuthorizationService(authService, rolePermissionService, serviceScopeFactory);
            });

            // --- CONFIGURATION SETUP ---
            IConfiguration? configuration = null;
            
            try
            {
                // Try base directory first
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var appSettingsPath = Path.Combine(basePath, "appsettings.json");
                
                // If not found in base directory, try the application directory
                if (!File.Exists(appSettingsPath))
                {
                    var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    if (!string.IsNullOrEmpty(appDirectory))
                    {
                        appSettingsPath = Path.Combine(appDirectory, "appsettings.json");
                        if (File.Exists(appSettingsPath))
                        {
                            basePath = appDirectory;
                        }
                    }
                }
                
                // If still not found, try current directory
                if (!File.Exists(appSettingsPath))
                {
                    var currentDir = Directory.GetCurrentDirectory();
                    appSettingsPath = Path.Combine(currentDir, "appsettings.json");
                    if (File.Exists(appSettingsPath))
                    {
                        basePath = currentDir;
                    }
                }

                var configurationBuilder = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                configuration = configurationBuilder.Build();
            }
            catch (Exception configEx)
            {
                // If configuration loading fails, create an empty configuration
                // The app will use default connection strings defined in code
                System.Diagnostics.Debug.WriteLine($"Warning: Failed to load appsettings.json: {configEx.Message}");
                System.Diagnostics.Debug.WriteLine("Application will use default connection strings.");
                
                var configurationBuilder = new ConfigurationBuilder();
                configuration = configurationBuilder.Build();
            }
            
            builder.Services.AddSingleton<IConfiguration>(configuration);

            // --- LOCAL DATABASE SETUP ---
            // Get connection strings
            var localConnectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(localConnectionString))
            {
                localConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Initial Catalog=DB_BrightEnroll_DES;";
            }

            // Get SuperAdmin connection string
            var superAdminConnectionString = configuration.GetConnectionString("SuperAdminConnection");
            
            if (string.IsNullOrWhiteSpace(superAdminConnectionString))
            {
                // Default to same server but different database
                var builderConn = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(localConnectionString);
                builderConn.InitialCatalog = "DB_BrightEnroll_SuperAdmin";
                superAdminConnectionString = builderConn.ConnectionString;
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

            // Add DbContextFactory for thread-safe DbContext creation (required for sync operations)
            builder.Services.AddDbContextFactory<AppDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(localConnectionString);

#if DEBUG
                options.EnableSensitiveDataLogging();
                options.LogTo(message => System.Diagnostics.Debug.WriteLine($"SQL: {message}"),
                    Microsoft.Extensions.Logging.LogLevel.Information);
#endif
            });

            // Register SuperAdminDbContext with separate connection string
            builder.Services.AddDbContext<SuperAdminDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(superAdminConnectionString);

#if DEBUG
                options.EnableSensitiveDataLogging();
                options.LogTo(message => System.Diagnostics.Debug.WriteLine($"SuperAdmin SQL: {message}"),
                    Microsoft.Extensions.Logging.LogLevel.Information);
#endif
            });

            // Add DbContextFactory for SuperAdminDbContext
            builder.Services.AddDbContextFactory<SuperAdminDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(superAdminConnectionString);

#if DEBUG
                options.EnableSensitiveDataLogging();
                options.LogTo(message => System.Diagnostics.Debug.WriteLine($"SuperAdmin SQL: {message}"),
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
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.Students.ReEnrollmentService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.Audit.AuditLogService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.SuperAdmin.SuperAdminAuditLogService>();
            builder.Services.AddScoped<EmployeeService>();
            builder.Services.AddScoped<SalaryChangeRequestService>();
            builder.Services.AddScoped<TimeRecordExcelService>();
            builder.Services.AddScoped<TimeRecordService>();
            builder.Services.AddScoped<AttendanceReportService>();
            builder.Services.AddScoped<FeeService>();
            builder.Services.AddScoped<DiscountService>();
            builder.Services.AddScoped<JournalEntryService>();
            builder.Services.AddScoped<ChartOfAccountsService>();
            builder.Services.AddScoped<PeriodClosingService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.Notifications.NotificationService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.Notifications.ApprovalNotificationService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.Notifications.UserNotificationReadService>();
            builder.Services.AddScoped<ExpenseService>();
            builder.Services.AddScoped<StudentLedgerService>();
            builder.Services.AddScoped<PaymentService>();
            builder.Services.AddScoped<CurriculumService>();
            builder.Services.AddScoped<GradeService>();
            builder.Services.AddScoped<TeacherService>();

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
            builder.Services.AddScoped<BrightEnroll_DES.Services.QuestPDF.ReportCardPdfGenerator>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.QuestPDF.GradeFormPdfGenerator>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.QuestPDF.Form137PdfGenerator>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.QuestPDF.SalaryChangeLogPdfGenerator>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.QuestPDF.AttendanceReportPdfGenerator>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.QuestPDF.PayrollReportPdfGenerator>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.QuestPDF.FinanceReportsPdfGenerator>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.QuestPDF.SuperAdminSalesPdfGenerator>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.QuestPDF.CustomerContractPdfGenerator>();

            // Inventory services
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.Inventory.AssetService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.Inventory.InventoryService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.Inventory.AssetAssignmentService>();

            // Database Sync Services
            builder.Services.AddScoped<BrightEnroll_DES.Services.Database.Sync.IDatabaseSyncService, BrightEnroll_DES.Services.Database.Sync.DatabaseSyncService>();
            builder.Services.AddSingleton<BrightEnroll_DES.Services.Database.Sync.ISyncStatusService, BrightEnroll_DES.Services.Database.Sync.SyncStatusService>();
            
            // SuperAdmin Database Sync Services
            builder.Services.AddScoped<BrightEnroll_DES.Services.Database.SuperAdmin_Sync.ISuperAdminDatabaseSyncService, BrightEnroll_DES.Services.Database.SuperAdmin_Sync.SuperAdminDatabaseSyncService>();
            builder.Services.AddSingleton<BrightEnroll_DES.Services.Database.SuperAdmin_Sync.ISuperAdminSyncStatusService, BrightEnroll_DES.Services.Database.SuperAdmin_Sync.SuperAdminSyncStatusService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Database.Sync.IOfflineQueueService, BrightEnroll_DES.Services.Database.Sync.OfflineQueueService>();

            // Auto Sync Scheduler (Background Service)
            builder.Services.AddHostedService<BrightEnroll_DES.Services.Database.Sync.AutoSyncScheduler>();

            // SuperAdmin Services
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.SuperAdmin.SchoolDatabaseService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.SuperAdmin.SchoolAdminSeeder>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.SuperAdmin.SuperAdminService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.SuperAdmin.SuperAdminAuditLogService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.SuperAdmin.SuperAdminNotificationService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.SuperAdmin.SuperAdminBIRService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.SuperAdmin.SuperAdminBIRFilingService>();
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.SuperAdmin.AccountsReceivableService>();

            // School Information Service
            builder.Services.AddScoped<BrightEnroll_DES.Services.Business.SchoolInformationService>();

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

                    // Initialize main database
                    var initializer = new BrightEnroll_DES.Services.Database.Initialization.DatabaseInitializer(connectionString);
                    await initializer.InitializeDatabaseAsync();

                    // Initialize SuperAdmin database
                    var superAdminConnectionString = configuration.GetConnectionString("SuperAdminConnection");
                    if (string.IsNullOrWhiteSpace(superAdminConnectionString))
                    {
                        var builderConn = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
                        builderConn.InitialCatalog = "DB_BrightEnroll_SuperAdmin";
                        superAdminConnectionString = builderConn.ConnectionString;
                    }
                    
                    var superAdminInitializer = new BrightEnroll_DES.Services.Database.Initialization.SuperAdminDatabaseInitializer(superAdminConnectionString);
                    await superAdminInitializer.InitializeDatabaseAsync();
                    System.Diagnostics.Debug.WriteLine("SuperAdmin database initialization completed.");

                    await Task.Delay(1500);

                    using (var scope = app.Services.CreateScope())
                    {
                        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                        var loggerFactory = app.Services.GetService<ILoggerFactory>();
                        var logger = loggerFactory?.CreateLogger("MauiProgram");

                        // NOTE: Static user seeders have been removed. Users are now created dynamically through the Add Customer feature.
                        // The SchoolAdminSeeder handles creating admin users for each school's database when a customer is added.
                        
                        // First, seed all roles (still needed for role-based access control)
                        try
                        {
                            await seeder.SeedAllRolesAsync();
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Role seeding failed: {Message}", ex.Message);
                        }

                        // Seed SuperAdmin to SuperAdmin database (for local development/testing)
                        // NOTE: In production, SuperAdmin should also be in cloud database
                        // Login logic checks cloud first, then SuperAdmin database
                        try
                        {
                            var superAdminContext = scope.ServiceProvider.GetRequiredService<SuperAdminDbContext>();
                            await seeder.SeedSuperAdminUserToSuperAdminDatabaseAsync(superAdminContext);
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Super Admin user seeding to SuperAdmin database failed: {Message}", ex.Message);
                        }

                        // Seed SuperAdmin data (customers, sales, BIR compliance, support tickets, subscriptions)
                        try
                        {
                            var superAdminContext = scope.ServiceProvider.GetRequiredService<SuperAdminDbContext>();
                            var superAdminSeeder = scope.ServiceProvider.GetRequiredService<SuperAdminSeeder>();
                            await superAdminSeeder.SeedAllAsync();
                            logger?.LogInformation("SuperAdmin data seeded successfully.");
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "SuperAdmin data seeding failed: {Message}", ex.Message);
                        }

                        try
                        {
                            await seeder.SeedAdminUserAsync();
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Admin user seeding failed: {Message}", ex.Message);
                        }

                        try
                        {
                            await seeder.SeedDeductionsAsync();
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Deductions seeding failed: {Message}", ex.Message);
                        }

                        // Seed Chart of Accounts
                        try
                        {
                            await seeder.SeedChartOfAccountsAsync();
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Chart of Accounts seeding failed: {Message}", ex.Message);
                        }

                        // Seed Curriculum Data (Grade Levels, Classrooms, Sections, Subjects)
                        try
                        {
                            var curriculumSeeder = scope.ServiceProvider.GetRequiredService<CurriculumSeeder>();
                            await curriculumSeeder.SeedAllAsync();
                            logger?.LogInformation("Curriculum data seeded successfully.");
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Curriculum seeding failed: {Message}", ex.Message);
                        }

                        // Seed School Admin Users (50 records)
                        try
                        {
                            var adminUserSeeder = scope.ServiceProvider.GetRequiredService<AdminUserSeeder>();
                            await adminUserSeeder.SeedAsync(50);
                            logger?.LogInformation("School admin users seeded successfully.");
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "School admin seeding failed: {Message}", ex.Message);
                        }

                        // Seed Employees (50 records)
                        try
                        {
                            var employeeSeeder = scope.ServiceProvider.GetRequiredService<EmployeeSeeder>();
                            await employeeSeeder.SeedAsync(50);
                            logger?.LogInformation("Employees seeded successfully.");
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Employee seeding failed: {Message}", ex.Message);
                        }

                        // Seed Students for Enrollment (50 records)
                        try
                        {
                            var studentForEnrollmentSeeder = scope.ServiceProvider.GetRequiredService<StudentForEnrollmentSeeder>();
                            await studentForEnrollmentSeeder.SeedAsync(50);
                            logger?.LogInformation("Students for enrollment seeded successfully.");
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Student for enrollment seeding failed: {Message}", ex.Message);
                        }

                        // Seed Enrolled Students (50 records)
                        try
                        {
                            var enrolledStudentSeeder = scope.ServiceProvider.GetRequiredService<EnrolledStudentSeeder>();
                            await enrolledStudentSeeder.SeedAsync(50);
                            logger?.LogInformation("Enrolled students seeded successfully.");
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Enrolled student seeding failed: {Message}", ex.Message);
                        }

                        // Seed Archived Students (50 records)
                        try
                        {
                            var archivedStudentSeeder = scope.ServiceProvider.GetRequiredService<ArchivedStudentSeeder>();
                            await archivedStudentSeeder.SeedAsync(50);
                            logger?.LogInformation("Archived students seeded successfully.");
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Archived student seeding failed: {Message}", ex.Message);
                        }

                        // Seed Archived Employees (50 records)
                        try
                        {
                            var archivedEmployeeSeeder = scope.ServiceProvider.GetRequiredService<ArchivedEmployeeSeeder>();
                            await archivedEmployeeSeeder.SeedAsync(50);
                            logger?.LogInformation("Archived employees seeded successfully.");
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Archived employee seeding failed: {Message}", ex.Message);
                        }

                        // Seed Teacher Assignments (30 records)
                        try
                        {
                            var teacherAssignmentSeeder = scope.ServiceProvider.GetRequiredService<TeacherAssignmentSeeder>();
                            await teacherAssignmentSeeder.SeedAsync(30);
                            logger?.LogInformation("Teacher assignments seeded successfully.");
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Teacher assignment seeding failed: {Message}", ex.Message);
                        }

                        // Seed Fee Setup
                        try
                        {
                            var feeSetupSeeder = scope.ServiceProvider.GetRequiredService<FeeSetupSeeder>();
                            await feeSetupSeeder.SeedAsync();
                            logger?.LogInformation("Fee setup seeded successfully.");
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Fee setup seeding failed: {Message}", ex.Message);
                        }

                        // Seed Discounts
                        try
                        {
                            var discountSeeder = scope.ServiceProvider.GetRequiredService<DiscountSeeder>();
                            await discountSeeder.SeedAsync();
                            logger?.LogInformation("Discounts seeded successfully.");
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Discount seeding failed: {Message}", ex.Message);
                        }

                        // Seed Payments (for enrolled students)
                        try
                        {
                            var paymentSeeder = scope.ServiceProvider.GetRequiredService<PaymentSeeder>();
                            await paymentSeeder.SeedAsync();
                            logger?.LogInformation("Payments seeded successfully.");
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Payment seeding failed: {Message}", ex.Message);
                        }

                        // NOTE: User verification removed. Users are now created dynamically through the Add Customer feature.
                        logger?.LogInformation("Database seeding completed. Users will be created dynamically when customers are added.");
                    }
                }
                catch (Exception ex)
                {
                    var loggerFactory = app.Services.GetService<ILoggerFactory>();
                    var logger = loggerFactory?.CreateLogger("MauiProgram");
                    logger?.LogError(ex, "Error initializing database: {Message}", ex.Message);
                }
            });

#pragma warning restore CA1416 // Validate platform compatibility
            return app;
        }
    }
}
