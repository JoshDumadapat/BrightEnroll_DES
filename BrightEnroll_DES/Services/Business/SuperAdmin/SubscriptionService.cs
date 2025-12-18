using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Data.Models.SuperAdmin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using BrightEnroll_DES.Services.Authentication;
using System.Net;
using System.Net.NetworkInformation;

namespace BrightEnroll_DES.Services.Business.SuperAdmin;

/// <summary>
/// Service for managing customer subscriptions and module entitlements
/// Replaces Notes-based module parsing with proper relational tables
/// </summary>
public interface ISubscriptionService
{
    Task<CustomerSubscription?> GetActiveSubscriptionAsync(int customerId);
    Task<CustomerSubscription> CreateSubscriptionAsync(int customerId, int? planId, List<string>? customModules, DateTime startDate, DateTime? endDate, decimal monthlyFee, bool autoRenewal, int? createdBy);
    Task<CustomerSubscription> UpdateSubscriptionAsync(int subscriptionId, int? planId, List<string>? customModules, string? status, DateTime? endDate, decimal? monthlyFee, bool? autoRenewal, int? updatedBy);
    Task<bool> RevokeModuleAsync(int subscriptionId, string modulePackageId, int? revokedBy);
    Task<bool> GrantModuleAsync(int subscriptionId, string modulePackageId, int? grantedBy);
    Task RefreshTenantModulesAsync(int? customerId = null);
    Task<List<string>> GetCustomerModulesAsync(int customerId);
}

public class SubscriptionService : ISubscriptionService
{
    private readonly SuperAdminDbContext _context;
    private readonly ILogger<SubscriptionService>? _logger;
    private readonly IServiceScopeFactory? _serviceScopeFactory;
    private readonly ICustomerModuleService? _customerModuleService;

    public SubscriptionService(
        SuperAdminDbContext context, 
        ILogger<SubscriptionService>? logger = null,
        IServiceScopeFactory? serviceScopeFactory = null,
        ICustomerModuleService? customerModuleService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _customerModuleService = customerModuleService;
    }

    /// <summary>
    /// Gets the local machine's IP address for audit logging
    /// </summary>
    private string GetLocalIpAddress()
    {
        try
        {
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
        return "127.0.0.1";
    }

    /// <summary>
    /// Get the active subscription for a customer
    /// </summary>
    public async Task<CustomerSubscription?> GetActiveSubscriptionAsync(int customerId)
    {
        return await _context.CustomerSubscriptions
            .Include(cs => cs.Plan)
                .ThenInclude(p => p!.PlanModules)
            .Include(cs => cs.CustomerSubscriptionModules)
            .AsNoTracking()
            .Where(cs => cs.CustomerId == customerId && cs.Status == "Active")
            .OrderByDescending(cs => cs.StartDate)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Create a new subscription for a customer
    /// Supports both predefined plans and custom module selections
    /// </summary>
    public async Task<CustomerSubscription> CreateSubscriptionAsync(
        int customerId, 
        int? planId, 
        List<string>? customModules, 
        DateTime startDate, 
        DateTime? endDate, 
        decimal monthlyFee, 
        bool autoRenewal, 
        int? createdBy)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Deactivate existing active subscriptions
            var existingSubscriptions = await _context.CustomerSubscriptions
                .Where(cs => cs.CustomerId == customerId && cs.Status == "Active")
                .ToListAsync();

            foreach (var existing in existingSubscriptions)
            {
                existing.Status = "Cancelled";
                existing.UpdatedAt = DateTime.Now;
                existing.UpdatedBy = createdBy;
            }

            // Create new subscription
            var subscription = new CustomerSubscription
            {
                CustomerId = customerId,
                PlanId = planId,
                SubscriptionType = planId.HasValue ? "predefined" : "custom",
                Status = "Active",
                StartDate = startDate,
                EndDate = endDate,
                MonthlyFee = monthlyFee,
                AutoRenewal = autoRenewal,
                CreatedAt = DateTime.Now,
                CreatedBy = createdBy
            };

            _context.CustomerSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            // If custom subscription, add modules
            if (planId == null && customModules != null && customModules.Any())
            {
                // Always include 'core' module
                if (!customModules.Contains("core", StringComparer.OrdinalIgnoreCase))
                {
                    customModules.Insert(0, "core");
                }

                foreach (var moduleId in customModules.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    _context.CustomerSubscriptionModules.Add(new CustomerSubscriptionModule
                    {
                        SubscriptionId = subscription.SubscriptionId,
                        ModulePackageId = moduleId.ToLower(),
                        GrantedDate = DateTime.Now,
                        GrantedBy = createdBy
                    });
                }

                await _context.SaveChangesAsync();
            }

            // Refresh tenant modules cache
            await RefreshTenantModulesAsync(customerId);
            
            // Clear in-memory module cache for this customer to ensure fresh data on next permission check
            _customerModuleService?.ClearCache(customerId);
            
            // Verify TenantModules were populated
            var tenantModulesCount = await _context.TenantModules
                .Where(tm => tm.CustomerId == customerId && tm.IsActive)
                .CountAsync();
            
            _logger?.LogInformation("TenantModules cache refreshed. Active modules for customer {CustomerId}: {Count}", 
                customerId, tenantModulesCount);

            await transaction.CommitAsync();

            _logger?.LogInformation("Created subscription {SubscriptionId} for customer {CustomerId}. Type: {Type}, PlanId: {PlanId}, Modules in cache: {ModuleCount}. In-memory cache cleared.",
                subscription.SubscriptionId, customerId, subscription.SubscriptionType, planId, tenantModulesCount);

            // Audit logging (non-blocking, background task)
            var subscriptionForAudit = subscription; // Capture for closure
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_serviceScopeFactory != null && subscriptionForAudit != null)
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var superAdminAuditLogService = scope.ServiceProvider.GetRequiredService<SuperAdminAuditLogService>();
                        var authService = scope.ServiceProvider.GetService<IAuthService>();
                        
                        Customer? customer = null;
                        if (customerId > 0)
                        {
                            customer = await _context.Customers
                                .AsNoTracking()
                                .FirstOrDefaultAsync(c => c.CustomerId == customerId);
                        }
                        
                        var currentUser = authService?.CurrentUser;
                        var userName = currentUser != null ? $"{currentUser.first_name} {currentUser.last_name}".Trim() : "System";
                        if (string.IsNullOrWhiteSpace(userName) && currentUser != null)
                            userName = currentUser.email ?? "System";
                        var userRole = currentUser?.user_role ?? "SuperAdmin";
                        var userId = currentUser?.user_ID;
                        var ipAddress = GetLocalIpAddress();
                        
                        var moduleList = planId.HasValue 
                            ? $"Predefined Plan (ID: {planId})"
                            : string.Join(", ", customModules ?? new List<string>());
                        
                        await superAdminAuditLogService.CreateTransactionLogAsync(
                            action: "Create Subscription",
                            module: "Subscription Management",
                            description: $"Created subscription for customer: {customer?.SchoolName} ({customer?.CustomerCode}). Type: {subscription.SubscriptionType}, Monthly Fee: {monthlyFee:C}, Modules: {moduleList}",
                            userName: userName,
                            userRole: userRole,
                            userId: userId,
                            entityType: "Subscription",
                            entityId: subscription.SubscriptionId.ToString(),
                            oldValues: null,
                            newValues: $"CustomerId: {customerId}, Type: {subscription.SubscriptionType}, PlanId: {planId}, MonthlyFee: {monthlyFee}, AutoRenewal: {autoRenewal}, Modules: {moduleList}",
                            ipAddress: ipAddress,
                            status: "Success",
                            severity: "High",
                            customerCode: customer?.CustomerCode,
                            customerName: customer?.SchoolName
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to create SuperAdmin audit log for subscription creation: {Message}", ex.Message);
                }
            });

            return subscription;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger?.LogError(ex, "Error creating subscription for customer {CustomerId}", customerId);
            throw;
        }
    }

    /// <summary>
    /// Update an existing subscription
    /// </summary>
    public async Task<CustomerSubscription> UpdateSubscriptionAsync(
        int subscriptionId, 
        int? planId, 
        List<string>? customModules, 
        string? status, 
        DateTime? endDate, 
        decimal? monthlyFee, 
        bool? autoRenewal, 
        int? updatedBy)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var subscription = await _context.CustomerSubscriptions
                .Include(cs => cs.CustomerSubscriptionModules)
                .FirstOrDefaultAsync(cs => cs.SubscriptionId == subscriptionId);

            if (subscription == null)
            {
                throw new ArgumentException($"Subscription {subscriptionId} not found", nameof(subscriptionId));
            }

            // Update subscription properties
            if (planId.HasValue)
            {
                subscription.PlanId = planId;
                subscription.SubscriptionType = "predefined";
                
                // Remove custom modules if switching to predefined plan
                var customModulesToRemove = subscription.CustomerSubscriptionModules.ToList();
                _context.CustomerSubscriptionModules.RemoveRange(customModulesToRemove);
            }
            else if (customModules != null)
            {
                subscription.PlanId = null;
                subscription.SubscriptionType = "custom";
                
                // Update custom modules
                var existingModules = subscription.CustomerSubscriptionModules
                    .Where(m => m.RevokedDate == null)
                    .Select(m => m.ModulePackageId.ToLower())
                    .ToList();

                var modulesToAdd = customModules
                    .Select(m => m.ToLower())
                    .Where(m => !existingModules.Contains(m))
                    .ToList();

                var modulesToRemove = existingModules
                    .Where(m => !customModules.Any(cm => cm.Equals(m, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                // Add new modules
                foreach (var moduleId in modulesToAdd)
                {
                    _context.CustomerSubscriptionModules.Add(new CustomerSubscriptionModule
                    {
                        SubscriptionId = subscriptionId,
                        ModulePackageId = moduleId,
                        GrantedDate = DateTime.Now,
                        GrantedBy = updatedBy
                    });
                }

                // Revoke removed modules (soft delete)
                foreach (var moduleId in modulesToRemove)
                {
                    var module = subscription.CustomerSubscriptionModules
                        .FirstOrDefault(m => m.ModulePackageId.Equals(moduleId, StringComparison.OrdinalIgnoreCase) && m.RevokedDate == null);
                    
                    if (module != null)
                    {
                        module.RevokedDate = DateTime.Now;
                        module.RevokedBy = updatedBy;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                subscription.Status = status;
            }

            if (endDate.HasValue)
            {
                subscription.EndDate = endDate;
            }

            if (monthlyFee.HasValue)
            {
                subscription.MonthlyFee = monthlyFee.Value;
            }

            if (autoRenewal.HasValue)
            {
                subscription.AutoRenewal = autoRenewal.Value;
            }

            subscription.UpdatedAt = DateTime.Now;
            subscription.UpdatedBy = updatedBy;

            await _context.SaveChangesAsync();

            // Refresh tenant modules cache
            await RefreshTenantModulesAsync(subscription.CustomerId);
            
            // Clear in-memory module cache for this customer to ensure fresh data on next permission check
            _customerModuleService?.ClearCache(subscription.CustomerId);

            await transaction.CommitAsync();

            _logger?.LogInformation("Updated subscription {SubscriptionId} for customer {CustomerId}. In-memory cache cleared.",
                subscriptionId, subscription.CustomerId);

            // Audit logging (non-blocking, background task)
            var subscriptionForAudit = subscription; // Capture for closure
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_serviceScopeFactory != null && subscriptionForAudit != null)
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var superAdminAuditLogService = scope.ServiceProvider.GetRequiredService<SuperAdminAuditLogService>();
                        var authService = scope.ServiceProvider.GetService<IAuthService>();
                        
                        Customer? customer = null;
                        if (subscriptionForAudit.CustomerId > 0)
                        {
                            customer = await _context.Customers
                                .AsNoTracking()
                                .FirstOrDefaultAsync(c => c.CustomerId == subscriptionForAudit.CustomerId);
                        }
                        
                        var currentUser = authService?.CurrentUser;
                        var userName = currentUser != null ? $"{currentUser.first_name} {currentUser.last_name}".Trim() : "System";
                        if (string.IsNullOrWhiteSpace(userName) && currentUser != null)
                            userName = currentUser.email ?? "System";
                        var userRole = currentUser?.user_role ?? "SuperAdmin";
                        var userId = currentUser?.user_ID;
                        var ipAddress = GetLocalIpAddress();
                        
                        var changes = new List<string>();
                        if (planId.HasValue) changes.Add($"PlanId: {planId}");
                        if (customModules != null) changes.Add($"Modules: {string.Join(", ", customModules)}");
                        if (!string.IsNullOrWhiteSpace(status)) changes.Add($"Status: {status}");
                        if (endDate.HasValue) changes.Add($"EndDate: {endDate.Value:yyyy-MM-dd}");
                        if (monthlyFee.HasValue) changes.Add($"MonthlyFee: {monthlyFee.Value:C}");
                        if (autoRenewal.HasValue) changes.Add($"AutoRenewal: {autoRenewal.Value}");
                        
                        await superAdminAuditLogService.CreateTransactionLogAsync(
                            action: "Update Subscription",
                            module: "Subscription Management",
                            description: $"Updated subscription {subscriptionId} for customer: {customer?.SchoolName} ({customer?.CustomerCode}). Changes: {string.Join(", ", changes)}",
                            userName: userName,
                            userRole: userRole,
                            userId: userId,
                            entityType: "Subscription",
                            entityId: subscriptionId.ToString(),
                            oldValues: $"Type: {subscription.SubscriptionType}, Status: {subscription.Status}, MonthlyFee: {subscription.MonthlyFee}",
                            newValues: string.Join(", ", changes),
                            ipAddress: ipAddress,
                            status: "Success",
                            severity: "High",
                            customerCode: customer?.CustomerCode,
                            customerName: customer?.SchoolName
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to create SuperAdmin audit log for subscription update: {Message}", ex.Message);
                }
            });

            return subscription;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger?.LogError(ex, "Error updating subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    /// <summary>
    /// Revoke a module from a custom subscription (soft delete)
    /// </summary>
    public async Task<bool> RevokeModuleAsync(int subscriptionId, string modulePackageId, int? revokedBy)
    {
        var module = await _context.CustomerSubscriptionModules
            .FirstOrDefaultAsync(m => 
                m.SubscriptionId == subscriptionId 
                && m.ModulePackageId.Equals(modulePackageId, StringComparison.OrdinalIgnoreCase)
                && m.RevokedDate == null);

        if (module == null)
        {
            return false;
        }

        // Don't allow revoking 'core' module
        if (module.ModulePackageId.Equals("core", StringComparison.OrdinalIgnoreCase))
        {
            _logger?.LogWarning("Attempted to revoke 'core' module from subscription {SubscriptionId}", subscriptionId);
            return false;
        }

        module.RevokedDate = DateTime.Now;
        module.RevokedBy = revokedBy;

        await _context.SaveChangesAsync();

        var subscription = await _context.CustomerSubscriptions
            .FirstOrDefaultAsync(cs => cs.SubscriptionId == subscriptionId);

        if (subscription != null)
        {
            await RefreshTenantModulesAsync(subscription.CustomerId);
            // Clear in-memory module cache for this customer to ensure fresh data on next permission check
            _customerModuleService?.ClearCache(subscription.CustomerId);
        }

        _logger?.LogInformation("Revoked module {ModuleId} from subscription {SubscriptionId}. In-memory cache cleared.", modulePackageId, subscriptionId);

        // Audit logging (non-blocking, background task)
        var subscriptionForAudit = subscription; // Capture for closure
        _ = Task.Run(async () =>
        {
            try
            {
                if (_serviceScopeFactory != null && subscriptionForAudit != null)
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var superAdminAuditLogService = scope.ServiceProvider.GetRequiredService<SuperAdminAuditLogService>();
                    var authService = scope.ServiceProvider.GetService<IAuthService>();
                    
                    Customer? customer = null;
                    if (subscriptionForAudit.CustomerId > 0)
                    {
                        customer = await _context.Customers
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.CustomerId == subscriptionForAudit.CustomerId);
                    }
                    
                    var currentUser = authService?.CurrentUser;
                    var userName = currentUser != null ? $"{currentUser.first_name} {currentUser.last_name}".Trim() : "System";
                    if (string.IsNullOrWhiteSpace(userName) && currentUser != null)
                        userName = currentUser.email ?? "System";
                    var userRole = currentUser?.user_role ?? "SuperAdmin";
                    var userId = currentUser?.user_ID;
                    var ipAddress = GetLocalIpAddress();
                    
                    await superAdminAuditLogService.CreateTransactionLogAsync(
                        action: "Revoke Module",
                        module: "Subscription Management",
                        description: $"Revoked module '{modulePackageId}' from subscription {subscriptionId} for customer: {customer?.SchoolName} ({customer?.CustomerCode})",
                        userName: userName,
                        userRole: userRole,
                        userId: userId,
                        entityType: "Subscription Module",
                        entityId: $"{subscriptionId}-{modulePackageId}",
                        oldValues: $"Module: {modulePackageId} (Active)",
                        newValues: $"Module: {modulePackageId} (Revoked)",
                        ipAddress: ipAddress,
                        status: "Success",
                        severity: "Medium",
                        customerCode: customer?.CustomerCode,
                        customerName: customer?.SchoolName
                    );
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to create SuperAdmin audit log for module revocation: {Message}", ex.Message);
            }
        });

        return true;
    }

    /// <summary>
    /// Grant a module to a custom subscription
    /// </summary>
    public async Task<bool> GrantModuleAsync(int subscriptionId, string modulePackageId, int? grantedBy)
    {
        var subscription = await _context.CustomerSubscriptions
            .FirstOrDefaultAsync(cs => cs.SubscriptionId == subscriptionId);

        if (subscription == null)
        {
            return false;
        }

        if (subscription.SubscriptionType != "custom")
        {
            _logger?.LogWarning("Attempted to grant module to non-custom subscription {SubscriptionId}", subscriptionId);
            return false;
        }

        // Check if module already exists (not revoked)
        var existing = await _context.CustomerSubscriptionModules
            .FirstOrDefaultAsync(m => 
                m.SubscriptionId == subscriptionId 
                && m.ModulePackageId.Equals(modulePackageId, StringComparison.OrdinalIgnoreCase)
                && m.RevokedDate == null);

        if (existing != null)
        {
            return true; // Already granted
        }

        // Check if module was previously revoked and restore it
        var revoked = await _context.CustomerSubscriptionModules
            .FirstOrDefaultAsync(m => 
                m.SubscriptionId == subscriptionId 
                && m.ModulePackageId.Equals(modulePackageId, StringComparison.OrdinalIgnoreCase)
                && m.RevokedDate != null);

        if (revoked != null)
        {
            revoked.RevokedDate = null;
            revoked.RevokedBy = null;
            revoked.GrantedDate = DateTime.Now;
            revoked.GrantedBy = grantedBy;
        }
        else
        {
            _context.CustomerSubscriptionModules.Add(new CustomerSubscriptionModule
            {
                SubscriptionId = subscriptionId,
                ModulePackageId = modulePackageId.ToLower(),
                GrantedDate = DateTime.Now,
                GrantedBy = grantedBy
            });
        }

        await _context.SaveChangesAsync();
        await RefreshTenantModulesAsync(subscription.CustomerId);
        
        // Clear in-memory module cache for this customer to ensure fresh data on next permission check
        _customerModuleService?.ClearCache(subscription.CustomerId);

        _logger?.LogInformation("Granted module {ModuleId} to subscription {SubscriptionId}. In-memory cache cleared.", modulePackageId, subscriptionId);

        // Audit logging (non-blocking, background task)
        var subscriptionForAudit = subscription; // Capture for closure
        _ = Task.Run(async () =>
        {
            try
            {
                if (_serviceScopeFactory != null && subscriptionForAudit != null)
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var superAdminAuditLogService = scope.ServiceProvider.GetRequiredService<SuperAdminAuditLogService>();
                    var authService = scope.ServiceProvider.GetService<IAuthService>();
                    
                    Customer? customer = null;
                    if (subscriptionForAudit.CustomerId > 0)
                    {
                        customer = await _context.Customers
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.CustomerId == subscriptionForAudit.CustomerId);
                    }
                    
                    var currentUser = authService?.CurrentUser;
                    var userName = currentUser != null ? $"{currentUser.first_name} {currentUser.last_name}".Trim() : "System";
                    if (string.IsNullOrWhiteSpace(userName) && currentUser != null)
                        userName = currentUser.email ?? "System";
                    var userRole = currentUser?.user_role ?? "SuperAdmin";
                    var userId = currentUser?.user_ID;
                    var ipAddress = GetLocalIpAddress();
                    
                    await superAdminAuditLogService.CreateTransactionLogAsync(
                        action: "Grant Module",
                        module: "Subscription Management",
                        description: $"Granted module '{modulePackageId}' to subscription {subscriptionId} for customer: {customer?.SchoolName} ({customer?.CustomerCode})",
                        userName: userName,
                        userRole: userRole,
                        userId: userId,
                        entityType: "Subscription Module",
                        entityId: $"{subscriptionId}-{modulePackageId}",
                        oldValues: $"Module: {modulePackageId} (Not Granted)",
                        newValues: $"Module: {modulePackageId} (Granted)",
                        ipAddress: ipAddress,
                        status: "Success",
                        severity: "Medium",
                        customerCode: customer?.CustomerCode,
                        customerName: customer?.SchoolName
                    );
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to create SuperAdmin audit log for module grant: {Message}", ex.Message);
            }
        });

        return true;
    }

    /// <summary>
    /// Refresh the TenantModules materialized cache for a customer (or all customers)
    /// </summary>
    public async Task RefreshTenantModulesAsync(int? customerId = null)
    {
        try
        {
            // Delete existing entries
            if (customerId.HasValue)
            {
                var existing = await _context.TenantModules
                    .Where(tm => tm.CustomerId == customerId.Value)
                    .ToListAsync();
                _context.TenantModules.RemoveRange(existing);
            }
            else
            {
                var allExisting = await _context.TenantModules.ToListAsync();
                _context.TenantModules.RemoveRange(allExisting);
            }

            await _context.SaveChangesAsync();

            // Re-populate from predefined plan subscriptions
            var predefinedSubscriptions = await _context.CustomerSubscriptions
                .Include(cs => cs.Plan!)
                    .ThenInclude(p => p.PlanModules)
                .Where(cs => cs.SubscriptionType == "predefined" 
                    && (customerId == null || cs.CustomerId == customerId.Value))
                .ToListAsync();

            foreach (var subscription in predefinedSubscriptions)
            {
                if (subscription.Plan?.PlanModules != null)
                {
                    foreach (var planModule in subscription.Plan.PlanModules)
                    {
                        var isActive = subscription.Status == "Active" 
                            && (subscription.EndDate == null || subscription.EndDate >= DateTime.Today);

                        _context.TenantModules.Add(new TenantModule
                        {
                            CustomerId = subscription.CustomerId,
                            ModulePackageId = planModule.ModulePackageId,
                            SubscriptionId = subscription.SubscriptionId,
                            GrantedDate = subscription.CreatedAt,
                            IsActive = isActive,
                            LastUpdated = DateTime.Now
                        });
                    }
                }
            }

            // Re-populate from custom subscriptions
            var customSubscriptions = await _context.CustomerSubscriptions
                .Include(cs => cs.CustomerSubscriptionModules)
                .Where(cs => cs.SubscriptionType == "custom" 
                    && (customerId == null || cs.CustomerId == customerId.Value))
                .ToListAsync();

            foreach (var subscription in customSubscriptions)
            {
                var activeModules = subscription.CustomerSubscriptionModules
                    .Where(m => m.RevokedDate == null)
                    .ToList();

                foreach (var module in activeModules)
                {
                    var isActive = subscription.Status == "Active" 
                        && (subscription.EndDate == null || subscription.EndDate >= DateTime.Today);

                    _context.TenantModules.Add(new TenantModule
                    {
                        CustomerId = subscription.CustomerId,
                        ModulePackageId = module.ModulePackageId,
                        SubscriptionId = subscription.SubscriptionId,
                        GrantedDate = module.GrantedDate,
                        IsActive = isActive,
                        LastUpdated = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Clear in-memory module cache for affected customers to ensure fresh data on next permission check
            if (customerId.HasValue)
            {
                _customerModuleService?.ClearCache(customerId.Value);
            }
            else
            {
                // If refreshing all customers, clear all cache
                _customerModuleService?.ClearAllCache();
            }

            _logger?.LogInformation("Refreshed TenantModules cache for {Scope}. In-memory cache cleared.", 
                customerId.HasValue ? $"customer {customerId.Value}" : "all customers");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error refreshing TenantModules cache for customer {CustomerId}", customerId);
            throw;
        }
    }

    /// <summary>
    /// Get all active modules for a customer (from TenantModules cache for performance)
    /// </summary>
    public async Task<List<string>> GetCustomerModulesAsync(int customerId)
    {
        var modules = await _context.TenantModules
            .AsNoTracking()
            .Where(tm => tm.CustomerId == customerId && tm.IsActive)
            .Select(tm => tm.ModulePackageId)
            .Distinct()
            .ToListAsync();

        // Always include 'core' if not already present
        if (!modules.Contains("core", StringComparer.OrdinalIgnoreCase))
        {
            modules.Insert(0, "core");
        }

        return modules;
    }
}
