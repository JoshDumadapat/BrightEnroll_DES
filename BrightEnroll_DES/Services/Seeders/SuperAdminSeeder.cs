using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Data.Models.SuperAdmin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace BrightEnroll_DES.Services.Seeders;

public class SuperAdminSeeder
{
    private readonly SuperAdminDbContext _context;
    private readonly ILogger<SuperAdminSeeder>? _logger;

    public SuperAdminSeeder(SuperAdminDbContext context, ILogger<SuperAdminSeeder>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    /// <summary>
    /// Seeds all SuperAdmin data: Super Admin user, customers, sales, BIR compliance, support tickets, and subscriptions
    /// </summary>
    public async Task SeedAllAsync()
    {
        try
        {
            _logger?.LogInformation("=== STARTING SUPERADMIN SEEDING ===");

            // 0. Seed subscription plans first (needed for subscriptions)
            await SeedSubscriptionPlansAsync();

            // 1. Seed Super Admin user
            await SeedSuperAdminUserAsync();

            // 2. Seed 80 customers
            await SeedCustomersAsync(80);

            // 3. Seed sales (invoices) based on customer purchases
            await SeedSalesAsync();

            // 4. Seed 30 BIR compliance records
            await SeedBIRComplianceAsync(30);

            // 5. Seed 50 support tickets
            await SeedSupportTicketsAsync(50);

            // 6. Seed subscriptions with at least 30 expiring in next 30 days
            await SeedSubscriptionsAsync();

            _logger?.LogInformation("=== SUPERADMIN SEEDING COMPLETED SUCCESSFULLY ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding SuperAdmin data: {Message}", ex.Message);
            throw new Exception($"Failed to seed SuperAdmin data: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Seeds subscription plans (Basic, Standard, Premium, Enterprise)
    /// </summary>
    public async Task SeedSubscriptionPlansAsync()
    {
        try
        {
            _logger?.LogInformation("Seeding subscription plans...");

            var existingPlans = await _context.SubscriptionPlans.CountAsync();
            if (existingPlans > 0)
            {
                _logger?.LogInformation($"Subscription plans already seeded ({existingPlans} plans exist). Skipping.");
                return;
            }

            var plans = new List<SubscriptionPlan>
            {
                new SubscriptionPlan
                {
                    PlanCode = "basic",
                    PlanName = "Basic Plan",
                    Description = "Includes Core and Enrollment modules",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new SubscriptionPlan
                {
                    PlanCode = "standard",
                    PlanName = "Standard Plan",
                    Description = "Includes Core, Enrollment, and Finance modules",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new SubscriptionPlan
                {
                    PlanCode = "premium",
                    PlanName = "Premium Plan",
                    Description = "Includes all modules: Core, Enrollment, Finance, and HR & Payroll",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new SubscriptionPlan
                {
                    PlanCode = "enterprise",
                    PlanName = "Enterprise Plan",
                    Description = "Full access to all modules with priority support and custom features",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                }
            };

            _context.SubscriptionPlans.AddRange(plans);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            _logger?.LogInformation($"=== SUBSCRIPTION PLANS SEEDED: {plans.Count} plans created ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding subscription plans: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Seeds Super Admin user
    /// </summary>
    public async Task SeedSuperAdminUserAsync()
    {
        try
        {
            _logger?.LogInformation("Seeding Super Admin user...");

            const string superAdminEmail = "superadmin@brightenroll.com";
            const string superAdminPassword = "SuperAdmin123456";
            const string superAdminSystemId = "SA001";

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == superAdminEmail || u.SystemId == superAdminSystemId);

            if (existingUser != null)
            {
                _logger?.LogInformation($"Super Admin user already exists: {superAdminEmail}");
                return;
            }

            var birthdate = new DateTime(DateTime.Now.Year - 30, 1, 1);
            var age = (byte)(DateTime.Today.Year - birthdate.Year);
            if (birthdate.Date > DateTime.Today.AddYears(-age)) age--;

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(superAdminPassword);

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

            _context.Users.Add(superAdminUser);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            _logger?.LogInformation($"Super Admin user created successfully: {superAdminEmail}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding Super Admin user: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Seeds customers
    /// </summary>
    public async Task SeedCustomersAsync(int count = 80)
    {
        try
        {
            _logger?.LogInformation($"Seeding {count} customers...");

            var existingCount = await _context.Customers.CountAsync();

            if (existingCount >= count)
            {
                _logger?.LogInformation($"Customers already seeded ({existingCount} exist). Skipping.");
                return;
            }

            var customersToCreate = count - existingCount;
            var random = new Random();
            var schoolTypes = new[] { "Elementary", "High School", "College", "University", "Vocational", "Technical" };
            var statuses = new[] { "Active", "Active", "Active", "Suspended", "Inactive" }; // Mostly Active
            var subscriptionPlans = new[] { "Basic", "Standard", "Premium", "Enterprise" };
            var positions = new[] { "Principal", "Administrator", "Registrar", "IT Manager", "School Director" };
            var firstNames = new[] { "Maria", "Juan", "Jose", "Ana", "Carlos", "Rosa", "Pedro", "Carmen", "Miguel", "Elena" };
            var lastNames = new[] { "Dela Cruz", "Garcia", "Reyes", "Ramos", "Mendoza", "Santos", "Cruz", "Torres", "Fernandez", "Gonzalez" };
            var provinces = new[] { "Metro Manila", "Laguna", "Cavite", "Batangas", "Rizal", "Bulacan", "Pampanga", "Quezon" };
            var cities = new[] { "Manila", "Makati", "Quezon City", "Pasig", "Taguig", "Caloocan", "Las Pinas", "Paranaque" };

            var customers = new List<Customer>();

            for (int i = 0; i < customersToCreate; i++)
            {
                var customerCode = $"CUST{existingCount + i + 1:D4}";
                var schoolName = $"{GetRandomSchoolName(random)} School";
                var schoolType = schoolTypes[random.Next(schoolTypes.Length)];
                var province = provinces[random.Next(provinces.Length)];
                var city = cities[random.Next(cities.Length)];
                var address = $"{random.Next(1, 999)} {GetRandomStreetName(random)} St., {city}, {province}";
                var contactPerson = $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}";
                var contactPosition = positions[random.Next(positions.Length)];
                var contactEmail = $"contact.{schoolName.ToLower().Replace(" ", "")}@school.edu.ph";
                var contactPhone = $"09{random.Next(100000000, 999999999)}";
                var subscriptionPlan = subscriptionPlans[random.Next(subscriptionPlans.Length)];
                var monthlyFee = subscriptionPlan switch
                {
                    "Basic" => 5000m,
                    "Standard" => 10000m,
                    "Premium" => 20000m,
                    "Enterprise" => 35000m,
                    _ => 10000m
                };
                var status = statuses[random.Next(statuses.Length)];
                var contractStartDate = DateTime.Today.AddDays(-random.Next(30, 730)); // Last 2 years
                var contractDurationMonths = random.Next(6, 36); // 6 to 36 months
                var contractEndDate = contractStartDate.AddMonths(contractDurationMonths);
                var studentCount = random.Next(50, 2000);

                // BIR Compliance fields
                var birTin = $"{random.Next(100, 999)}-{random.Next(100, 999)}-{random.Next(100, 999)}-{random.Next(100, 999)}";
                var birBusinessName = schoolName;
                var birAddress = address;
                var birRegistrationType = random.Next(2) == 0 ? "VAT" : "Non-VAT";
                var isVatRegistered = birRegistrationType == "VAT";
                var vatRate = isVatRegistered ? 12.00m : (decimal?)null;

                var customer = new Customer
                {
                    CustomerCode = customerCode,
                    SchoolName = schoolName,
                    SchoolType = schoolType,
                    Address = address,
                    ContactPerson = contactPerson,
                    ContactPosition = contactPosition,
                    ContactEmail = contactEmail,
                    ContactPhone = contactPhone,
                    SubscriptionPlan = subscriptionPlan,
                    MonthlyFee = monthlyFee,
                    ContractStartDate = contractStartDate,
                    ContractEndDate = contractEndDate,
                    ContractDurationMonths = contractDurationMonths,
                    StudentCount = studentCount,
                    Status = status,
                    DateRegistered = contractStartDate,
                    BirTin = birTin,
                    BirBusinessName = birBusinessName,
                    BirAddress = birAddress,
                    BirRegistrationType = birRegistrationType,
                    IsVatRegistered = isVatRegistered,
                    VatRate = vatRate
                };

                customers.Add(customer);
            }

            _context.Customers.AddRange(customers);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            _logger?.LogInformation($"=== CUSTOMERS SEEDED: {customersToCreate} customers created ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding customers: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Seeds sales (invoices) based on customer purchases
    /// </summary>
    public async Task SeedSalesAsync()
    {
        try
        {
            _logger?.LogInformation("Seeding sales (invoices) based on customer purchases...");

            var customers = await _context.Customers.Where(c => c.Status == "Active").ToListAsync();
            if (!customers.Any())
            {
                _logger?.LogWarning("No active customers found. Skipping sales seeding.");
                return;
            }

            var existingInvoices = await _context.CustomerInvoices.CountAsync();
            if (existingInvoices > 0)
            {
                _logger?.LogInformation($"Sales already seeded ({existingInvoices} invoices exist). Skipping.");
                return;
            }

            var random = new Random();
            var invoices = new List<CustomerInvoice>();

            foreach (var customer in customers)
            {
                // Each customer gets 1-5 invoices
                var invoiceCount = random.Next(1, 6);

                for (int i = 0; i < invoiceCount; i++)
                {
                    var invoiceDate = DateTime.Today.AddDays(-random.Next(0, 365)); // Last year
                    var dueDate = invoiceDate.AddDays(30); // 30 days payment terms
                    var billingPeriodStart = invoiceDate.AddMonths(-1);
                    var billingPeriodEnd = invoiceDate;

                    var subtotal = customer.MonthlyFee;
                    var vatAmount = customer.IsVatRegistered ? subtotal * (customer.VatRate ?? 0.12m) / 100 : 0;
                    var totalAmount = subtotal + vatAmount;

                    // Some invoices are paid, some are pending
                    var isPaid = random.Next(3) == 0; // 33% chance of being paid
                    var amountPaid = isPaid ? totalAmount : (random.Next(2) == 0 ? 0 : totalAmount * (decimal)random.NextDouble() * 0.5m);
                    var balance = totalAmount - amountPaid;
                    var status = isPaid ? "Paid" : (balance > 0 && dueDate < DateTime.Today ? "Overdue" : "Pending");

                    var invoiceNumber = $"INV-{customer.CustomerCode}-{invoiceDate:yyyyMM}-{i + 1:D3}";

                    var invoice = new CustomerInvoice
                    {
                        CustomerId = customer.CustomerId,
                        InvoiceNumber = invoiceNumber,
                        InvoiceDate = invoiceDate,
                        DueDate = dueDate,
                        BillingPeriodStart = billingPeriodStart,
                        BillingPeriodEnd = billingPeriodEnd,
                        Subtotal = subtotal,
                        VatAmount = vatAmount,
                        TotalAmount = totalAmount,
                        AmountPaid = amountPaid,
                        Balance = balance,
                        Status = status,
                        PaymentTerms = "Net 30",
                        CreatedAt = invoiceDate
                    };

                    invoices.Add(invoice);
                }
            }

            _context.CustomerInvoices.AddRange(invoices);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            _logger?.LogInformation($"=== SALES SEEDED: {invoices.Count} invoices created ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding sales: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Seeds BIR compliance records
    /// </summary>
    public async Task SeedBIRComplianceAsync(int count = 30)
    {
        try
        {
            _logger?.LogInformation($"Seeding {count} BIR compliance records...");

            var existingCount = await _context.SuperAdminBIRFilings.CountAsync();

            if (existingCount >= count)
            {
                _logger?.LogInformation($"BIR compliance already seeded ({existingCount} records exist). Skipping.");
                return;
            }

            var filingsToCreate = count - existingCount;
            var random = new Random();
            var filingTypes = new[] { "Quarterly VAT", "Monthly VAT", "Annual ITR", "Withholding Tax", "Percentage Tax" };
            var statuses = new[] { "Pending", "Filed", "Filed", "Overdue", "Late Filed" }; // Mostly filed

            var filings = new List<SuperAdminBIRFiling>();
            var currentYear = DateTime.Now.Year;
            var currentMonth = DateTime.Now.Month;
            var currentQuarter = (currentMonth - 1) / 3 + 1;

            for (int i = 0; i < filingsToCreate; i++)
            {
                var filingType = filingTypes[random.Next(filingTypes.Length)];
                string period;
                DateTime filingDate;
                DateTime dueDate;

                if (filingType.Contains("Annual"))
                {
                    var year = currentYear - random.Next(0, 3);
                    period = year.ToString();
                    filingDate = new DateTime(year, 4, 15); // April 15 deadline
                    dueDate = filingDate;
                }
                else if (filingType.Contains("Quarterly"))
                {
                    var quarter = random.Next(1, 5);
                    var year = currentYear - (quarter > currentQuarter ? 1 : 0);
                    period = $"Q{quarter} {year}";
                    filingDate = new DateTime(year, quarter * 3, 25); // 25th of last month of quarter
                    dueDate = filingDate;
                }
                else // Monthly
                {
                    var month = random.Next(1, 13);
                    var year = currentYear - (month > currentMonth ? 1 : 0);
                    period = $"{new DateTime(year, month, 1):MMM yyyy}";
                    filingDate = new DateTime(year, month, 20); // 20th of month
                    dueDate = filingDate;
                }

                var status = statuses[random.Next(statuses.Length)];
                var amount = random.Next(50000, 500000);
                var referenceNumber = status == "Filed" ? $"BIR-{random.Next(100000, 999999)}" : null;

                var filing = new SuperAdminBIRFiling
                {
                    FilingType = filingType,
                    Period = period,
                    FilingDate = filingDate,
                    DueDate = dueDate,
                    Status = status,
                    Amount = amount,
                    ReferenceNumber = referenceNumber,
                    Notes = status == "Overdue" ? "Requires immediate attention" : null,
                    CreatedAt = filingDate
                };

                filings.Add(filing);
            }

            _context.SuperAdminBIRFilings.AddRange(filings);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            _logger?.LogInformation($"=== BIR COMPLIANCE SEEDED: {filingsToCreate} records created ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding BIR compliance: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Seeds support tickets
    /// </summary>
    public async Task SeedSupportTicketsAsync(int count = 50)
    {
        try
        {
            _logger?.LogInformation($"Seeding {count} support tickets...");

            var existingCount = await _context.SupportTickets.CountAsync();

            if (existingCount >= count)
            {
                _logger?.LogInformation($"Support tickets already seeded ({existingCount} records exist). Skipping.");
                return;
            }

            var ticketsToCreate = count - existingCount;
            var customers = await _context.Customers.ToListAsync();
            if (!customers.Any())
            {
                _logger?.LogWarning("No customers found. Skipping support ticket seeding.");
                return;
            }

            var random = new Random();
            var priorities = new[] { "Low", "Medium", "Medium", "High", "Critical" };
            var statuses = new[] { "Open", "Open", "In Progress", "Resolved", "Closed" };
            var categories = new[] { "Technical Issue", "Billing", "Feature Request", "Bug Report", "Account Issue", "General Inquiry" };
            var subjects = new[]
            {
                "Unable to access dashboard",
                "Payment processing error",
                "Request for new feature",
                "System performance issue",
                "Account login problem",
                "Invoice discrepancy",
                "Subscription renewal question",
                "Data export issue",
                "Report generation error",
                "User permission problem"
            };

            var tickets = new List<SupportTicket>();

            for (int i = 0; i < ticketsToCreate; i++)
            {
                var customer = customers[random.Next(customers.Count)];
                var ticketNumber = $"TKT-{DateTime.Now:yyyyMM}-{existingCount + i + 1:D4}";
                var subject = subjects[random.Next(subjects.Length)];
                var priority = priorities[random.Next(priorities.Length)];
                var status = statuses[random.Next(statuses.Length)];
                var category = categories[random.Next(categories.Length)];
                var createdAt = DateTime.Now.AddDays(-random.Next(0, 180)); // Last 6 months
                var resolvedAt = status == "Resolved" || status == "Closed" 
                    ? createdAt.AddDays(random.Next(1, 30)) 
                    : (DateTime?)null;

                var descriptions = new[]
                {
                    $"Customer {customer.SchoolName} reported: {subject}. Please investigate and resolve.",
                    $"Issue reported by {customer.ContactPerson} from {customer.SchoolName}. Details: {subject}",
                    $"{subject} - Reported by customer {customer.CustomerCode}",
                    $"Support request: {subject}. Customer: {customer.SchoolName}"
                };

                var ticket = new SupportTicket
                {
                    TicketNumber = ticketNumber,
                    CustomerId = customer.CustomerId,
                    Subject = subject,
                    Description = descriptions[random.Next(descriptions.Length)],
                    Priority = priority,
                    Status = status,
                    Category = category,
                    CreatedAt = createdAt,
                    ResolvedAt = resolvedAt,
                    UpdatedAt = resolvedAt ?? createdAt
                };

                tickets.Add(ticket);
            }

            _context.SupportTickets.AddRange(tickets);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            _logger?.LogInformation($"=== SUPPORT TICKETS SEEDED: {ticketsToCreate} tickets created ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding support tickets: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Seeds subscriptions with at least 30 expiring in the next 30 days
    /// </summary>
    public async Task SeedSubscriptionsAsync()
    {
        try
        {
            _logger?.LogInformation("Seeding subscriptions...");

            var customers = await _context.Customers.Where(c => c.Status == "Active").ToListAsync();
            if (!customers.Any())
            {
                _logger?.LogWarning("No active customers found. Skipping subscription seeding.");
                return;
            }

            var existingSubscriptions = await _context.CustomerSubscriptions.CountAsync();
            if (existingSubscriptions > 0)
            {
                _logger?.LogInformation($"Subscriptions already seeded ({existingSubscriptions} subscriptions exist). Skipping.");
                return;
            }

            var plans = await _context.SubscriptionPlans.Where(p => p.IsActive).ToListAsync();
            var random = new Random();
            var subscriptions = new List<CustomerSubscription>();

            // Ensure at least 30 subscriptions expire in the next 30 days
            var expiringCount = 0;
            var targetExpiringCount = 30;

            foreach (var customer in customers)
            {
                var startDate = customer.ContractStartDate ?? DateTime.Today.AddDays(-random.Next(30, 365));
                DateTime? endDate;
                string status;

                // First 30 customers get subscriptions expiring soon
                if (expiringCount < targetExpiringCount)
                {
                    // Expire in 1-30 days
                    endDate = DateTime.Today.AddDays(random.Next(1, 31));
                    status = "Active";
                    expiringCount++;
                }
                else
                {
                    // Other subscriptions can expire later or be active without expiration
                    if (random.Next(3) == 0) // 33% chance of no expiration
                    {
                        endDate = null;
                        status = "Active";
                    }
                    else
                    {
                        endDate = DateTime.Today.AddDays(random.Next(31, 730)); // 31 days to 2 years
                        status = endDate < DateTime.Today ? "Expired" : "Active";
                    }
                }

                // Map customer's subscription plan to plan ID
                int? planId = null;
                if (!string.IsNullOrEmpty(customer.SubscriptionPlan) && plans.Any())
                {
                    var planCode = customer.SubscriptionPlan.ToLower();
                    var matchingPlan = plans.FirstOrDefault(p => p.PlanCode.ToLower() == planCode);
                    planId = matchingPlan?.PlanId;
                }

                // If no match found, randomly assign a plan
                if (!planId.HasValue && plans.Any())
                {
                    planId = plans[random.Next(plans.Count)].PlanId;
                }

                var monthlyFee = customer.MonthlyFee;
                var autoRenewal = random.Next(2) == 0; // 50% chance

                var subscription = new CustomerSubscription
                {
                    CustomerId = customer.CustomerId,
                    PlanId = planId,
                    SubscriptionType = planId.HasValue ? "predefined" : "custom",
                    Status = status,
                    StartDate = startDate,
                    EndDate = endDate,
                    MonthlyFee = monthlyFee,
                    AutoRenewal = autoRenewal,
                    CreatedAt = startDate
                };

                subscriptions.Add(subscription);
            }

            _context.CustomerSubscriptions.AddRange(subscriptions);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            _logger?.LogInformation($"=== SUBSCRIPTIONS SEEDED: {subscriptions.Count} subscriptions created ({expiringCount} expiring in next 30 days) ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error seeding subscriptions: {Message}", ex.Message);
            throw;
        }
    }

    // Helper methods
    private string GetRandomSchoolName(Random random)
    {
        var prefixes = new[] { "Saint", "Holy", "Divine", "Sacred", "Blessed", "Immaculate", "Our Lady of", "Saint Mary", "Saint Joseph", "Saint John" };
        var names = new[] { "Mary", "Joseph", "John", "Peter", "Paul", "Francis", "Theresa", "Catherine", "Michael", "Gabriel", "Hope", "Faith", "Grace", "Mercy", "Peace" };
        var suffixes = new[] { "Academy", "College", "Institute", "School", "Learning Center", "Educational Center" };

        if (random.Next(2) == 0)
        {
            return $"{prefixes[random.Next(prefixes.Length)]} {names[random.Next(names.Length)]} {suffixes[random.Next(suffixes.Length)]}";
        }
        else
        {
            return $"{names[random.Next(names.Length)]} {suffixes[random.Next(suffixes.Length)]}";
        }
    }

    private string GetRandomStreetName(Random random)
    {
        var streets = new[] { "Rizal", "Bonifacio", "Aguinaldo", "Mabini", "Luna", "Del Pilar", "Burgos", "Gomez", "Zamora", "Jacinto", "Recto", "Ayala", "Ortigas", "EDSA", "Commonwealth" };
        return streets[random.Next(streets.Length)];
    }
}
