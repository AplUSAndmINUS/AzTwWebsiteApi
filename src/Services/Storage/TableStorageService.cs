using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace AzTwWebsiteApi.Services.Storage;

public class TableStorageService<T> : ITableStorageService<T> where T : class, ITableEntity, new()
{
    private readonly TableClient _tableClient;
    private readonly ILogger<TableStorageService<T>> _logger;

    public TableStorageService(
        string connectionString,
        string tableName,
        ILogger<TableStorageService<T>> logger)
    {
        _tableClient = new TableClient(connectionString, tableName);
        _logger = logger;
        
        // Ensure table exists
        _tableClient.CreateIfNotExists();
    }

    public async Task<T?> GetEntityAsync(string partitionKey, string rowKey)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<T>(partitionKey, rowKey);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogInformation("Entity not found: PartitionKey={PartitionKey}, RowKey={RowKey}", 
                partitionKey, rowKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entity: PartitionKey={PartitionKey}, RowKey={RowKey}", 
                partitionKey, rowKey);
            throw;
        }
    }

    public async Task<IEnumerable<T>> GetAllAsync(string? filter = null)
    {
        try
        {
            var results = new List<T>();
            AsyncPageable<T> queryResults = _tableClient.QueryAsync<T>(filter);

            await foreach (var entity in queryResults)
            {
                results.Add(entity);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entities with filter: {Filter}", filter);
            throw;
        }
    }

    public async Task<T> AddEntityAsync(T entity)
    {
        try
        {
            await _tableClient.AddEntityAsync(entity);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding entity: {Entity}", entity);
            throw;
        }
    }

    public async Task UpdateEntityAsync(T entity)
    {
        try
        {
            await _tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity: {Entity}", entity);
            throw;
        }
    }

    public async Task DeleteEntityAsync(string partitionKey, string rowKey)
    {
        try
        {
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogInformation("Entity to delete not found: PartitionKey={PartitionKey}, RowKey={RowKey}", 
                partitionKey, rowKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity: PartitionKey={PartitionKey}, RowKey={RowKey}", 
                partitionKey, rowKey);
            throw;
        }
    }

    public async Task<(IEnumerable<T> Items, string? ContinuationToken)> GetPagedResultsAsync(
        int maxPerPage,
        string? continuationToken = null,
        string? filter = null)
    {
        try
        {
            var results = new List<T>();
            var queryResults = _tableClient.QueryAsync<T>(
                filter: filter,
                maxPerPage: maxPerPage);

            await foreach (var page in queryResults.AsPages(continuationToken, maxPerPage))
            {
                results.AddRange(page.Values);
                if (results.Count >= maxPerPage)
                {
                    return (results, page.ContinuationToken);
                }
            }

            return (results, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged results. MaxPerPage={MaxPerPage}, ContinuationToken={ContinuationToken}, Filter={Filter}", 
                maxPerPage, continuationToken, filter);
            throw;
        }
    }
}