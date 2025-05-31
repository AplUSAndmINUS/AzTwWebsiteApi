using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Services.Storage;
using System.Text.Json;
using Azure.Storage.Blobs;

namespace AzTwWebsiteApi.Functions;

public static class HandleCrudFunctions
{
    private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole());

    public static async Task<IEnumerable<T>> HandleCrudOperation<T>(
        string operation,
        string entityType,
        string? filter = null,
        T? data = default,
        int? pageSize = null,
        string? continuationToken = null) where T : class, new()
    {
        var (serviceName, storageType) = GetStorageServiceInfo(entityType);
        var storageConnectionString = Environment.GetEnvironmentVariable("StorageConnectionString")
            ?? throw new InvalidOperationException("Storage connection string not configured");

        return storageType switch
        {
            StorageType.Table when typeof(ITableEntity).IsAssignableFrom(typeof(T)) =>
                await HandleTableStorageOperation<T>(operation, serviceName, storageConnectionString, data as ITableEntity, filter, pageSize, continuationToken),
            StorageType.Blob =>
                await HandleBlobStorageOperation<T>(operation, serviceName, storageConnectionString, data, filter),
            _ => throw new ArgumentException($"Unsupported storage type {storageType} for entity type {typeof(T).Name}")
        };
    }

    private static (string ServiceName, Constants.Storage.StorageType StorageType) GetStorageServiceInfo(string entityType)
    {
        var serviceName = GetStorageService.GetStorageServiceName(entityType);
        var storageType = Constants.Storage.EntityStorageTypes.TryGetValue(entityType, out var type)
            ? type
            : throw new ArgumentException($"Unknown entity type: {entityType}");
        return (serviceName, storageType);
    }

    private static async Task<IEnumerable<T>> HandleTableStorageOperation<T>(
        string operation,
        string tableName,
        string connectionString,
        ITableEntity? data,
        string? filter = null,
        int? pageSize = null,
        string? continuationToken = null) where T : class, ITableEntity, new()
    {
        var logger = LoggerFactory.CreateLogger<TableStorageService<T>>();
        var tableService = new TableStorageService<T>(connectionString, tableName, logger);

        switch (operation.ToLowerInvariant())
        {
            case "get" when pageSize.HasValue:
                var (items, token) = await tableService.GetPagedResultsAsync(pageSize.Value, continuationToken, filter);
                return items;

            case "get":
                return await tableService.GetAllAsync(filter);

            case "set":
                if (data == null) throw new ArgumentNullException(nameof(data));
                await tableService.AddEntityAsync((T)data);
                return new[] { (T)data };

            case "update":
                if (data == null) throw new ArgumentNullException(nameof(data));
                await tableService.UpdateEntityAsync((T)data);
                return new[] { (T)data };

            case "delete":
                if (string.IsNullOrEmpty(filter))
                    throw new ArgumentException("Filter required for delete operation", nameof(filter));
                
                var entities = await tableService.GetAllAsync(filter);
                foreach (var entity in entities)
                {
                    await tableService.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
                }
                return Array.Empty<T>();

            default:
                throw new ArgumentException($"Invalid operation: {operation}", nameof(operation));
        }
    }

    private static async Task<IEnumerable<T>> HandleBlobStorageOperation<T>(
        string operation,
        string containerName,
        string connectionString,
        T? data = default,
        string? blobName = null)
    {
        var blobServiceClient = new BlobServiceClient(connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        switch (operation.ToLowerInvariant())
        {
            case "get" when !string.IsNullOrEmpty(blobName):
                var blobClient = containerClient.GetBlobClient(blobName);
                if (!await blobClient.ExistsAsync())
                    return Array.Empty<T>();

                var download = await blobClient.DownloadContentAsync();
                var content = download.Value.Content.ToString();
                return new[] { JsonSerializer.Deserialize<T>(content)! };

            case "get":
                var blobs = new List<T>();
                await foreach (var blob in containerClient.GetBlobsAsync())
                {
                    var client = containerClient.GetBlobClient(blob.Name);
                    var blobContent = await client.DownloadContentAsync();
                    blobs.Add(JsonSerializer.Deserialize<T>(blobContent.Value.Content.ToString())!);
                }
                return blobs;

            case "set":
                if (data == null) throw new ArgumentNullException(nameof(data));
                if (string.IsNullOrEmpty(blobName)) throw new ArgumentException("Blob name required for set operation", nameof(blobName));

                var setBlobClient = containerClient.GetBlobClient(blobName);
                var json = JsonSerializer.Serialize(data);
                await setBlobClient.UploadAsync(BinaryData.FromString(json), overwrite: true);
                return new[] { data };

            case "update":
                if (data == null) throw new ArgumentNullException(nameof(data));
                if (string.IsNullOrEmpty(blobName)) throw new ArgumentException("Blob name required for update operation", nameof(blobName));

                var updateBlobClient = containerClient.GetBlobClient(blobName);
                if (!await updateBlobClient.ExistsAsync())
                    throw new InvalidOperationException($"Blob {blobName} does not exist");

                var updateJson = JsonSerializer.Serialize(data);
                await updateBlobClient.UploadAsync(BinaryData.FromString(updateJson), overwrite: true);
                return new[] { data };

            case "delete":
                if (string.IsNullOrEmpty(blobName))
                    throw new ArgumentException("Blob name required for delete operation", nameof(blobName));

                var deleteBlobClient = containerClient.GetBlobClient(blobName);
                await deleteBlobClient.DeleteIfExistsAsync();
                return Array.Empty<T>();

            default:
                throw new ArgumentException($"Invalid operation: {operation}", nameof(operation));
        }
    }

    private enum StorageType
    {
        Table,
        Blob
    }
}