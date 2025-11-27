using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace BrightEnroll_DES.Services.Sync;

/// <summary>
/// Background service that periodically syncs offline data to cloud database
/// For MAUI Blazor Hybrid - manually started service
/// </summary>
public class SyncBackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SyncBackgroundService>? _logger;
    private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(5); // Sync every 5 minutes
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _backgroundTask;
    private bool _isRunning = false;

    public SyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SyncBackgroundService>? logger = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger;
    }

    /// <summary>
    /// Starts the background sync loop
    /// </summary>
    public void Start()
    {
        if (_isRunning)
        {
            _logger?.LogWarning("SyncBackgroundService is already running");
            return;
        }

        _logger?.LogInformation("SyncBackgroundService starting. Sync interval: {Interval} minutes", _syncInterval.TotalMinutes);
        
        _isRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();
        _backgroundTask = RunBackgroundLoop(_cancellationTokenSource.Token);
    }

    private async Task RunBackgroundLoop(CancellationToken cancellationToken)
    {
        // Wait a bit before first sync to let app initialize
        await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();

                _logger?.LogDebug("Starting periodic sync...");
                var result = await syncService.SyncAsync();
                
                if (result)
                {
                    _logger?.LogInformation("Periodic sync completed successfully");
                }
                else
                {
                    _logger?.LogDebug("Periodic sync skipped (offline or no data to sync)");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during periodic sync");
            }

            // Wait for the next sync interval
            try
            {
                await Task.Delay(_syncInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
        }

        _isRunning = false;
        _logger?.LogInformation("SyncBackgroundService background loop stopped");
    }

    /// <summary>
    /// Stops the background sync loop
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        _logger?.LogInformation("SyncBackgroundService stopping...");
        
        _cancellationTokenSource?.Cancel();
        
        if (_backgroundTask != null)
        {
            try
            {
                _backgroundTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error waiting for background task to complete");
            }
        }
        
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _backgroundTask = null;
        _isRunning = false;
    }

    /// <summary>
    /// Gets whether the service is currently running
    /// </summary>
    public bool IsRunning => _isRunning;
}
