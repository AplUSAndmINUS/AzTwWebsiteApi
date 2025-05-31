using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;

namespace AzTwWebsiteApi.Services.Storage
{
    public interface IBlobStorageService<T> where T : class
    {
        Task<T?> GetBlobAsync(string blobName);
        Task<IEnumerable<T>> GetAllBlobsAsync();
        Task<T> UploadBlobAsync(string blobName, T data, IDictionary<string, string>? metadata = null);
        Task<T> UpdateBlobAsync(string blobName, T data, IDictionary<string, string>? metadata = null);
        Task DeleteBlobAsync(string blobName);
        Task<(IEnumerable<T> Items, string? ContinuationToken)> GetPagedBlobsAsync(
            int maxResults,
            string? continuationToken = null);
    }
}