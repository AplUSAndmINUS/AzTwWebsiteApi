// C# app- GetBlogSinglePostFunction.cs
// Endpoint: api/blog/image/{id}

using System.Net; // HTTP status codes
using Microsoft.Azure.Functions.Worker; // Azure Functions Worker SDK
using Microsoft.Azure.Functions.Worker.Http; // HTTP trigger and response types

using Microsoft.Extensions.Logging; // Structured logging support

using Azure.Storage.Blobs; // Azure Blob Storage operations
using AzTwWebsiteApi.Utils;

// Commenting out for now due to simple structure functions
// using AzTwWebsiteApi.Models.Blog; // Your blog-related models
// using AzTwWebsiteApi.Utils; // Constants for your application

// namespace is for the Azure Function
namespace BlogFunctions
{
  // This class defines an Azure Function to get a blog image
  // It uses the BlobStorageService to retrieve the image from Azure Blob Storage
  // The function is triggered by an HTTP request
  // The function returns the image as a file response

  public class GetBlogSingleImageFunction
  {
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<GetBlogSingleImageFunction> _logger;

    public GetBlogSingleImageFunction(BlobServiceClient blobServiceClient, ILogger<GetBlogSingleImageFunction> logger)
    {
      _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
      _logger = logger;
    }

    [Function("GetBlogSingleImage")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blog/image/{id}")] HttpRequestData req,
        string id)
    {
      _logger.LogFunctionStart(Constants.Modules.Blog, Constants.Functions.GetBlogImage);
      _logger.LogInformation("C# HTTP trigger function processed a request.");
      _logger.LogInformation("Fetching blog image with ID: {Id}", id);
      
      
      // Placeholder logic: Return 404 Not Found
      _logger.LogFunctionComplete(Constants.Modules.Blog, Constants.Functions.GetBlogImage);
      return req.CreateResponse(HttpStatusCode.NotFound);
    }
  }
}