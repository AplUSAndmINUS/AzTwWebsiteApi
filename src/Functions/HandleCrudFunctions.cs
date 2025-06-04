using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Services.Storage;
using System.Text.Json;
using Azure.Storage.Blobs;
using AzTwWebsiteApi.Services.Utils;

namespace AzTwWebsiteApi.Functions;

public class HandleCrudFunctions
{
    private readonly ILogger<HandleCrudFunctions> _logger;
    private readonly IMetricsService _metrics;
    private readonly RetryPolicy _retryPolicy;
    private readonly CircuitBreaker _circuitBreaker;
    private readonly ILoggerFactory _loggerFactory;

    public HandleCrudFunctions(
        ILogger<HandleCrudFunctions> logger,
        IMetricsService metrics,
        ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _metrics = metrics;
        _loggerFactory = loggerFactory;
        _retryPolicy = new RetryPolicy(logger);
        _circuitBreaker = new CircuitBreaker(logger);
    }

    public async Task<IEnumerable<T>> HandleCrudOperation<T>(
        string operation,
        string entityType,
        string? filter = null,
        T? data = default,
        int? pageSize = null,
        string? continuationToken = null) where T : class, ITableEntity, new()
    {
        var operationName = $"{operation}_{typeof(T).Name}";
        using var timer = new OperationTimer(operationName, _metrics);

        try
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var (serviceName, storageType) = GetStorageServiceInfo(entityType);
                    var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                        ?? throw new InvalidOperationException("AzureWebJobsStorage connection string not configured");

                    IEnumerable<T> result;
                    if (storageType == Constants.Storage.StorageType.Table)
                    {
                        if (!typeof(ITableEntity).IsAssignableFrom(typeof(T)))
                        {
                            throw new ArgumentException($"Type {typeof(T).Name} must implement ITableEntity for table storage operations");
                        }

                        // Safe to cast since we've verified T implements ITableEntity
                        var tableResult = await HandleTableStorageOperation<T>(
                            operation, serviceName, storageConnectionString, data as ITableEntity, filter, pageSize, continuationToken);
                        result = tableResult;
                    }
                    else if (storageType == Constants.Storage.StorageType.Blob)
                    {
                        result = await HandleBlobStorageOperation<T>(
                            operation, serviceName, storageConnectionString, data, filter);
                    }
                    else
                    {
                        throw new ArgumentException($"Unsupported storage type {storageType} for entity type {typeof(T).Name}");
                    }

                    _metrics.IncrementCounter($"{operationName}_Success");
                    _metrics.RecordValue($"{operationName}_ResultCount", result.Count());
                    return result;
                }, operationName);
            }, operationName);
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter($"{operationName}_Error");
            _logger.LogError(ex, "Error in {Operation} for type {Type}: {Error}",
                operation, typeof(T).Name, ex.Message);
            throw;
        }
    }

    private async Task<IEnumerable<T>> HandleTableStorageOperation<T>(
        string operation,
        string tableName,
        string connectionString,
        ITableEntity? data,
        string? filter = null,
        int? pageSize = null,
        string? continuationToken = null) where T : class, ITableEntity, new()
    {
        var tableStorageLogger = _loggerFactory.CreateLogger<TableStorageService<T>>();
        var tableStorage = new TableStorageService<T>(connectionString, tableName, tableStorageLogger, _metrics);

        var results = new List<T>();
        switch (operation.ToLowerInvariant())
        {
            case Constants.Storage.Operations.Get when pageSize.HasValue:
                var pagedResults = await tableStorage.GetPagedResultsAsync(pageSize.Value, continuationToken, filter);
                results.AddRange(pagedResults.Items);
                break;

            case Constants.Storage.Operations.Get:
                var entities = await tableStorage.GetAllAsync(filter);
                results.AddRange(entities);
                break;

            case Constants.Storage.Operations.Set:
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "Data is required for Set operation");
                var addedEntity = await tableStorage.AddEntityAsync(data as T 
                    ?? throw new ArgumentException("Invalid entity type for table storage"));
                results.Add(addedEntity);
                break;

            case Constants.Storage.Operations.Update:
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "Data is required for Update operation");
                await tableStorage.UpdateEntityAsync(data as T 
                    ?? throw new ArgumentException("Invalid entity type for table storage"));
                results.Add((data as T)!);
                break;

            case Constants.Storage.Operations.Delete:
                if (string.IsNullOrEmpty(filter))
                    throw new ArgumentException("Filter is required for Delete operation");
                var entitiesToDelete = await tableStorage.GetAllAsync(filter);
                foreach (var entity in entitiesToDelete)
                {
                    await tableStorage.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
                }
                break;

            default:
                throw new ArgumentException($"Unsupported operation: {operation}");
        }

        return results;
    }

    private async Task<IEnumerable<T>> HandleBlobStorageOperation<T>(
        string operation,
        string containerName,
        string connectionString,
        T? data,
        string? blobName = null) where T : class
    {
        var blobStorageLogger = _loggerFactory.CreateLogger<BlobStorageService<T>>();
        var blobStorage = new BlobStorageService<T>(connectionString, containerName, blobStorageLogger, _metrics);

        var results = new List<T>();
        switch (operation.ToLowerInvariant())
        {
            case Constants.Storage.Operations.Get when !string.IsNullOrEmpty(blobName):
                var blob = await blobStorage.GetBlobAsync(blobName);
                if (blob != null) results.Add(blob);
                break;

            case Constants.Storage.Operations.Get:
                var blobs = await blobStorage.GetAllBlobsAsync();
                results.AddRange(blobs);
                break;

            case Constants.Storage.Operations.Set:
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "Data is required for Set operation");
                if (string.IsNullOrEmpty(blobName))
                    throw new ArgumentException("Blob name is required for Set operation");
                var uploadedBlob = await blobStorage.UploadBlobAsync(blobName, data);
                results.Add(uploadedBlob);
                break;

            default:
                throw new ArgumentException($"Unsupported operation: {operation}");
        }

        return results;
    }

    public async Task<IEnumerable<T>> HandleBlobOperation<T>(
        string operation,
        string entityType,
        string? blobName = null,
        T? data = default) where T : class
    {
        var operationName = $"{operation}_{typeof(T).Name}";
        using var timer = new OperationTimer(operationName, _metrics);

        try
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var (serviceName, storageType) = GetStorageServiceInfo(entityType);
                    
                    if (storageType != Constants.Storage.StorageType.Blob)
                    {
                        throw new ArgumentException($"Entity type {entityType} is not configured for blob storage");
                    }

                    var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                        ?? throw new InvalidOperationException("AzureWebJobsStorage connection string not configured");

                    var result = await HandleBlobStorageOperation<T>(
                        operation, serviceName, storageConnectionString, data, blobName);

                    _metrics.IncrementCounter($"{operationName}_Success");
                    _metrics.RecordValue($"{operationName}_ResultCount", result.Count());
                    return result;
                }, operationName);
            }, operationName);
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter($"{operationName}_Error");
            _logger.LogError(ex, "Error in blob operation {Operation} for type {Type}: {Error}",
                operation, typeof(T).Name, ex.Message);
            throw;
        }
    }

    private static (string ServiceName, Constants.Storage.StorageType StorageType) GetStorageServiceInfo(string entityType)
    {
        if (!Constants.Storage.EntityStorageTypes.ContainsKey(entityType))
        {
            throw new ArgumentException($"Unknown entity type: {entityType}");
        }

        return (entityType, Constants.Storage.EntityStorageTypes[entityType]);
    }
}