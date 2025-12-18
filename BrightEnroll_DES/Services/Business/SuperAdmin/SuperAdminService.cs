using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Data.Models.SuperAdmin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using BrightEnroll_DES.Services.Database.Definitions;
using BrightEnroll_DES.Services.Business.Audit;
using BrightEnroll_DES.Services.Authentication;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Net;
using System.Net.NetworkInformation;
using BrightEnroll_DES.Services.Business.SuperAdmin;

namespace BrightEnroll_DES.Services.Business.SuperAdmin;

public class SuperAdminService
{
    private readonly SuperAdminDbContext _context;
    private readonly ILogger<SuperAdminService>? _logger;
    private readonly SchoolDatabaseService? _databaseService;
    private readonly SchoolAdminSeeder? _adminSeeder;
    private readonly IServiceScopeFactory? _serviceScopeFactory;
    private readonly IAuthService? _authService;

    public SuperAdminService(
        SuperAdminDbContext context, 
        ILogger<SuperAdminService>? logger = null,
        SchoolDatabaseService? databaseService = null,
        SchoolAdminSeeder? adminSeeder = null,
        IServiceScopeFactory? serviceScopeFactory = null,
        IAuthService? authService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
        _databaseService = databaseService;
        _adminSeeder = adminSeeder;
        _serviceScopeFactory = serviceScopeFactory;
        _authService = authService;
    }

    #region Customer Operations

    public async Task<List<Customer>> GetAllCustomersAsync()
    {
        if (_context == null || _context.Database == null)
        {
            return new List<Customer>();
        }

        try
        {
            // Check database connection with timeout
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                if (!await _context.Database.CanConnectAsync(cts.Token))
                {
                    _logger?.LogWarning("Database connection not available.");
                    return new List<Customer>();
                }
            }
            catch (OperationCanceledException)
            {
                _logger?.LogWarning("Database connection check timed out.");
                return new List<Customer>();
            }
            catch (Exception dbEx)
            {
                _logger?.LogWarning(dbEx, "Database connection check failed.");
                return new List<Customer>();
            }

            // Check if table exists
            try
            {
                var count = await _context.Customers.CountAsync();
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208 || sqlEx.Message.Contains("Invalid object name"))
            {
                _logger?.LogWarning("Table tbl_Customers does not exist yet.");
                return new List<Customer>();
            }
            catch (InvalidOperationException)
            {
                _logger?.LogWarning("Invalid operation checking customers table.");
                return new List<Customer>();
            }

            // Load customers
            return await _context.Customers
                .OrderByDescending(c => c.DateRegistered)
                .ToListAsync();
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 208 || ex.Message.Contains("Invalid object name"))
        {
            _logger?.LogWarning(ex, "Table does not exist yet: tbl_Customers");
            return new List<Customer>();
        }
        catch (InvalidOperationException ex)
        {
            _logger?.LogWarning(ex, "Invalid operation loading customers. Returning empty list.");
            return new List<Customer>();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error loading customers. Tables may not exist yet.");
            return new List<Customer>();
        }
    }

    public async Task<Customer?> GetCustomerByIdAsync(int customerId)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);
    }

    public async Task<Customer> CreateCustomerAsync(
        Customer customer, 
        int? createdBy = null,
        string? adminFirstName = null,
        string? adminMiddleName = null,
        string? adminLastName = null,
        string? adminSuffix = null,
        string? adminRole = null,
        string? planName = null,
        List<string>? selectedModules = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(customer.CustomerCode))
            {
                customer.CustomerCode = await GenerateCustomerCodeAsync();
            }

            customer.CreatedBy = createdBy;
            customer.DateRegistered = DateTime.Now;

            _logger?.LogInformation("Creating customer: {SchoolName}, Code: {CustomerCode}", customer.SchoolName, customer.CustomerCode);
            
            // Auto-generate initial invoice if needed
            if (customer.ContractStartDate.HasValue && customer.MonthlyFee > 0)
            {
                try
                {
                    // Invoice created after customer is saved
                }
                catch (Exception invoiceEx)
                {
                    _logger?.LogWarning(invoiceEx, "Failed to create initial invoice for customer {CustomerCode}: {Message}", customer.CustomerCode, invoiceEx.Message);
                }
            }

            // Initialize cloud database (local DB created on first login)
            if (!string.IsNullOrWhiteSpace(customer.CloudConnectionString))
            {
                try
                {
                    _logger?.LogInformation("Validating cloud database connection for school: {SchoolName}", customer.SchoolName);
                    
                    // Extract database name from connection string
                    if (string.IsNullOrWhiteSpace(customer.DatabaseName))
                    {
                        var builder = new SqlConnectionStringBuilder(customer.CloudConnectionString);
                        customer.DatabaseName = builder.InitialCatalog ?? string.Empty;
                    }

                    // Validate cloud connection
                    _logger?.LogInformation("Cloud database validated: {DatabaseName}", customer.DatabaseName);

                    // Seed school information
                    await SeedSchoolInformationAsync(customer, customer.CloudConnectionString);

                    // Seed admin account
                    SchoolAdminInfo adminInfo;
                    if (_adminSeeder != null)
                    {
                        adminInfo = await _adminSeeder.SeedAdminAccountAsync(
                            customer.CloudConnectionString,
                            customer,
                            defaultPassword: "Admin123456",
                            firstName: adminFirstName,
                            middleName: adminMiddleName,
                            lastName: adminLastName,
                            suffix: adminSuffix,
                            role: adminRole ?? customer.ContactPosition ?? "Admin");
                    }
                    else
                    {
                        _logger?.LogWarning("AdminSeeder is not available. Cannot create admin account.");
                        adminInfo = new SchoolAdminInfo
                        {
                            Success = false,
                            ErrorMessage = "AdminSeeder service is not available"
                        };
                    }

                    if (adminInfo.Success)
                    {
                        customer.AdminUsername = adminInfo.Email;
                        customer.AdminPassword = adminInfo.Password; // Store plain password for initial setup
                        
                        _logger?.LogInformation("Admin account created: {Email}, SystemID: {SystemId}", 
                            adminInfo.Email, adminInfo.SystemId);

                        // Admin account is already created in cloud database, no need to sync
                        _logger?.LogInformation("Admin account created in cloud database: {Email}", adminInfo.Email);
                    }
                    else
                    {
                        _logger?.LogWarning("Failed to create admin account: {Error}", adminInfo.ErrorMessage);
                        // Set default credentials even if seeding failed, so customer can still login
                        if (string.IsNullOrWhiteSpace(customer.AdminUsername))
                        {
                            customer.AdminUsername = customer.ContactEmail ?? $"{customer.CustomerCode}@school.edu.ph";
                        }
                        if (string.IsNullOrWhiteSpace(customer.AdminPassword))
                        {
                            customer.AdminPassword = "Admin123456"; // Default password
                            _logger?.LogWarning("AdminPassword was not set, using default password for customer {CustomerCode}", customer.CustomerCode);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error during database initialization or admin seeding: {Message}", ex.Message);
                    // Continue with customer creation even if initialization fails
                    // Ensure AdminUsername and AdminPassword are set even if seeding failed
                    if (string.IsNullOrWhiteSpace(customer.AdminUsername))
                    {
                        customer.AdminUsername = customer.ContactEmail ?? $"{customer.CustomerCode}@school.edu.ph";
                    }
                    if (string.IsNullOrWhiteSpace(customer.AdminPassword))
                    {
                        customer.AdminPassword = "Admin123456"; // Default password
                        _logger?.LogWarning("AdminPassword was not set due to exception, using default password for customer {CustomerCode}", customer.CustomerCode);
                    }
                }
            }
            else if (string.IsNullOrWhiteSpace(customer.CloudConnectionString))
            {
                _logger?.LogWarning("No cloud connection string provided. Customer will not be able to sync data.");
                // Set default credentials even if cloud connection is not provided
                if (string.IsNullOrWhiteSpace(customer.AdminUsername))
                {
                    customer.AdminUsername = customer.ContactEmail ?? $"{customer.CustomerCode}@school.edu.ph";
                }
                if (string.IsNullOrWhiteSpace(customer.AdminPassword))
                {
                    customer.AdminPassword = "Admin123456"; // Default password
                }
            }
            
            // Auto-generate local database connection string (will be used on first login)
            // This uses LocalDB pattern: Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=DB_{CustomerCode};...
            if (string.IsNullOrWhiteSpace(customer.DatabaseConnectionString))
            {
                var localDbName = $"DB_{customer.CustomerCode}";
                customer.DatabaseConnectionString = $"Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog={localDbName};Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;";
                _logger?.LogInformation("Auto-generated local database connection string: {LocalDbName}", localDbName);
            }

            // Final safety check: Ensure AdminUsername and AdminPassword are always set before saving
            if (string.IsNullOrWhiteSpace(customer.AdminUsername))
            {
                customer.AdminUsername = customer.ContactEmail ?? $"{customer.CustomerCode}@school.edu.ph";
                _logger?.LogWarning("AdminUsername was still null before save, using ContactEmail for customer {CustomerCode}", customer.CustomerCode);
            }
            
            if (string.IsNullOrWhiteSpace(customer.AdminPassword))
            {
                customer.AdminPassword = "Admin123456";
                _logger?.LogWarning("AdminPassword was still null before save, using default password for customer {CustomerCode}", customer.CustomerCode);
            }

            _context.Customers.Add(customer);
            var result = await _context.SaveChangesAsync();

            _logger?.LogInformation("Customer created successfully: {CustomerCode} - {SchoolName}, Rows affected: {Result}", 
                customer.CustomerCode, customer.SchoolName, result);

            // Create subscription with modules after customer is created
            if (result > 0 && customer.CustomerId > 0)
            {
                try
                {
                    if (_serviceScopeFactory != null)
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var subscriptionService = scope.ServiceProvider.GetService<ISubscriptionService>();
                        
                        if (subscriptionService != null)
                        {
                            int? planId = null;
                            List<string>? modulesToAdd = null;
                            
                            // If a plan name is provided, find the plan ID
                            if (!string.IsNullOrWhiteSpace(planName))
                            {
                                // Try exact match first
                                var plan = await _context.SubscriptionPlans
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(p => p.PlanName == planName || p.PlanName == $"{planName} Plan");
                                
                                // If not found, try by plan code (Basic -> basic, Standard -> standard, etc.)
                                if (plan == null)
                                {
                                    var planCode = planName.ToLower();
                                    plan = await _context.SubscriptionPlans
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(p => p.PlanCode == planCode);
                                }
                                
                                if (plan != null)
                                {
                                    planId = plan.PlanId;
                                    _logger?.LogInformation("Found plan {PlanName} (ID: {PlanId}) for customer {CustomerCode}", 
                                        plan.PlanName, planId, customer.CustomerCode);
                                }
                                else
                                {
                                    _logger?.LogWarning("Plan '{PlanName}' not found for customer {CustomerCode}. Creating custom subscription.", 
                                        planName, customer.CustomerCode);
                                }
                            }
                            
                            // If no plan ID and modules are provided, use custom modules
                            if (!planId.HasValue && selectedModules != null && selectedModules.Any())
                            {
                                modulesToAdd = new List<string>(selectedModules);
                                // Ensure 'core' is always included
                                if (!modulesToAdd.Contains("core", StringComparer.OrdinalIgnoreCase))
                                {
                                    modulesToAdd.Insert(0, "core");
                                }
                                _logger?.LogInformation("Creating custom subscription with modules: {Modules} for customer {CustomerCode}", 
                                    string.Join(", ", modulesToAdd), customer.CustomerCode);
                            }
                            // If plan ID found, use predefined plan
                            else if (planId.HasValue)
                            {
                                _logger?.LogInformation("Creating predefined plan subscription (PlanId: {PlanId}) for customer {CustomerCode}", 
                                    planId, customer.CustomerCode);
                            }
                            // Default: create subscription with core module only
                            else
                            {
                                modulesToAdd = new List<string> { "core" };
                                _logger?.LogInformation("No plan or modules specified. Creating subscription with core module only for customer {CustomerCode}", 
                                    customer.CustomerCode);
                            }
                            
                            var startDate = customer.ContractStartDate ?? DateTime.Today;
                            var endDate = customer.ContractEndDate;
                            var monthlyFee = customer.MonthlyFee;
                            var autoRenewal = customer.AutoRenewal;
                            
                            await subscriptionService.CreateSubscriptionAsync(
                                customerId: customer.CustomerId,
                                planId: planId,
                                customModules: modulesToAdd,
                                startDate: startDate,
                                endDate: endDate,
                                monthlyFee: monthlyFee,
                                autoRenewal: autoRenewal,
                                createdBy: createdBy);
                            
                            _logger?.LogInformation("Subscription created successfully for customer {CustomerCode}", customer.CustomerCode);
                            
                            // Verify modules were stored correctly
                            try
                            {
                                var storedModules = await subscriptionService.GetCustomerModulesAsync(customer.CustomerId);
                                _logger?.LogInformation("Verified modules stored for customer {CustomerCode} (CustomerId: {CustomerId}): {Modules}", 
                                    customer.CustomerCode, customer.CustomerId, string.Join(", ", storedModules));
                                
                                if (!storedModules.Any() || (!storedModules.Contains("core", StringComparer.OrdinalIgnoreCase) && storedModules.Count == 0))
                                {
                                    _logger?.LogWarning("WARNING: No modules found for customer {CustomerCode} after subscription creation! Expected modules: {ExpectedModules}", 
                                        customer.CustomerCode, 
                                        planId.HasValue ? $"Plan modules (PlanId: {planId})" : string.Join(", ", modulesToAdd ?? new List<string>()));
                                }
                            }
                            catch (Exception verifyEx)
                            {
                                _logger?.LogError(verifyEx, "Error verifying modules for customer {CustomerCode}: {Message}", customer.CustomerCode, verifyEx.Message);
                            }
                        }
                        else
                        {
                            _logger?.LogWarning("SubscriptionService not available. Cannot create subscription for customer {CustomerCode}", customer.CustomerCode);
                        }
                    }
                }
                catch (Exception subEx)
                {
                    _logger?.LogError(subEx, "Failed to create subscription for customer {CustomerCode}: {Message}", customer.CustomerCode, subEx.Message);
                    // Don't fail customer creation if subscription creation fails
                }
            }

            // Create initial invoice for the customer if contract start date is set
            if (result > 0 && customer.CustomerId > 0 && customer.ContractStartDate.HasValue && customer.MonthlyFee > 0)
            {
                try
                {
                    await CreateInitialInvoiceAsync(customer, createdBy);
                }
                catch (Exception invoiceEx)
                {
                    _logger?.LogWarning(invoiceEx, "Failed to create initial invoice for customer {CustomerCode}: {Message}", customer.CustomerCode, invoiceEx.Message);
                    // Don't fail customer creation if invoice generation fails
                }
            }

            // Audit logging (non-blocking, background task)
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_serviceScopeFactory != null)
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var superAdminAuditLogService = scope.ServiceProvider.GetRequiredService<SuperAdminAuditLogService>();
                        var authService = scope.ServiceProvider.GetService<IAuthService>();
                        
                        var currentUser = authService?.CurrentUser;
                        var userName = currentUser != null ? $"{currentUser.first_name} {currentUser.last_name}".Trim() : "System";
                        if (string.IsNullOrWhiteSpace(userName) && currentUser != null)
                            userName = currentUser.email ?? "System";
                        var userRole = currentUser?.user_role ?? "SuperAdmin";
                        var userId = currentUser?.user_ID;
                        var ipAddress = GetLocalIpAddress();
                        
                        await superAdminAuditLogService.CreateTransactionLogAsync(
                            action: "Create Customer",
                            module: "Customer Management",
                            description: $"Created customer: {customer.SchoolName} ({customer.CustomerCode}) - Plan: {customer.SubscriptionPlan}, Status: {customer.Status}",
                            userName: userName,
                            userRole: userRole,
                            userId: userId,
                            entityType: "Customer",
                            entityId: customer.CustomerId.ToString(),
                            oldValues: null,
                            newValues: $"Code: {customer.CustomerCode}, School: {customer.SchoolName}, Plan: {customer.SubscriptionPlan}, Status: {customer.Status}, MonthlyFee: {customer.MonthlyFee}",
                            ipAddress: ipAddress,
                            status: "Success",
                            severity: "High",
                            customerCode: customer.CustomerCode,
                            customerName: customer.SchoolName
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to create SuperAdmin audit log for customer creation: {Message}", ex.Message);
                }
            });

            return customer;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            _logger?.LogError(dbEx, "Database error creating customer: {Message}", dbEx.Message);
            if (dbEx.InnerException != null)
            {
                _logger?.LogError("Inner exception: {Message}", dbEx.InnerException.Message);
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating customer: {Message}", ex.Message);
            throw;
        }
    }

    private async Task EnsureCustomersTableExistsAsync()
    {
        try
        {
            // Check if table exists by trying to query it
            var checkTableQuery = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tbl_Customers'";
            
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = checkTableQuery;
            var result = await command.ExecuteScalarAsync();
            var tableExists = Convert.ToInt32(result) > 0;
            
            if (!tableExists)
            {
                _logger?.LogInformation("tbl_Customers table does not exist. Creating it now...");
                
                // Get table definition
                var tableDef = TableDefinitions.GetCustomersTableDefinition();
                
                // Create table with IF NOT EXISTS check
                var createTableScript = $@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{tableDef.TableName}' AND schema_id = SCHEMA_ID('{tableDef.SchemaName}'))
                    BEGIN
                        {tableDef.CreateTableScript}
                    END";
                
                await _context.Database.ExecuteSqlRawAsync(createTableScript);
                
                // Create indexes
                foreach (var indexScript in tableDef.CreateIndexesScripts)
                {
                    try
                    {
                        await _context.Database.ExecuteSqlRawAsync(indexScript);
                    }
                    catch (Exception idxEx)
                    {
                        _logger?.LogWarning(idxEx, "Error creating index: {Message}", idxEx.Message);
                    }
                }
                
                _logger?.LogInformation("tbl_Customers table created successfully");
            }
            else
            {
                // Table exists, check if new columns need to be added
                _logger?.LogInformation("tbl_Customers table exists. Checking for new columns...");
                await EnsureNewColumnsExistAsync(connection);
            }
            
            await connection.CloseAsync();
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 2714 || sqlEx.Message.Contains("already exists") || sqlEx.Message.Contains("duplicate"))
        {
            // Table already exists (race condition), that's fine
            _logger?.LogInformation("tbl_Customers table already exists");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error ensuring Customers table exists: {Message}", ex.Message);
            // Don't throw - let the actual save operation handle the error
        }
    }

    private async Task EnsureNewColumnsExistAsync(System.Data.Common.DbConnection connection)
    {
        try
        {
            // Check and add database_name column
            var checkDbNameQuery = @"
                SELECT COUNT(*) 
                FROM sys.columns 
                WHERE object_id = OBJECT_ID('dbo.tbl_Customers') 
                AND name = 'database_name'";

            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.CommandText = checkDbNameQuery;
                var result = await checkCmd.ExecuteScalarAsync();
                var columnExists = result != null && Convert.ToInt32(result) > 0;

                if (!columnExists)
                {
                    var addColumnQuery = @"
                        ALTER TABLE [dbo].[tbl_Customers]
                        ADD [database_name] NVARCHAR(200) NULL";
                    using (var addCmd = connection.CreateCommand())
                    {
                        addCmd.CommandText = addColumnQuery;
                        await addCmd.ExecuteNonQueryAsync();
                        _logger?.LogInformation("Added database_name column to tbl_Customers");
                    }
                }
            }

            // Check and add database_connection_string column
            var checkConnStringQuery = @"
                SELECT COUNT(*) 
                FROM sys.columns 
                WHERE object_id = OBJECT_ID('dbo.tbl_Customers') 
                AND name = 'database_connection_string'";

            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.CommandText = checkConnStringQuery;
                var result = await checkCmd.ExecuteScalarAsync();
                var columnExists = result != null && Convert.ToInt32(result) > 0;

                if (!columnExists)
                {
                    var addColumnQuery = @"
                        ALTER TABLE [dbo].[tbl_Customers]
                        ADD [database_connection_string] NVARCHAR(MAX) NULL";
                    using (var addCmd = connection.CreateCommand())
                    {
                        addCmd.CommandText = addColumnQuery;
                        await addCmd.ExecuteNonQueryAsync();
                        _logger?.LogInformation("Added database_connection_string column to tbl_Customers");
                    }
                }
            }

            // Check and add admin_username column
            var checkAdminUserQuery = @"
                SELECT COUNT(*) 
                FROM sys.columns 
                WHERE object_id = OBJECT_ID('dbo.tbl_Customers') 
                AND name = 'admin_username'";

            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.CommandText = checkAdminUserQuery;
                var result = await checkCmd.ExecuteScalarAsync();
                var columnExists = result != null && Convert.ToInt32(result) > 0;

                if (!columnExists)
                {
                    var addColumnQuery = @"
                        ALTER TABLE [dbo].[tbl_Customers]
                        ADD [admin_username] NVARCHAR(100) NULL";
                    using (var addCmd = connection.CreateCommand())
                    {
                        addCmd.CommandText = addColumnQuery;
                        await addCmd.ExecuteNonQueryAsync();
                        _logger?.LogInformation("Added admin_username column to tbl_Customers");
                    }
                }
            }

            // Check and add admin_password column
            var checkAdminPassQuery = @"
                SELECT COUNT(*) 
                FROM sys.columns 
                WHERE object_id = OBJECT_ID('dbo.tbl_Customers') 
                AND name = 'admin_password'";

            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.CommandText = checkAdminPassQuery;
                var result = await checkCmd.ExecuteScalarAsync();
                var columnExists = result != null && Convert.ToInt32(result) > 0;

                if (!columnExists)
                {
                    var addColumnQuery = @"
                        ALTER TABLE [dbo].[tbl_Customers]
                        ADD [admin_password] NVARCHAR(255) NULL";
                    using (var addCmd = connection.CreateCommand())
                    {
                        addCmd.CommandText = addColumnQuery;
                        await addCmd.ExecuteNonQueryAsync();
                        _logger?.LogInformation("Added admin_password column to tbl_Customers");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error ensuring new columns exist: {Message}", ex.Message);
        }
    }

    public async Task<Customer> UpdateCustomerAsync(Customer customer)
    {
        // Get original values for audit log
        var originalCustomer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
        var oldValues = originalCustomer != null ? $"Code: {originalCustomer.CustomerCode}, School: {originalCustomer.SchoolName}, Plan: {originalCustomer.SubscriptionPlan}, Status: {originalCustomer.Status}, MonthlyFee: {originalCustomer.MonthlyFee}" : null;

        customer.UpdatedAt = DateTime.Now;
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();

        _logger?.LogInformation("Customer updated: {CustomerCode}", customer.CustomerCode);

        // Audit logging (non-blocking, background task)
        _ = Task.Run(async () =>
        {
            try
            {
                if (_serviceScopeFactory != null)
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var superAdminAuditLogService = scope.ServiceProvider.GetRequiredService<SuperAdminAuditLogService>();
                    var authService = scope.ServiceProvider.GetService<IAuthService>();
                    
                    var currentUser = authService?.CurrentUser;
                    var userName = currentUser != null ? $"{currentUser.first_name} {currentUser.last_name}".Trim() : "System";
                    if (string.IsNullOrWhiteSpace(userName) && currentUser != null)
                        userName = currentUser.email ?? "System";
                    var userRole = currentUser?.user_role ?? "SuperAdmin";
                    var userId = currentUser?.user_ID;
                    var ipAddress = GetLocalIpAddress();
                    
                    await superAdminAuditLogService.CreateTransactionLogAsync(
                        action: "Update Customer",
                        module: "Customer Management",
                        description: $"Updated customer: {customer.SchoolName} ({customer.CustomerCode})",
                        userName: userName,
                        userRole: userRole,
                        userId: userId,
                        entityType: "Customer",
                        entityId: customer.CustomerId.ToString(),
                        oldValues: oldValues,
                        newValues: $"Code: {customer.CustomerCode}, School: {customer.SchoolName}, Plan: {customer.SubscriptionPlan}, Status: {customer.Status}, MonthlyFee: {customer.MonthlyFee}",
                        ipAddress: ipAddress,
                        status: "Success",
                        severity: "High",
                        customerCode: customer.CustomerCode,
                        customerName: customer.SchoolName
                    );
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to create SuperAdmin audit log for customer update: {Message}", ex.Message);
            }
        });

        return customer;
    }

    // Creates initial invoice for a new customer
    private async Task CreateInitialInvoiceAsync(Customer customer, int? createdBy = null)
    {
        if (!customer.ContractStartDate.HasValue || customer.MonthlyFee <= 0)
        {
            return;
        }

        try
        {
            // Calculate subtotal and VAT
            decimal subtotal = customer.MonthlyFee;
            decimal vatAmount = 0;
            decimal totalAmount = customer.MonthlyFee;

            if (customer.IsVatRegistered && customer.VatRate.HasValue && customer.VatRate.Value > 0)
            {
                // VAT is included in MonthlyFee, so calculate backwards
                subtotal = customer.MonthlyFee / (1 + (customer.VatRate.Value / 100m));
                vatAmount = customer.MonthlyFee - subtotal;
                totalAmount = customer.MonthlyFee;
            }

            var invoiceDate = customer.ContractStartDate.Value;
            var dueDate = invoiceDate.AddDays(30); // Default 30-day payment terms
            var billingPeriodEnd = invoiceDate.AddMonths(1).AddDays(-1);

            var invoice = new CustomerInvoice
            {
                CustomerId = customer.CustomerId,
                InvoiceDate = invoiceDate,
                DueDate = dueDate,
                BillingPeriodStart = customer.ContractStartDate.Value,
                BillingPeriodEnd = billingPeriodEnd,
                Subtotal = Math.Round(subtotal, 2),
                VatAmount = Math.Round(vatAmount, 2),
                TotalAmount = Math.Round(totalAmount, 2),
                Balance = Math.Round(totalAmount, 2),
                Status = "Pending",
                PaymentTerms = customer.PaymentTerms ?? "Net 30",
                CreatedBy = createdBy
            };

            // Generate invoice number
            var year = invoiceDate.Year;
            var month = invoiceDate.Month.ToString("00");
            var prefix = $"INV-{year}{month}-";

            var lastInvoice = await _context.CustomerInvoices
                .Where(i => i.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastInvoice != null)
            {
                var parts = lastInvoice.InvoiceNumber.Split('-');
                if (parts.Length > 2 && int.TryParse(parts[2], out int lastSeq))
                {
                    sequence = lastSeq + 1;
                }
            }
            invoice.InvoiceNumber = $"{prefix}{sequence:0000}";

            _context.CustomerInvoices.Add(invoice);
            await _context.SaveChangesAsync();

            _logger?.LogInformation("Initial invoice created: {InvoiceNumber} for customer {CustomerCode}", invoice.InvoiceNumber, customer.CustomerCode);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating initial invoice for customer {CustomerCode}: {Message}", customer.CustomerCode, ex.Message);
            throw;
        }
    }

    public async Task<bool> DeleteCustomerAsync(int customerId)
    {
        try
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
            {
                _logger?.LogWarning("Customer not found for deletion: {CustomerId}", customerId);
                return false;
            }

            var oldValues = $"Code: {customer.CustomerCode}, School: {customer.SchoolName}, Plan: {customer.SubscriptionPlan}, Status: {customer.Status}";

            _context.Customers.Remove(customer);
            var result = await _context.SaveChangesAsync();

            _logger?.LogInformation("Customer deleted: {CustomerId}, Rows affected: {Result}", customerId, result);

            // Audit logging (non-blocking, background task)
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_serviceScopeFactory != null)
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var superAdminAuditLogService = scope.ServiceProvider.GetRequiredService<SuperAdminAuditLogService>();
                        var authService = scope.ServiceProvider.GetService<IAuthService>();
                        
                        var currentUser = authService?.CurrentUser;
                        var userName = currentUser != null ? $"{currentUser.first_name} {currentUser.last_name}".Trim() : "System";
                        if (string.IsNullOrWhiteSpace(userName) && currentUser != null)
                            userName = currentUser.email ?? "System";
                        var userRole = currentUser?.user_role ?? "SuperAdmin";
                        var userId = currentUser?.user_ID;
                        var ipAddress = GetLocalIpAddress();
                        
                        await superAdminAuditLogService.CreateTransactionLogAsync(
                            action: "Delete Customer",
                            module: "Customer Management",
                            description: $"Deleted customer: {customer.SchoolName} ({customer.CustomerCode})",
                            userName: userName,
                            userRole: userRole,
                            userId: userId,
                            entityType: "Customer",
                            entityId: customerId.ToString(),
                            oldValues: oldValues,
                            newValues: null,
                            ipAddress: ipAddress,
                            status: "Success",
                            severity: "Critical",
                            customerCode: customer.CustomerCode,
                            customerName: customer.SchoolName
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to create SuperAdmin audit log for customer deletion: {Message}", ex.Message);
                }
            });

            return result > 0;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting customer {CustomerId}: {Message}", customerId, ex.Message);
            throw;
        }
    }

    // Seeds school information into the school's database
    private async Task SeedSchoolInformationAsync(Customer customer, string? connectionString = null)
    {
        try
        {
            var dbConnectionString = connectionString ?? customer.DatabaseConnectionString;
            if (string.IsNullOrWhiteSpace(dbConnectionString))
            {
                return;
            }

            using var connection = new SqlConnection(dbConnectionString);
            await connection.OpenAsync();

            // Parse address components
            var (houseNo, streetName, barangay, city, province, zipCode) = ParseAddress(customer.Address ?? string.Empty);

            // Check if school information already exists
            var checkQuery = "SELECT COUNT(*) FROM [dbo].[tbl_SchoolInformation]";
            using var checkCommand = new SqlCommand(checkQuery, connection);
            var existingCount = await checkCommand.ExecuteScalarAsync();

            if (existingCount != null && (int)existingCount > 0)
            {
                // Update existing record
                var updateQuery = @"
                    UPDATE [dbo].[tbl_SchoolInformation]
                    SET [school_name] = @SchoolName,
                        [contact_number] = @ContactPhone,
                        [email] = @ContactEmail,
                        [house_no] = @HouseNo,
                        [street_name] = @StreetName,
                        [barangay] = @Barangay,
                        [city] = @City,
                        [province] = @Province,
                        [zip_code] = @ZipCode,
                        [bir_tin] = @BirTin,
                        [bir_business_name] = @BirBusinessName,
                        [bir_address] = @BirAddress,
                        [bir_registration_type] = @BirRegistrationType,
                        [vat_rate] = @VatRate,
                        [is_vat_registered] = @IsVatRegistered,
                        [updated_at] = GETDATE()
                    WHERE [school_info_id] = (SELECT TOP 1 [school_info_id] FROM [dbo].[tbl_SchoolInformation])";

                using var updateCommand = new SqlCommand(updateQuery, connection);
                updateCommand.Parameters.AddWithValue("@SchoolName", customer.SchoolName);
                updateCommand.Parameters.AddWithValue("@ContactPhone", (object?)customer.ContactPhone ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@ContactEmail", (object?)customer.ContactEmail ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@HouseNo", (object?)houseNo ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@StreetName", (object?)streetName ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@Barangay", (object?)barangay ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@City", (object?)city ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@Province", (object?)province ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@ZipCode", (object?)zipCode ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@BirTin", (object?)customer.BirTin ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@BirBusinessName", (object?)customer.BirBusinessName ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@BirAddress", (object?)customer.BirAddress ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@BirRegistrationType", (object?)customer.BirRegistrationType ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@VatRate", (object?)customer.VatRate ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@IsVatRegistered", customer.IsVatRegistered);

                await updateCommand.ExecuteNonQueryAsync();
                _logger?.LogInformation("School information updated successfully");
            }
            else
            {
                // Insert new record
                var insertQuery = @"
                    INSERT INTO [dbo].[tbl_SchoolInformation] (
                        [school_name], [school_code], [contact_number], [email],
                        [house_no], [street_name], [barangay], [city], [province], [country], [zip_code],
                        [bir_tin], [bir_business_name], [bir_address], [bir_registration_type],
                        [vat_rate], [is_vat_registered], [updated_at]
                    )
                    VALUES (
                        @SchoolName, @SchoolCode, @ContactPhone, @ContactEmail,
                        @HouseNo, @StreetName, @Barangay, @City, @Province, @Country, @ZipCode,
                        @BirTin, @BirBusinessName, @BirAddress, @BirRegistrationType,
                        @VatRate, @IsVatRegistered, GETDATE()
                    )";

                using var insertCommand = new SqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@SchoolName", customer.SchoolName);
                insertCommand.Parameters.AddWithValue("@SchoolCode", (object?)customer.CustomerCode ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@ContactPhone", (object?)customer.ContactPhone ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@ContactEmail", (object?)customer.ContactEmail ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@HouseNo", (object?)houseNo ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@StreetName", (object?)streetName ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@Barangay", (object?)barangay ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@City", (object?)city ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@Province", (object?)province ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@Country", "Philippines");
                insertCommand.Parameters.AddWithValue("@ZipCode", (object?)zipCode ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@BirTin", (object?)customer.BirTin ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@BirBusinessName", (object?)customer.BirBusinessName ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@BirAddress", (object?)customer.BirAddress ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@BirRegistrationType", (object?)customer.BirRegistrationType ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@VatRate", (object?)customer.VatRate ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@IsVatRegistered", customer.IsVatRegistered);

                await insertCommand.ExecuteNonQueryAsync();
                _logger?.LogInformation("School information seeded successfully");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding school information: {Message}", ex.Message);
            // Don't throw - allow customer creation to continue
        }
    }

    // Helper method to parse address string into components
    private (string? houseNo, string? streetName, string? barangay, string? city, string? province, string? zipCode) ParseAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return (null, null, null, null, null, null);
        }

        // Simple parsing - can be enhanced
        var parts = address.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        return (
            houseNo: parts.Length > 0 ? parts[0] : null,
            streetName: parts.Length > 1 ? parts[1] : null,
            barangay: parts.Length > 2 ? parts[2] : null,
            city: parts.Length > 3 ? parts[3] : null,
            province: parts.Length > 4 ? parts[4] : null,
            zipCode: null // ZIP code typically needs separate field
        );
    }

    private async Task<string> GenerateCustomerCodeAsync()
    {
        try
        {
            var lastCustomer = await _context.Customers
                .OrderByDescending(c => c.CustomerId)
                .FirstOrDefaultAsync();

            if (lastCustomer == null)
            {
                return "CUST-001";
            }

            var lastNumber = int.Parse(lastCustomer.CustomerCode.Split('-')[1]);
            return $"CUST-{(lastNumber + 1):D3}";
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error generating customer code. Returning default code.");
            return "CUST-001";
        }
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        // Return default values immediately if context is not available
        if (_context == null || _context.Database == null)
        {
            _logger?.LogWarning("DbContext is not available for dashboard stats.");
            return new DashboardStats
            {
                TotalCustomers = 0,
                ActiveSubscriptions = 0,
                MonthlyRevenue = 0,
                OpenTickets = 0
            };
        }

        try
        {
            // Check if database is available with timeout to prevent hanging
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                if (!await _context.Database.CanConnectAsync(cts.Token))
                {
                    _logger?.LogWarning("Database connection not available for dashboard stats.");
                    return new DashboardStats
                    {
                        TotalCustomers = 0,
                        ActiveSubscriptions = 0,
                        MonthlyRevenue = 0,
                        OpenTickets = 0
                    };
                }
            }
            catch (OperationCanceledException)
            {
                _logger?.LogWarning("Database connection check timed out. Returning default values.");
                return new DashboardStats
                {
                    TotalCustomers = 0,
                    ActiveSubscriptions = 0,
                    MonthlyRevenue = 0,
                    OpenTickets = 0
                };
            }
            catch (Exception dbEx)
            {
                _logger?.LogWarning(dbEx, "Database connection check failed. Returning default values.");
                return new DashboardStats
                {
                    TotalCustomers = 0,
                    ActiveSubscriptions = 0,
                    MonthlyRevenue = 0,
                    OpenTickets = 0
                };
            }

            // Check if tables exist before querying - use raw SQL to avoid EF issues
            try
            {
                var tableExists = await _context.Database.ExecuteSqlRawAsync(
                    "SELECT CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tbl_Customers') THEN 1 ELSE 0 END");
            }
            catch
            {
                _logger?.LogWarning("Could not check if tables exist. Returning default values.");
                return new DashboardStats
                {
                    TotalCustomers = 0,
                    ActiveSubscriptions = 0,
                    MonthlyRevenue = 0,
                    OpenTickets = 0
                };
            }

            // Try to query tables - use simple queries without complex operations
            int totalCustomers = 0;
            int activeSubscriptions = 0;
            decimal monthlyRevenue = 0;
            int openTickets = 0;

            try
            {
                totalCustomers = await _context.Customers.CountAsync();
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208 || sqlEx.Message.Contains("Invalid object name"))
            {
                _logger?.LogWarning("Table tbl_Customers does not exist yet.");
            }
            catch (InvalidOperationException)
            {
                _logger?.LogWarning("Invalid operation querying customers table.");
            }

            try
            {
                activeSubscriptions = await _context.Customers.CountAsync(c => c.Status == "Active" && c.ContractEndDate > DateTime.Now);
            }
            catch
            {
                // Ignore - use 0
            }

            try
            {
                // Calculate actual monthly revenue from payments received in current month
                // Exclude credit payments - only count actual cash/bank/online payments
                var currentMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);
                
                monthlyRevenue = await _context.CustomerPayments
                    .Where(p => p.PaymentDate >= currentMonthStart && 
                                p.PaymentDate <= currentMonthEnd &&
                                p.PaymentMethod != null &&
                                !p.PaymentMethod.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;
                
                // If no payments this month, fallback to sum of active customer monthly fees
                if (monthlyRevenue == 0)
                {
                    var activeCustomers = await _context.Customers
                        .Where(c => c.Status == "Active")
                        .ToListAsync();
                    
                    foreach (var customer in activeCustomers)
                    {
                        // MonthlyFee already includes VAT if IsVatRegistered
                        monthlyRevenue += customer.MonthlyFee;
                    }
                }
            }
            catch
            {
                // Ignore - use 0
            }
            
            try
            {
                openTickets = await _context.SupportTickets.CountAsync(t => t.Status == "Open" || t.Status == "In Progress");
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208 || sqlEx.Message.Contains("Invalid object name"))
            {
                _logger?.LogWarning("Table tbl_SupportTickets does not exist yet.");
            }
            catch
            {
                // Ignore - use 0
            }

            return new DashboardStats
            {
                TotalCustomers = totalCustomers,
                ActiveSubscriptions = activeSubscriptions,
                MonthlyRevenue = monthlyRevenue,
                OpenTickets = openTickets
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger?.LogWarning(ex, "Invalid operation loading dashboard stats. Returning default values.");
            return new DashboardStats
            {
                TotalCustomers = 0,
                ActiveSubscriptions = 0,
                MonthlyRevenue = 0,
                OpenTickets = 0
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error loading dashboard stats. Returning default values.");
            return new DashboardStats
            {
                TotalCustomers = 0,
                ActiveSubscriptions = 0,
                MonthlyRevenue = 0,
                OpenTickets = 0
            };
        }
    }

    public async Task<Dictionary<string, decimal>> GetRevenueTrendAsync(int months = 6)
    {
        var revenueTrend = new Dictionary<string, decimal>();
        
        if (_context == null || _context.Database == null)
        {
            return revenueTrend;
        }

        try
        {
            var endDate = DateTime.Now;
            var startDate = endDate.AddMonths(-months);

            // Get payments grouped by month
            // Exclude credit payments - only count actual cash/bank/online payments
            var payments = await _context.CustomerPayments
                .Where(p => p.PaymentDate >= startDate && 
                           p.PaymentDate <= endDate &&
                           p.PaymentMethod != null &&
                           !p.PaymentMethod.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                .ToListAsync();

            // Group by month and sum amounts
            for (int i = months - 1; i >= 0; i--)
            {
                var monthDate = endDate.AddMonths(-i);
                var monthKey = monthDate.ToString("MMM");
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var monthRevenue = payments
                    .Where(p => p.PaymentDate >= monthStart && p.PaymentDate <= monthEnd)
                    .Sum(p => p.Amount);

                revenueTrend[monthKey] = monthRevenue;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error loading revenue trend. Returning empty data.");
        }

        return revenueTrend;
    }

    public async Task<Dictionary<string, int>> GetSalesByPlanAsync()
    {
        var salesByPlan = new Dictionary<string, int>();
        
        if (_context == null || _context.Database == null)
        {
            return salesByPlan;
        }

        try
        {
            var customers = await _context.Customers
                .Where(c => c.Status == "Active" && !string.IsNullOrEmpty(c.SubscriptionPlan))
                .ToListAsync();

            salesByPlan = customers
                .GroupBy(c => c.SubscriptionPlan ?? "Custom")
                .ToDictionary(g => g.Key, g => g.Count());
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error loading sales by plan. Returning empty data.");
        }

        return salesByPlan;
    }

    public async Task<List<Customer>> GetExpiringSubscriptionsAsync(int daysAhead = 30)
    {
        if (_context == null || _context.Database == null)
        {
            return new List<Customer>();
        }

        try
        {
            var expiryDate = DateTime.Now.AddDays(daysAhead);
            
            return await _context.Customers
                .Where(c => c.Status == "Active" && 
                           c.ContractEndDate.HasValue &&
                           c.ContractEndDate.Value >= DateTime.Now && 
                           c.ContractEndDate.Value <= expiryDate)
                .OrderBy(c => c.ContractEndDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error loading expiring subscriptions. Returning empty list.");
            return new List<Customer>();
        }
    }

    public async Task<List<CustomerPayment>> GetRecentPaymentsAsync(int count = 5)
    {
        if (_context == null || _context.Database == null)
        {
            return new List<CustomerPayment>();
        }

        try
        {
            return await _context.CustomerPayments
                .Include(p => p.Customer)
                .OrderByDescending(p => p.PaymentDate)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error loading recent payments. Returning empty list.");
            return new List<CustomerPayment>();
        }
    }

    #endregion

    // Sales Lead Operations removed - Sales Lead functionality not needed

    #region Support Ticket Operations

    public async Task<List<SupportTicket>> GetAllSupportTicketsAsync()
    {
        try
        {
            // Use AsNoTracking to ensure fresh data from database without caching
            return await _context.SupportTickets
                .AsNoTracking()
                .Include(t => t.Customer)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error loading support tickets. Tables may not exist yet.");
            return new List<SupportTicket>();
        }
    }

    public async Task<SupportTicket?> GetSupportTicketByIdAsync(int ticketId)
    {
        return await _context.SupportTickets
            .Include(t => t.Customer)
            .Include(t => t.AssignedToUser)
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);
    }

    public async Task<List<SupportTicket>> GetOpenTicketsAsync()
    {
        return await _context.SupportTickets
            .Include(t => t.Customer)
            .Where(t => t.Status == "Open" || t.Status == "In Progress")
            .OrderByDescending(t => t.Priority == "Critical")
            .ThenByDescending(t => t.Priority == "High")
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<SupportTicket> CreateSupportTicketAsync(SupportTicket ticket)
    {
        const int maxRetries = 5;
        int retryCount = 0;
        string originalTicketNumber = ticket.TicketNumber;
        
        while (retryCount < maxRetries)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ticket.TicketNumber))
                {
                    ticket.TicketNumber = await GenerateTicketNumberAsync();
                }

                ticket.CreatedAt = DateTime.Now;
                
                // If this is a retry, we need to detach the entity first
                if (retryCount > 0)
                {
                    var entry = _context.Entry(ticket);
                    if (entry.State != Microsoft.EntityFrameworkCore.EntityState.Detached)
                    {
                        entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                    // Create a new ticket object for retry to avoid tracking issues
                    ticket = new SupportTicket
                    {
                        CustomerId = ticket.CustomerId,
                        Subject = ticket.Subject,
                        Description = ticket.Description,
                        Priority = ticket.Priority,
                        Status = ticket.Status,
                        Category = ticket.Category,
                        AssignedTo = ticket.AssignedTo,
                        TicketNumber = ticket.TicketNumber,
                        CreatedAt = DateTime.Now
                    };
                }
                
                _context.SupportTickets.Add(ticket);
                await _context.SaveChangesAsync();

                _logger?.LogInformation("Support ticket created: {TicketNumber}", ticket.TicketNumber);
                break; // Success - exit retry loop
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx) when (dbEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && 
                                                                              (sqlEx.Number == 2627 || sqlEx.Number == 2601)) // Unique constraint violation
            {
                retryCount++;
                
                // Detach the entity from context before retry
                var entry = _context.Entry(ticket);
                if (entry.State != Microsoft.EntityFrameworkCore.EntityState.Detached)
                {
                    entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                
                if (retryCount >= maxRetries)
                {
                    _logger?.LogError(dbEx, "Failed to create support ticket after {RetryCount} retries due to duplicate ticket number: {TicketNumber}", retryCount, ticket.TicketNumber);
                    throw new InvalidOperationException($"Unable to generate unique ticket number after {maxRetries} attempts. Please try again.", dbEx);
                }
                
                // Generate a new ticket number and retry
                _logger?.LogWarning("Duplicate ticket number detected: {TicketNumber}. Retrying with new number (attempt {RetryCount}/{MaxRetries})", ticket.TicketNumber, retryCount, maxRetries);
                ticket.TicketNumber = await GenerateTicketNumberAsync();
                
                // Small delay to reduce contention
                await Task.Delay(50 * retryCount);
            }
        }

        // Audit logging (non-blocking, background task)
        _ = Task.Run(async () =>
        {
            try
            {
                if (_serviceScopeFactory != null)
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var auditLogService = scope.ServiceProvider.GetRequiredService<AuditLogService>();
                    var authService = scope.ServiceProvider.GetService<IAuthService>();
                    
                    var currentUser = authService?.CurrentUser;
                    var userName = currentUser != null ? $"{currentUser.first_name} {currentUser.last_name}".Trim() : "System";
                    var userRole = currentUser?.user_role ?? "System";
                    var userId = currentUser?.user_ID;
                    
                    await auditLogService.CreateTransactionLogAsync(
                        action: "Create Support Ticket",
                        module: "Super Admin",
                        description: $"Created support ticket: {ticket.TicketNumber} - Subject: {ticket.Subject}, Priority: {ticket.Priority}",
                        userName: userName,
                        userRole: userRole,
                        userId: userId,
                        entityType: "SupportTicket",
                        entityId: ticket.TicketId.ToString(),
                        oldValues: null,
                        newValues: $"Number: {ticket.TicketNumber}, Subject: {ticket.Subject}, Priority: {ticket.Priority}, Status: {ticket.Status}, CustomerId: {ticket.CustomerId}",
                        status: "Success",
                        severity: "Medium"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to create audit log for support ticket creation: {Message}", ex.Message);
            }
        });

        // Create notification for SuperAdmin (non-blocking, background task)
        var ticketId = ticket.TicketId;
        var ticketNumber = ticket.TicketNumber;
        var ticketSubject = ticket.Subject;
        var ticketPriority = ticket.Priority;
        var customerId = ticket.CustomerId;
        
        _ = Task.Run(async () =>
        {
            try
            {
                if (_serviceScopeFactory != null)
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var notificationService = scope.ServiceProvider.GetService<SuperAdminNotificationService>();
                    var scopedContext = scope.ServiceProvider.GetService<SuperAdminDbContext>();
                    
                    if (notificationService != null)
                    {
                        // Determine priority based on ticket priority
                        string notificationPriority = ticketPriority switch
                        {
                            "Critical" => "Urgent",
                            "High" => "High",
                            "Medium" => "Normal",
                            "Low" => "Low",
                            _ => "Normal"
                        };

                        // Get customer name if available
                        string customerName = "Unknown Customer";
                        if (customerId.HasValue && scopedContext != null)
                        {
                            try
                            {
                                var customer = await scopedContext.Customers
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(c => c.CustomerId == customerId.Value);
                                if (customer != null)
                                {
                                    customerName = customer.SchoolName;
                                }
                            }
                            catch
                            {
                                // Ignore errors getting customer name
                            }
                        }

                        await notificationService.CreateNotificationAsync(
                            notificationType: "SupportTicket",
                            title: $"New Support Ticket: {ticketNumber}",
                            message: $"New support ticket from {customerName}: {ticketSubject}",
                            referenceType: "SupportTicket",
                            referenceId: ticketId,
                            actionUrl: "/system-admin/support",
                            priority: notificationPriority,
                            createdBy: null
                        );
                        
                        _logger?.LogInformation("Notification created for support ticket: {TicketNumber}", ticketNumber);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to create notification for support ticket creation: {Message}", ex.Message);
            }
        });

        return ticket;
    }

    public async Task<SupportTicket> UpdateSupportTicketAsync(SupportTicket ticket)
    {
        if (ticket.Status == "Resolved" || ticket.Status == "Closed")
        {
            ticket.ResolvedAt = DateTime.Now;
        }

        // Get original values for audit log
        var originalTicket = await _context.SupportTickets.AsNoTracking().FirstOrDefaultAsync(t => t.TicketId == ticket.TicketId);
        var oldValues = originalTicket != null ? $"Number: {originalTicket.TicketNumber}, Status: {originalTicket.Status}, Priority: {originalTicket.Priority}" : null;

        ticket.UpdatedAt = DateTime.Now;
        _context.SupportTickets.Update(ticket);
        await _context.SaveChangesAsync();

        _logger?.LogInformation("Support ticket updated: {TicketNumber}", ticket.TicketNumber);

        // Audit logging (non-blocking, background task)
        _ = Task.Run(async () =>
        {
            try
            {
                if (_serviceScopeFactory != null)
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var auditLogService = scope.ServiceProvider.GetRequiredService<AuditLogService>();
                    var authService = scope.ServiceProvider.GetService<IAuthService>();
                    
                    var currentUser = authService?.CurrentUser;
                    var userName = currentUser != null ? $"{currentUser.first_name} {currentUser.last_name}".Trim() : "System";
                    var userRole = currentUser?.user_role ?? "System";
                    var userId = currentUser?.user_ID;
                    
                    await auditLogService.CreateTransactionLogAsync(
                        action: "Update Support Ticket",
                        module: "Super Admin",
                        description: $"Updated support ticket: {ticket.TicketNumber} - Status: {ticket.Status}",
                        userName: userName,
                        userRole: userRole,
                        userId: userId,
                        entityType: "SupportTicket",
                        entityId: ticket.TicketId.ToString(),
                        oldValues: oldValues,
                        newValues: $"Number: {ticket.TicketNumber}, Status: {ticket.Status}, Priority: {ticket.Priority}",
                        status: "Success",
                        severity: "Medium"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to create audit log for support ticket update: {Message}", ex.Message);
            }
        });

        return ticket;
    }

    private async Task<string> GenerateTicketNumberAsync()
    {
        try
        {
            var year = DateTime.Now.Year;
            
            // Use AsNoTracking for read-only query to avoid concurrency issues
            var lastTicket = await _context.SupportTickets
                .AsNoTracking()
                .Where(t => t.TicketNumber.StartsWith($"TKT-{year}-"))
                .OrderByDescending(t => t.TicketId)
                .FirstOrDefaultAsync();

            if (lastTicket == null)
            {
                return $"TKT-{year}-001";
            }

            var parts = lastTicket.TicketNumber.Split('-');
            if (parts.Length >= 3 && int.TryParse(parts[2], out var lastNumber))
            {
                return $"TKT-{year}-{(lastNumber + 1):D3}";
            }
            else
            {
                // Fallback if ticket number format is unexpected
                _logger?.LogWarning("Unexpected ticket number format: {TicketNumber}. Using timestamp-based fallback.", lastTicket.TicketNumber);
                var timestamp = DateTime.Now.Ticks % 1000000; // Use last 6 digits of ticks
                return $"TKT-{year}-{timestamp:D6}";
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error generating ticket number. Using timestamp-based fallback.");
            // Fallback: Use timestamp to ensure uniqueness
            var year = DateTime.Now.Year;
            var timestamp = DateTime.Now.Ticks % 1000000; // Use last 6 digits of ticks
            return $"TKT-{year}-{timestamp:D6}";
        }
    }

    #endregion

    // Contract Operations removed - Contract information is stored in tbl_Customers table
    // (ContractStartDate, ContractEndDate, ContractDurationMonths, ContractTermsText, etc.)

    #region System Update Operations

    public async Task<List<SystemUpdate>> GetAllSystemUpdatesAsync()
    {
        try
        {
            return await _context.SystemUpdates
                .OrderByDescending(u => u.ReleaseDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error loading system updates. Tables may not exist yet.");
            return new List<SystemUpdate>();
        }
    }

    public async Task<SystemUpdate?> GetSystemUpdateByIdAsync(int updateId)
    {
        return await _context.SystemUpdates
            .FirstOrDefaultAsync(u => u.UpdateId == updateId);
    }

    public async Task<SystemUpdate> CreateSystemUpdateAsync(SystemUpdate update, int? createdBy = null)
    {
        update.CreatedBy = createdBy;
        update.CreatedAt = DateTime.Now;
        _context.SystemUpdates.Add(update);
        await _context.SaveChangesAsync();

        _logger?.LogInformation("System update created: {VersionNumber}", update.VersionNumber);
        return update;
    }

    public async Task<SystemUpdate> UpdateSystemUpdateAsync(SystemUpdate update)
    {
        update.UpdatedAt = DateTime.Now;
        _context.SystemUpdates.Update(update);
        await _context.SaveChangesAsync();

        _logger?.LogInformation("System update updated: {VersionNumber}", update.VersionNumber);
        return update;
    }

    #endregion

    #region Cloud Sync Methods

    /// <summary>
    /// Syncs a newly created admin account to the cloud database
    /// </summary>
    private async Task SyncAdminToCloudAsync(string cloudConnectionString, string? schoolDatabaseConnectionString, SchoolAdminInfo adminInfo)
    {
        try
        {
            using var connection = new SqlConnection(cloudConnectionString);
            await connection.OpenAsync();
            
            var query = @"
                IF NOT EXISTS (SELECT 1 FROM tbl_Users WHERE email = @Email)
                INSERT INTO tbl_Users (system_ID, first_name, last_name, email, password, user_role, status, date_hired)
                VALUES (@SystemId, @FirstName, @LastName, @Email, @Password, @UserRole, @Status, @DateHired)";
            
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SystemId", adminInfo.SystemId ?? "");
            command.Parameters.AddWithValue("@FirstName", adminInfo.FirstName ?? "");
            command.Parameters.AddWithValue("@LastName", adminInfo.LastName ?? "");
            command.Parameters.AddWithValue("@Email", adminInfo.Email ?? "");
            
            // Get hashed password from school's local database
            string hashedPassword = "";
            if (!string.IsNullOrWhiteSpace(schoolDatabaseConnectionString) && !string.IsNullOrWhiteSpace(adminInfo.Email))
            {
                try
                {
                    using var localConnection = new SqlConnection(schoolDatabaseConnectionString);
                    await localConnection.OpenAsync();
                    
                    var getPasswordQuery = "SELECT password FROM tbl_Users WHERE email = @Email";
                    using var getPasswordCommand = new SqlCommand(getPasswordQuery, localConnection);
                    getPasswordCommand.Parameters.AddWithValue("@Email", adminInfo.Email);
                    var passwordResult = await getPasswordCommand.ExecuteScalarAsync();
                    
                    if (passwordResult != null && passwordResult != DBNull.Value)
                    {
                        hashedPassword = passwordResult.ToString() ?? "";
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Could not retrieve hashed password from school database, will hash plain password");
                }
            }
            
            // If we couldn't get the hashed password, hash the plain password
            if (string.IsNullOrWhiteSpace(hashedPassword))
            {
                hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminInfo.Password ?? "Admin123456");
            }
            
            command.Parameters.AddWithValue("@Password", hashedPassword);
            command.Parameters.AddWithValue("@UserRole", "Admin");
            command.Parameters.AddWithValue("@Status", "active");
            command.Parameters.AddWithValue("@DateHired", DateTime.Now);
            
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error syncing admin to cloud: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets the local machine's IP address for audit logging
    /// </summary>
    private string GetLocalIpAddress()
    {
        try
        {
            // Try to get the first non-loopback IPv4 address
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch
        {
            // Fallback if DNS lookup fails
        }

        // Fallback to localhost
        return "127.0.0.1";
    }

    #endregion
}

public class DashboardStats
{
    public int TotalCustomers { get; set; }
    public int ActiveSubscriptions { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int OpenTickets { get; set; }
}

