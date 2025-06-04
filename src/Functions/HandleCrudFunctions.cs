using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Services.Storage;
using System.Text.Json;
using Azure.Storage.Blobs;
using AzTwWebsiteApi.Services.Utils;

namespace AzTwWebsiteApi.Functions;

public class CrudOperationResult<T> where T : class
{
    public List<T> Items { get; set; } = new();
    public string? ContinuationToken { get; set; }
    public int DeletedCount { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class CrudOperationException : Exception
{
    public string ErrorCode { get; }

    public CrudOperationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = Constants.Storage.ErrorCodes.StorageError;
    }

    public CrudOperationException(string message, string errorCode, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

public class CrudOperationOptions
{
    public required string ConnectionString { get; set; }
    public string? Filter { get; set; }
    public object? Data { get; set; }
    public int? PageSize { get; set; }
    public string? ContinuationToken { get; set; }
    public string? BlobName { get; set; }
    public string? Prefix { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public bool? IncludeMetadata { get; set; }
}

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

    public async Task<CrudOperationResult<T>> HandleCrudOperation<T>(
        string operation,
        string entityType,
        CrudOperationOptions options) where T : class, ITableEntity, new()
    {
        ArgumentNullException.ThrowIfNull(options);
        var operationName = $"{operation}_{typeof(T).Name}";
        using var timer = new OperationTimer(operationName, _metrics);

        try
        {
            var (serviceName, storageType) = GetStorageServiceInfo(entityType);
            
            _logger.LogInformation(
                "Starting {Operation} operation for {EntityType} using {StorageType}", 
                operation, entityType, storageType);

            var result = storageType switch
            {
                Constants.Storage.StorageType.Table => await _retryPolicy.ExecuteAsync(
                    async () => await _circuitBreaker.ExecuteAsync(
                        async () => await HandleTableStorageOperation<T>(
                            operation, serviceName, options),
                        $"TableStorage_{operationName}"
                    ),
                    $"Retry_{operationName}"
                ),
                
                Constants.Storage.StorageType.Blob => await _retryPolicy.ExecuteAsync(
                    async () => await _circuitBreaker.ExecuteAsync(
                        async () => await HandleBlobStorageOperation<T>(
                            operation, serviceName, options),
                        $"BlobStorage_{operationName}"
                    ),
                    $"Retry_{operationName}"
                ),
                
                _ => throw new ArgumentException($"Unsupported storage type for entity type {entityType}")
            };

            _metrics.IncrementCounter($"{operationName}_Success");
            return result;
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter($"{operationName}_Error");
            _logger.LogError(ex, "Error in {Operation} for {EntityType}: {Error}", 
                operation, entityType, ex.Message);
            throw new CrudOperationException(
                $"Failed to perform {operation} on {entityType}", ex);
        }
    }

    private async Task<CrudOperationResult<T>> HandleTableStorageOperation<T>(
        string operation,
        string tableName,
        CrudOperationOptions options) where T : class, ITableEntity, new()
    {
        var tableStorageLogger = _loggerFactory.CreateLogger<TableStorageService<T>>();
        var tableStorage = new TableStorageService<T>(
            options.ConnectionString, 
            tableName, 
            tableStorageLogger, 
            _metrics);

        var result = new CrudOperationResult<T>();

        switch (operation.ToLowerInvariant())
        {
            case Constants.Storage.Operations.GetPaged:
                var pagedResults = await tableStorage.GetPagedResultsAsync(
                    options.PageSize ?? 25,
                    options.ContinuationToken,
                    options.Filter);
                result.Items.AddRange(pagedResults.Items);
                result.ContinuationToken = pagedResults.ContinuationToken;
                break;

            case Constants.Storage.Operations.Get:
                var entities = await tableStorage.GetAllAsync(options.Filter);
                result.Items.AddRange(entities);
                break;

            case Constants.Storage.Operations.Set:
                if (options.Data == null)
                    throw new ArgumentNullException(nameof(options.Data), "Data is required for Set operation");
                var addedEntity = await tableStorage.AddEntityAsync(options.Data as T 
                    ?? throw new ArgumentException("Invalid entity type for table storage"));
                result.Items.Add(addedEntity);
                break;

            case Constants.Storage.Operations.Update:
                if (options.Data == null)
                    throw new ArgumentNullException(nameof(options.Data), "Data is required for Update operation");
                var entity = options.Data as T ?? throw new ArgumentException("Invalid entity type for table storage");
                await tableStorage.UpdateEntityAsync(entity);
                result.Items.Add(entity);
                break;

            case Constants.Storage.Operations.Delete:
                if (string.IsNullOrEmpty(options.Filter))
                    throw new ArgumentException("Filter is required for Delete operation", nameof(options.Filter));
                var entitiesToDelete = await tableStorage.GetAllAsync(options.Filter);
                foreach (var entityToDelete in entitiesToDelete)
                {
                    await tableStorage.DeleteEntityAsync(entityToDelete.PartitionKey, entityToDelete.RowKey);
                    result.DeletedCount++;
                }
                break;

            default:
                throw new ArgumentException($"Unsupported operation: {operation}");
        }

        return result;
    }

    private async Task<CrudOperationResult<T>> HandleBlobStorageOperation<T>(
        string operation,
        string containerName,
        CrudOperationOptions options) where T : class
    {
        var blobStorageLogger = _loggerFactory.CreateLogger<BlobStorageService<T>>();
        var blobStorage = new BlobStorageService<T>(
            options.ConnectionString, 
            containerName, 
            blobStorageLogger, 
            _metrics);

        var result = new CrudOperationResult<T>();

        switch (operation.ToLowerInvariant())
        {
            case Constants.Storage.Operations.Get when !string.IsNullOrEmpty(options.BlobName):
                var blob = await blobStorage.GetBlobAsync(options.BlobName);
                if (blob != null) result.Items.Add(blob);
                if (options.IncludeMetadata == true)
                {
                    result.Metadata = await blobStorage.GetBlobMetadataAsync(options.BlobName);
                }
                break;

            case Constants.Storage.Operations.Get:
                var blobs = await blobStorage.GetAllBlobsAsync();
                result.Items.AddRange(blobs);
                break;

            case Constants.Storage.Operations.Set:
                if (options.Data == null || string.IsNullOrEmpty(options.BlobName))
                    throw new ArgumentException("Both Data and BlobName are required for Set operation");
                
                var data = options.Data as T ?? throw new ArgumentException("Invalid data type for blob storage");
                var uploadedBlob = await blobStorage.UploadBlobAsync(options.BlobName, data);
                
                if (options.Metadata != null)
                {
                    await blobStorage.SetBlobMetadataAsync(options.BlobName, options.Metadata);
                }
                
                result.Items.Add(uploadedBlob);
                break;

            case Constants.Storage.Operations.Delete:
                if (string.IsNullOrEmpty(options.BlobName))
                    throw new ArgumentException("BlobName is required for Delete operation");
                
                await blobStorage.DeleteBlobAsync(options.BlobName);
                result.DeletedCount = 1;
                break;

            default:
                throw new ArgumentException($"Unsupported operation: {operation}");
        }

        return result;
    }

    private static (string ServiceName, Constants.Storage.StorageType StorageType) 
        GetStorageServiceInfo(string entityType)
    {
        if (!Constants.Storage.EntityStorageTypes.ContainsKey(entityType))
        {
            throw new ArgumentException($"Unknown entity type: {entityType}");
        }

        return (entityType, Constants.Storage.EntityStorageTypes[entityType]);
    }
}