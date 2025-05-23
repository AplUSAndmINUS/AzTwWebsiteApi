// C3 app- GetBlogPostsFunction.cs
// Endpoint: api/blog/posts

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using BlogReaderFunction.Models;
using BlogReaderFunction.Services;
using BlogReaderFunction.Utils;

namespace BlogReaderFunction.Functions
{
    public class GetBlogPostsFunction
    {
        private readonly ILogger<GetBlogPostsFunction> _logger;

        public GetBlogPostsFunction(ILogger<GetBlogPostsFunction> logger)
        {
            _logger = logger;
        }

        [FunctionName("GetBlogPosts")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blog/posts")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // Return an empty array for now
            var posts = new List<BlogPost>();

            return new OkObjectResult(posts);
        }
    }
}