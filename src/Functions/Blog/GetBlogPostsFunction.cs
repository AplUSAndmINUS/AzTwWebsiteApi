// C# app- GetBlogPostsFunction.cs
// Endpoint: _api/blog/posts

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Models.Blog;
using AzTwWebsiteApi.Services;
using AzTwWebsiteApi.Services.Blog;
using AzTwWebsiteApi.Services.Utils;

namespace AzTwWebsiteApi.Functions.Blog
{
  public class GetBlogPostsFunction
  {
    private readonly ILogger<GetBlogPostsFunction> _logger;
    private readonly IBlogService _blogService;

    public GetBlogPostsFunction(ILogger<GetBlogPostsFunction> logger, IBlogService blogService)
    {
      _logger = logger;
      _blogService = blogService;
    }

    [Function("GetBlogPosts")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "_api/blog/posts")] HttpRequestData req)
    {
      _logger.LogInformation("Function Start: {Module} - {Function}", Constants.Modules.Blog, Constants.Functions.GetBlogPosts);

      try
      {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        int pageSize = int.TryParse(query["pageSize"], out var size) ? size : 25;
        string? continuationToken = query["continuationToken"];

        (IEnumerable<BlogPost> posts, string? nextPageToken) = await _blogService.GetBlogPostsAsync(pageSize, continuationToken);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { 
            posts,
            nextPageToken
        });

        _logger.LogInformation("Function Complete: {Module} - {Function}", Constants.Modules.Blog, Constants.Functions.GetBlogPosts);
        return response;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error occurred while fetching blog posts");
        var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
        await errorResponse.WriteStringAsync("An error occurred while retrieving blog posts.");
        return errorResponse;
      }
    }
  }
}