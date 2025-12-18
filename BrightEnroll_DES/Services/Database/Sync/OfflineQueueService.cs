using Microsoft.EntityFrameworkCore;
using BrightEnroll_DES.Data;
using BrightEnroll_DES.Data.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BrightEnroll_DES.Services.Database.Sync;

// Service to manage offline operations queue
public interface IOfflineQueueService
{
    Task QueueCreateAsync<T>(T entity, string tableName, string primaryKeyColumn) where T : class;
    Task QueueUpdateAsync<T>(T entity, string tableName, string primaryKeyColumn) where T : class;
    Task QueueDeleteAsync<T>(object primaryKey, string tableName, string primaryKeyColumn) where T : class;
    Task<List<QueuedOperation>> GetPendingOperationsAsync();
    Task<int> ProcessPendingOperationsAsync();
    Task ClearProcessedOperationsAsync();
    Task<int> GetPendingCountAsync();
}

public class QueuedOperation
{
    public int Id { get; set; }
    public string OperationType { get; set; } = string.Empty; // Create, Update, Delete
    public string TableName { get; set; } = string.Empty;
    public string PrimaryKeyColumn { get; set; } = string.Empty;
    public string? PrimaryKeyValue { get; set; }
    public string? TempId { get; set; } // Temporary ID for creates
    public string EntityData { get; set; } = string.Empty; // JSON serialized entity
    public DateTime QueuedAt { get; set; } = DateTime.Now;
    public DateTime? ProcessedAt { get; set; }
    public bool IsProcessed { get; set; } = false;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
}

public class OfflineQueueService : IOfflineQueueService
{
    private readonly AppDbContext _context;
    private readonly ILogger<OfflineQueueService>? _logger;
    private const int MaxRetries = 3;

    public OfflineQueueService(AppDbContext context, ILogger<OfflineQueueService>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    public async Task QueueCreateAsync<T>(T entity, string tableName, string primaryKeyColumn) where T : class
    {
        try
        {
            // Generate temporary ID if needed
            var pkProperty = typeof(T).GetProperty(primaryKeyColumn.Replace("_", ""));
            if (pkProperty != null)
            {
                var currentValue = pkProperty.GetValue(entity);
                if (currentValue == null || string.IsNullOrEmpty(currentValue.ToString()))
                {
                    // Generate temp ID
                    var tempId = $"TEMP_{Guid.NewGuid():N}";
                    pkProperty.SetValue(entity, tempId);
                }
            }

            var operation = new QueuedOperation
            {
                OperationType = "Create",
                TableName = tableName,
                PrimaryKeyColumn = primaryKeyColumn,
                EntityData = JsonSerializer.Serialize(entity),
                QueuedAt = DateTime.Now
            };

            // Store in a simple queue table (we'll create this)
            await SaveQueuedOperationAsync(operation);
            _logger?.LogInformation("Queued Create operation for {Table}", tableName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error queueing create operation");
            throw;
        }
    }

    public async Task QueueUpdateAsync<T>(T entity, string tableName, string primaryKeyColumn) where T : class
    {
        try
        {
            var pkProperty = typeof(T).GetProperty(primaryKeyColumn.Replace("_", ""));
            var pkValue = pkProperty?.GetValue(entity)?.ToString();

            var operation = new QueuedOperation
            {
                OperationType = "Update",
                TableName = tableName,
                PrimaryKeyColumn = primaryKeyColumn,
                PrimaryKeyValue = pkValue,
                EntityData = JsonSerializer.Serialize(entity),
                QueuedAt = DateTime.Now
            };

            await SaveQueuedOperationAsync(operation);
            _logger?.LogInformation("Queued Update operation for {Table} {Id}", tableName, pkValue);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error queueing update operation");
            throw;
        }
    }

    public async Task QueueDeleteAsync<T>(object primaryKey, string tableName, string primaryKeyColumn) where T : class
    {
        try
        {
            var operation = new QueuedOperation
            {
                OperationType = "Delete",
                TableName = tableName,
                PrimaryKeyColumn = primaryKeyColumn,
                PrimaryKeyValue = primaryKey.ToString(),
                QueuedAt = DateTime.Now
            };

            await SaveQueuedOperationAsync(operation);
            _logger?.LogInformation("Queued Delete operation for {Table} {Id}", tableName, primaryKey);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error queueing delete operation");
            throw;
        }
    }

    public Task<List<QueuedOperation>> GetPendingOperationsAsync()
    {
        // For now, use a simple in-memory list. In production, use a database table.
        // Simplified version
        return Task.FromResult(new List<QueuedOperation>());
    }

    public async Task<int> ProcessPendingOperationsAsync()
    {
        var operations = await GetPendingOperationsAsync();
        int processed = 0;

        foreach (var operation in operations.Where(o => !o.IsProcessed))
        {
            try
            {
                // This will be implemented by the sync service
                // For now, just mark as processed
                operation.IsProcessed = true;
                operation.ProcessedAt = DateTime.Now;
                processed++;
            }
            catch (Exception ex)
            {
                operation.RetryCount++;
                operation.ErrorMessage = ex.Message;
                if (operation.RetryCount >= MaxRetries)
                {
                    operation.IsProcessed = true; // Mark as failed after max retries
                }
                _logger?.LogError(ex, "Error processing queued operation {Id}", operation.Id);
            }
        }

        return processed;
    }

    public async Task ClearProcessedOperationsAsync()
    {
        // Clear processed operations older than 7 days
        var operations = await GetPendingOperationsAsync();
        var toRemove = operations.Where(o => o.IsProcessed && 
            o.ProcessedAt.HasValue && 
            o.ProcessedAt.Value < DateTime.Now.AddDays(-7)).ToList();
        
        // Remove from storage
        _logger?.LogInformation("Cleared {Count} processed operations", toRemove.Count);
    }

    public async Task<int> GetPendingCountAsync()
    {
        // Return count of pending operations
        var operations = await GetPendingOperationsAsync();
        return operations.Count(o => !o.IsProcessed);
    }


    private async Task SaveQueuedOperationAsync(QueuedOperation operation)
    {
        // In a real implementation, save to database table
        // For now, this is a placeholder
        await Task.CompletedTask;
    }
}

