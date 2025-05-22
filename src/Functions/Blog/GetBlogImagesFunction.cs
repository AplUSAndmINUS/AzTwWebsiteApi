using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using AzTwWebsiteApi.Models.Blog;
using AzTwWebsiteApi.Utils;

namespace AzTwWebsiteApi.Functions.Blog
{
    public class GetBlogImagesFunction
    {
        private readonly ILogger<GetBlogImagesFunction> _logger;

        public GetBlogImagesFunction(ILogger<GetBlogImagesFunction> logger)
        {
            _logger = logger;
        }

        [FunctionName("GetBlogImages")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blog/images")] HttpRequest req)
        {
            _logger.LogFunctionStart(Constants.Modules.Blog, "GetBlogImages");

            // Return an empty array for now
            var images = new List<BlogImage>();

            _logger.LogFunctionComplete(Constants.Modules.Blog, "GetBlogImages");
            return new OkObjectResult(images);
        }
    }
}