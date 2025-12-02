using System.Collections.Concurrent;

namespace BrightEnroll_DES.Services.Database.Sync;

/// <summary>
/// Service to track and expose sync status to UI components.
/// </summary>
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

    public bool IsOnline
    {
        get { lock (_lock) { return _isOnline; } }
    }

    public bool IsSyncing
    {
        get { lock (_lock) { return _isSyncing; } }
    }

    public DateTime? LastSyncTime
    {
        get { lock (_lock) { return _lastSyncTime; } }
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

