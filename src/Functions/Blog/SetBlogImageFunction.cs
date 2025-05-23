// C# app- GetBlogSingleImageFunction.cs
// Endpoint: api/blog/image/{id}

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Utils;

namespace BlogFunctions
{
  public class SetBlogImageFunction
  {
    private readonly ILogger<SetBlogImageFunction> _logger;

    public SetBlogImageFunction(ILogger<SetBlogImageFunction> logger)
    {
      _logger = logger;
    }

    [Function("SetBlogImage")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog/images/{id}")] HttpRequestData req,
        string id)
    {
      _logger.LogFunctionStart(Constants.Modules.Blog, Constants.Functions.SetBlogImage);

      _logger.LogInformation("C# HTTP trigger function processed a request.");

      // Return success for now
      _logger.LogFunctionComplete(Constants.Modules.Blog, Constants.Functions.SetBlogImage);
      return req.CreateResponse(HttpStatusCode.OK);
    }

    // [FunctionName(Constants.Functions.SetBlogImage)]
    // public async Task<IActionResult> Run(
    //     [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog/images/{id}")] HttpRequest req,
    //     string id)
    // {
    //   _logger.LogFunctionStart(Constants.Modules.Blog, Constants.Functions.SetBlogImage);

    //   // Return success for now
    //   _logger.LogFunctionComplete(Constants.Modules.Blog, Constants.Functions.SetBlogImage);
    //   return new OkResult();
    // }
  }
}