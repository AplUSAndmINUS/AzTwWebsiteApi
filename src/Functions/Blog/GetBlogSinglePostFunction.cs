using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Utils;

namespace AzTwWebsiteApi.Functions.Blog
{
  public class GetBlogSinglePostFunction
  {
    private readonly ILogger<GetBlogSinglePostFunction> _logger;

    public GetBlogSinglePostFunction(ILogger<GetBlogSinglePostFunction> logger)
    {
      _logger = logger;
    }

    [FunctionName("GetBlogSinglePost")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blog/post/{id}")] HttpRequest req,
        string id)
    {
      _logger.LogFunctionStart("The GetBlogSinglePost Function is starting");

      _logger.LogInformation("C# HTTP trigger function processed a request.");

      // Return 404 Not Found for now
      _logger.LogFunctionComplete("The GetBlogSinglePost Function has completed and got a 404");
      return new NotFoundResult();
    }
  }
}