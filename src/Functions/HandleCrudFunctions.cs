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
        
        // Get storage info before the try block so we can use it in error logging
        var (serviceName, storageType) = GetStorageServiceInfo(entityType);

        try
        {
            
            _logger.LogInformation(
                "Starting {Operation} operation for {TableName} using {StorageType}", 
                operation, serviceName, storageType);

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
            _logger.LogError(ex, "Error in {Operation} for table {TableName}: {Error}", 
                operation, serviceName, ex.Message);
            throw new CrudOperationException(
                $"Failed to perform {operation} on {serviceName}", ex);
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
                var updateEntity = options.Data as T ?? throw new ArgumentException("Invalid entity type for table storage");
                
                _logger.LogInformation("Fetching existing entity for update: PartitionKey={PartitionKey}, RowKey={RowKey}",
                    updateEntity.PartitionKey, updateEntity.RowKey);
                    
                // First get the existing entity
                var existingEntity = await tableStorage.GetEntityAsync(updateEntity.PartitionKey, updateEntity.RowKey);
                if (existingEntity == null)
                {
                    throw new KeyNotFoundException($"Entity with PartitionKey={updateEntity.PartitionKey}, RowKey={updateEntity.RowKey} not found");
                }

                _logger.LogInformation("Existing entity before update: {Entity}", JsonSerializer.Serialize(existingEntity));

                // Copy non-null properties from update to existing entity to preserve existing values
                foreach (var property in typeof(T).GetProperties())
                {
                    if (property.Name == nameof(ITableEntity.PartitionKey) || 
                        property.Name == nameof(ITableEntity.RowKey) ||
                        property.Name == nameof(ITableEntity.ETag) ||
                        property.Name == nameof(ITableEntity.Timestamp) ||
                        !property.CanWrite) // Skip read-only properties
                        continue;

                    var updateValue = property.GetValue(updateEntity);
                    if (updateValue != null &&
                        !string.Equals(updateValue.ToString(), property.PropertyType.GetDefault()?.ToString()))
                    {
                        _logger.LogInformation("Updating property {Property}: {OldValue} -> {NewValue}",
                            property.Name,
                            property.GetValue(existingEntity),
                            updateValue);
                            
                        property.SetValue(existingEntity, updateValue);
                    }
                }

                _logger.LogInformation("Merged entity before update: {Entity}", JsonSerializer.Serialize(existingEntity));
                await tableStorage.UpdateEntityAsync(existingEntity);
                
                // After update, retrieve the entity to confirm the update
                var updatedEntity = await tableStorage.GetEntityAsync(existingEntity.PartitionKey, existingEntity.RowKey);
                if (updatedEntity != null)
                {
                    _logger.LogInformation("Retrieved updated entity: {Entity}", JsonSerializer.Serialize(updatedEntity));
                    result.Items.Add(updatedEntity);
                }
                else
                {
                    _logger.LogWarning("Updated entity could not be retrieved. Adding merged entity to result.");
                    result.Items.Add(existingEntity);
                }
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
                    var metadata = await blobStorage.GetBlobMetadataAsync(options.BlobName);
                    result.Metadata = metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
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
                    await blobStorage.UpdateBlobMetadataAsync(options.BlobName, options.Metadata);
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
        GetStorageServiceInfo(string tableName)
    {
        // Use Constants to look up the storage type
        var storageTypeKey = Constants.Storage.EntityTypes.Blog; // Default to blog type

        // Get the storage type from our constants
        if (!Constants.Storage.EntityStorageTypes.TryGetValue(storageTypeKey, out var storageType))
        {
            throw new ArgumentException($"Unknown entity type: {storageTypeKey}");
        }

        // Return the original table name unchanged and just the storage type
        return (tableName, storageType);
    }
}