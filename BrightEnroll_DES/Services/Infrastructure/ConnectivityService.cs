using Microsoft.JSInterop;
using System.Runtime.InteropServices;
using BrightEnroll_DES.Services.Database.Sync;
using Microsoft.Extensions.DependencyInjection;

namespace BrightEnroll_DES.Services.Infrastructure;

public interface IConnectivityService
{
    bool IsConnected { get; }
    bool HasShownInitialToast { get; set; }
    event EventHandler<bool> ConnectivityChanged;
    Task<bool> CheckConnectivityAsync();
    Task<bool> CheckCloudConnectivityAsync();
    void SetJSRuntime(Microsoft.JSInterop.IJSRuntime jsRuntime);
    Task StartMonitoringAsync();
    Task StopMonitoringAsync();
}

public class ConnectivityService : IConnectivityService, IDisposable
{
    private bool _isConnected = true;
    private bool _isMonitoring;
    private IJSRuntime? _jsRuntime;
    private DotNetObjectReference<ConnectivityService>? _dotNetRef;
    private readonly object _lockObject = new object();
    private ISyncStatusService? _syncStatusService;
    private IDatabaseSyncService? _syncService;
    private IServiceScopeFactory? _serviceScopeFactory;
    private System.Threading.CancellationTokenSource? _pollingCancellationTokenSource;
    private System.Threading.Timer? _pollingTimer;
    private const int PollingIntervalSeconds = 5; // Check connectivity every 5 seconds

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            lock (_lockObject)
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    ConnectivityChanged?.Invoke(this, value);
                }
            }
        }
    }

    public bool HasShownInitialToast { get; set; } = false;

    public event EventHandler<bool>? ConnectivityChanged;

    public ConnectivityService(IJSRuntime? jsRuntime = null)
    {
        _jsRuntime = jsRuntime;
        _isConnected = true;
    }

    // Set sync services for integration
    public void SetSyncServices(ISyncStatusService? syncStatusService, IDatabaseSyncService? syncService)
    {
        _syncStatusService = syncStatusService;
        _syncService = syncService;
    }

    // Set service scope factory for background operations
    public void SetServiceScopeFactory(IServiceScopeFactory? serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    // Set JS Runtime (called from components that have access to it)
    public void SetJSRuntime(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<bool> CheckConnectivityAsync()
    {
        if (_jsRuntime == null)
        {
            // Fallback: assume connected if JS runtime is not available
            IsConnected = true;
            _syncStatusService?.SetOnline(true);
            return true;
        }

        try
        {
            // Check navigator.onLine via JavaScript (fast check)
            var isOnline = await _jsRuntime.InvokeAsync<bool>("window.connectivityMonitor.checkConnectivity");
            
            // If navigator says offline, we're definitely offline
            if (!isOnline)
            {
                IsConnected = false;
                _syncStatusService?.SetOnline(false);
                return false;
            }
            
            // If navigator says online, verify with actual cloud connection test (with timeout)
            var cloudOnline = await CheckCloudConnectivityWithTimeoutAsync();
            var finalStatus = isOnline && cloudOnline;
            
            // Only update if status actually changed (property setter handles event firing)
            IsConnected = finalStatus;
            _syncStatusService?.SetOnline(finalStatus);
            return finalStatus;
        }
        catch (Exception)
        {
            // If JavaScript interop fails, assume online to avoid blocking the app
            IsConnected = true;
            _syncStatusService?.SetOnline(true);
            return true;
        }
    }

    public async Task<bool> CheckCloudConnectivityAsync()
    {
        if (_syncService == null)
            return true; // Assume online if sync service not available

        try
        {
            return await _syncService.TestCloudConnectionAsync();
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckCloudConnectivityWithTimeoutAsync()
    {
        if (_syncService == null)
            return true; // Assume online if sync service not available

        try
        {
            // Use a timeout to make the check faster and more responsive
            // If cloud check takes too long, consider it offline
            var cloudCheckTask = _syncService.TestCloudConnectionAsync();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(3));
            
            var completedTask = await Task.WhenAny(cloudCheckTask, timeoutTask);
            
            if (completedTask == cloudCheckTask)
            {
                // Cloud check completed before timeout
                return await cloudCheckTask;
            }
            
            // Timeout occurred - assume offline if cloud check is too slow
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task StartMonitoringAsync()
    {
        if (_isMonitoring) return;
        
        if (_jsRuntime == null)
        {
            // Cannot start monitoring without JS runtime
            return;
        }

        lock (_lockObject)
        {
            if (_isMonitoring) return;
            _isMonitoring = true;
        }

        try
        {
            // Create DotNetObjectReference for JavaScript callbacks
            _dotNetRef = DotNetObjectReference.Create(this);

            // Initialize the JavaScript connectivity monitor
            await _jsRuntime.InvokeVoidAsync("window.connectivityMonitor.initialize", _dotNetRef);

            // Perform initial connectivity check
            await CheckConnectivityAsync();

            // Start periodic polling for real-time connectivity detection
            StartPeriodicPolling();
        }
        catch (Exception)
        {
            // If initialization fails, stop monitoring and fallback to default behavior
            lock (_lockObject)
            {
                _isMonitoring = false;
            }
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
    }

    private void StartPeriodicPolling()
    {
        // Stop any existing polling
        StopPeriodicPolling();

        // Create cancellation token source for polling
        _pollingCancellationTokenSource = new System.Threading.CancellationTokenSource();

        // Start periodic polling using Timer
        // Timer callback is synchronous, fire-and-forget async operation
        _pollingTimer = new System.Threading.Timer(_ =>
        {
            if (_pollingCancellationTokenSource?.Token.IsCancellationRequested == true)
                return;

            // Fire and forget async operation
            _ = Task.Run(async () =>
            {
                try
                {
                    // Perform connectivity check
                    await CheckConnectivityAsync();
                }
                catch
                {
                    // Ignore errors in polling - don't break monitoring
                }
            });
        }, null, TimeSpan.FromSeconds(PollingIntervalSeconds), TimeSpan.FromSeconds(PollingIntervalSeconds));
    }

    private void StopPeriodicPolling()
    {
        _pollingCancellationTokenSource?.Cancel();
        _pollingCancellationTokenSource?.Dispose();
        _pollingCancellationTokenSource = null;

        _pollingTimer?.Dispose();
        _pollingTimer = null;
    }

    // Legacy method for backward compatibility - calls async version
    public void StartMonitoring()
    {
        _ = StartMonitoringAsync();
    }

    public async Task StopMonitoringAsync()
    {
        if (!_isMonitoring) return;

        lock (_lockObject)
        {
            if (!_isMonitoring) return;
            _isMonitoring = false;
        }

        // Stop periodic polling
        StopPeriodicPolling();

        try
        {
            if (_jsRuntime != null)
            {
                await _jsRuntime.InvokeVoidAsync("window.connectivityMonitor.dispose");
            }
        }
        catch (Exception)
        {
            // Ignore errors during cleanup
        }
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
    }

    // Legacy method for backward compatibility - calls async version
    public void StopMonitoring()
    {
        _ = StopMonitoringAsync();
    }

    [JSInvokable]
    public async void OnConnectivityChanged(bool isOnline)
    {
        // Called from JavaScript when connectivity changes
        try
        {
            // If navigator says offline, we're definitely offline (no need to check cloud)
            if (!isOnline)
            {
                IsConnected = false;
                _syncStatusService?.SetOnline(false);
                return;
            }

            // If navigator says online, verify with actual cloud connection (with timeout)
            var cloudOnline = await CheckCloudConnectivityWithTimeoutAsync();
            var finalStatus = isOnline && cloudOnline;
            
            // Only update if status actually changed (property setter handles event firing)
            IsConnected = finalStatus;
            _syncStatusService?.SetOnline(finalStatus);

            // If we just came back online, trigger a sync using IServiceScopeFactory
            if (finalStatus && _serviceScopeFactory != null)
            {
                // Trigger sync in background with new scope to prevent concurrency errors
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(2000); // Wait 2 seconds for connection to stabilize
                        
                        // Create new scope for sync operation
                        using var scope = _serviceScopeFactory.CreateScope();
                        var syncService = scope.ServiceProvider.GetRequiredService<IDatabaseSyncService>();
                        await syncService.FullSyncAsync();
                    }
                    catch
                    {
                        // Ignore errors in background sync
                    }
                });
            }
        }
        catch
        {
            // Ignore errors - don't break connectivity monitoring
        }
    }

    public void Dispose()
    {
        StopPeriodicPolling();
        StopMonitoring();
    }
}

