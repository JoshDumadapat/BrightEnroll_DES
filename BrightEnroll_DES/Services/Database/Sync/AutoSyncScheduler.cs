using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BrightEnroll_DES.Services.Infrastructure;

namespace BrightEnroll_DES.Services.Database.Sync;

// Background service that automatically syncs data at configured intervals
public class AutoSyncScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConnectivityService _connectivityService;
    private readonly ILogger<AutoSyncScheduler> _logger;
    private readonly IConfiguration _configuration;
    private readonly ISyncStatusService _syncStatusService;
    private DateTime _lastSyncTime = DateTime.MinValue;
    private bool _isSyncing = false;
    private readonly object _syncLock = new object();

    public AutoSyncScheduler(
        IServiceScopeFactory serviceScopeFactory,
        IConnectivityService connectivityService,
        ISyncStatusService syncStatusService,
        ILogger<AutoSyncScheduler> logger,
        IConfiguration configuration)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _connectivityService = connectivityService;
        _syncStatusService = syncStatusService;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Get sync interval from configuration (default: 5 minutes)
        var syncIntervalMinutes = _configuration.GetValue<int>("Sync:AutoSyncIntervalMinutes", 5);
        var syncInterval = TimeSpan.FromMinutes(syncIntervalMinutes);

        _logger.LogInformation("AutoSyncScheduler started. Sync interval: {Interval} minutes", syncIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(syncInterval, stoppingToken);

                // Check if we should sync
                if (ShouldSync())
                {
                    await PerformSyncAsync();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoSyncScheduler loop");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait 1 minute before retrying
            }
        }

        _logger.LogInformation("AutoSyncScheduler stopped");
    }

    private bool ShouldSync()
    {
        lock (_syncLock)
        {
            // Don't sync if already syncing
            if (_isSyncing)
            {
                _logger.LogDebug("Sync already in progress, skipping");
                return false;
            }

            // Don't sync if offline
            if (!_connectivityService.IsConnected)
            {
                _logger.LogDebug("Offline, skipping sync");
                return false;
            }

            // Check if enough time has passed since last sync
            var syncIntervalMinutes = _configuration.GetValue<int>("Sync:AutoSyncIntervalMinutes", 5);
            var timeSinceLastSync = DateTime.Now - _lastSyncTime;
            if (timeSinceLastSync.TotalMinutes < syncIntervalMinutes)
            {
                _logger.LogDebug("Not enough time since last sync, skipping");
                return false;
            }

            return true;
        }
    }

    private async Task PerformSyncAsync()
    {
        lock (_syncLock)
        {
            if (_isSyncing)
                return;
            _isSyncing = true;
        }

        try
        {
            _logger.LogInformation("Starting automatic sync");
            _syncStatusService.SetSyncing(true);

            // Create new scope for sync operation to prevent concurrency errors
            // BackgroundService is Singleton, so we must create new scope for each operation
            using var scope = _serviceScopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IDatabaseSyncService>();

            // Perform incremental sync (push local changes, then pull cloud changes)
            var result = await syncService.FullSyncAsync();

            if (result.Success)
            {
                _lastSyncTime = DateTime.Now;
                _syncStatusService.UpdateLastSyncTime(_lastSyncTime);
                _syncStatusService.ClearErrors();
                _logger.LogInformation("Automatic sync completed successfully. Pushed: {Pushed}, Pulled: {Pulled}", 
                    result.RecordsPushed, result.RecordsPulled);
            }
            else
            {
                _syncStatusService.AddError(result.Message);
                _logger.LogWarning("Automatic sync completed with errors: {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automatic sync");
            _syncStatusService.AddError($"Sync error: {ex.Message}");
        }
        finally
        {
            lock (_syncLock)
            {
                _isSyncing = false;
            }
            _syncStatusService.SetSyncing(false);
        }
    }

    public async Task ForceSyncAsync()
    {
        await PerformSyncAsync();
    }
}

