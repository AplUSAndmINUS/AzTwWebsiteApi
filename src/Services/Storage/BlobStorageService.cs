using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Services.Utils;

namespace AzTwWebsiteApi.Services.Storage
{
    public class BlobStorageService<T> : IBlobStorageService<T> where T : class
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<BlobStorageService<T>> _logger;
        private readonly IMetricsService? _metrics;
        private readonly RetryPolicy _retryPolicy;
        private readonly CircuitBreaker _circuitBreaker;

        public BlobStorageService(
            string connectionString,
            string containerName,
            ILogger<BlobStorageService<T>> logger,
            IMetricsService? metrics = null)
        {
            _logger = logger;
            _metrics = metrics;
            _retryPolicy = new RetryPolicy(logger);
            _circuitBreaker = new CircuitBreaker(logger);

            try
            {
                var blobServiceClient = new BlobServiceClient(connectionString);
                _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                _containerClient.CreateIfNotExists();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing BlobStorageService for container: {ContainerName}", containerName);
                throw;
            }
        }

        public async Task<T?> GetBlobAsync(string blobName)
        {
            var operation = $"GetBlob_{typeof(T).Name}";
            using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var blobClient = _containerClient.GetBlobClient(blobName);

                        if (!await blobClient.ExistsAsync())
                        {
                            _logger.LogWarning("Blob {BlobName} not found", blobName);
                            return null;
                        }

                        var response = await blobClient.DownloadContentAsync();
                        var content = response.Value.Content.ToString();
                        return JsonSerializer.Deserialize<T>(content);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error retrieving blob {BlobName}", blobName);
                        throw;
                    }
                }, operation);
            }, operation);
        }

        public async Task<IEnumerable<T>> GetAllBlobsAsync()
        {
            var operation = $"GetAllBlobs_{typeof(T).Name}";
            using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var blobs = new List<T>();
                        await foreach (var blobItem in _containerClient.GetBlobsAsync())
                        {
                            var blob = await GetBlobAsync(blobItem.Name);
                            if (blob != null)
                            {
                                blobs.Add(blob);
                            }
                        }
                        return blobs;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error retrieving all blobs");
                        throw;
                    }
                }, operation);
            }, operation);
        }

        public async Task<T> UploadBlobAsync(string blobName, T data, IDictionary<string, string>? metadata = null)
        {
            var operation = $"UploadBlob_{typeof(T).Name}";
            using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var blobClient = _containerClient.GetBlobClient(blobName);
                        var content = JsonSerializer.Serialize(data);
                        var options = new BlobUploadOptions { Metadata = metadata };
                        
                        await blobClient.UploadAsync(BinaryData.FromString(content), options);
                        _logger.LogInformation("Successfully uploaded blob {BlobName}", blobName);
                        return data;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading blob {BlobName}", blobName);
                        throw;
                    }
                }, operation);
            }, operation);
        }

        public async Task<(IEnumerable<T> Items, string? ContinuationToken)> GetPagedBlobsAsync(
            int maxResults,
            string? prefix = null,
            string? continuationToken = null)
        {
            var operation = $"GetPagedBlobs_{typeof(T).Name}";
            using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var results = new List<T>();
                        var options = new BlobTraits();
                        var pages = _containerClient.GetBlobsAsync(options)
                            .AsPages(continuationToken, maxResults);

                        await foreach (var page in pages)
                        {
                            foreach (var blobItem in page.Values)
                            {
                                if (string.IsNullOrEmpty(prefix) || blobItem.Name.StartsWith(prefix))
                                {
                                    var blob = await GetBlobAsync(blobItem.Name);
                                    if (blob != null)
                                    {
                                        results.Add(blob);
                                    }
                                }
                            }
                            return (results, page.ContinuationToken);
                        }

                        return (results, (string?)null);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error retrieving paged blobs");
                        throw;
                    }
                }, operation);
            }, operation);
        }

        public async Task<IDictionary<string, string>> GetBlobMetadataAsync(string blobName)
        {
            var operation = $"GetBlobMetadata_{typeof(T).Name}";
            using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var blobClient = _containerClient.GetBlobClient(blobName);
                        var properties = await blobClient.GetPropertiesAsync();
                        return properties.Value.Metadata;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting metadata for blob {BlobName}", blobName);
                        throw;
                    }
                }, operation);
            }, operation);
        }

        public async Task UpdateBlobMetadataAsync(string blobName, IDictionary<string, string> metadata)
        {
            var operation = $"UpdateBlobMetadata_{typeof(T).Name}";
            using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

            await _circuitBreaker.ExecuteAsync(async () =>
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var blobClient = _containerClient.GetBlobClient(blobName);
                        await blobClient.SetMetadataAsync(metadata);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating metadata for blob {BlobName}", blobName);
                        throw;
                    }
                }, operation);
                return true;
            }, operation);
        }

        public async Task<(T? Content, IDictionary<string, string> Metadata)> GetBlobWithMetadataAsync(
            string blobName,
            bool includeMetadata = true)
        {
            var operation = $"GetBlobWithMetadata_{typeof(T).Name}";
            using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var content = await GetBlobAsync(blobName);
                        var metadata = includeMetadata ? 
                            await GetBlobMetadataAsync(blobName) : 
                            new Dictionary<string, string>();
                        
                        return (content, metadata);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting blob with metadata {BlobName}", blobName);
                        throw;
                    }
                }, operation);
            }, operation);
        }

        public async Task<T> UpdateBlobAsync(string blobName, T data, IDictionary<string, string>? metadata = null)
        {
            var operation = $"UpdateBlob_{typeof(T).Name}";
            using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var blobClient = _containerClient.GetBlobClient(blobName);
                        if (!await blobClient.ExistsAsync())
                        {
                            throw new InvalidOperationException($"Blob {blobName} does not exist");
                        }

                        var content = JsonSerializer.Serialize(data);
                        var options = new BlobUploadOptions { Metadata = metadata };
                        await blobClient.UploadAsync(BinaryData.FromString(content), options);
                        
                        _logger.LogInformation("Successfully updated blob {BlobName}", blobName);
                        return data;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating blob {BlobName}", blobName);
                        throw;
                    }
                }, operation);
            }, operation);
        }

        public async Task DeleteBlobAsync(string blobName)
        {
            var operation = $"DeleteBlob_{typeof(T).Name}";
            using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

            await _circuitBreaker.ExecuteAsync(async () =>
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var blobClient = _containerClient.GetBlobClient(blobName);
                        await blobClient.DeleteIfExistsAsync();
                        _logger.LogInformation("Successfully deleted blob {BlobName}", blobName);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting blob {BlobName}", blobName);
                        throw;
                    }
                }, operation);
                return true;
            }, operation);
        }

        public async Task<BlobProperties> GetBlobPropertiesAsync(string blobName)
        {
            var operation = $"GetBlobProperties_{typeof(T).Name}";
            using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var blobClient = _containerClient.GetBlobClient(blobName);
                        var properties = await blobClient.GetPropertiesAsync();
                        return properties.Value;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting properties for blob {BlobName}", blobName);
                        throw;
                    }
                }, operation);
            }, operation);
        }

        public async Task<string> StartBlobCopyAsync(string sourceBlobName, string destinationBlobName)
        {
            var operation = $"StartBlobCopy_{typeof(T).Name}";
            using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var sourceBlob = _containerClient.GetBlobClient(sourceBlobName);
                        var destinationBlob = _containerClient.GetBlobClient(destinationBlobName);

                        var copyOperation = await destinationBlob.StartCopyFromUriAsync(sourceBlob.Uri);
                        var properties = await destinationBlob.GetPropertiesAsync();
                        return properties.Value.CopyId ?? throw new InvalidOperationException("Copy ID not available");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error starting blob copy from {SourceBlob} to {DestinationBlob}", 
                            sourceBlobName, destinationBlobName);
                        throw;
                    }
                }, operation);
            }, operation);
        }

        public async Task<bool> WaitForBlobCopyAsync(string blobName, string copyId, TimeSpan timeout)
        {
            var operation = $"WaitForBlobCopy_{typeof(T).Name}";
            using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var blobClient = _containerClient.GetBlobClient(blobName);
                        var startTime = DateTime.UtcNow;

                        while (DateTime.UtcNow - startTime < timeout)
                        {
                            var properties = await blobClient.GetPropertiesAsync();
                            
                            if (properties.Value.CopyId != copyId)
                            {
                                return false;
                            }

                            switch (properties.Value.CopyStatus)
                            {
                                case CopyStatus.Success:
                                    return true;
                                case CopyStatus.Failed:
                                case CopyStatus.Aborted:
                                    return false;
                                case CopyStatus.Pending:
                                    await Task.Delay(1000);
                                    break;
                            }
                        }

                        return false;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error waiting for blob copy to complete for {BlobName}", blobName);
                        throw;
                    }
                }, operation);
            }, operation);
        }

        public async Task MoveBlobAsync(string sourceBlobName, string destinationBlobName)
        {
            var operation = $"MoveBlob_{typeof(T).Name}";
            using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

            await _circuitBreaker.ExecuteAsync(async () =>
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var copyId = await StartBlobCopyAsync(sourceBlobName, destinationBlobName);
                        var timeout = TimeSpan.FromMinutes(5);
                        
                        if (await WaitForBlobCopyAsync(destinationBlobName, copyId, timeout))
                        {
                            await DeleteBlobAsync(sourceBlobName);
                            return true;
                        }
                        
                        throw new TimeoutException($"Move operation timed out after {timeout.TotalMinutes} minutes");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error moving blob from {SourceBlob} to {DestinationBlob}", 
                            sourceBlobName, destinationBlobName);
                        throw;
                    }
                }, operation);
                return true;
            }, operation);
        }

        public async Task<T> UploadBlobWithMetadataAsync(
            string blobName,
            T data,
            IDictionary<string, string> metadata,
            string contentType)
        {
            var operation = $"UploadBlobWithMetadata_{typeof(T).Name}";
            using var timer = _metrics != null ? new OperationTimer(operation, _metrics) : null;

            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        var blobClient = _containerClient.GetBlobClient(blobName);
                        var content = JsonSerializer.Serialize(data);
                        
                        var options = new BlobUploadOptions
                        {
                            Metadata = metadata,
                            HttpHeaders = new BlobHttpHeaders
                            {
                                ContentType = contentType
                            }
                        };

                        await blobClient.UploadAsync(BinaryData.FromString(content), options);
                        _logger.LogInformation("Successfully uploaded blob {BlobName} with metadata", blobName);
                        return data;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading blob {BlobName} with metadata", blobName);
                        throw;
                    }
                }, operation);
            }, operation);
        }
    }
}