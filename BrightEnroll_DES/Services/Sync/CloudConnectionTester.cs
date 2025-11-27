using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BrightEnroll_DES.Data;

namespace BrightEnroll_DES.Services.Sync;

/// <summary>
/// Helper service to test cloud database connectivity
/// </summary>
public class CloudConnectionTester
{
    private readonly IDbContextFactory<CloudDbContext> _cloudContextFactory;
    private readonly ILogger<CloudConnectionTester>? _logger;

    public CloudConnectionTester(
        IDbContextFactory<CloudDbContext> cloudContextFactory,
        ILogger<CloudConnectionTester>? logger = null)
    {
        _cloudContextFactory = cloudContextFactory ?? throw new ArgumentNullException(nameof(cloudContextFactory));
        _logger = logger;
    }

    /// <summary>
    /// Tests if the cloud database is accessible
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await using var context = await _cloudContextFactory.CreateDbContextAsync();
            
            // Try to connect and execute a simple query
            var canConnect = await context.Database.CanConnectAsync();
            
            if (canConnect)
            {
                // Try a simple query to ensure the connection works
                await context.Database.ExecuteSqlRawAsync("SELECT 1");
                _logger?.LogInformation("Cloud database connection test successful");
                return true;
            }
            
            _logger?.LogWarning("Cloud database connection test failed: Cannot connect");
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Cloud database connection test failed: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Gets detailed connection error information
    /// </summary>
    public async Task<string> GetConnectionErrorDetailsAsync()
    {
        try
        {
            await using var context = await _cloudContextFactory.CreateDbContextAsync();
            await context.Database.CanConnectAsync();
            return "Connection successful";
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx)
        {
            var errorDetails = $"SQL Error {sqlEx.Number}: {sqlEx.Message}";
            if (sqlEx.Number == 53 || sqlEx.Number == -1)
            {
                errorDetails += "\n\nPossible causes:\n" +
                               "• Server name or IP address is incorrect\n" +
                               "• Firewall is blocking port 1433\n" +
                               "• SQL Server is not configured for remote connections\n" +
                               "• Network path not found";
            }
            else if (sqlEx.Number == 18456)
            {
                errorDetails += "\n\nPossible causes:\n" +
                               "• Incorrect username or password\n" +
                               "• SQL Server authentication failed";
            }
            else if (sqlEx.Number == 2)
            {
                errorDetails += "\n\nPossible causes:\n" +
                               "• SQL Server is not running\n" +
                               "• Server is unreachable";
            }
            return errorDetails;
        }
        catch (Exception ex)
        {
            var errorDetails = $"Connection failed: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorDetails += $"\nInner Exception: {ex.InnerException.Message}";
            }
            return errorDetails;
        }
    }
}

