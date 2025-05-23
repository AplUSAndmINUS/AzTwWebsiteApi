// C# app- SetBlogSingleImageFunction.cs
// Endpoint: api/blog/post/{id}

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Utils;

namespace BlogFunctions
{
    public class SetBlogPostsFunction
    {
        private readonly ILogger<SetBlogPostsFunction> _logger;

        public SetBlogPostsFunction(ILogger<SetBlogPostsFunction> logger)
        {
            _logger = logger;
        }

        [Function("SetBlogPost")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog/posts")] HttpRequestData req)
        {
            _logger.LogFunctionStart(Constants.Modules.Blog, Constants.Functions.SetBlogPosts);

            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // Return success for now
            _logger.LogFunctionComplete(Constants.Modules.Blog, Constants.Functions.SetBlogPosts);
            return req.CreateResponse(HttpStatusCode.OK);
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