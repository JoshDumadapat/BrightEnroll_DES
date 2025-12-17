using BrightEnroll_DES.Models;
using BrightEnroll_DES.Services.DataAccess.Repositories;
using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using BrightEnroll_DES.Services.Database.Definitions;
using Microsoft.Data.SqlClient;

namespace BrightEnroll_DES.Services.Seeders
{
    public class DatabaseSeeder
    {
        private readonly IUserRepository _userRepository;
        private readonly AppDbContext _context;

        public DatabaseSeeder(IUserRepository userRepository, AppDbContext context)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // NOTE: Static user seeders have been removed. Users are now created dynamically through the Add Customer feature.
        // The SchoolAdminSeeder handles creating admin users for each school's database when a customer is added.

        public async Task SeedAdminRoleAsync()
        {
            try
            {
                // Check if Admin role already exists
                var existingRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleName == "Admin");
                
                if (existingRole != null)
                {
                    System.Diagnostics.Debug.WriteLine("Admin role already exists.");
                    return;
                }

                // Create Admin role with salary configuration matching the admin user
                var adminRole = new Role
                {
                    RoleName = "Admin",
                    BaseSalary = 50000.00m,
                    Allowance = 5000.00m,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.Roles.Add(adminRole);
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Admin role created successfully.");
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
            {
                System.Diagnostics.Debug.WriteLine("WARNING: tbl_roles table does not exist. Skipping role seeding.");
                // Don't throw - allow user creation to continue
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WARNING: Error seeding admin role: {ex.Message}. Continuing without role...");
                // Don't throw - allow user creation to continue
            }
        }

        // Seeds all required roles
        public async Task SeedAllRolesAsync()
        {
            try
            {
                var requiredRoles = new[]
                {
                    new { Name = "SuperAdmin", BaseSalary = 60000.00m, Allowance = 10000.00m },
                    new { Name = "Admin", BaseSalary = 50000.00m, Allowance = 5000.00m },
                    new { Name = "Registrar", BaseSalary = 35000.00m, Allowance = 3000.00m },
                    new { Name = "Cashier", BaseSalary = 30000.00m, Allowance = 2000.00m },
                    new { Name = "Teacher", BaseSalary = 40000.00m, Allowance = 4000.00m },
                    new { Name = "HR", BaseSalary = 45000.00m, Allowance = 5000.00m }
                };

                foreach (var roleInfo in requiredRoles)
                {
                    var existingRole = await _context.Roles
                        .FirstOrDefaultAsync(r => r.RoleName == roleInfo.Name);
                    
                    if (existingRole == null)
                    {
                        var role = new Role
                        {
                            RoleName = roleInfo.Name,
                            BaseSalary = roleInfo.BaseSalary,
                            Allowance = roleInfo.Allowance,
                            IsActive = true,
                            CreatedDate = DateTime.Now
                        };

                        _context.Roles.Add(role);
                    }
                    else
                    {
                        // Update existing role to ensure it's active
                        existingRole.IsActive = true;
                        if (existingRole.UpdatedDate == null)
                        {
                            existingRole.UpdatedDate = DateTime.Now;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("All roles seeded successfully.");
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
            {
                System.Diagnostics.Debug.WriteLine("WARNING: tbl_roles table does not exist. Skipping role seeding.");
                // Don't throw - allow user seeding to continue
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WARNING: Error seeding all roles: {ex.Message}. This is non-critical.");
                // Don't throw - allow user seeding to continue
            }
        }

        public async Task SeedDeductionsAsync()
        {
            try
            {
                // Check if deductions already exist
                var existingDeductions = await _context.Deductions.AnyAsync();
                if (existingDeductions)
                {
                    System.Diagnostics.Debug.WriteLine("Deductions already exist. Skipping seeding.");
                    return;
                }

                var deductions = new List<Deduction>
                {
                    // SSS Contribution
                    new Deduction
                    {
                        DeductionType = "SSS",
                        DeductionName = "Social Security System",
                        RateOrValue = 0.11m, // 11%
                        IsPercentage = true,
                        MaxAmount = 2000.00m, // Capped at ₱2,000/month
                        MinAmount = null,
                        Description = "SSS contribution at 11% of base salary, capped at ₱2,000/month. Based on Republic Act No. 11199 (Social Security Act of 2018).",
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    },
                    // PhilHealth Contribution
                    new Deduction
                    {
                        DeductionType = "PHILHEALTH",
                        DeductionName = "Philippine Health Insurance Corporation",
                        RateOrValue = 0.03m, // 3%
                        IsPercentage = true,
                        MaxAmount = null,
                        MinAmount = null,
                        Description = "PhilHealth contribution at 3% of base salary. Based on Republic Act No. 11223 (Universal Health Care Act).",
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    },
                    // Pag-IBIG Contribution
                    new Deduction
                    {
                        DeductionType = "PAGIBIG",
                        DeductionName = "Home Development Mutual Fund",
                        RateOrValue = 0.02m, // 2%
                        IsPercentage = true,
                        MaxAmount = 200.00m, // Capped at ₱200/month
                        MinAmount = null,
                        Description = "Pag-IBIG contribution at 2% of base salary, capped at ₱200/month. Based on Republic Act No. 9679 (Pag-IBIG Fund Law of 2009).",
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    },
                    // Withholding Tax (progressive brackets)
                    new Deduction
                    {
                        DeductionType = "WITHHOLDING_TAX",
                        DeductionName = "Withholding Tax (Income Tax)",
                        RateOrValue = 0.00m, // Progressive brackets - calculated separately
                        IsPercentage = false, // Not a simple percentage
                        MaxAmount = null,
                        MinAmount = null,
                        Description = "Withholding tax based on TRAIN Law (RA 10963) progressive tax brackets. Taxable income = Gross Pay - (SSS + PhilHealth + Pag-IBIG). No tax if taxable income ≤ ₱20,833.33/month.",
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    }
                };

                _context.Deductions.AddRange(deductions);
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Deductions seeded successfully.");
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
            {
                System.Diagnostics.Debug.WriteLine("WARNING: tbl_deductions table does not exist. Skipping deductions seeding.");
                // Don't throw - allow seeding to continue
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WARNING: Error seeding deductions: {ex.Message}. This is non-critical.");
                // Don't throw - allow seeding to continue
            }
        }

        // Verifies and creates finance tables if they don't exist
        public async Task EnsureFinanceTablesExistAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== CHECKING FINANCE TABLES ===");
                
                // Get connection string from context
                var dbConnection = _context.Database.GetDbConnection();
                var connectionString = dbConnection.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new Exception("Cannot get connection string from database context.");
                }

                // Build connection string to target database
                var builder = new SqlConnectionStringBuilder(connectionString);
                string dbConnectionString = builder.ConnectionString;

                using var connection = new SqlConnection(dbConnectionString);
                await connection.OpenAsync();
                
                // List of finance tables that should exist
                var financeTables = new[]
                {
                    "tbl_GradeLevel",
                    "tbl_Fees",
                    "tbl_FeeBreakdown",
                    "tbl_Expenses",
                    "tbl_ExpenseAttachments",
                    "tbl_StudentPayments",
                    "tbl_discounts",
                    "tbl_StudentLedgers",
                    "tbl_LedgerCharges"
                };

                // Check which tables exist
                string checkAllTablesQuery = @"
                    SELECT t.name
                    FROM sys.tables t
                    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE s.name = 'dbo' AND t.name IN ('tbl_GradeLevel', 'tbl_Fees', 'tbl_FeeBreakdown', 'tbl_Expenses', 'tbl_ExpenseAttachments', 'tbl_StudentPayments', 'tbl_discounts', 'tbl_StudentLedgers', 'tbl_LedgerCharges')";
                
                var existingTables = new HashSet<string>();
                using var checkCommand = new SqlCommand(checkAllTablesQuery, connection);
                using var reader = await checkCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    existingTables.Add(reader["name"].ToString() ?? string.Empty);
                }
                await reader.CloseAsync();

                // Get table definitions for finance tables
                var tableDefinitions = TableDefinitions.GetAllTableDefinitions();
                var financeTableDefinitions = new Dictionary<string, TableDefinition>();
                
                foreach (var tableDef in tableDefinitions)
                {
                    if (financeTables.Contains(tableDef.TableName))
                    {
                        financeTableDefinitions[tableDef.TableName] = tableDef;
                    }
                }

                bool anyTableCreated = false;

                // Create missing tables
                foreach (var tableName in financeTables)
                {
                    if (!existingTables.Contains(tableName))
                    {
                        System.Diagnostics.Debug.WriteLine($"Creating missing finance table: {tableName}");
                        
                        if (financeTableDefinitions.TryGetValue(tableName, out var tableDef))
                        {
                            // Create table
                            using var createCommand = new SqlCommand(tableDef.CreateTableScript, connection);
                            await createCommand.ExecuteNonQueryAsync();
                            
                            // Create indexes
                            foreach (var indexScript in tableDef.CreateIndexesScripts)
                            {
                                if (!string.IsNullOrWhiteSpace(indexScript))
                                {
                                    using var indexCommand = new SqlCommand(indexScript, connection);
                                    await indexCommand.ExecuteNonQueryAsync();
                                }
                            }
                            
                            anyTableCreated = true;
                            System.Diagnostics.Debug.WriteLine($"Successfully created table: {tableName}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"WARNING: Table definition not found for: {tableName}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Table already exists: {tableName}");
                    }
                }

                // Check and add discount_id column to tbl_LedgerCharges if table exists
                if (existingTables.Contains("tbl_LedgerCharges"))
                {
                    string checkDiscountIdColumnQuery = @"
                        SELECT COUNT(*) 
                        FROM sys.columns 
                        WHERE object_id = OBJECT_ID('dbo.tbl_LedgerCharges') 
                        AND name = 'discount_id'";

                    using var checkDiscountIdCommand = new SqlCommand(checkDiscountIdColumnQuery, connection);
                    var discountIdColumnExists = await checkDiscountIdCommand.ExecuteScalarAsync();
                    var hasDiscountIdColumn = discountIdColumnExists != null && (int)discountIdColumnExists > 0;

                    if (!hasDiscountIdColumn)
                    {
                        System.Diagnostics.Debug.WriteLine("Adding discount_id column to tbl_LedgerCharges...");
                        
                        // First ensure tbl_discounts exists
                        if (!existingTables.Contains("tbl_discounts"))
                        {
                            var discountsTableDef = tableDefinitions.FirstOrDefault(t => t.TableName == "tbl_discounts");
                            if (discountsTableDef != null)
                            {
                                using var createDiscountsCommand = new SqlCommand(discountsTableDef.CreateTableScript, connection);
                                await createDiscountsCommand.ExecuteNonQueryAsync();
                                
                                foreach (var indexScript in discountsTableDef.CreateIndexesScripts)
                                {
                                    if (!string.IsNullOrWhiteSpace(indexScript))
                                    {
                                        using var indexCommand = new SqlCommand(indexScript, connection);
                                        await indexCommand.ExecuteNonQueryAsync();
                                    }
                                }
                                
                                System.Diagnostics.Debug.WriteLine("Created tbl_discounts table.");
                                anyTableCreated = true;
                            }
                        }

                        // Add discount_id column
                        string addDiscountIdColumnQuery = @"
                            ALTER TABLE [dbo].[tbl_LedgerCharges]
                            ADD [discount_id] INT NULL";

                        using var addDiscountIdCommand = new SqlCommand(addDiscountIdColumnQuery, connection);
                        await addDiscountIdCommand.ExecuteNonQueryAsync();

                        // Add foreign key constraint if tbl_discounts exists
                        string checkFkQuery = @"
                            SELECT COUNT(*) 
                            FROM sys.foreign_keys 
                            WHERE name = 'FK_tbl_LedgerCharges_tbl_discounts'";

                        using var checkFkCommand = new SqlCommand(checkFkQuery, connection);
                        var fkExists = await checkFkCommand.ExecuteScalarAsync();
                        if (fkExists != null && (int)fkExists == 0)
                        {
                            string addFkQuery = @"
                                ALTER TABLE [dbo].[tbl_LedgerCharges]
                                ADD CONSTRAINT FK_tbl_LedgerCharges_tbl_discounts 
                                FOREIGN KEY ([discount_id]) REFERENCES [dbo].[tbl_discounts]([discount_id]) ON DELETE SET NULL";

                            using var addFkCommand = new SqlCommand(addFkQuery, connection);
                            await addFkCommand.ExecuteNonQueryAsync();
                        }

                        System.Diagnostics.Debug.WriteLine("Added discount_id column and foreign key to tbl_LedgerCharges.");
                        anyTableCreated = true;
                    }
                }

                if (anyTableCreated)
                {
                    System.Diagnostics.Debug.WriteLine("=== FINANCE TABLES CREATED/UPDATED SUCCESSFULLY ===");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("=== ALL FINANCE TABLES ALREADY EXIST ===");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: Failed to ensure finance tables exist: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new Exception($"Failed to ensure finance tables exist: {ex.Message}", ex);
            }
        }

        // Seeds the Chart of Accounts with standard accounts
        public async Task SeedChartOfAccountsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== SEEDING CHART OF ACCOUNTS ===");

                // Check if accounts already exist
                if (await _context.ChartOfAccounts.AnyAsync())
                {
                    System.Diagnostics.Debug.WriteLine("Chart of Accounts already seeded, skipping.");
                    return;
                }

                var accounts = new List<ChartOfAccount>
                {
                    // ASSETS (1000-1999)
                    new ChartOfAccount { AccountCode = "1000", AccountName = "Cash", AccountType = "Asset", NormalBalance = "Debit", Description = "Cash on hand and in bank" },
                    new ChartOfAccount { AccountCode = "1100", AccountName = "Accounts Receivable", AccountType = "Asset", NormalBalance = "Debit", Description = "Amounts owed by students" },
                    
                    // LIABILITIES (2000-2999)
                    new ChartOfAccount { AccountCode = "2000", AccountName = "Accounts Payable", AccountType = "Liability", NormalBalance = "Credit", Description = "Amounts owed to vendors" },
                    new ChartOfAccount { AccountCode = "2100", AccountName = "Accrued Payroll Taxes", AccountType = "Liability", NormalBalance = "Credit", Description = "SSS, PhilHealth, Pag-IBIG contributions" },
                    
                    // EQUITY (3000-3999)
                    new ChartOfAccount { AccountCode = "3000", AccountName = "Capital", AccountType = "Equity", NormalBalance = "Credit", Description = "Owner's capital" },
                    new ChartOfAccount { AccountCode = "3100", AccountName = "Retained Earnings", AccountType = "Equity", NormalBalance = "Credit", Description = "Accumulated profits" },
                    
                    // REVENUE (4000-4999)
                    new ChartOfAccount { AccountCode = "4000", AccountName = "Tuition Revenue", AccountType = "Revenue", NormalBalance = "Credit", Description = "Revenue from student tuition fees" },
                    new ChartOfAccount { AccountCode = "4100", AccountName = "Other Income", AccountType = "Revenue", NormalBalance = "Credit", Description = "Other sources of income" },
                    
                    // EXPENSES (5000-5999)
                    new ChartOfAccount { AccountCode = "5000", AccountName = "Salaries Expense", AccountType = "Expense", NormalBalance = "Debit", Description = "Employee salaries and wages" },
                    new ChartOfAccount { AccountCode = "5100", AccountName = "Other Expenses", AccountType = "Expense", NormalBalance = "Debit", Description = "General operating expenses" },
                    new ChartOfAccount { AccountCode = "5200", AccountName = "Utilities Expense", AccountType = "Expense", NormalBalance = "Debit", Description = "Electricity, water, internet, etc." },
                    new ChartOfAccount { AccountCode = "5300", AccountName = "Supplies Expense", AccountType = "Expense", NormalBalance = "Debit", Description = "Office and school supplies" },
                    new ChartOfAccount { AccountCode = "5400", AccountName = "Rent Expense", AccountType = "Expense", NormalBalance = "Debit", Description = "Rental payments" },
                    new ChartOfAccount { AccountCode = "5500", AccountName = "Maintenance Expense", AccountType = "Expense", NormalBalance = "Debit", Description = "Repairs and maintenance" },
                    new ChartOfAccount { AccountCode = "5600", AccountName = "Office Expense", AccountType = "Expense", NormalBalance = "Debit", Description = "General office expenses" }
                };

                _context.ChartOfAccounts.AddRange(accounts);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"=== CHART OF ACCOUNTS SEEDED SUCCESSFULLY ({accounts.Count} accounts) ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: Failed to seed Chart of Accounts: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new Exception($"Failed to seed Chart of Accounts: {ex.Message}", ex);
            }
        }

        // NOTE: SeedSuperAdminUserAsync() is DISABLED for security
        // SuperAdmin should NOT be seeded to school databases (AppDbContext)
        // SuperAdmin should ONLY be in:
        //   1. SuperAdmin database (DB_BrightEnroll_SuperAdmin) - for local dev/testing
        //   2. Cloud database - for production
        // Use SeedSuperAdminUserToSuperAdminDatabaseAsync() instead
        // 
        // The old SeedSuperAdminUserAsync() method has been removed to prevent
        // SuperAdmin from being created in school databases

        /// <summary>
        /// Seeds SuperAdmin user to SuperAdmin database (for local development/testing)
        /// NOTE: In production, SuperAdmin should be in cloud database only
        /// </summary>
        public async Task SeedSuperAdminUserToSuperAdminDatabaseAsync(SuperAdminDbContext superAdminContext)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== SEEDING SUPER ADMIN USER TO SUPERADMIN DATABASE ===");

                const string superAdminEmail = "superadmin@brightenroll.com";
                const string superAdminPassword = "SuperAdmin123456";
                const string superAdminSystemId = "SA001";

                // Check if super admin user already exists
                var existingUser = await superAdminContext.Users
                    .FirstOrDefaultAsync(u => u.Email == superAdminEmail || u.SystemId == superAdminSystemId);

                if (existingUser != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Super admin user already exists in SuperAdmin database: {superAdminEmail}");
                    return;
                }

                // Calculate age from birthdate (default to 30 years old)
                var birthdate = new DateTime(DateTime.Now.Year - 30, 1, 1);
                var age = (byte)(DateTime.Today.Year - birthdate.Year);
                if (birthdate.Date > DateTime.Today.AddYears(-age)) age--;

                // Hash password using BCrypt
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(superAdminPassword);

                // Create super admin user
                var superAdminUser = new UserEntity
                {
                    SystemId = superAdminSystemId,
                    FirstName = "Super",
                    MidName = null,
                    LastName = "Admin",
                    Suffix = null,
                    Birthdate = birthdate,
                    Age = age,
                    Gender = "Male",
                    ContactNum = "09123456789",
                    UserRole = "SuperAdmin",
                    Email = superAdminEmail,
                    Password = hashedPassword,
                    DateHired = DateTime.Now,
                    Status = "active"
                };

                superAdminContext.Users.Add(superAdminUser);
                await superAdminContext.SaveChangesAsync();
                
                System.Diagnostics.Debug.WriteLine($"Super admin user created successfully in SuperAdmin database: {superAdminEmail}");
                System.Diagnostics.Debug.WriteLine("NOTE: For production, ensure SuperAdmin is also seeded in cloud database!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: Failed to seed Super Admin user to SuperAdmin database: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new Exception($"Failed to seed Super Admin user to SuperAdmin database: {ex.Message}", ex);
            }
        }

        // Seeds an admin user account
        public async Task SeedAdminUserAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== SEEDING ADMIN USER ===");

                const string adminEmail = "admin@brightenroll.com";
                const string adminPassword = "Admin123456";
                const string adminSystemId = "ADMIN001";

                // Check if admin user already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == adminEmail || u.SystemId == adminSystemId);

                if (existingUser != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Admin user already exists with email: {adminEmail}");
                    return;
                }

                // Calculate age from birthdate (default to 30 years old)
                var birthdate = new DateTime(DateTime.Now.Year - 30, 1, 1);
                var age = (byte)(DateTime.Today.Year - birthdate.Year);
                if (birthdate.Date > DateTime.Today.AddYears(-age)) age--;

                // Hash password using BCrypt
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminPassword);

                // Create admin user
                var adminUser = new UserEntity
                {
                    SystemId = adminSystemId,
                    FirstName = "System",
                    MidName = null,
                    LastName = "Administrator",
                    Suffix = null,
                    Birthdate = birthdate,
                    Age = age,
                    Gender = "Male",
                    ContactNum = "09123456788",
                    UserRole = "Admin",
                    Email = adminEmail,
                    Password = hashedPassword,
                    DateHired = DateTime.Now,
                    Status = "active"
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"=== ADMIN USER SEEDED SUCCESSFULLY ===");
                System.Diagnostics.Debug.WriteLine($"Email: {adminEmail}");
                System.Diagnostics.Debug.WriteLine($"System ID: {adminSystemId}");
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
            {
                System.Diagnostics.Debug.WriteLine("WARNING: tbl_Users table does not exist. Skipping admin user seeding.");
                // Don't throw - allow seeding to continue
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WARNING: Error seeding admin user: {ex.Message}. This is non-critical.");
                // Don't throw - allow seeding to continue
            }
        }
    }
}
