// // C# app- GetBlogSinglePostFunction.cs
// // Endpoint: api/blog/post/{id}

// using System.Net;
// using Microsoft.Azure.Functions.Worker;
// using Microsoft.Azure.Functions.Worker.Http;
// using Microsoft.Extensions.Logging;
// using AzTwWebsiteApi.Utils;

// namespace BlogFunctions
// {
//   public class GetBlogSinglePostFunction
//   {
//     private readonly ILogger<GetBlogSinglePostFunction> _logger;

//     public GetBlogSinglePostFunction(ILogger<GetBlogSinglePostFunction> logger)
//     {
//       _logger = logger;
//     }

//     [Function("GetBlogSinglePost")]
//     public async Task<HttpResponseData> Run(
//         [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blog/post/{id}")] HttpRequestData req,
//         string id)
//     {
//       _logger.LogFunctionStart(Constants.Modules.Blog, "GetBlogSinglePost");

//       _logger.LogInformation("C# HTTP trigger function processed a request.");

//       // Return 404 Not Found for now
//       _logger.LogFunctionComplete(Constants.Modules.Blog, "GetBlogSinglePost");
//       return req.CreateResponse(HttpStatusCode.NotFound);
//     }
//   }
// }