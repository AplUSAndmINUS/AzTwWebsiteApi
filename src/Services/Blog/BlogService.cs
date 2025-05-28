using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Models.Blog;
using AzTwWebsiteApi.Services.Storage;

namespace AzTwWebsiteApi.Services.Blog
{
    public class BlogService : IBlogService
    {
        private readonly ILogger<BlogService> _logger;
        private readonly ITableStorageService<BlogPost> _tableStorageService;
        private const int MaxPageSize = 100;

        public BlogService(
            ILogger<BlogService> logger,
            ITableStorageService<BlogPost> tableStorageService)
        {
            _logger = logger;
            _tableStorageService = tableStorageService;
        }

        public async Task<(IEnumerable<BlogPost> Posts, string? ContinuationToken)> GetBlogPostsAsync(
            int pageSize = 25,
            string? continuationToken = null)
        {
            _logger.LogInformation("Getting blog posts. PageSize={PageSize}, ContinuationToken={ContinuationToken}",
                pageSize, continuationToken);

            try
            {
                // Enforce upper limit to prevent excessive queries
                pageSize = Math.Min(pageSize, MaxPageSize);

                var filter = "IsPublished eq true";
                var (posts, nextToken) = await _tableStorageService.GetPagedResultsAsync<BlogPost>(
                    pageSize,
                    continuationToken,
                    filter);

                if (!posts.Any())
                {
                    _logger.LogInformation("No blog posts found");
                    return (Array.Empty<BlogPost>(), null);
                }

                _logger.LogInformation("Retrieved {Count} blog posts", posts.Count());
                return (posts, nextToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving blog posts");
                throw; // Let the caller handle the exception
            }
        }
  }
}