using Microsoft.JSInterop;
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
            return true;
        }

        try
        {
            // Check navigator.onLine via JavaScript
            var isOnline = await _jsRuntime.InvokeAsync<bool>("window.connectivityMonitor.checkConnectivity");
            IsConnected = isOnline;
            return isOnline;
        }
        catch (Exception)
        {
            // If JavaScript interop fails, assume online to avoid blocking the app
            IsConnected = true;
            return true;
        }
    }

    public async void StartMonitoring()
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

    public async void StopMonitoring()
    {
        if (!_isMonitoring) return;

        lock (_lockObject)
        {
            if (!_isMonitoring) return;
            _isMonitoring = false;
        }

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

    [JSInvokable]
    public void OnConnectivityChanged(bool isOnline)
    {
        // This method is called from JavaScript when connectivity changes
        IsConnected = isOnline;
    }

    public void Dispose()
    {
        StopMonitoring();
    }
}

