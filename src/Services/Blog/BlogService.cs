// C# functions- BlogService.cs
// Endpoints: 

// *BLOG POSTS*
// api/blog/posts
// api/blog/posts/{id}
// api/blog/posts/{id}/update

// *BLOG IMAGES*
// api/blog/images
// api/blog/images/{id}                             
// api/blog/images/{id}/delete
// api/blog/images/{id}/update

// *BLOG COMMENTS*`
// api/blog/posts/{id}/comments
// api/blog/posts/{id}/comments/{commentId}
// api/blog/posts/{id}/comments/{commentId}/delete
// api/blog/posts/{id}/comments/{commentId}/update
// api/blog/posts/{id}/comments/{commentId}/reactions
// api/blog/posts/{id}/comments/{commentId}/replies
// api/blog/posts/{id}/comments/{commentId}/replies/{replyId}
// api/blog/posts/{id}/comments/{commentId}/replies/{replyId}/reactions

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
  public class BlogService
  {
    private readonly ILogger<BlogService> _logger;
    private readonly ITableStorageService _tableStorageService;
    private readonly IBlobStorageService _blobStorageService;

    public BlogService(ILogger<BlogService> logger,
                       ITableStorageService tableStorageService,
                       IBlobStorageService blobStorageService)
    {
      _logger = logger;
      _tableStorageService = tableStorageService;
      _blobStorageService = blobStorageService;
    }

    // Get all blog posts, paginated at 25 per each
    // Endpoint: api/blog/posts
    // Query parameter: pageSize (optional, default 25, max 100)
    [Function("GetBlogPosts")]
    public async Task<HttpResponseData> GetBlogPosts(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blog/posts")] HttpRequestData req)
    {
      _logger.LogInformation("C# HTTP trigger function processed a request for blog posts.");

      int defaultPageSize = 25;
      int maxPageSize = 100; // Prevent excessive queries
      int pageSize = defaultPageSize;

      if (req.Query.ContainsKey("pageSize") && 
          int.TryParse(req.Query["pageSize"], out int requestedPageSize) && requestedPageSize > 0)
      {
          // Enforce upper limit to prevent excessive queries
          pageSize = Math.Min(requestedPageSize, maxPageSize);
      }

      try
      {
        var (posts, newContinuationToken) = await _tableStorageService.GetPaginatedAsync<BlogPost>(pageSize, continuationToken);
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { posts, continuationToken = newContinuationToken });
        return response;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving blog posts");
        var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
        await errorResponse.WriteStringAsync("An error occurred while retrieving blog posts.");
        return errorResponse;
      }
    }

    // Get a specific blog post by ID
    // Endpoint: api/blog/posts/{id}
    [Function("GetBlogPostById")]
    public async Task<HttpResponseData> GetBlogPostById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blog/posts/{id}")] HttpRequestData req,
        string id)
    {
      _logger.LogInformation($"C# HTTP trigger function processed a request for blog post with ID: {id}");

      try
      {
        var post = await _tableStorageService.GetAsync<BlogPost>(authorId, id);
        if (post == null)
        {
          var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
          await notFoundResponse.WriteStringAsync("Blog post not found.");
          return notFoundResponse;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(post);
        return response;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving blog post with ID: {id}, Requester IP: {ip}", id, req.HttpContext.Connection.RemoteIpAddress);
        _logger.LogError("Exception details: {exception}", ex.ToString());
        var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
        await errorResponse.WriteStringAsync("An error occurred while retrieving the blog post.");
        return errorResponse;
      }
    }
  }
}