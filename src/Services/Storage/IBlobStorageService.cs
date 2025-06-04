using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;

namespace AzTwWebsiteApi.Services.Storage
{
    public interface IBlobStorageService<T> where T : class
    {
        // Basic operations
        Task<T?> GetBlobAsync(string blobName);
        Task<IEnumerable<T>> GetAllBlobsAsync();
        Task<T> UploadBlobAsync(string blobName, T data, IDictionary<string, string>? metadata = null);
        Task<T> UpdateBlobAsync(string blobName, T data, IDictionary<string, string>? metadata = null);
        Task DeleteBlobAsync(string blobName);

        // Advanced operations
        Task<(IEnumerable<T> Items, string? ContinuationToken)> GetPagedBlobsAsync(
            int maxResults,
            string? prefix = null,
            string? continuationToken = null);
        
        // Metadata operations
        Task<IDictionary<string, string>> GetBlobMetadataAsync(string blobName);
        Task UpdateBlobMetadataAsync(string blobName, IDictionary<string, string> metadata);
        Task<BlobProperties> GetBlobPropertiesAsync(string blobName);
        
        // Copy and move operations
        Task<string> StartBlobCopyAsync(string sourceBlobName, string destinationBlobName);
        Task<bool> WaitForBlobCopyAsync(string blobName, string copyId, TimeSpan timeout);
        Task MoveBlobAsync(string sourceBlobName, string destinationBlobName);

        // Specialized operations
        Task<(T? Content, IDictionary<string, string> Metadata)> GetBlobWithMetadataAsync(
            string blobName,
            bool includeMetadata = true);
        Task<T> UploadBlobWithMetadataAsync(
            string blobName,
            T data,
            IDictionary<string, string> metadata,
            string contentType);
    }
}