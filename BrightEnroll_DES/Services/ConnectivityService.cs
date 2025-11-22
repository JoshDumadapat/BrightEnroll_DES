using Microsoft.Maui.Networking;
using System;
using System.Threading.Tasks;

namespace BrightEnroll_DES.Services;

public interface IConnectivityService
{
    bool IsConnected { get; }
    bool HasShownInitialToast { get; set; }
    event EventHandler<bool> ConnectivityChanged;
    Task<bool> CheckConnectivityAsync();
    void StartMonitoring();
    void StopMonitoring();
}

public class ConnectivityService : IConnectivityService
{
    private bool _isConnected;
    private bool _isMonitoring;

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (_isConnected != value)
            {
                _isConnected = value;
                ConnectivityChanged?.Invoke(this, value);
            }
        }
    }

    public bool HasShownInitialToast { get; set; } = false;

    public event EventHandler<bool>? ConnectivityChanged;

    public ConnectivityService()
    {
        _isConnected = CheckCurrentConnectivity();
    }

    public Task<bool> CheckConnectivityAsync()
    {
        try
        {
            var networkAccess = Connectivity.Current.NetworkAccess;
            var isConnected = networkAccess == NetworkAccess.Internet;
            IsConnected = isConnected;
            return Task.FromResult(isConnected);
        }
        catch
        {
            IsConnected = false;
            return Task.FromResult(false);
        }
    }

    private bool CheckCurrentConnectivity()
    {
        try
        {
            var networkAccess = Connectivity.Current.NetworkAccess;
            return networkAccess == NetworkAccess.Internet;
        }
        catch
        {
            return false;
        }
    }

    public void StartMonitoring()
    {
        if (_isMonitoring) return;

        _isMonitoring = true;
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;

        // Initial check
        _ = Task.Run(async () =>
        {
            await CheckConnectivityAsync();
        });
    }

    public void StopMonitoring()
    {
        if (!_isMonitoring) return;

        _isMonitoring = false;
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            await CheckConnectivityAsync();
        });
    }
}

