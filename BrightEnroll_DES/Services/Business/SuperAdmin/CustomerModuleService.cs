using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Data.Models.SuperAdmin;
using BrightEnroll_DES.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.SuperAdmin;

// Manage customer module access based on subscription packages
public interface ICustomerModuleService
{
    Task<List<string>> GetAvailableModulePackageIdsAsync(string? customerEmail = null);
    Task<List<string>> GetAvailableModulePackageIdsByCustomerIdAsync(int customerId);
    Task<List<string>> GetAvailablePermissionsAsync(string? customerEmail = null);
    Task<List<string>> GetAvailablePermissionsByCustomerIdAsync(int customerId);
    bool HasModuleAccess(string packageId, string? customerEmail = null);
    bool HasModuleAccessByCustomerId(string packageId, int customerId);
    bool HasPermissionFromModule(string permission, string? customerEmail = null);
    bool HasPermissionFromModuleByCustomerId(string permission, int customerId);
    void ClearCache(int customerId);
    void ClearAllCache();
}

public class CustomerModuleService : ICustomerModuleService
{
    private readonly SuperAdminDbContext _superAdminContext;
    private readonly ISubscriptionService? _subscriptionService;
    private readonly ILogger<CustomerModuleService>? _logger;
    
    // Cache keyed by CustomerId (TenantId) - NOT email
    private readonly Dictionary<int, CachedModuleData> _customerModulesCache = new();
    private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);
    
    // Cache entry with expiration
    private class CachedModuleData
    {
        public List<string> ModulePackageIds { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public DateTime CachedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    // Cache configuration
    private const int CACHE_TTL_MINUTES = 5; // Cache expires after 5 minutes (reduced from 15 for faster updates)
    private const int QUERY_TIMEOUT_SECONDS = 5; // All queries timeout after 5 seconds

    public CustomerModuleService(
        SuperAdminDbContext superAdminContext, 
        ISubscriptionService? subscriptionService = null,
        ILogger<CustomerModuleService>? logger = null)
    {
        _superAdminContext = superAdminContext ?? throw new ArgumentNullException(nameof(superAdminContext));
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    // Get available module package IDs by email
    public async Task<List<string>> GetAvailableModulePackageIdsAsync(string? customerEmail = null)
    {
        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            _logger?.LogWarning("GetAvailableModulePackageIdsAsync called with null/empty email. Returning core only.");
            return new List<string> { "core" };
        }

        try
        {
            // Resolve email to CustomerId
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(QUERY_TIMEOUT_SECONDS));
            
            var emailLower = customerEmail.Trim().ToLower();
            var customer = await _superAdminContext.Customers
                .AsNoTracking()
                .Where(c => 
                    (c.AdminUsername != null && c.AdminUsername.Trim().ToLower() == emailLower) || 
                    (c.ContactEmail != null && c.ContactEmail.Trim().ToLower() == emailLower))
                .Select(c => new { c.CustomerId, c.AdminUsername, c.ContactEmail, c.CustomerCode, c.SchoolName })
                .FirstOrDefaultAsync(cts.Token);

            if (customer == null)
            {
                _logger?.LogWarning("Customer not found for email: {Email}. Searched in AdminUsername and ContactEmail. Returning core only.", customerEmail);
                
                // Debug: Log all customers to help diagnose
                try
                {
                    var allCustomers = await _superAdminContext.Customers
                        .AsNoTracking()
                        .Select(c => new { c.CustomerId, c.AdminUsername, c.ContactEmail, c.CustomerCode })
                        .Take(10)
                        .ToListAsync();
                    _logger?.LogDebug("Sample customers in database (first 10): {Customers}", 
                        string.Join(", ", allCustomers.Select(c => $"Code: {c.CustomerCode}, Admin: {c.AdminUsername}, Contact: {c.ContactEmail}")));
                }
                catch { }
                
                return new List<string> { "core" };
            }
            
            _logger?.LogInformation("Resolved email {Email} to CustomerId {CustomerId} (Code: {CustomerCode}, School: {SchoolName})", 
                customerEmail, customer.CustomerId, customer.CustomerCode, customer.SchoolName);

            // Use CustomerId-based method
            return await GetAvailableModulePackageIdsByCustomerIdAsync(customer.CustomerId);
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Timeout resolving customer email {Email} to CustomerId (exceeded {Timeout}s). Returning core only.", 
                customerEmail, QUERY_TIMEOUT_SECONDS);
            return new List<string> { "core" };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error resolving customer email {Email} to CustomerId. Returning core only.", customerEmail);
            return new List<string> { "core" };
        }
    }

    /// <summary>
    /// Get available module package IDs for a customer by CustomerId (TenantId)
    /// 
    /// PSEUDOCODE:
    /// 1. Check in-memory cache (keyed by CustomerId)
    ///    IF cache hit AND not expired:
    ///       RETURN cached modules
    /// 
    /// 2. Try to get from TenantModules cache table (fast path)
    ///    IF TenantModules has active entries for CustomerId:
    ///       Load modules from TenantModules
    ///       Cache in memory
    ///       RETURN modules
    /// 
    /// 3. Fallback: Resolve from subscription tables (slow path)
    ///    Query tbl_CustomerSubscriptions for active subscription
    ///    IF subscription_type = 'predefined':
    ///       Query tbl_PlanModules for modules
    ///    ELSE IF subscription_type = 'custom':
    ///       Query tbl_CustomerSubscriptionModules for modules (where revoked_date IS NULL)
    ///    Always include 'core' module
    ///    Update TenantModules cache table
    ///    Cache in memory
    ///    RETURN modules
    /// 
    /// 4. On any error/timeout:
    ///    RETURN ['core'] only (fail-safe)
    /// </summary>
    public async Task<List<string>> GetAvailableModulePackageIdsByCustomerIdAsync(int customerId)
    {
        // Step 1: Check in-memory cache
        await _cacheLock.WaitAsync();
        try
        {
            if (_customerModulesCache.TryGetValue(customerId, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            {
                _logger?.LogDebug("Cache hit for CustomerId {CustomerId}. Returning cached modules.", customerId);
                return new List<string>(cached.ModulePackageIds);
            }
        }
        finally
        {
            _cacheLock.Release();
        }

        // Step 2: Try TenantModules cache table (fast path)
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(QUERY_TIMEOUT_SECONDS));
            
            var tenantModules = await _superAdminContext.TenantModules
                .AsNoTracking()
                .Where(tm => tm.CustomerId == customerId && tm.IsActive)
                .Select(tm => tm.ModulePackageId)
                .Distinct()
                .ToListAsync(cts.Token);

            if (tenantModules.Any())
            {
                // Always ensure 'core' is included
                if (!tenantModules.Contains("core", StringComparer.OrdinalIgnoreCase))
                {
                    tenantModules.Insert(0, "core");
                }

                // Cache in memory
                await CacheModulesAsync(customerId, tenantModules);
                
                _logger?.LogInformation("Retrieved modules for CustomerId {CustomerId} from TenantModules cache: {Modules}", 
                    customerId, string.Join(", ", tenantModules));
                
                return tenantModules;
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Timeout querying TenantModules for CustomerId {CustomerId} (exceeded {Timeout}s). Falling back to subscription resolution.", 
                customerId, QUERY_TIMEOUT_SECONDS);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error querying TenantModules for CustomerId {CustomerId}. Falling back to subscription resolution. Error: {Message}", 
                customerId, ex.Message);
        }

        // Step 3: Fallback - Resolve from subscription tables (slow path)
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(QUERY_TIMEOUT_SECONDS));
            
            // Get active subscription
            var activeSubscription = await _superAdminContext.CustomerSubscriptions
                .AsNoTracking()
                .Where(cs => cs.CustomerId == customerId && cs.Status == "Active")
                .OrderByDescending(cs => cs.StartDate)
                .FirstOrDefaultAsync(cts.Token);

            if (activeSubscription == null)
            {
                _logger?.LogWarning("No active subscription found for CustomerId {CustomerId}. Returning core only.", customerId);
                var coreOnly = new List<string> { "core" };
                await CacheModulesAsync(customerId, coreOnly);
                return coreOnly;
            }

            List<string> modules;

            // Resolve modules based on subscription type
            if (activeSubscription.SubscriptionType == "predefined" && activeSubscription.PlanId.HasValue)
            {
                // Predefined plan: Get modules from tbl_PlanModules
                var planModules = await _superAdminContext.PlanModules
                    .AsNoTracking()
                    .Where(pm => pm.PlanId == activeSubscription.PlanId.Value)
                    .Select(pm => pm.ModulePackageId)
                    .Distinct()
                    .ToListAsync(cts.Token);

                modules = planModules.ToList();
            }
            else if (activeSubscription.SubscriptionType == "custom")
            {
                // Custom subscription: Get modules from tbl_CustomerSubscriptionModules
                var customModules = await _superAdminContext.CustomerSubscriptionModules
                    .AsNoTracking()
                    .Where(csm => csm.SubscriptionId == activeSubscription.SubscriptionId && csm.RevokedDate == null)
                    .Select(csm => csm.ModulePackageId)
                    .Distinct()
                    .ToListAsync(cts.Token);

                modules = customModules.ToList();
            }
            else
            {
                _logger?.LogWarning("Invalid subscription type '{Type}' for CustomerId {CustomerId}. Returning core only.", 
                    activeSubscription.SubscriptionType, customerId);
                modules = new List<string>();
            }

            // Always include 'core' module
            if (!modules.Contains("core", StringComparer.OrdinalIgnoreCase))
            {
                modules.Insert(0, "core");
            }

            // Update TenantModules cache table (if SubscriptionService is available)
            if (_subscriptionService != null)
            {
                try
                {
                    await _subscriptionService.RefreshTenantModulesAsync(customerId);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to refresh TenantModules cache for CustomerId {CustomerId}. Continuing with in-memory cache.", customerId);
                }
            }

            // Cache in memory
            await CacheModulesAsync(customerId, modules);

            _logger?.LogInformation("Resolved modules for CustomerId {CustomerId} from subscription tables: {Modules}", 
                customerId, string.Join(", ", modules));

            return modules;
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Timeout resolving modules from subscription tables for CustomerId {CustomerId} (exceeded {Timeout}s). Returning core only.", 
                customerId, QUERY_TIMEOUT_SECONDS);
            return new List<string> { "core" };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error resolving modules from subscription tables for CustomerId {CustomerId}. Returning core only. Error: {Message}", 
                customerId, ex.Message);
            return new List<string> { "core" };
        }
    }

    /// <summary>
    /// Get all available permissions for a customer by email
    /// </summary>
    public async Task<List<string>> GetAvailablePermissionsAsync(string? customerEmail = null)
    {
        if (string.IsNullOrWhiteSpace(customerEmail))
            return ModulePackages.CorePackage.Permissions;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(QUERY_TIMEOUT_SECONDS));
            
            var customer = await _superAdminContext.Customers
                .AsNoTracking()
                .Where(c => 
                    (c.AdminUsername != null && c.AdminUsername.Trim().ToLower() == customerEmail.Trim().ToLower()) || 
                    (c.ContactEmail != null && c.ContactEmail.Trim().ToLower() == customerEmail.Trim().ToLower()))
                .Select(c => new { c.CustomerId })
                .FirstOrDefaultAsync(cts.Token);

            if (customer == null)
                return ModulePackages.CorePackage.Permissions;

            return await GetAvailablePermissionsByCustomerIdAsync(customer.CustomerId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting permissions for email {Email}. Returning core permissions only.", customerEmail);
            return ModulePackages.CorePackage.Permissions;
        }
    }

    /// <summary>
    /// Get all available permissions for a customer by CustomerId
    /// </summary>
    public async Task<List<string>> GetAvailablePermissionsByCustomerIdAsync(int customerId)
    {
        // Check cache first
        await _cacheLock.WaitAsync();
        try
        {
            if (_customerModulesCache.TryGetValue(customerId, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            {
                return new List<string>(cached.Permissions);
            }
        }
        finally
        {
            _cacheLock.Release();
        }

        // Get modules and convert to permissions
        var packageIds = await GetAvailableModulePackageIdsByCustomerIdAsync(customerId);
        var permissions = ModulePackages.GetPermissionsFromPackages(packageIds);

        // Cache permissions
        await _cacheLock.WaitAsync();
        try
        {
            if (_customerModulesCache.TryGetValue(customerId, out var cached))
            {
                cached.Permissions = permissions;
            }
        }
        finally
        {
            _cacheLock.Release();
        }

        return permissions;
    }

    // Check module access by email (synchronous, uses cache)
    public bool HasModuleAccess(string packageId, string? customerEmail = null)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            return false;

        // Core is always available
        if (packageId.Equals("core", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.IsNullOrWhiteSpace(customerEmail))
            return false;

        // Try to resolve CustomerId from cache or quick lookup
        try
        {
            var customer = _superAdminContext.Customers
                .AsNoTracking()
                .Where(c => 
                    (c.AdminUsername != null && c.AdminUsername.Trim().ToLower() == customerEmail.Trim().ToLower()) || 
                    (c.ContactEmail != null && c.ContactEmail.Trim().ToLower() == customerEmail.Trim().ToLower()))
                .Select(c => new { c.CustomerId })
                .FirstOrDefault();

            if (customer != null)
            {
                return HasModuleAccessByCustomerId(packageId, customer.CustomerId);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking module access for email {Email}. Returning false.", customerEmail);
        }

        return false;
    }

    // Check module access by customer ID (synchronous, uses cache)
    public bool HasModuleAccessByCustomerId(string packageId, int customerId)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            return false;

        // Core is always available
        if (packageId.Equals("core", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check cache (synchronous - no blocking)
        _cacheLock.Wait();
        try
        {
            if (_customerModulesCache.TryGetValue(customerId, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            {
                return cached.ModulePackageIds.Any(m => m.Equals(packageId, StringComparison.OrdinalIgnoreCase));
            }
        }
        finally
        {
            _cacheLock.Release();
        }

        // Not in cache - return false (components should preload modules)
        _logger?.LogDebug("Module {ModuleId} not in cache for CustomerId {CustomerId}. Returning false. Ensure modules are preloaded.", 
            packageId, customerId);
        return false;
    }

    /// <summary>
    /// Check if customer has a specific permission (by email)
    /// NOTE: Synchronous method - uses cache only. For accurate results, use async version.
    /// </summary>
    public bool HasPermissionFromModule(string permission, string? customerEmail = null)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return false;

        if (string.IsNullOrWhiteSpace(customerEmail))
            return false;

        try
        {
            var customer = _superAdminContext.Customers
                .AsNoTracking()
                .Where(c => 
                    (c.AdminUsername != null && c.AdminUsername.Trim().ToLower() == customerEmail.Trim().ToLower()) || 
                    (c.ContactEmail != null && c.ContactEmail.Trim().ToLower() == customerEmail.Trim().ToLower()))
                .Select(c => new { c.CustomerId })
                .FirstOrDefault();

            if (customer != null)
            {
                return HasPermissionFromModuleByCustomerId(permission, customer.CustomerId);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking permission for email {Email}. Returning false.", customerEmail);
        }

        return false;
    }

    // Check permission by customer ID (synchronous, uses cache)
    /// This ensures permissions work even if cache hasn't been populated yet.
    /// </summary>
    public bool HasPermissionFromModuleByCustomerId(string permission, int customerId)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return false;

        // Check cache first (synchronous - no blocking)
        _cacheLock.Wait();
        bool cacheHit = false;
        bool hasPermission = false;
        try
        {
            if (_customerModulesCache.TryGetValue(customerId, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            {
                cacheHit = true;
                hasPermission = cached.Permissions.Any(p => p.Equals(permission, StringComparison.OrdinalIgnoreCase));
            }
        }
        finally
        {
            _cacheLock.Release();
        }

        // If cache hit, return the result
        if (cacheHit)
        {
            return hasPermission;
        }

        // Cache miss - fallback to TenantModules table (synchronous query)
        // This ensures permissions work even if cache hasn't been populated yet
        try
        {
            var activeModules = _superAdminContext.TenantModules
                .AsNoTracking()
                .Where(tm => tm.CustomerId == customerId && tm.IsActive)
                .Select(tm => tm.ModulePackageId)
                .Distinct()
                .ToList();

            // Always include 'core' module
            if (!activeModules.Contains("core", StringComparer.OrdinalIgnoreCase))
            {
                activeModules.Insert(0, "core");
            }

            // Convert modules to permissions
            var permissions = ModulePackages.GetPermissionsFromPackages(activeModules);
            
            // Cache the result for future checks
            _ = Task.Run(async () => await CacheModulesAsync(customerId, activeModules));
            
            // Check if permission is in the list
            return permissions.Any(p => p.Equals(permission, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error checking permission {Permission} for CustomerId {CustomerId} from TenantModules. Returning false.", 
                permission, customerId);
            return false;
        }
    }

    /// <summary>
    /// Cache modules for a customer (thread-safe)
    /// </summary>
    private async Task CacheModulesAsync(int customerId, List<string> modules)
    {
        await _cacheLock.WaitAsync();
        try
        {
            var permissions = ModulePackages.GetPermissionsFromPackages(modules);
            
            _customerModulesCache[customerId] = new CachedModuleData
            {
                ModulePackageIds = new List<string>(modules),
                Permissions = permissions,
                CachedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(CACHE_TTL_MINUTES)
            };

            _logger?.LogDebug("Cached modules for CustomerId {CustomerId}. Expires at {ExpiresAt}", 
                customerId, _customerModulesCache[customerId].ExpiresAt);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Clear cache for a specific customer
    /// </summary>
    public void ClearCache(int customerId)
    {
        _cacheLock.Wait();
        try
        {
            var removed = _customerModulesCache.Remove(customerId);
            _logger?.LogInformation("Cleared module cache for CustomerId {CustomerId}. Removed: {Removed}", customerId, removed);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Clear all cache entries
    /// </summary>
    public void ClearAllCache()
    {
        _cacheLock.Wait();
        try
        {
            var count = _customerModulesCache.Count;
            _customerModulesCache.Clear();
            _logger?.LogInformation("Cleared all module cache entries. Removed {Count} entries.", count);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Clean up expired cache entries (call periodically)
    /// </summary>
    public void CleanupExpiredCache()
    {
        _cacheLock.Wait();
        try
        {
            var expiredKeys = _customerModulesCache
                .Where(kvp => kvp.Value.ExpiresAt <= DateTime.UtcNow)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _customerModulesCache.Remove(key);
            }

            if (expiredKeys.Any())
            {
                _logger?.LogDebug("Cleaned up {Count} expired cache entries.", expiredKeys.Count);
            }
        }
        finally
        {
            _cacheLock.Release();
        }
    }
}
