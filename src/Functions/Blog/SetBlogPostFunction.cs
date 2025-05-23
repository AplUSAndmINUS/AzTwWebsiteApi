using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using AzTwWebsiteApi.Utils;

namespace AzTwWebsiteApi.Functions.Blog
{
    public class SetBlogPostsFunction
    {
        private readonly ILogger<SetBlogPostsFunction> _logger;

        public SetBlogPostsFunction(ILogger<SetBlogPostsFunction> logger)
        {
            _logger = logger;
        }

        [FunctionName("SetBlogPost")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog/posts")] HttpRequest req)
        {
            _logger.LogFunctionStart(Constants.Modules.Blog, Constants.Functions.SetBlogPosts);

            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // Return success for now
            _logger.LogFunctionComplete(Constants.Modules.Blog, Constants.Functions.SetBlogPosts);
            return new OkResult();
        }

        // [FunctionName(Constants.Functions.SetBlogPosts)]
        // public async Task<IActionResult> Run(
        //     [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog/posts")] HttpRequest req)
        // {
        //     _logger.LogFunctionStart(Constants.Modules.Blog, Constants.Functions.SetBlogPosts);

        //     // Return success for now
        //     _logger.LogFunctionComplete(Constants.Modules.Blog, Constants.Functions.SetBlogPosts);
        //     return new OkResult();
        // }
    }
}