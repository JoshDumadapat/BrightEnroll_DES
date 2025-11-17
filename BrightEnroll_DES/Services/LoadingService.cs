namespace BrightEnroll_DES.Services;

/// <summary>
/// Service to manage application loading state
/// </summary>
public interface ILoadingService
{
    bool IsLoading { get; }
    string LoadingMessage { get; }
    event Action? OnLoadingStateChanged;
    void ShowLoading(string? message = null);
    void HideLoading();
}

public class LoadingService : ILoadingService
{
    private bool _isLoading = false;
    private string _loadingMessage = "Loading...";

    public bool IsLoading => _isLoading;
    public string LoadingMessage => _loadingMessage;
    
    public event Action? OnLoadingStateChanged;

    public void ShowLoading(string? message = null)
    {
        _isLoading = true;
        _loadingMessage = message ?? "Loading...";
        OnLoadingStateChanged?.Invoke();
    }

    public void HideLoading()
    {
        _isLoading = false;
        _loadingMessage = "Loading...";
        OnLoadingStateChanged?.Invoke();
    }
}

