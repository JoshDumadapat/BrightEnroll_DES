using BrightEnroll_DES.Services.Business.SuperAdmin;
using BrightEnroll_DES.Services.RoleBase;
using BrightEnroll_DES.Services.Authentication;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.SuperAdmin
{
    /// <summary>
    /// Tenant capability model - represents what a tenant can do based on their subscription
    /// </summary>
    public class TenantCapabilities
    {
        public int CustomerId { get; set; }
        public List<string> EnabledModulePackageIds { get; set; } = new();
        public List<string> EnabledPermissions { get; set; } = new();
        public Dictionary<string, bool> ModuleCapabilities { get; set; } = new(); // Module ID -> IsEnabled
        public Dictionary<string, bool> PermissionCapabilities { get; set; } = new(); // Permission -> IsGranted
        
        // Convenience methods
        public bool HasModule(string modulePackageId) => ModuleCapabilities.GetValueOrDefault(modulePackageId, false);
        public bool HasPermission(string permission) => PermissionCapabilities.GetValueOrDefault(permission, false);
    }

    /// <summary>
    /// Service to provide tenant capabilities for dashboard and feature gating
    /// </summary>
    public interface ITenantCapabilityService
    {
        Task<TenantCapabilities> GetTenantCapabilitiesAsync(int? customerId = null);
        Task<TenantCapabilities> GetTenantCapabilitiesByEmailAsync(string? customerEmail = null);
        bool HasModule(string modulePackageId, int? customerId = null);
        bool HasPermission(string permission, int? customerId = null);
    }

    public class TenantCapabilityService : ITenantCapabilityService
    {
        private readonly ICustomerModuleService _customerModuleService;
        private readonly ILogger<TenantCapabilityService>? _logger;
        private readonly IAuthService? _authService;

        public TenantCapabilityService(
            ICustomerModuleService customerModuleService,
            ILogger<TenantCapabilityService>? logger = null,
            IAuthService? authService = null)
        {
            _customerModuleService = customerModuleService ?? throw new ArgumentNullException(nameof(customerModuleService));
            _logger = logger;
            _authService = authService;
        }

        public async Task<TenantCapabilities> GetTenantCapabilitiesAsync(int? customerId = null)
        {
            try
            {
                // Resolve customer ID if not provided
                int resolvedCustomerId;
                if (customerId.HasValue)
                {
                    resolvedCustomerId = customerId.Value;
                }
                else
                {
                    // Try to get from current user
                    if (_authService?.IsAuthenticated == true && _authService.CurrentUser != null)
                    {
                        // For school admin, we need to get customer ID from email
                        var email = _authService.CurrentUser.email;
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            var modules = await _customerModuleService.GetAvailableModulePackageIdsAsync(email);
                            // We can't get customer ID directly from email here, so we'll use a different approach
                            // For now, return capabilities based on modules
                            return await BuildCapabilitiesFromModules(modules, email);
                        }
                    }
                    
                    _logger?.LogWarning("Cannot resolve customer ID for tenant capabilities");
                    return CreateEmptyCapabilities();
                }

                // Get modules and permissions for the customer
                var modulePackageIds = await _customerModuleService.GetAvailableModulePackageIdsByCustomerIdAsync(resolvedCustomerId);
                var permissions = await _customerModuleService.GetAvailablePermissionsByCustomerIdAsync(resolvedCustomerId);

                return BuildCapabilities(resolvedCustomerId, modulePackageIds, permissions);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting tenant capabilities");
                return CreateEmptyCapabilities();
            }
        }

        public async Task<TenantCapabilities> GetTenantCapabilitiesByEmailAsync(string? customerEmail = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerEmail))
                {
                    // Try to get from current user
                    if (_authService?.IsAuthenticated == true && _authService.CurrentUser != null)
                    {
                        customerEmail = _authService.CurrentUser.email;
                    }
                }

                if (string.IsNullOrWhiteSpace(customerEmail))
                {
                    _logger?.LogWarning("Cannot resolve customer email for tenant capabilities");
                    return CreateEmptyCapabilities();
                }

                var modulePackageIds = await _customerModuleService.GetAvailableModulePackageIdsAsync(customerEmail);
                var permissions = await _customerModuleService.GetAvailablePermissionsAsync(customerEmail);

                return await BuildCapabilitiesFromModules(modulePackageIds, customerEmail, permissions);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting tenant capabilities by email");
                return CreateEmptyCapabilities();
            }
        }

        public bool HasModule(string modulePackageId, int? customerId = null)
        {
            try
            {
                if (customerId.HasValue)
                {
                    return _customerModuleService.HasModuleAccessByCustomerId(modulePackageId, customerId.Value);
                }
                else
                {
                    // Try to get from current user
                    if (_authService?.IsAuthenticated == true && _authService.CurrentUser != null)
                    {
                        var email = _authService.CurrentUser.email;
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            return _customerModuleService.HasModuleAccess(modulePackageId, email);
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking module access");
                return false;
            }
        }

        public bool HasPermission(string permission, int? customerId = null)
        {
            try
            {
                if (customerId.HasValue)
                {
                    return _customerModuleService.HasPermissionFromModuleByCustomerId(permission, customerId.Value);
                }
                else
                {
                    // Try to get from current user
                    if (_authService?.IsAuthenticated == true && _authService.CurrentUser != null)
                    {
                        var email = _authService.CurrentUser.email;
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            return _customerModuleService.HasPermissionFromModule(permission, email);
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking permission");
                return false;
            }
        }

        private TenantCapabilities BuildCapabilities(int customerId, List<string> modulePackageIds, List<string> permissions)
        {
            var capabilities = new TenantCapabilities
            {
                CustomerId = customerId,
                EnabledModulePackageIds = modulePackageIds,
                EnabledPermissions = permissions
            };

            // Build module capability dictionary
            var allModules = ModulePackages.GetAllPackages();
            foreach (var module in allModules)
            {
                capabilities.ModuleCapabilities[module.PackageId] = modulePackageIds.Contains(module.PackageId, StringComparer.OrdinalIgnoreCase);
            }

            // Build permission capability dictionary
            foreach (var permission in permissions)
            {
                capabilities.PermissionCapabilities[permission] = true;
            }

            return capabilities;
        }

        private async Task<TenantCapabilities> BuildCapabilitiesFromModules(List<string> modulePackageIds, string email, List<string>? permissions = null)
        {
            // For email-based lookup, we don't have customer ID, so use 0
            var capabilities = new TenantCapabilities
            {
                CustomerId = 0, // Unknown when using email
                EnabledModulePackageIds = modulePackageIds,
                EnabledPermissions = permissions ?? new List<string>()
            };

            // Build module capability dictionary
            var allModules = ModulePackages.GetAllPackages();
            foreach (var module in allModules)
            {
                capabilities.ModuleCapabilities[module.PackageId] = modulePackageIds.Contains(module.PackageId, StringComparer.OrdinalIgnoreCase);
            }

            // Build permission capability dictionary
            if (permissions != null)
            {
                foreach (var permission in permissions)
                {
                    capabilities.PermissionCapabilities[permission] = true;
                }
            }
            else
            {
                // If permissions not provided, get them
                var resolvedPermissions = await _customerModuleService.GetAvailablePermissionsAsync(email);
                capabilities.EnabledPermissions = resolvedPermissions;
                foreach (var permission in resolvedPermissions)
                {
                    capabilities.PermissionCapabilities[permission] = true;
                }
            }

            return capabilities;
        }

        private TenantCapabilities CreateEmptyCapabilities()
        {
            // Return minimal capabilities (core only)
            var capabilities = new TenantCapabilities
            {
                CustomerId = 0,
                EnabledModulePackageIds = new List<string> { "core" },
                EnabledPermissions = ModulePackages.CorePackage.Permissions
            };

            // Only core module enabled
            var allModules = ModulePackages.GetAllPackages();
            foreach (var module in allModules)
            {
                capabilities.ModuleCapabilities[module.PackageId] = module.PackageId == "core";
            }

            // Only core permissions enabled
            foreach (var permission in ModulePackages.CorePackage.Permissions)
            {
                capabilities.PermissionCapabilities[permission] = true;
            }

            return capabilities;
        }
    }
}
