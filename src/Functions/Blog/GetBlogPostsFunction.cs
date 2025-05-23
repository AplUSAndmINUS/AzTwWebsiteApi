// C3 app- GetBlogPostsFunction.cs
// Endpoint: api/blog/posts

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Utils;
using AzTwWebsiteApi.Models.Blog;

// Commenting out for now due to simple structure functions
// using AzTwWebsiteApi.Models;
// using AzTwWebsiteApi.Services;

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
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blog/posts")] HttpRequestData req)
    {
      _logger.LogFunctionStart(Constants.Modules.Blog, Constants.Functions.GetBlogPosts);
      _logger.LogInformation("C# HTTP trigger function processed a request.");

      // Return an empty array for now
      var posts = new List<BlogPost>();

      var response = req.CreateResponse(HttpStatusCode.OK);
      await response.WriteAsJsonAsync(posts);

      _logger.LogFunctionComplete(Constants.Modules.Blog, Constants.Functions.GetBlogPosts);
      return response;
    }
  }
}