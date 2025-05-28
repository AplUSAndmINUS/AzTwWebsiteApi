// C# app- GetBlogPostsFunction.cs
// Endpoint: _api/blog/posts

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using Azure.Identity;
using AzTwWebsiteApi.Services.Utils;
using AzTwWebsiteApi.Models.Blog;

namespace AzTwWebsiteApi.Functions.Blog
{
  public class GetBlogPostsFunction
  {
    private readonly ILogger<GetBlogPostsFunction> _logger;

    public GetBlogPostsFunction(ILogger<GetBlogPostsFunction> logger)
    {
      _logger = logger;
    }

    [Function("GetBlogPosts")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "_api/blog/posts")] HttpRequestData req)
    {
      _logger.LogInformation("Function Start: {Module} - {Function}", Constants.Modules.Blog, Constants.Functions.GetBlogPosts);
      _logger.LogInformation("C# HTTP trigger function processed a request.");

      // Return an empty array for now
      var posts = new List<BlogPost>();

      var response = req.CreateResponse(HttpStatusCode.OK);
      await response.WriteAsJsonAsync(posts);

      _logger.LogInformation("Function Complete: {Module} - {Function}", Constants.Modules.Blog, Constants.Functions.GetBlogPosts);
      return response;
    }
  }
}