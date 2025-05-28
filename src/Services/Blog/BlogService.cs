// C# functions- BlogService.cs
// Endpoints: 

// *BLOG POSTS*
// _api/blog/posts
// _api/blog/posts/{id}
// _api/blog/posts/{id}/update

// *BLOG IMAGES*
// _api/blog/images
// _api/blog/images/{id}                             
// _api/blog/images/{id}/delete
// _api/blog/images/{id}/update

// *BLOG COMMENTS*
// _api/blog/posts/{id}/comments
// _api/blog/posts/{id}/comments/{commentId}
// _api/blog/posts/{id}/comments/{commentId}/delete
// _api/blog/posts/{id}/comments/{commentId}/update
// _api/blog/posts/{id}/comments/{commentId}/reactions
// _api/blog/posts/{id}/comments/{commentId}/replies
// _api/blog/posts/{id}/comments/{commentId}/replies/{replyId}
// _api/blog/posts/{id}/comments/{commentId}/replies/{replyId}/reactions

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.Data.Tables;
using AzTwWebsiteApi.Utils;
using AzTwWebsiteApi.Models.Blog;
using AzTwWebsiteApi.Services.Storage;
using AzTwWebsiteApi.Services.Blog;

namespace AzTwWebsiteApi.Functions.Blog
{
  public class BlogService : IBlogService
  {
    private readonly ILogger<BlogService> _logger;
    private readonly TableClient _tableClient;
    private readonly IBlobStorageService _blobStorageService;

    public BlogService(ILogger<BlogService> logger,
                       ITableStorageService tableStorageService,
                       IBlobStorageService blobStorageService)
    {
      _logger = logger;
      _tableClient = tableStorageService.GetTableClient("BlogPosts");
      _blobStorageService = blobStorageService;
    }

    // Get all blog posts
    public async Task<IEnumerable<BlogPost>> GetBlogPostsAsync(int pageSize = 25, string continuationToken = null)
    {
      _logger.LogInformation("Fetching blog posts from Azure Table Storage...");
      var posts = new List<BlogPost>();
      int maxPageSize = 100; // Define a maximum page size to prevent excessive queries

      // Enforce upper limit to prevent excessive queries
      pageSize = Math.Min(pageSize, maxPageSize);

      try
      {
        _logger.LogInformation($"Retrieving blog posts with page size: {pageSize}, continuation token: {newContinuationToken}");

        // Retrieve blog posts using the continuation token
        await foreach (var entity in _tableClient.QueryAsync<BlogPost>(
            filter: null, 
            maxPerPage: pageSize,
            continuationToken: continuationToken))
        {
          // Add the post to the list regardless of its publication status (for now)
          posts.Add(entity);
        }
      }

      if (posts.Count == 0)
      {
        _logger.LogInformation("No blog posts found.");
      }
      else
      {
        _logger.LogInformation($"Retrieved {posts.Count} blog posts.");
      }

      return posts;
    }

    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving blog posts");
      throw; // Let the caller handle the exception
    }
  }
}