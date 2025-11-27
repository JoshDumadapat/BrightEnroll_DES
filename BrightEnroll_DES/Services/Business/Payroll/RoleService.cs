using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Business.Payroll;

/// <summary>
/// Service for managing Role entities with proper DbContext lifetime management
/// </summary>
public interface IRoleService
{
    Task<List<Role>> GetAllRolesAsync();
    Task<Role?> GetRoleByIdAsync(int roleId);
    Task<Role?> GetRoleByNameAsync(string roleName);
    Task<Role> CreateRoleAsync(Role role);
    Task<Role> UpdateRoleAsync(Role role);
    Task<bool> DeleteRoleAsync(int roleId);
    Task<bool> RoleExistsAsync(string roleName, int? excludeRoleId = null);
}

public class RoleService : IRoleService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<RoleService>? _logger;

    public RoleService(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<RoleService>? logger = null)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _logger = logger;
    }

    public async Task<List<Role>> GetAllRolesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Roles
            .OrderBy(r => r.RoleName)
            .ToListAsync();
    }

    public async Task<Role?> GetRoleByIdAsync(int roleId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Roles.FindAsync(roleId);
    }

    public async Task<Role?> GetRoleByNameAsync(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return null;

        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Roles
            .FirstOrDefaultAsync(r => r.RoleName.ToLower() == roleName.ToLower());
    }

    public async Task<Role> CreateRoleAsync(Role role)
    {
        if (role == null)
            throw new ArgumentNullException(nameof(role));

        // Ensure IsSynced is explicitly set to false for new entities
        role.IsSynced = false;
        role.CreatedDate = DateTime.Now;

        await using var context = await _contextFactory.CreateDbContextAsync();
        
        // Check for duplicate
        var exists = await context.Roles
            .AnyAsync(r => r.RoleName.ToLower() == role.RoleName.ToLower());
        
        if (exists)
        {
            throw new InvalidOperationException($"Role '{role.RoleName}' already exists");
        }

        context.Roles.Add(role);
        await context.SaveChangesAsync();
        
        _logger?.LogInformation("Role created: {RoleName} (ID: {RoleId})", role.RoleName, role.RoleId);
        return role;
    }

    public async Task<Role> UpdateRoleAsync(Role role)
    {
        if (role == null)
            throw new ArgumentNullException(nameof(role));

        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var existingRole = await context.Roles.FindAsync(role.RoleId);
        if (existingRole == null)
        {
            throw new InvalidOperationException($"Role with ID {role.RoleId} not found");
        }

        // Check if role name changed and if new name already exists
        if (existingRole.RoleName.ToLower() != role.RoleName.ToLower())
        {
            var duplicate = await context.Roles
                .AnyAsync(r => r.RoleName.ToLower() == role.RoleName.ToLower() && r.RoleId != role.RoleId);
            
            if (duplicate)
            {
                throw new InvalidOperationException($"Role '{role.RoleName}' already exists");
            }
        }

        // Update properties
        existingRole.RoleName = role.RoleName;
        existingRole.BaseSalary = role.BaseSalary;
        existingRole.Allowance = role.Allowance;
        existingRole.IsActive = role.IsActive;
        existingRole.UpdatedDate = DateTime.Now;
        
        // Mark as unsynced when updated
        existingRole.IsSynced = false;

        await context.SaveChangesAsync();
        
        _logger?.LogInformation("Role updated: {RoleName} (ID: {RoleId})", role.RoleName, role.RoleId);
        return existingRole;
    }

    public async Task<bool> DeleteRoleAsync(int roleId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var role = await context.Roles.FindAsync(roleId);
        if (role == null)
            return false;

        context.Roles.Remove(role);
        await context.SaveChangesAsync();
        
        _logger?.LogInformation("Role deleted: {RoleName} (ID: {RoleId})", role.RoleName, roleId);
        return true;
    }

    public async Task<bool> RoleExistsAsync(string roleName, int? excludeRoleId = null)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return false;

        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = context.Roles.Where(r => r.RoleName.ToLower() == roleName.ToLower());
        
        if (excludeRoleId.HasValue)
        {
            query = query.Where(r => r.RoleId != excludeRoleId.Value);
        }

        return await query.AnyAsync();
    }
}

