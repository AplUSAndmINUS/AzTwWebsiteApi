using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Models.Blog;
using AzTwWebsiteApi.Services.Storage;
using AzTwWebsiteApi.Services.Utils;

namespace AzTwWebsiteApi.Services.Blog
{
    public class BlogService : IBlogService
    {
        private readonly ITableStorageService<BlogPost> _tableStorage;
        private readonly ILogger<BlogService> _logger;
        private readonly IMetricsService _metrics;
        private readonly CircuitBreaker _circuitBreaker;
        private readonly RetryPolicy _retryPolicy;

        public BlogService(
            ITableStorageService<BlogPost> tableStorage,
            ILogger<BlogService> logger,
            IMetricsService metrics)
        {
            _tableStorage = tableStorage;
            _logger = logger;
            _metrics = metrics;
            _circuitBreaker = new CircuitBreaker(logger);
            _retryPolicy = new RetryPolicy(logger);
        }

        public async Task<(IEnumerable<BlogPost> Posts, string? ContinuationToken)> GetBlogPostsAsync(
            int pageSize = 25,
            string? continuationToken = null)
        {
            const string operation = "GetBlogPosts";
            using var timer = new OperationTimer(operation, _metrics);

            try
            {
                _logger.LogInformation("Retrieving blog posts. PageSize: {PageSize}, ContinuationToken: {ContinuationToken}",
                    pageSize, continuationToken);

                var result = await _circuitBreaker.ExecuteAsync(async () =>
                {
                    return await _retryPolicy.ExecuteAsync(async () =>
                    {
                        var pagedResult = await _tableStorage.GetPagedResultsAsync(pageSize, continuationToken);
                        _metrics.IncrementCounter($"{operation}_Success");
                        _metrics.RecordValue($"{operation}_Count", pagedResult.Items.Count());
                        return pagedResult;
                    }, operation);
                }, operation);

                _logger.LogInformation("Retrieved {Count} blog posts", result.Items.Count());
                return (result.Items, result.ContinuationToken);
            }
            catch (Exception ex)
            {
                _metrics.IncrementCounter($"{operation}_Error");
                _logger.LogError(ex, "Error retrieving blog posts");
                throw;
            }
        }

        public async Task<BlogPost?> GetBlogPostWithImageAsync(
            string partitionKey,
            string rowKey,
            IBlobStorageService<BlogPost> blobStorageService)
        {
            const string operation = "GetBlogPostWithImage";
            using var timer = new OperationTimer(operation, _metrics);

            try
            {
                _logger.LogInformation(
                    "Retrieving blog post with PartitionKey: {PartitionKey}, RowKey: {RowKey}",
                    partitionKey, rowKey);

                return await _circuitBreaker.ExecuteAsync(async () =>
                {
                    return await _retryPolicy.ExecuteAsync(async () =>
                    {
                        var blogPost = await _tableStorage.GetEntityAsync(partitionKey, rowKey);
                        if (blogPost == null)
                        {
                            _logger.LogWarning(
                                "Blog post not found: PartitionKey={PartitionKey}, RowKey={RowKey}",
                                partitionKey, rowKey);
                            _metrics.IncrementCounter($"{operation}_NotFound");
                            return null;
                        }

                        if (!string.IsNullOrEmpty(blogPost.ImageUrl))
                        {
                            _logger.LogInformation("Retrieving associated image for blog post: {RowKey}", rowKey);
                            var imageBlob = await blobStorageService.GetBlobAsync(blogPost.ImageUrl);
                            if (imageBlob != null)
                            {
                                blogPost.ImageUrl = imageBlob.ToString() ?? string.Empty;
                            }
                            else
                            {
                                _logger.LogWarning("Image blob is null for blog post: {RowKey}", rowKey);
                                _metrics.IncrementCounter($"{operation}_ImageNotFound");
                            }
                        }

                        _metrics.IncrementCounter($"{operation}_Success");
                        return blogPost;
                    }, operation);
                }, operation);
            }
            catch (Exception ex)
            {
                _metrics.IncrementCounter($"{operation}_Error");
                _logger.LogError(ex,
                    "Error retrieving blog post with image: PartitionKey={PartitionKey}, RowKey={RowKey}",
                    partitionKey, rowKey);
                throw;
            }
        }
    }
}