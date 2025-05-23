using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Utils;

namespace AzTwWebsiteApi.Functions.Blog
{
  public class SetBlogImageFunction
  {
    private readonly ILogger<SetBlogImageFunction> _logger;

    public SetBlogImageFunction(ILogger<SetBlogImageFunction> logger)
    {
      _logger = logger;
    }

    [FunctionName("SetBlogImage")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog/images/{id}")] HttpRequest req,
        string id)
    {
      _logger.LogFunctionStart(Constants.Modules.Blog, Constants.Functions.SetBlogImage);

      _logger.LogInformation("C# HTTP trigger function processed a request.");

      // Return success for now
      _logger.LogFunctionComplete(Constants.Modules.Blog, Constants.Functions.SetBlogImage);
      return new OkResult();
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