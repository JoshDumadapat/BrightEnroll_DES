# EF Core Concurrency Error Analysis
## "A second operation was started on this context instance before a previous operation completed"

---

## ROOT CAUSE SUMMARY

The error occurs because **multiple threads are trying to use the same DbContext instance simultaneously**. EF Core DbContext is **NOT thread-safe** and must only be used by one thread at a time.

**Primary Issues:**
1. **Background tasks (Task.Run) using injected services with DbContext** - Most critical
2. **Blocking async calls (.Result, .GetAwaiter().GetResult())** - Causes deadlocks and concurrency issues
3. **async void methods** - Can't be awaited, causing overlapping operations
4. **BackgroundService using injected DbContext** - Needs IServiceScopeFactory
5. **Timers accessing DbContext** - May cause concurrent access

---

## CRITICAL ISSUES FOUND

### 🔴 **ISSUE #1: MainLayout.razor - Background Task Using Injected NotificationService**
**File:** `Components/Layout/MainLayout.razor`  
**Lines:** 196-214

**Problem:**
```csharp
_ = Task.Run(async () =>
{
    while (!_notificationRefreshTokenSource.Token.IsCancellationRequested && !_isDisposed)
    {
        await Task.Delay(3000, _notificationRefreshTokenSource.Token);
        
        if (!_isDisposed)
        {
            await InvokeAsync(async () =>
            {
                if (!_isDisposed)
                {
                    await LoadNotificationCount(); // Uses NotificationService with DbContext
                    StateHasChanged();
                }
            });
        }
    }
}, _notificationRefreshTokenSource.Token);
```

**Why This Causes Error:**
- `NotificationService` is injected (Scoped) and contains `AppDbContext`
- `Task.Run` creates a background thread
- The same `NotificationService` instance (with its DbContext) is used from:
  - The UI thread (initial call to `LoadNotificationCount()`)
  - The background thread (Task.Run loop)
- When both try to query simultaneously → **concurrency error**

**Fix Required:**
Use `IServiceScopeFactory` to create a new scope for background operations:
```csharp
@inject IServiceScopeFactory ServiceScopeFactory

private async Task LoadNotificationCount()
{
    if (_isDisposed) return;
    
    try
    {
        using var scope = ServiceScopeFactory.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
        notificationCount = await notificationService.GetUnreadCountAsync();
    }
    catch (Exception) { }
}
```

---

### 🔴 **ISSUE #2: NotificationDropdown.razor - Background Task Using Injected NotificationService**
**File:** `Components/Shared/NotificationDropdown.razor`  
**Lines:** 144-161

**Problem:**
```csharp
_ = Task.Run(async () =>
{
    while (!_cancellationTokenSource.Token.IsCancellationRequested && !_isDisposed)
    {
        await Task.Delay(3000, _cancellationTokenSource.Token);
        
        if (isOpen && hasDataLoaded && !isLoading && !_isLoadingInProgress && !_isDisposed)
        {
            await InvokeAsync(async () =>
            {
                if (!_isDisposed && isOpen && hasDataLoaded && !isLoading && !_isLoadingInProgress)
                {
                    await LoadNotificationsInternal(silent: true); // Uses NotificationService
                }
            });
        }
    }
}, _cancellationTokenSource.Token);
```

**Why This Causes Error:**
- Same issue as MainLayout - background thread using injected service with DbContext
- Can overlap with UI thread operations on the same DbContext

**Fix Required:**
Use `IServiceScopeFactory` to create new scope in background task:
```csharp
@inject IServiceScopeFactory ServiceScopeFactory

private async Task LoadNotificationsInternal(bool silent = false)
{
    // ... existing semaphore logic ...
    
    try
    {
        using var scope = ServiceScopeFactory.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
        var allNotifications = await notificationService.GetAllNotificationsAsync(limit: 5);
        // ... rest of logic ...
    }
    catch (Exception) { }
}
```

---

### 🔴 **ISSUE #3: ConnectivityService - async void and Task.Run with DbContext**
**File:** `Services/Infrastructure/ConnectivityService.cs`  
**Lines:** 115, 154, 183, 199-203

**Problem:**
```csharp
public async void StartMonitoring() // ❌ async void
public async void StopMonitoring() // ❌ async void
public async void OnConnectivityChanged(bool isOnline) // ❌ async void
{
    // ...
    _ = Task.Run(async () =>
    {
        await Task.Delay(2000);
        await _syncService.FullSyncAsync(); // Uses DbContext
    });
}
```

**Why This Causes Error:**
1. `async void` methods can't be awaited, causing overlapping operations
2. `Task.Run` uses `_syncService` which likely has DbContext
3. Multiple connectivity changes can trigger multiple syncs simultaneously

**Fix Required:**
1. Change `async void` to `async Task`
2. Use `IServiceScopeFactory` in Task.Run:
```csharp
private readonly IServiceScopeFactory _serviceScopeFactory;

public async Task OnConnectivityChanged(bool isOnline) // ✅ async Task
{
    // ...
    if (finalStatus && _serviceScopeFactory != null)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);
            using var scope = _serviceScopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IDatabaseSyncService>();
            await syncService.FullSyncAsync();
        });
    }
}
```

---

### 🔴 **ISSUE #4: SchoolYearService - Blocking Async Calls**
**File:** `Services/Business/Academic/SchoolYearService.cs`  
**Lines:** 305, 318, 331, 402

**Problem:**
```csharp
public List<string> GetAvailableSchoolYears()
{
    return Task.Run(async () => await GetAvailableSchoolYearsAsync()).GetAwaiter().GetResult();
}

public bool AddSchoolYear(string schoolYear)
{
    return Task.Run(async () => await AddSchoolYearAsync(schoolYear)).GetAwaiter().GetResult();
}

public bool RemoveSchoolYear(string schoolYear)
{
    return Task.Run(async () => await RemoveSchoolYearAsync(schoolYear)).GetAwaiter().GetResult();
}

public void RemoveFinishedSchoolYears()
{
    Task.Run(async () => await RemoveFinishedSchoolYearsAsync()).GetAwaiter().GetResult();
}
```

**Why This Causes Error:**
- `.GetAwaiter().GetResult()` blocks the current thread
- `Task.Run` creates a new thread
- Both threads may try to use the same DbContext instance
- Can cause deadlocks and concurrency errors

**Fix Required:**
Remove these synchronous wrappers. If synchronous methods are needed, they should NOT use DbContext directly. Instead:
1. Make all callers use async methods
2. OR create a new scope for each call:
```csharp
// DON'T DO THIS - Remove these methods entirely
// Force callers to use async versions
```

---

### 🔴 **ISSUE #5: OfflineQueueService - Blocking Async Call**
**File:** `Services/Database/Sync/OfflineQueueService.cs`  
**Line:** 191

**Problem:**
```csharp
public int GetPendingCount()
{
    return GetPendingOperationsAsync().Result.Count(o => !o.IsProcessed);
}
```

**Why This Causes Error:**
- `.Result` blocks the thread waiting for async operation
- If called from multiple threads, can cause concurrent access to DbContext

**Fix Required:**
```csharp
public async Task<int> GetPendingCountAsync()
{
    var operations = await GetPendingOperationsAsync();
    return operations.Count(o => !o.IsProcessed);
}

// If synchronous version is absolutely needed:
public int GetPendingCount()
{
    // Use ConfigureAwait(false) and handle properly
    // BUT BETTER: Remove and make all callers async
    return GetPendingCountAsync().GetAwaiter().GetResult();
}
```

---

### 🔴 **ISSUE #6: AutoSyncScheduler - BackgroundService Using Injected Scoped Service**
**File:** `Services/Database/Sync/AutoSyncScheduler.cs`  
**Lines:** 23-35, 54

**Problem:**
```csharp
public class AutoSyncScheduler : BackgroundService
{
    private readonly IDatabaseSyncService _syncService; // Injected, contains DbContext
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ...
        await PerformSyncAsync(); // Uses _syncService with DbContext
    }
}
```

**Registration Issue (MauiProgram.cs Line 140, 145):**
```csharp
builder.Services.AddScoped<IDatabaseSyncService, DatabaseSyncService>(); // Scoped
builder.Services.AddHostedService<AutoSyncScheduler>(); // Singleton (BackgroundService)
```

**Why This Causes Error:**
- `BackgroundService` is a **Singleton** (lives for app lifetime)
- `IDatabaseSyncService` is registered as **Scoped**
- When Singleton injects Scoped service, the Scoped service becomes effectively Singleton
- The `DatabaseSyncService` gets a DbContext instance that lives for the app lifetime
- Background thread uses this long-lived DbContext
- Request threads also try to use DbContext
- Multiple threads using same DbContext instance → **concurrency error**

**Fix Required:**
Use `IServiceScopeFactory` to create new scope for each operation:
```csharp
public class AutoSyncScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    
    public AutoSyncScheduler(IServiceScopeFactory serviceScopeFactory, ...)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }
    
    private async Task PerformSyncAsync()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<IDatabaseSyncService>();
        var result = await syncService.FullSyncAsync();
        // ...
    }
}
```

---

### 🟡 **ISSUE #7: Timers Potentially Accessing DbContext**
**Files:**
- `Components/Pages/Auth/StudentRegistration/StudentRegistration.razor.cs` (Line 100)
- `Components/Pages/Admin/Settings/SchoolYearManagement.razor` (Line 309)
- `Components/ToastNotification.razor` (Line 183)
- `Components/Shared/SyncStatus.razor` (Line 98)
- `Components/ConnectivityToast.razor` (Line 77)

**Problem:**
Timers may call methods that use DbContext. If timer fires while another operation is using the same context → error.

**Fix Required:**
Ensure timers use `IServiceScopeFactory` if they access DbContext, or ensure they don't overlap with other operations.

---

## DbContext Registration Check

**File:** `MauiProgram.cs`  
**Line:** 74

**Status:** ✅ **CORRECT**
```csharp
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(localConnectionString);
});
```

DbContext is registered as **Scoped** (default), which is correct. The issue is not the registration, but how it's being used.

---

## SUMMARY OF FIXES NEEDED

### Priority 1 (Critical - Must Fix Immediately):

1. **MainLayout.razor** - Use `IServiceScopeFactory` in background task
2. **NotificationDropdown.razor** - Use `IServiceScopeFactory` in background task
3. **ConnectivityService.cs** - Change `async void` to `async Task`, use `IServiceScopeFactory` in Task.Run
4. **SchoolYearService.cs** - Remove blocking synchronous wrappers (or make them create new scopes)
5. **OfflineQueueService.cs** - Remove `.Result` blocking call
6. **AutoSyncScheduler.cs** - Use `IServiceScopeFactory` instead of injected service

### Priority 2 (Important):

7. Review all timers to ensure they don't cause concurrent DbContext access
8. Check for any other `Task.Run` usage with DbContext-dependent services

---

## PERMANENT FIX STRATEGY

**The root cause:** Background threads and blocking calls are using the same DbContext instance that's meant for request-scoped operations.

**The solution:**
1. **Never use injected DbContext in background tasks** - Always use `IServiceScopeFactory.CreateScope()`
2. **Remove all blocking async calls** - Use async/await throughout
3. **Change async void to async Task** - Allows proper awaiting
4. **BackgroundService must use IServiceScopeFactory** - Never inject Scoped services directly

**Pattern to follow:**
```csharp
// ❌ WRONG - Background task using injected service
_ = Task.Run(async () =>
{
    await injectedService.DoSomethingAsync(); // Uses DbContext
});

// ✅ CORRECT - Background task creating new scope
@inject IServiceScopeFactory ServiceScopeFactory

_ = Task.Run(async () =>
{
    using var scope = ServiceScopeFactory.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<MyService>();
    await service.DoSomethingAsync(); // Uses new DbContext
});
```

---

## FILES TO FIX

### Priority 1 (Critical - Causes Concurrency Errors):

1. **`Components/Layout/MainLayout.razor`**
   - Lines 196-214: Background Task.Run using NotificationService
   - Lines 232-250: LoadNotificationCount using injected service
   - **Fix:** Inject `IServiceScopeFactory`, create scope in background task

2. **`Components/Shared/NotificationDropdown.razor`**
   - Lines 144-161: Background Task.Run using NotificationService
   - Lines 229-275: LoadNotificationsInternal using injected service
   - **Fix:** Inject `IServiceScopeFactory`, create scope in background task

3. **`Services/Infrastructure/ConnectivityService.cs`**
   - Lines 115, 154: `async void` methods
   - Line 183: `async void OnConnectivityChanged`
   - Lines 199-203: Task.Run using _syncService (has DbContext)
   - **Fix:** Change to `async Task`, use `IServiceScopeFactory` in Task.Run

4. **`Services/Database/Sync/AutoSyncScheduler.cs`**
   - Entire file: BackgroundService injecting Scoped service
   - **Fix:** Inject `IServiceScopeFactory` instead, create scope for each sync

5. **`Services/Business/Academic/SchoolYearService.cs`**
   - Lines 300-311: `GetAvailableSchoolYears()` - blocking call
   - Lines 313-324: `AddSchoolYear()` - blocking call
   - Lines 326-337: `RemoveSchoolYear()` - blocking call
   - Lines 397-408: `RemoveFinishedSchoolYears()` - blocking call
   - **Fix:** Remove these synchronous wrappers, force callers to use async

6. **`Services/Database/Sync/OfflineQueueService.cs`**
   - Line 188-192: `GetPendingCount()` - blocking `.Result` call
   - **Fix:** Make async, remove synchronous wrapper

### Priority 2 (May Cause Issues):

7. **Timer Components** - Review for potential concurrent access:
   - `Components/Pages/Auth/StudentRegistration/StudentRegistration.razor.cs` (Line 100)
   - `Components/Pages/Admin/Settings/SchoolYearManagement.razor` (Line 309)
   - `Components/ToastNotification.razor` (Line 183)
   - `Components/Shared/SyncStatus.razor` (Line 98)
   - `Components/ConnectivityToast.razor` (Line 77)
   - **Note:** These may be fine if they don't access DbContext, but should be reviewed

---

*This analysis identifies all root causes of the EF Core concurrency error. Fixing these issues will permanently resolve the problem.*

