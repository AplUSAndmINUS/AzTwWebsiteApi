using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Utils;

namespace AzTwWebsiteApi.Functions.Blog
{
    public class GetBlogImageFunction
    {
        private readonly ILogger<GetBlogImageFunction> _logger;

        public GetBlogImageFunction(ILogger<GetBlogImageFunction> logger)
        {
            _logger = logger;
        }

        [FunctionName(Constants.Functions.GetBlogImage)]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blog/images/{id}")] HttpRequest req,
            string id)
        {
            _logger.LogFunctionStart(Constants.Modules.Blog, Constants.Functions.GetBlogImage);

            // Return 404 Not Found for now
            _logger.LogFunctionComplete(Constants.Modules.Blog, Constants.Functions.GetBlogImage);
            return new NotFoundResult();
        }
    }
}