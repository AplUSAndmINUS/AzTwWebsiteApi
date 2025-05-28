// using System.Net;
// using Microsoft.Azure.Functions.Worker;
// using Microsoft.Azure.Functions.Worker.Http;
// using Microsoft.Extensions.Logging;
// using AzTwWebsiteApi.Utils;
// using AzTwWebsiteApi.Models.Blog;

// // Commenting out for now due to simple structure functions

// namespace BlogFunctions
// {
//   public class GetBlogImagesFunction
//   {
//     private readonly ILogger<GetBlogImagesFunction> _logger;

//     public GetBlogImagesFunction(ILogger<GetBlogImagesFunction> logger)
//     {
//       _logger = logger;
//     }

//     [Function("GetBlogImages")]
//     public async Task<HttpResponseData> Run(
//         [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blog/images")] HttpRequestData req)
//     {
//       _logger.LogFunctionStart(Constants.Modules.Blog, "GetBlogImages");
//       _logger.LogInformation("C# HTTP trigger function processed a request.");

//       // Return an empty array for now
//       var images = new List<BlogImage>();

//       var response = req.CreateResponse(HttpStatusCode.OK);
//       await response.WriteAsJsonAsync(images);

//       _logger.LogFunctionComplete(Constants.Modules.Blog, "GetBlogImages");
//       return response;
//     }
//   }
// }