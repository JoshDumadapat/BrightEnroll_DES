using Microsoft.JSInterop;
using System.Text.Json;

namespace BrightEnroll_DES.Components.Pages.Auth.Handlers;

public class RememberMeHandler
{
    private readonly IJSRuntime _jsRuntime;
    private const string STORAGE_KEY_EMAIL = "remembered_email";
    private const string STORAGE_KEY_PASSWORD = "remembered_password";
    private const string STORAGE_KEY_REMEMBER_ME = "remember_me_enabled";

    public RememberMeHandler(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }

    // Saves login credentials to browser storage when remember me is checked
    public async Task SaveCredentialsAsync(string emailOrSystemId, string password, bool rememberMe)
    {
        try
        {
            if (rememberMe && !string.IsNullOrWhiteSpace(emailOrSystemId) && !string.IsNullOrWhiteSpace(password))
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", STORAGE_KEY_EMAIL, emailOrSystemId);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", STORAGE_KEY_PASSWORD, password);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", STORAGE_KEY_REMEMBER_ME, "true");
            }
            else
            {
                // Clear saved credentials if remember me is unchecked
                await ClearCredentialsAsync();
            }
        }
        catch (Exception)
        {
            // If localStorage isn't available, just continue - don't break login
        }
    }

    // Loads saved credentials from browser storage
    public async Task<(string? emailOrSystemId, string? password, bool rememberMe)> LoadCredentialsAsync()
    {
        try
        {
            var rememberMeEnabled = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", STORAGE_KEY_REMEMBER_ME);
            
            if (rememberMeEnabled == "true")
            {
                var emailOrSystemId = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", STORAGE_KEY_EMAIL);
                var password = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", STORAGE_KEY_PASSWORD);

                return (emailOrSystemId, password, true);
            }

            return (null, null, false);
        }
        catch (Exception)
        {
            return (null, null, false);
        }
    }

    // Removes saved credentials from browser storage
    public async Task ClearCredentialsAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", STORAGE_KEY_EMAIL);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", STORAGE_KEY_PASSWORD);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", STORAGE_KEY_REMEMBER_ME);
        }
        catch (Exception)
        {
            // Ignore errors - storage might not be available
        }
    }
}

