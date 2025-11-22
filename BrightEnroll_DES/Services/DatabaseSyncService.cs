using BrightEnroll_DES.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BrightEnroll_DES.Services;

public interface IDatabaseSyncService
{
    Task SyncToCloudAsync();
    Task<bool> TrySyncToCloudAsync();
    Task SyncFromCloudAsync();
    Task<bool> TrySyncFromCloudAsync();
    Task<bool> IsLocalDatabaseEmptyAsync();
}

public class DatabaseSyncService : IDatabaseSyncService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSyncService>? _logger;
    private readonly IConnectivityService _connectivityService;
    private bool _isSyncing = false;

    public DatabaseSyncService(
        IServiceProvider serviceProvider,
        IConnectivityService connectivityService,
        ILogger<DatabaseSyncService>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _connectivityService = connectivityService;
        _logger = logger;
    }

    public async Task SyncToCloudAsync()
    {
        if (_isSyncing)
        {
            _logger?.LogWarning("Sync already in progress, skipping...");
            return;
        }

        if (!_connectivityService.IsConnected)
        {
            _logger?.LogInformation("No internet connection, skipping cloud sync");
            return;
        }

        _isSyncing = true;
        try
        {
            using var localScope = _serviceProvider.CreateScope();
            var cloudContextFactory = _serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

            var localContext = localScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await using var cloudContext = await cloudContextFactory.CreateDbContextAsync();

            // Sync all entities from local to cloud
            await SyncEntitiesAsync(localContext, cloudContext);

            _logger?.LogInformation("Cloud sync completed successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during cloud sync");
            throw;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    public async Task<bool> TrySyncToCloudAsync()
    {
        try
        {
            await SyncToCloudAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task SyncFromCloudAsync()
    {
        if (_isSyncing)
        {
            _logger?.LogWarning("Sync already in progress, skipping...");
            return;
        }

        if (!_connectivityService.IsConnected)
        {
            _logger?.LogInformation("No internet connection, skipping cloud to local sync");
            return;
        }

        _isSyncing = true;
        try
        {
            using var localScope = _serviceProvider.CreateScope();
            var cloudContextFactory = _serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

            var localContext = localScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await using var cloudContext = await cloudContextFactory.CreateDbContextAsync();

            // Sync all entities from cloud to local
            await SyncEntitiesFromCloudAsync(cloudContext, localContext);

            _logger?.LogInformation("Cloud to local sync completed successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during cloud to local sync");
            throw;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    public async Task<bool> TrySyncFromCloudAsync()
    {
        try
        {
            await SyncFromCloudAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsLocalDatabaseEmptyAsync()
    {
        try
        {
            using var localScope = _serviceProvider.CreateScope();
            var localContext = localScope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Check if any major tables have data
            var hasUsers = await localContext.Users.AnyAsync();
            var hasStudents = await localContext.Students.AnyAsync();
            var hasEmployees = await localContext.EmployeeAddresses.AnyAsync();

            // Database is considered empty if no users, students, or employees exist
            return !hasUsers && !hasStudents && !hasEmployees;
        }
        catch
        {
            // If we can't check, assume it's not empty to be safe
            return false;
        }
    }

    private async Task SyncEntitiesAsync(AppDbContext localContext, AppDbContext cloudContext)
    {
        // Sync Users
        await SyncTableAsync(localContext.Users, cloudContext.Users, u => u.UserId, cloudContext);

        // Sync Students (uses string key)
        await SyncTableAsyncStringKey(localContext.Students, cloudContext.Students, s => s.StudentId, cloudContext);

        // Sync Guardians
        await SyncTableAsync(localContext.Guardians, cloudContext.Guardians, g => g.GuardianId, cloudContext);

        // Sync Student Requirements
        await SyncTableAsync(localContext.StudentRequirements, cloudContext.StudentRequirements, r => r.RequirementId, cloudContext);

        // Sync Employee Addresses
        await SyncTableAsync(localContext.EmployeeAddresses, cloudContext.EmployeeAddresses, e => e.AddressId, cloudContext);

        // Sync Employee Emergency Contacts
        await SyncTableAsync(localContext.EmployeeEmergencyContacts, cloudContext.EmployeeEmergencyContacts, e => e.EmergencyId, cloudContext);

        // Sync Salary Infos
        await SyncTableAsync(localContext.SalaryInfos, cloudContext.SalaryInfos, s => s.SalaryId, cloudContext);

        // Sync Grade Levels
        await SyncTableAsync(localContext.GradeLevels, cloudContext.GradeLevels, g => g.GradeLevelId, cloudContext);

        // Sync Fees
        await SyncTableAsync(localContext.Fees, cloudContext.Fees, f => f.FeeId, cloudContext);

        // Sync Fee Breakdowns
        await SyncTableAsync(localContext.FeeBreakdowns, cloudContext.FeeBreakdowns, f => f.BreakdownId, cloudContext);

        // Sync User Status Logs
        await SyncTableAsync(localContext.UserStatusLogs, cloudContext.UserStatusLogs, u => u.LogId, cloudContext);
    }

    // Sync method for entities with int primary keys
    private async Task SyncTableAsync<T>(DbSet<T> localSet, DbSet<T> cloudSet, Func<T, int> getIdFunc, AppDbContext cloudContext) where T : class
    {
        var localEntities = await localSet.ToListAsync();
        
        foreach (var localEntity in localEntities)
        {
            var id = getIdFunc(localEntity);
            var cloudEntity = await cloudSet.FindAsync(id);

            if (cloudEntity == null)
            {
                // Entity doesn't exist in cloud, add it
                cloudSet.Add(localEntity);
            }
            else
            {
                // Entity exists, update it
                cloudContext.Entry(cloudEntity).CurrentValues.SetValues(localEntity);
            }
        }

        await cloudContext.SaveChangesAsync();
    }

    // Sync method for entities with string primary keys (e.g., Student)
    private async Task SyncTableAsyncStringKey<T>(DbSet<T> localSet, DbSet<T> cloudSet, Func<T, string> getIdFunc, AppDbContext cloudContext) where T : class
    {
        var localEntities = await localSet.ToListAsync();
        
        foreach (var localEntity in localEntities)
        {
            var id = getIdFunc(localEntity);
            var cloudEntity = await cloudSet.FindAsync(id);

            if (cloudEntity == null)
            {
                // Entity doesn't exist in cloud, add it
                cloudSet.Add(localEntity);
            }
            else
            {
                // Entity exists, update it
                cloudContext.Entry(cloudEntity).CurrentValues.SetValues(localEntity);
            }
        }

        await cloudContext.SaveChangesAsync();
    }

    // Sync entities FROM CLOUD TO LOCAL (for new devices or when pulling cloud data)
    private async Task SyncEntitiesFromCloudAsync(AppDbContext cloudContext, AppDbContext localContext)
    {
        // Sync Users
        await SyncTableFromCloudAsync(cloudContext.Users, localContext.Users, u => u.UserId, localContext);

        // Sync Students (uses string key)
        await SyncTableFromCloudStringKeyAsync(cloudContext.Students, localContext.Students, s => s.StudentId, localContext);

        // Sync Guardians
        await SyncTableFromCloudAsync(cloudContext.Guardians, localContext.Guardians, g => g.GuardianId, localContext);

        // Sync Student Requirements
        await SyncTableFromCloudAsync(cloudContext.StudentRequirements, localContext.StudentRequirements, r => r.RequirementId, localContext);

        // Sync Employee Addresses
        await SyncTableFromCloudAsync(cloudContext.EmployeeAddresses, localContext.EmployeeAddresses, e => e.AddressId, localContext);

        // Sync Employee Emergency Contacts
        await SyncTableFromCloudAsync(cloudContext.EmployeeEmergencyContacts, localContext.EmployeeEmergencyContacts, e => e.EmergencyId, localContext);

        // Sync Salary Infos
        await SyncTableFromCloudAsync(cloudContext.SalaryInfos, localContext.SalaryInfos, s => s.SalaryId, localContext);

        // Sync Grade Levels
        await SyncTableFromCloudAsync(cloudContext.GradeLevels, localContext.GradeLevels, g => g.GradeLevelId, localContext);

        // Sync Fees
        await SyncTableFromCloudAsync(cloudContext.Fees, localContext.Fees, f => f.FeeId, localContext);

        // Sync Fee Breakdowns
        await SyncTableFromCloudAsync(cloudContext.FeeBreakdowns, localContext.FeeBreakdowns, f => f.BreakdownId, localContext);

        // Sync User Status Logs
        await SyncTableFromCloudAsync(cloudContext.UserStatusLogs, localContext.UserStatusLogs, u => u.LogId, localContext);
    }

    // Sync method FROM CLOUD TO LOCAL for entities with int primary keys
    private async Task SyncTableFromCloudAsync<T>(DbSet<T> cloudSet, DbSet<T> localSet, Func<T, int> getIdFunc, AppDbContext localContext) where T : class
    {
        var cloudEntities = await cloudSet.ToListAsync();
        
        foreach (var cloudEntity in cloudEntities)
        {
            var id = getIdFunc(cloudEntity);
            var localEntity = await localSet.FindAsync(id);

            if (localEntity == null)
            {
                // Entity doesn't exist in local, add it from cloud
                localSet.Add(cloudEntity);
            }
            else
            {
                // Entity exists in local, update it with cloud data (cloud is source of truth for initial sync)
                localContext.Entry(localEntity).CurrentValues.SetValues(cloudEntity);
            }
        }

        await localContext.SaveChangesAsync();
    }

    // Sync method FROM CLOUD TO LOCAL for entities with string primary keys (e.g., Student)
    private async Task SyncTableFromCloudStringKeyAsync<T>(DbSet<T> cloudSet, DbSet<T> localSet, Func<T, string> getIdFunc, AppDbContext localContext) where T : class
    {
        var cloudEntities = await cloudSet.ToListAsync();
        
        foreach (var cloudEntity in cloudEntities)
        {
            var id = getIdFunc(cloudEntity);
            var localEntity = await localSet.FindAsync(id);

            if (localEntity == null)
            {
                // Entity doesn't exist in local, add it from cloud
                localSet.Add(cloudEntity);
            }
            else
            {
                // Entity exists in local, update it with cloud data (cloud is source of truth for initial sync)
                localContext.Entry(localEntity).CurrentValues.SetValues(cloudEntity);
            }
        }

        await localContext.SaveChangesAsync();
    }
}

