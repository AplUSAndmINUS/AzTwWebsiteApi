using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Models.Blog;
using AzTwWebsiteApi.Services.Storage;

namespace AzTwWebsiteApi.Services.Blog
{
    public class BlogService : IBlogService
    {
        private readonly ITableStorageService<BlogPost> _tableStorage;
        private readonly ILogger<BlogService> _logger;

        public BlogService(ITableStorageService<BlogPost> tableStorage, ILogger<BlogService> logger)
        {
            _tableStorage = tableStorage;
            _logger = logger;
        }

        public async Task<(IEnumerable<BlogPost> Posts, string? ContinuationToken)> GetBlogPostsAsync(
            int pageSize = 25,
            string? continuationToken = null)
        {
            try
            {
                _logger.LogInformation("Retrieving blog posts. PageSize: {PageSize}, ContinuationToken: {ContinuationToken}",
                    pageSize, continuationToken);

                var result = await _tableStorage.GetPagedResultsAsync(pageSize, continuationToken);
                
                _logger.LogInformation("Retrieved {Count} blog posts", result.Items.Count());
                
                return (result.Items, result.ContinuationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving blog posts");
                throw;
            }
        }

        public async Task<BlogPost?> GetBlogPostWithImageAsync(string partitionKey, string rowKey, IBlobStorageService<BlogPost> blobStorageService)
        {
            try
            {
                _logger.LogInformation("Retrieving blog post with PartitionKey: {PartitionKey}, RowKey: {RowKey}", partitionKey, rowKey);

                var blogPost = await _tableStorage.GetEntityAsync(partitionKey, rowKey);
                if (blogPost == null)
                {
                    _logger.LogWarning("Blog post not found: PartitionKey={PartitionKey}, RowKey={RowKey}", partitionKey, rowKey);
                    return null;
                }

                _logger.LogInformation("Retrieving associated image for blog post: {RowKey}", rowKey);
                var imageBlob = await blobStorageService.GetBlobAsync(blogPost.ImageUrl);

                if (imageBlob != null)
                {
                    blogPost.ImageUrl = imageBlob.ToString();
                }
                else
                {
                    _logger.LogWarning("Image blob is null for blog post: {RowKey}", rowKey);
                }

                return blogPost;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving blog post with image: PartitionKey={PartitionKey}, RowKey={RowKey}", partitionKey, rowKey);
                throw;
            }
        }
    }
}