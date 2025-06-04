using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Services.Utils;

namespace AzTwWebsiteApi.Services.Storage;

public class TableStorageService<T> : ITableStorageService<T> where T : class, ITableEntity, new()
{
    private readonly TableClient _tableClient;
    private readonly ILogger<TableStorageService<T>> _logger;
    private readonly RetryPolicy _retryPolicy;
    private readonly CircuitBreaker _circuitBreaker;
    private readonly IMetricsService? _metrics;

    public TableStorageService(
        string connectionString,
        string tableName,
        ILogger<TableStorageService<T>> logger,
        IMetricsService? metrics = null)
    {
        _logger = logger;
        _retryPolicy = new RetryPolicy(logger);
        _circuitBreaker = new CircuitBreaker(logger);
        _metrics = metrics;
        
        try
        {
            // Log exactly what we receive - no transformations
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
        var operation = $"GetEntity_{typeof(T).Name}";
        using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                try
                {
                    var response = await _tableClient.GetEntityAsync<T>(partitionKey, rowKey);
                    _metrics?.IncrementCounter($"{operation}_Success");
                    return response.Value;
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    _logger.LogInformation("Entity not found: PartitionKey={PartitionKey}, RowKey={RowKey}", 
                        partitionKey, rowKey);
                    _metrics?.IncrementCounter($"{operation}_NotFound");
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving entity: PartitionKey={PartitionKey}, RowKey={RowKey}", 
                        partitionKey, rowKey);
                    _metrics?.IncrementCounter($"{operation}_Error");
                    throw;
                }
            }, operation);
        }, operation);
    }

    public async Task<IEnumerable<T>> GetAllAsync(string? filter = null)
    {
        var operation = $"GetAll_{typeof(T).Name}";
        using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await _retryPolicy.ExecuteAsync(async () =>
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
                    _metrics?.IncrementCounter($"{operation}_Success");
                    _metrics?.RecordValue($"{operation}_Count", results.Count);
                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving entities with filter: {Filter}", filter);
                    _metrics?.IncrementCounter($"{operation}_Error");
                    throw;
                }
            }, operation);
        }, operation);
    }

    public async Task<T> AddEntityAsync(T entity)
    {
        var operation = $"AddEntity_{typeof(T).Name}";
        using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                try
                {
                    await _tableClient.AddEntityAsync(entity);
                    _logger.LogInformation("Entity added: {Entity}", entity);
                    _metrics?.IncrementCounter($"{operation}_Success");
                    return entity;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding entity: {Entity}. Error: {Error}", entity, ex.Message);
                    _metrics?.IncrementCounter($"{operation}_Error");
                    throw;
                }
            }, operation);
        }, operation);
    }

    public async Task UpdateEntityAsync(T entity)
    {
        var operation = $"UpdateEntity_{typeof(T).Name}";
        using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

        await _circuitBreaker.ExecuteAsync<object>(async () =>
        {
            await _retryPolicy.ExecuteAsync<object>(async () =>
            {
                try
                {
                    await _tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);
                    _logger.LogInformation("Entity updated: {Entity}", entity);
                    _metrics?.IncrementCounter($"{operation}_Success");
                    return null!;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating entity: {Entity}. Error: {Error}", entity, ex.Message);
                    _metrics?.IncrementCounter($"{operation}_Error");
                    throw;
                }
            }, operation);
            return null!;
        }, operation);
    }

    public async Task DeleteEntityAsync(string partitionKey, string rowKey)
    {
        var operation = $"DeleteEntity_{typeof(T).Name}";
        using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

        await _circuitBreaker.ExecuteAsync<object>(async () =>
        {
            await _retryPolicy.ExecuteAsync<object>(async () =>
            {
                try
                {
                    await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
                    _logger.LogInformation("Entity deleted: PartitionKey={PartitionKey}, RowKey={RowKey}", 
                        partitionKey, rowKey);
                    _metrics?.IncrementCounter($"{operation}_Success");
                    return null!;
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    _logger.LogInformation("Entity to delete not found: PartitionKey={PartitionKey}, RowKey={RowKey}", 
                        partitionKey, rowKey);
                    _metrics?.IncrementCounter($"{operation}_NotFound");
                    return null!;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting entity: PartitionKey={PartitionKey}, RowKey={RowKey}. Error: {Error}", 
                        partitionKey, rowKey, ex.Message);
                    _metrics?.IncrementCounter($"{operation}_Error");
                    throw;
                }
            }, operation);
            return null!;
        }, operation);
    }

    public async Task<(IEnumerable<T> Items, string? ContinuationToken)> GetPagedResultsAsync(
        int maxPerPage,
        string? continuationToken = null,
        string? filter = null)
    {
        var operation = $"GetPagedResults_{typeof(T).Name}";
        using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await _retryPolicy.ExecuteAsync(async () =>
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
                            _metrics?.IncrementCounter($"{operation}_Success");
                            return (results, page.ContinuationToken);
                        }
                    }

                    _logger.LogInformation("All results retrieved. Total count: {Count}", results.Count);
                    _metrics?.IncrementCounter($"{operation}_Success");
                    return (results, null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving paged results. MaxPerPage={MaxPerPage}, ContinuationToken={ContinuationToken}, Filter={Filter}, Error: {Error}", 
                        maxPerPage, continuationToken, filter, ex.Message);
                    _metrics?.IncrementCounter($"{operation}_Error");
                    throw;
                }
            }, operation);
        }, operation);
    }
}