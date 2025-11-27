using Microsoft.JSInterop;
using Microsoft.Maui.Networking;
using System.Runtime.InteropServices;

namespace BrightEnroll_DES.Services.Infrastructure;

public interface IConnectivityService
{
    bool IsConnected { get; }
    bool HasShownInitialToast { get; set; }
    event EventHandler<bool> ConnectivityChanged;
    Task<bool> CheckConnectivityAsync();
    void SetJSRuntime(Microsoft.JSInterop.IJSRuntime jsRuntime);
    void StartMonitoring();
    void StopMonitoring();
}

public class ConnectivityService : IConnectivityService, IDisposable
{
    private bool _isConnected = true;
    private bool _isMonitoring;
    private IJSRuntime? _jsRuntime;
    private DotNetObjectReference<ConnectivityService>? _dotNetRef;
    private readonly object _lockObject = new object();

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            lock (_lockObject)
            {
                if (_isConnected != value)
                {
                    var oldValue = _isConnected;
                    _isConnected = value;
                    System.Diagnostics.Debug.WriteLine($"[ConnectivityService] Connectivity changed: {oldValue} -> {value}");
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
        // Initialize with current connectivity status using MAUI's native API
        try
        {
            var networkAccess = Connectivity.Current.NetworkAccess;
            _isConnected = networkAccess == NetworkAccess.Internet;
            System.Diagnostics.Debug.WriteLine($"[ConnectivityService] Initial connectivity: {_isConnected} (NetworkAccess: {networkAccess})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConnectivityService] Error checking initial connectivity: {ex.Message}");
            _isConnected = true; // Default to connected
        }
    }

    // Set JS Runtime (called from components that have access to it)
    public void SetJSRuntime(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<bool> CheckConnectivityAsync()
    {
        try
        {
            // Use MAUI's native Connectivity API (most reliable)
            var networkAccess = Connectivity.Current.NetworkAccess;
            var isOnline = networkAccess == NetworkAccess.Internet;
            
            IsConnected = isOnline;
            System.Diagnostics.Debug.WriteLine($"[ConnectivityService] CheckConnectivityAsync: {isOnline} (NetworkAccess: {networkAccess})");
            return isOnline;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConnectivityService] Error in CheckConnectivityAsync: {ex.Message}");
            // Fallback to JavaScript if available
            if (_jsRuntime != null)
            {
                try
                {
                    var isOnline = await _jsRuntime.InvokeAsync<bool>("window.connectivityMonitor.checkConnectivity");
                    IsConnected = isOnline;
                    return isOnline;
                }
                catch
                {
                    // If both fail, assume online to avoid blocking the app
                    IsConnected = true;
                    return true;
                }
            }
            // If JavaScript interop fails, assume online to avoid blocking the app
            IsConnected = true;
            return true;
        }
    }

    public async void StartMonitoring()
    {
        if (_isMonitoring) return;

        lock (_lockObject)
        {
            if (_isMonitoring) return;
            _isMonitoring = true;
        }

        System.Diagnostics.Debug.WriteLine("[ConnectivityService] Starting connectivity monitoring...");

        try
        {
            // Subscribe to MAUI's native Connectivity.ConnectivityChanged event
            Connectivity.ConnectivityChanged += OnMauiConnectivityChanged;
            
            // Also set up JavaScript monitoring as backup (if JS runtime is available)
            if (_jsRuntime != null)
            {
                try
                {
                    _dotNetRef = DotNetObjectReference.Create(this);
                    await _jsRuntime.InvokeVoidAsync("window.connectivityMonitor.initialize", _dotNetRef);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ConnectivityService] JavaScript monitoring setup failed: {ex.Message}");
                    // Continue with native monitoring only
                }
            }

            // Perform initial connectivity check
            await CheckConnectivityAsync();
            
            System.Diagnostics.Debug.WriteLine("[ConnectivityService] Connectivity monitoring started successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConnectivityService] Error starting monitoring: {ex.Message}");
            // If initialization fails, stop monitoring and fallback to default behavior
            lock (_lockObject)
            {
                _isMonitoring = false;
            }
            Connectivity.ConnectivityChanged -= OnMauiConnectivityChanged;
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
    }

    private void OnMauiConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        try
        {
            var isOnline = e.NetworkAccess == NetworkAccess.Internet;
            System.Diagnostics.Debug.WriteLine($"[ConnectivityService] MAUI ConnectivityChanged event: {isOnline} (NetworkAccess: {e.NetworkAccess})");
            IsConnected = isOnline;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConnectivityService] Error handling MAUI connectivity change: {ex.Message}");
        }
    }

    public async void StopMonitoring()
    {
        if (!_isMonitoring) return;

        lock (_lockObject)
        {
            if (!_isMonitoring) return;
            _isMonitoring = false;
        }

        System.Diagnostics.Debug.WriteLine("[ConnectivityService] Stopping connectivity monitoring...");

        try
        {
            // Unsubscribe from MAUI's native Connectivity.ConnectivityChanged event
            Connectivity.ConnectivityChanged -= OnMauiConnectivityChanged;
            
            if (_jsRuntime != null)
            {
                try
                {
                    await _jsRuntime.InvokeVoidAsync("window.connectivityMonitor.dispose");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ConnectivityService] Error disposing JavaScript monitor: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConnectivityService] Error stopping monitoring: {ex.Message}");
        }
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
    }

    [JSInvokable]
    public void OnConnectivityChanged(bool isOnline)
    {
        // This method is called from JavaScript when connectivity changes (backup method)
        System.Diagnostics.Debug.WriteLine($"[ConnectivityService] JavaScript connectivity changed: {isOnline}");
        IsConnected = isOnline;
    }

    public void Dispose()
    {
        StopMonitoring();
    }
}

