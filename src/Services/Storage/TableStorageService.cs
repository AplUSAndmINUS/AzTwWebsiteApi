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
        _logger = logger;
        
        try
        {
            _logger.LogInformation("Initializing TableClient for table: {TableName}", tableName);
            _tableClient = new TableClient(connectionString, tableName);
            _logger.LogInformation("Creating table if not exists: {TableName}", tableName);
            _tableClient.CreateIfNotExists();
            _logger.LogInformation("Table initialization complete: {TableName}", tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing TableClient for table: {TableName}. Error: {Error}", 
                tableName, ex.Message);
            throw;
        }
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
            _logger.LogInformation("Fetching entities with filter: {Filter}", filter ?? "none");
            var results = new List<T>();
            AsyncPageable<T> queryResults = _tableClient.QueryAsync<T>(filter);

            await foreach (var entity in queryResults)
            {
                results.Add(entity);
            }

            _logger.LogInformation("Retrieved {Count} entities", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entities. Filter: {Filter}, Error: {Error}", 
                filter, ex.Message);
            throw;
        }
    }

    public async Task<T> AddEntityAsync(T entity)
    {
        try
        {
            await _tableClient.AddEntityAsync(entity);
            _logger.LogInformation("Entity added: {Entity}", entity);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding entity: {Entity}. Error: {Error}", entity, ex.Message);
            throw;
        }
    }

    public async Task UpdateEntityAsync(T entity)
    {
        try
        {
            await _tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);
            _logger.LogInformation("Entity updated: {Entity}", entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity: {Entity}. Error: {Error}", entity, ex.Message);
            throw;
        }
    }

    public async Task DeleteEntityAsync(string partitionKey, string rowKey)
    {
        try
        {
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
            _logger.LogInformation("Entity deleted: PartitionKey={PartitionKey}, RowKey={RowKey}", 
                partitionKey, rowKey);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogInformation("Entity to delete not found: PartitionKey={PartitionKey}, RowKey={RowKey}", 
                partitionKey, rowKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity: PartitionKey={PartitionKey}, RowKey={RowKey}. Error: {Error}", 
                partitionKey, rowKey, ex.Message);
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
                    _logger.LogInformation("Paged results retrieved. Count: {Count}, ContinuationToken: {ContinuationToken}", 
                        results.Count, page.ContinuationToken);
                    return (results, page.ContinuationToken);
                }
            }

            _logger.LogInformation("All results retrieved. Total count: {Count}", results.Count);
            return (results, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged results. MaxPerPage={MaxPerPage}, ContinuationToken={ContinuationToken}, Filter={Filter}, Error: {Error}", 
                maxPerPage, continuationToken, filter, ex.Message);
            throw;
        }
    }
}