namespace BrightEnroll_DES.Services.Infrastructure;

// Manages loading state for the app
public interface ILoadingService
{
    bool IsLoading { get; }
    string LoadingMessage { get; }
    event Action? OnLoadingStateChanged;
    void ShowLoading(string? message = null);
    void HideLoading();
    Task<T> LoadWithSmartLoadingAsync<T>(Func<Task<T>> loadAction, string? message = null, int delayThresholdMs = 250);
    Task LoadWithSmartLoadingAsync(Func<Task> loadAction, string? message = null, int delayThresholdMs = 250);
}

public class LoadingService : ILoadingService
{
    private bool _isLoading = false;
    private string _loadingMessage = "Loading...";
    private CancellationTokenSource? _delayedShowCancellation;
    private CancellationTokenSource? _hideLoadingCancellation;
    private DateTime? _lastStateChange;

    public bool IsLoading => _isLoading;
    public string LoadingMessage => _loadingMessage;
    
    public event Action? OnLoadingStateChanged;

    public void ShowLoading(string? message = null)
    {
        // Cancel any pending hide operation
        _hideLoadingCancellation?.Cancel();
        _hideLoadingCancellation?.Dispose();
        _hideLoadingCancellation = null;
        
        // Prevent rapid state changes (debounce)
        var now = DateTime.Now;
        if (_lastStateChange.HasValue && (now - _lastStateChange.Value).TotalMilliseconds < 50)
        {
            // Too soon after last change, skip to prevent stuttering
            return;
        }
        
        _isLoading = true;
        _loadingMessage = message ?? "Loading...";
        _lastStateChange = now;
        OnLoadingStateChanged?.Invoke();
    }

    public void HideLoading()
    {
        // Cancel any pending delayed show
        _delayedShowCancellation?.Cancel();
        _delayedShowCancellation?.Dispose();
        _delayedShowCancellation = null;
        
        // Cancel any pending hide operation (if ShowLoading was called again)
        _hideLoadingCancellation?.Cancel();
        _hideLoadingCancellation?.Dispose();
        _hideLoadingCancellation = null;
        
        // Prevent rapid state changes (debounce) - prevents stuttering
        var now = DateTime.Now;
        if (_lastStateChange.HasValue && (now - _lastStateChange.Value).TotalMilliseconds < 100)
        {
            // Too soon after last change, skip to prevent stuttering
            return;
        }
        
        _isLoading = false;
        _loadingMessage = "Loading...";
        _lastStateChange = now;
        OnLoadingStateChanged?.Invoke();
    }

    public async Task<T> LoadWithSmartLoadingAsync<T>(Func<Task<T>> loadAction, string? message = null, int delayThresholdMs = 250)
    {
        var startTime = DateTime.Now;
        var cts = new CancellationTokenSource();
        _delayedShowCancellation = cts;
        var loadingShown = false;

        // Start delayed loading screen show
        var delayedShowTask = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayThresholdMs, cts.Token);
                if (!cts.Token.IsCancellationRequested)
                {
                    loadingShown = true;
                    ShowLoading(message);
                }
            }
            catch (OperationCanceledException)
            {
                // Loading completed before threshold, don't show
            }
        });

        try
        {
            // Load data
            var result = await loadAction();
            
            // Cancel delayed show if it hasn't shown yet
            cts.Cancel();
            await delayedShowTask.ConfigureAwait(false);
            
            // If loading screen was shown, hide it
            if (loadingShown)
            {
                HideLoading();
            }
            
            return result;
        }
        catch
        {
            // Cancel delayed show on error
            cts.Cancel();
            await delayedShowTask.ConfigureAwait(false);
            
            // If loading screen was shown, hide it
            if (loadingShown)
            {
                HideLoading();
            }
            
            throw;
        }
        finally
        {
            cts.Dispose();
            if (_delayedShowCancellation == cts)
            {
                _delayedShowCancellation = null;
            }
        }
    }
    public async Task LoadWithSmartLoadingAsync(Func<Task> loadAction, string? message = null, int delayThresholdMs = 250)
    {
        var startTime = DateTime.Now;
        var cts = new CancellationTokenSource();
        _delayedShowCancellation = cts;
        var loadingShown = false;

        // Start delayed loading screen show
        var delayedShowTask = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayThresholdMs, cts.Token);
                if (!cts.Token.IsCancellationRequested)
                {
                    loadingShown = true;
                    ShowLoading(message);
                }
            }
            catch (OperationCanceledException)
            {
                // Loading completed before threshold, don't show
            }
        });

        try
        {
            // Load data
            await loadAction();
            
            // Cancel delayed show if it hasn't shown yet
            cts.Cancel();
            await delayedShowTask.ConfigureAwait(false);
            
            // If loading screen was shown, hide it
            if (loadingShown)
            {
                HideLoading();
            }
        }
        catch
        {
            // Cancel delayed show on error
            cts.Cancel();
            await delayedShowTask.ConfigureAwait(false);
            
            // If loading screen was shown, hide it
            if (loadingShown)
            {
                HideLoading();
            }
            
            throw;
        }
        finally
        {
            cts.Dispose();
            if (_delayedShowCancellation == cts)
            {
                _delayedShowCancellation = null;
            }
        }
    }
}

