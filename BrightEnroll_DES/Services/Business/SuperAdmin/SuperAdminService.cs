using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using BrightEnroll_DES.Services.Database.Definitions;

namespace BrightEnroll_DES.Services.Business.SuperAdmin;

public class SuperAdminService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SuperAdminService>? _logger;

    public SuperAdminService(AppDbContext context, ILogger<SuperAdminService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
        // Note: We don't check _context.Database here because it might not be initialized yet
        // during service injection. We'll check it lazily in each method instead.
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
            // Check if table exists by trying to query it
            if (!await _context.Database.CanConnectAsync())
            {
                _logger?.LogWarning("Database connection not available.");
                return new List<Customer>();
            }

            // Try to query without Include first to check if table exists
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

            // If table exists, try loading with navigation properties, but fallback to without Include if it fails
            try
            {
                return await _context.Customers
                    .Include(c => c.CreatedByUser)
                    .OrderByDescending(c => c.DateRegistered)
                    .ToListAsync();
            }
            catch (InvalidOperationException)
            {
                // Fallback: load without Include if navigation properties cause issues
                _logger?.LogWarning("Loading customers without navigation properties due to InvalidOperationException.");
                return await _context.Customers
                    .OrderByDescending(c => c.DateRegistered)
                    .ToListAsync();
            }
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
            .Include(c => c.CreatedByUser)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer, int? createdBy = null)
    {
        try
        {
            // Ensure the table exists before trying to save
            await EnsureCustomersTableExistsAsync();

            if (string.IsNullOrWhiteSpace(customer.CustomerCode))
            {
                customer.CustomerCode = await GenerateCustomerCodeAsync();
            }

            customer.CreatedBy = createdBy;
            customer.DateRegistered = DateTime.Now;

            _logger?.LogInformation("Creating customer: {SchoolName}, Code: {CustomerCode}", customer.SchoolName, customer.CustomerCode);

            _context.Customers.Add(customer);
            var result = await _context.SaveChangesAsync();

            _logger?.LogInformation("Customer created successfully: {CustomerCode} - {SchoolName}, Rows affected: {Result}", 
                customer.CustomerCode, customer.SchoolName, result);

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

    public async Task<Customer> UpdateCustomerAsync(Customer customer)
    {
        customer.UpdatedAt = DateTime.Now;
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();

        _logger?.LogInformation("Customer updated: {CustomerCode}", customer.CustomerCode);
        return customer;
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

            _context.Customers.Remove(customer);
            var result = await _context.SaveChangesAsync();

            _logger?.LogInformation("Customer deleted: {CustomerId}, Rows affected: {Result}", customerId, result);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting customer {CustomerId}: {Message}", customerId, ex.Message);
            throw;
        }
    }

    public async Task<bool> DeleteSalesLeadAsync(int leadId)
    {
        try
        {
            var lead = await _context.SalesLeads.FindAsync(leadId);
            if (lead == null)
            {
                _logger?.LogWarning("Sales lead not found for deletion: {LeadId}", leadId);
                return false;
            }

            _context.SalesLeads.Remove(lead);
            var result = await _context.SaveChangesAsync();

            _logger?.LogInformation("Sales lead deleted: {LeadId}, Rows affected: {Result}", leadId, result);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting sales lead {LeadId}: {Message}", leadId, ex.Message);
            throw;
        }
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
            // Check if database is available
            if (!await _context.Database.CanConnectAsync())
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
                monthlyRevenue = await _context.Customers
                    .Where(c => c.Status == "Active")
                    .SumAsync(c => c.MonthlyFee);
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

    #endregion

    #region Sales Lead Operations

    public async Task<List<SalesLead>> GetAllSalesLeadsAsync()
    {
        if (_context == null || _context.Database == null)
        {
            return new List<SalesLead>();
        }

        try
        {
            if (!await _context.Database.CanConnectAsync())
            {
                _logger?.LogWarning("Database connection not available.");
                return new List<SalesLead>();
            }

            // Check if table exists first
            try
            {
                var count = await _context.SalesLeads.CountAsync();
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208 || sqlEx.Message.Contains("Invalid object name"))
            {
                _logger?.LogWarning("Table tbl_SalesLeads does not exist yet.");
                return new List<SalesLead>();
            }
            catch (InvalidOperationException)
            {
                _logger?.LogWarning("Invalid operation checking sales leads table.");
                return new List<SalesLead>();
            }

            // Try with Include, fallback to without if it fails
            try
            {
                return await _context.SalesLeads
                    .Include(l => l.AssignedToUser)
                    .OrderByDescending(l => l.CreatedAt)
                    .ToListAsync();
            }
            catch (InvalidOperationException)
            {
                _logger?.LogWarning("Loading sales leads without navigation properties due to InvalidOperationException.");
                return await _context.SalesLeads
                    .OrderByDescending(l => l.CreatedAt)
                    .ToListAsync();
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 208 || ex.Message.Contains("Invalid object name"))
        {
            _logger?.LogWarning(ex, "Table does not exist yet: tbl_SalesLeads");
            return new List<SalesLead>();
        }
        catch (InvalidOperationException ex)
        {
            _logger?.LogWarning(ex, "Invalid operation loading sales leads. Returning empty list.");
            return new List<SalesLead>();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error loading sales leads. Tables may not exist yet.");
            return new List<SalesLead>();
        }
    }

    public async Task<SalesLead?> GetSalesLeadByIdAsync(int leadId)
    {
        return await _context.SalesLeads
            .Include(l => l.AssignedToUser)
            .FirstOrDefaultAsync(l => l.LeadId == leadId);
    }

    public async Task<List<SalesLead>> GetSalesLeadsByStageAsync(string stage)
    {
        return await _context.SalesLeads
            .Include(l => l.AssignedToUser)
            .Where(l => l.Stage == stage)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task<SalesLead> CreateSalesLeadAsync(SalesLead lead)
    {
        try
        {
            // Ensure the table exists before trying to save
            await EnsureSalesLeadsTableExistsAsync();

            if (string.IsNullOrWhiteSpace(lead.LeadCode))
            {
                lead.LeadCode = await GenerateLeadCodeAsync();
            }

            lead.CreatedAt = DateTime.Now;

            _logger?.LogInformation("Creating sales lead: {SchoolName}, Code: {LeadCode}", lead.SchoolName, lead.LeadCode);

            _context.SalesLeads.Add(lead);
            var result = await _context.SaveChangesAsync();

            _logger?.LogInformation("Sales lead created successfully: {LeadCode} - {SchoolName}, Rows affected: {Result}", 
                lead.LeadCode, lead.SchoolName, result);

            return lead;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            _logger?.LogError(dbEx, "Database error creating sales lead: {Message}", dbEx.Message);
            if (dbEx.InnerException != null)
            {
                _logger?.LogError("Inner exception: {Message}", dbEx.InnerException.Message);
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating sales lead: {Message}", ex.Message);
            throw;
        }
    }

    private async Task EnsureSalesLeadsTableExistsAsync()
    {
        try
        {
            // Check if table exists by trying to query it
            var checkTableQuery = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tbl_SalesLeads'";
            
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = checkTableQuery;
            var result = await command.ExecuteScalarAsync();
            var tableExists = Convert.ToInt32(result) > 0;
            
            if (!tableExists)
            {
                _logger?.LogInformation("tbl_SalesLeads table does not exist. Creating it now...");
                
                // Get table definition
                var tableDef = TableDefinitions.GetSalesLeadsTableDefinition();
                
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
                
                _logger?.LogInformation("tbl_SalesLeads table created successfully");
            }
            
            await connection.CloseAsync();
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 2714 || sqlEx.Message.Contains("already exists") || sqlEx.Message.Contains("duplicate"))
        {
            // Table already exists (race condition), that's fine
            _logger?.LogInformation("tbl_SalesLeads table already exists");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error ensuring SalesLeads table exists: {Message}", ex.Message);
            // Don't throw - let the actual save operation handle the error
        }
    }

    public async Task<SalesLead> UpdateSalesLeadAsync(SalesLead lead)
    {
        lead.UpdatedAt = DateTime.Now;
        _context.SalesLeads.Update(lead);
        await _context.SaveChangesAsync();

        _logger?.LogInformation("Sales lead updated: {LeadCode}", lead.LeadCode);
        return lead;
    }

    public async Task<SalesLead> ConvertLeadToCustomerAsync(int leadId, Customer customer, int? createdBy = null)
    {
        var lead = await GetSalesLeadByIdAsync(leadId);
        if (lead == null)
        {
            throw new Exception("Sales lead not found");
        }

        // Create customer
        customer.CreatedBy = createdBy;
        customer = await CreateCustomerAsync(customer, createdBy);

        // Update lead
        lead.Stage = "Converted";
        lead.ConversionDate = DateTime.Now;
        lead.ConvertedAmount = customer.MonthlyFee;
        lead.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        _logger?.LogInformation("Lead converted to customer: {LeadCode} -> {CustomerCode}", lead.LeadCode, customer.CustomerCode);
        return lead;
    }

    private async Task<string> GenerateLeadCodeAsync()
    {
        try
        {
            var lastLead = await _context.SalesLeads
                .OrderByDescending(l => l.LeadId)
                .FirstOrDefaultAsync();

            if (lastLead == null)
            {
                return "LEAD-001";
            }

            var lastNumber = int.Parse(lastLead.LeadCode.Split('-')[1]);
            return $"LEAD-{(lastNumber + 1):D3}";
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error generating lead code. Returning default code.");
            return "LEAD-001";
        }
    }

    #endregion

    #region Support Ticket Operations

    public async Task<List<SupportTicket>> GetAllSupportTicketsAsync()
    {
        try
        {
            return await _context.SupportTickets
                .Include(t => t.Customer)
                .Include(t => t.AssignedToUser)
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
            .Include(t => t.AssignedToUser)
            .Where(t => t.Status == "Open" || t.Status == "In Progress")
            .OrderByDescending(t => t.Priority == "Critical")
            .ThenByDescending(t => t.Priority == "High")
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<SupportTicket> CreateSupportTicketAsync(SupportTicket ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket.TicketNumber))
        {
            ticket.TicketNumber = await GenerateTicketNumberAsync();
        }

        ticket.CreatedAt = DateTime.Now;
        _context.SupportTickets.Add(ticket);
        await _context.SaveChangesAsync();

        _logger?.LogInformation("Support ticket created: {TicketNumber}", ticket.TicketNumber);
        return ticket;
    }

    public async Task<SupportTicket> UpdateSupportTicketAsync(SupportTicket ticket)
    {
        if (ticket.Status == "Resolved" || ticket.Status == "Closed")
        {
            ticket.ResolvedAt = DateTime.Now;
        }

        ticket.UpdatedAt = DateTime.Now;
        _context.SupportTickets.Update(ticket);
        await _context.SaveChangesAsync();

        _logger?.LogInformation("Support ticket updated: {TicketNumber}", ticket.TicketNumber);
        return ticket;
    }

    private async Task<string> GenerateTicketNumberAsync()
    {
        try
        {
            var year = DateTime.Now.Year;
            var lastTicket = await _context.SupportTickets
                .Where(t => t.TicketNumber.StartsWith($"TKT-{year}-"))
                .OrderByDescending(t => t.TicketId)
                .FirstOrDefaultAsync();

            if (lastTicket == null)
            {
                return $"TKT-{year}-001";
            }

            var parts = lastTicket.TicketNumber.Split('-');
            var lastNumber = int.Parse(parts[2]);
            return $"TKT-{year}-{(lastNumber + 1):D3}";
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error generating ticket number. Returning default code.");
            return $"TKT-{DateTime.Now.Year}-001";
        }
    }

    #endregion

    #region Contract Operations

    public async Task<List<Contract>> GetAllContractsAsync()
    {
        if (_context == null || _context.Database == null)
        {
            return new List<Contract>();
        }

        try
        {
            // Check if table exists first
            if (!await _context.Database.CanConnectAsync())
            {
                _logger?.LogWarning("Database connection not available.");
                return new List<Contract>();
            }

            // Ensure table exists
            await EnsureContractsTableExistsAsync();

            return await _context.Contracts
                .Include(c => c.Customer)
                .Include(c => c.CreatedByUser)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208 || sqlEx.Message.Contains("Invalid object name"))
        {
            _logger?.LogWarning(sqlEx, "Table tbl_Contracts does not exist. Attempting to create it.");
            try
            {
                await EnsureContractsTableExistsAsync();
                // Retry after creating table
                return await _context.Contracts
                    .Include(c => c.Customer)
                    .Include(c => c.CreatedByUser)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception retryEx)
            {
                _logger?.LogError(retryEx, "Error loading contracts after table creation.");
                return new List<Contract>();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error loading contracts. Tables may not exist yet.");
            return new List<Contract>();
        }
    }

    private async Task EnsureContractsTableExistsAsync()
    {
        if (_context == null || _context.Database == null)
        {
            _logger?.LogWarning("DbContext or Database is not available for table existence check.");
            return;
        }

        try
        {
            // Check if the database can connect
            if (!await _context.Database.CanConnectAsync())
            {
                _logger?.LogWarning("Database connection not available for table existence check.");
                return;
            }

            // Check if the table exists using INFORMATION_SCHEMA
            var tableName = "tbl_Contracts";
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '{tableName}'";
                var result = await command.ExecuteScalarAsync();
                if (result == null)
                {
                    _logger?.LogInformation($"Table '{tableName}' does not exist. Attempting to create it.");
                    var tableDefinition = TableDefinitions.GetContractsTableDefinition();
                    using (var createCommand = connection.CreateCommand())
                    {
                        createCommand.CommandText = tableDefinition.CreateTableScript;
                        await createCommand.ExecuteNonQueryAsync();
                        _logger?.LogInformation($"Table '{tableName}' created successfully.");

                        // Create indexes
                        foreach (var indexScript in tableDefinition.CreateIndexesScripts)
                        {
                            using (var indexCommand = connection.CreateCommand())
                            {
                                indexCommand.CommandText = indexScript;
                                await indexCommand.ExecuteNonQueryAsync();
                            }
                        }
                        _logger?.LogInformation($"Indexes for '{tableName}' created successfully.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error ensuring Contracts table exists.");
        }
    }

    public async Task<Contract?> GetContractByIdAsync(int contractId)
    {
        return await _context.Contracts
            .Include(c => c.Customer)
            .Include(c => c.CreatedByUser)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);
    }

    public async Task<List<Contract>> GetExpiringContractsAsync(int daysAhead = 30)
    {
        if (_context == null || _context.Database == null)
        {
            return new List<Contract>();
        }

        try
        {
            if (!await _context.Database.CanConnectAsync())
            {
                _logger?.LogWarning("Database connection not available.");
                return new List<Contract>();
            }

            // Check if table exists first
            try
            {
                var count = await _context.Contracts.CountAsync();
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208 || sqlEx.Message.Contains("Invalid object name"))
            {
                _logger?.LogWarning("Table tbl_Contracts does not exist yet.");
                return new List<Contract>();
            }
            catch (InvalidOperationException)
            {
                _logger?.LogWarning("Invalid operation checking contracts table.");
                return new List<Contract>();
            }

            var expiryDate = DateTime.Now.AddDays(daysAhead);
            
            // Try with Include, fallback to without if it fails
            try
            {
                return await _context.Contracts
                    .Include(c => c.Customer)
                    .Where(c => c.Status == "Active" && c.EndDate <= expiryDate && c.EndDate >= DateTime.Now)
                    .OrderBy(c => c.EndDate)
                    .ToListAsync();
            }
            catch (InvalidOperationException)
            {
                _logger?.LogWarning("Loading contracts without navigation properties due to InvalidOperationException.");
                return await _context.Contracts
                    .Where(c => c.Status == "Active" && c.EndDate <= expiryDate && c.EndDate >= DateTime.Now)
                    .OrderBy(c => c.EndDate)
                    .ToListAsync();
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 208 || ex.Message.Contains("Invalid object name"))
        {
            _logger?.LogWarning(ex, "Table does not exist yet: tbl_Contracts");
            return new List<Contract>();
        }
        catch (InvalidOperationException ex)
        {
            _logger?.LogWarning(ex, "Invalid operation loading contracts. Returning empty list.");
            return new List<Contract>();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error loading expiring contracts. Tables may not exist yet.");
            return new List<Contract>();
        }
    }

    public async Task<Contract> CreateContractAsync(Contract contract, int? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(contract.ContractNumber))
        {
            contract.ContractNumber = await GenerateContractNumberAsync();
        }

        contract.CreatedBy = createdBy;
        contract.CreatedAt = DateTime.Now;
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        _logger?.LogInformation("Contract created: {ContractNumber}", contract.ContractNumber);
        return contract;
    }

    public async Task<Contract> UpdateContractAsync(Contract contract)
    {
        contract.UpdatedAt = DateTime.Now;
        _context.Contracts.Update(contract);
        await _context.SaveChangesAsync();

        _logger?.LogInformation("Contract updated: {ContractNumber}", contract.ContractNumber);
        return contract;
    }

    private async Task<string> GenerateContractNumberAsync()
    {
        try
        {
            var year = DateTime.Now.Year;
            var lastContract = await _context.Contracts
                .Where(c => c.ContractNumber.StartsWith($"CNT-{year}-"))
                .OrderByDescending(c => c.ContractId)
                .FirstOrDefaultAsync();

            if (lastContract == null)
            {
                return $"CNT-{year}-001";
            }

            var parts = lastContract.ContractNumber.Split('-');
            var lastNumber = int.Parse(parts[2]);
            return $"CNT-{year}-{(lastNumber + 1):D3}";
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error generating contract number. Returning default code.");
            return $"CNT-{DateTime.Now.Year}-001";
        }
    }

    #endregion

    #region System Update Operations

    public async Task<List<SystemUpdate>> GetAllSystemUpdatesAsync()
    {
        try
        {
            return await _context.SystemUpdates
                .Include(u => u.CreatedByUser)
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
            .Include(u => u.CreatedByUser)
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
}

public class DashboardStats
{
    public int TotalCustomers { get; set; }
    public int ActiveSubscriptions { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int OpenTickets { get; set; }
}

