namespace BrightEnroll_DES.Services.Infrastructure;

/// <summary>
/// Service for showing progress indicators during long-running operations
/// </summary>
public interface IProgressService
{
    bool IsProgressVisible { get; }
    string CurrentMessage { get; }
    int CurrentPercentage { get; }
    event Action? OnProgressChanged;
    
    void ShowProgress(string message, int percentage = 0);
    void UpdateProgress(int percentage, string? message = null);
    void HideProgress();
}

public class ProgressService : IProgressService
{
    private bool _isProgressVisible = false;
    private string _currentMessage = "";
    private int _currentPercentage = 0;

    public bool IsProgressVisible => _isProgressVisible;
    public string CurrentMessage => _currentMessage;
    public int CurrentPercentage => _currentPercentage;
    
    public event Action? OnProgressChanged;

    public void ShowProgress(string message, int percentage = 0)
    {
        _isProgressVisible = true;
        _currentMessage = message;
        _currentPercentage = Math.Clamp(percentage, 0, 100);
        OnProgressChanged?.Invoke();
    }

    public void UpdateProgress(int percentage, string? message = null)
    {
        _currentPercentage = Math.Clamp(percentage, 0, 100);
        if (!string.IsNullOrWhiteSpace(message))
        {
            _currentMessage = message;
        }
        OnProgressChanged?.Invoke();
    }

    public void HideProgress()
    {
        _isProgressVisible = false;
        _currentMessage = "";
        _currentPercentage = 0;
        OnProgressChanged?.Invoke();
    }
}
