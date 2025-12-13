using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;

namespace BrightEnroll_DES.Services.Database.Sync;

// Service to track and expose sync status to UI components
public interface ISyncStatusService
{
    bool IsOnline { get; }
    bool IsSyncing { get; }
    DateTime? LastSyncTime { get; }
    int PendingOperationsCount { get; }
    List<string> Errors { get; }
    event EventHandler<SyncStatusChangedEventArgs> StatusChanged;
    void SetOnline(bool isOnline);
    void SetSyncing(bool isSyncing);
    void UpdateLastSyncTime(DateTime time);
    void AddError(string error);
    void ClearErrors();
    void UpdatePendingCount(int count);
}

public class SyncStatusChangedEventArgs : EventArgs
{
    public bool IsOnline { get; set; }
    public bool IsSyncing { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public int PendingOperationsCount { get; set; }
}

public class SyncStatusService : ISyncStatusService
{
    private bool _isOnline = true;
    private bool _isSyncing = false;
    private DateTime? _lastSyncTime;
    private int _pendingOperationsCount = 0;
    private readonly ConcurrentBag<string> _errors = new();
    private readonly object _lock = new object();
    private readonly IServiceProvider _serviceProvider;

    public bool IsOnline
    {
        get { lock (_lock) { return _isOnline; } }
    }

    public bool IsSyncing
    {
        get { lock (_lock) { return _isSyncing; } }
    }

    private bool _lastSyncTimeLoaded = false;

    public DateTime? LastSyncTime
    {
        get 
        { 
            lock (_lock) 
            { 
                // If not loaded yet, try to load from database (async, non-blocking)
                if (!_lastSyncTimeLoaded && _lastSyncTime == null)
                {
                    _lastSyncTimeLoaded = true; // Mark as loading to prevent multiple attempts
                    Task.Run(() => LoadLastSyncTimeFromDatabase());
                }
                return _lastSyncTime; 
            } 
        }
    }

    public SyncStatusService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        // Load last sync time from database on initialization (async, non-blocking)
        Task.Run(() => LoadLastSyncTimeFromDatabase());
    }

    private void LoadLastSyncTimeFromDatabase()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Get the single sync history record (should only be one)
            var lastSync = context.SyncHistories
                .OrderByDescending(s => s.SyncTime)
                .FirstOrDefault();
            
            if (lastSync != null && lastSync.Status == "Success")
            {
                lock (_lock)
                {
                    _lastSyncTime = lastSync.SyncTime;
                    _lastSyncTimeLoaded = true;
                    NotifyStatusChanged();
                }
            }
            else
            {
                lock (_lock)
                {
                    _lastSyncTimeLoaded = true;
                }
            }
        }
        catch
        {
            // If database is not available or table doesn't exist yet, keep null
            lock (_lock)
            {
                _lastSyncTimeLoaded = true;
            }
        }
    }

    public int PendingOperationsCount
    {
        get { lock (_lock) { return _pendingOperationsCount; } }
    }

    public List<string> Errors
    {
        get
        {
            lock (_lock)
            {
                return _errors.ToList();
            }
        }
    }

    public event EventHandler<SyncStatusChangedEventArgs>? StatusChanged;

    public void SetOnline(bool isOnline)
    {
        lock (_lock)
        {
            if (_isOnline != isOnline)
            {
                _isOnline = isOnline;
                NotifyStatusChanged();
            }
        }
    }

    public void SetSyncing(bool isSyncing)
    {
        lock (_lock)
        {
            if (_isSyncing != isSyncing)
            {
                _isSyncing = isSyncing;
                NotifyStatusChanged();
            }
        }
    }

    public void UpdateLastSyncTime(DateTime time)
    {
        lock (_lock)
        {
            _lastSyncTime = time;
            
            // Persist to database asynchronously
            Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    
                    // Check if table exists before trying to save
                    var tableExists = await context.Database.CanConnectAsync();
                    if (tableExists)
                    {
                        // The sync history will be saved by DatabaseSyncService after sync completes
                        // This just updates the in-memory cache
                    }
                }
                catch
                {
                    // Silently fail - database might not be ready yet
                }
            });
            
            NotifyStatusChanged();
        }
    }

    public void AddError(string error)
    {
        lock (_lock)
        {
            _errors.Add(error);
            // Keep only last 10 errors
            if (_errors.Count > 10)
            {
                var itemsToRemove = _errors.Take(_errors.Count - 10).ToList();
                foreach (var item in itemsToRemove)
                {
                    _errors.TryTake(out _);
                }
            }
            NotifyStatusChanged();
        }
    }

    public void ClearErrors()
    {
        lock (_lock)
        {
            while (_errors.TryTake(out _)) { }
            NotifyStatusChanged();
        }
    }

    public void UpdatePendingCount(int count)
    {
        lock (_lock)
        {
            if (_pendingOperationsCount != count)
            {
                _pendingOperationsCount = count;
                NotifyStatusChanged();
            }
        }
    }

    private void NotifyStatusChanged()
    {
        var args = new SyncStatusChangedEventArgs
        {
            IsOnline = _isOnline,
            IsSyncing = _isSyncing,
            LastSyncTime = _lastSyncTime,
            PendingOperationsCount = _pendingOperationsCount
        };

        StatusChanged?.Invoke(this, args);
    }
}

