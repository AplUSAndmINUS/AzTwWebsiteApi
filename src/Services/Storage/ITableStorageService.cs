using Azure.Data.Tables;

namespace AzTwWebsiteApi.Services.Storage
{
    public interface ITableStorageService<T> where T : class, ITableEntity, new()
    {
        Task<T?> GetEntityAsync(string partitionKey, string rowKey);
        Task<IEnumerable<T>> GetAllAsync(string? filter = null);
        Task<T> AddEntityAsync(T entity);
        Task UpdateEntityAsync(T entity);
        Task DeleteEntityAsync(string partitionKey, string rowKey);
        Task<(IEnumerable<T> Items, string? ContinuationToken)> GetPagedResultsAsync(
            int maxPerPage,
            string? continuationToken = null,
            string? filter = null);
    }
}