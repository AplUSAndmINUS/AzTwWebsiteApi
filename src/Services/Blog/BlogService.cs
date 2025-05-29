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
    }
}