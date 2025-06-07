using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Models.Blog;
using AzTwWebsiteApi.Services.Storage;
using AzTwWebsiteApi.Services.Utils;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AzTwWebsiteApi.Functions;

public class BlogImageFunctions
{
    private readonly ILogger<BlogImageFunctions> _logger;
    private readonly HandleCrudFunctions _crudFunctions;
    private readonly IMetricsService _metrics;
    private readonly string _blogImagesContainerName;
    private readonly string _connectionString;

    public BlogImageFunctions(
        ILogger<BlogImageFunctions> logger,
        HandleCrudFunctions crudFunctions,
        IMetricsService metrics)
    {
        _logger = logger;
        _crudFunctions = crudFunctions;
        _metrics = metrics;
        
        _connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") 
            ?? throw new ArgumentNullException("AzureWebJobsStorage connection string is not set");
        _blogImagesContainerName = StorageSettings.TransformMockName(
            Environment.GetEnvironmentVariable("BlogImagesContainerName") ?? "mock-blog-images");
        
        _logger.LogInformation("BlogImageFunctions initialized with settings:");
        _logger.LogInformation("Using blog images container name: {ContainerName}", _blogImagesContainerName);
    }

    [Function("GetBlogImageById")]
    public async Task<HttpResponseData> GetBlogImageById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blog/images/{id}")] HttpRequestData req,
        string id)
    {
        const string operation = "GetBlogImageById";
        _logger.LogInformation("Function Start: {Module} - {Operation}. Id: {Id}", 
            Constants.Modules.Blog, operation, id);

        try
        {
            // First get the image metadata from table storage
            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Filter = $"PartitionKey eq 'BlogImage' and RowKey eq '{id}'"
            };

            var result = await _crudFunctions.HandleCrudOperation<BlogImage>(
                operation: Constants.Storage.Operations.Get,
                entityType: Constants.Storage.EntityTypes.BlogImages,
                options: options);

            if (!result.Items.Any())
            {
                return await CreateNotFoundResponse(req, "Blog image not found");
            }

            var imageMetadata = result.Items.First();

            // Now get the actual image data from blob storage using the BlobName
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_blogImagesContainerName);
            var blobClient = containerClient.GetBlobClient(imageMetadata.BlobName);

            if (!await blobClient.ExistsAsync())
            {
                return await CreateNotFoundResponse(req, "Blog image blob not found");
            }

            var blobProperties = await blobClient.GetPropertiesAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", blobProperties.Value.ContentType);
            response.Headers.Add("Content-Length", blobProperties.Value.ContentLength.ToString());
            
            // Stream the blob content directly to the response
            var blobDownload = await blobClient.DownloadStreamingAsync();
            await blobDownload.Value.Content.CopyToAsync(response.Body);
            
            return response;
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter($"{operation}_Error");
            _logger.LogError(ex, "Error getting blog image {Id}: {Error}", id, ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    [Function("UploadBlogImage")]
    public async Task<HttpResponseData> UploadBlogImage(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog/images")] HttpRequestData req)
    {
        const string operation = "UploadBlogImage";
        _logger.LogInformation("Function Start: {Module} - {Operation}", Constants.Modules.Blog, operation);

        try
        {
            // First save the image data to blob storage
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_blogImagesContainerName);
            
            // Generate a unique blob name
            var blobName = $"{Guid.NewGuid()}{Path.GetExtension(req.Headers.GetValues("Content-Filename").FirstOrDefault() ?? "")}";
            var blobClient = containerClient.GetBlobClient(blobName);

            // Set content type from request headers
            var contentType = req.Headers.GetValues("Content-Type").FirstOrDefault() ?? "application/octet-stream";
            var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };

            // Upload the blob
            await blobClient.UploadAsync(req.Body, new BlobUploadOptions { HttpHeaders = blobHttpHeaders });

            // Create and store image metadata
            var blogImage = new BlogImage
            {
                BlogImageId = Guid.NewGuid().ToString(),
                PartitionKey = "BlogImage",
                BlobName = blobName,
                Url = blobClient.Uri.ToString(),
                MimeType = contentType,
                FileSize = req.Body.Length,
                FileName = req.Headers.GetValues("Content-Filename").FirstOrDefault() ?? blobName
            };
            blogImage.RowKey = blogImage.BlogImageId;

            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Data = blogImage
            };

            var result = await _crudFunctions.HandleCrudOperation<BlogImage>(
                operation: Constants.Storage.Operations.Set,
                entityType: Constants.Storage.EntityTypes.BlogImages,
                options: options);

            // Create and store additional metadata
            try
            {
                var metadata = new BlogImageMetadata
                {
                    BlogImageMetadataId = Guid.NewGuid().ToString(),
                    BlogImageId = blogImage.BlogImageId,
                    BlobName = blogImage.BlobName,
                    ImageUrl = blogImage.Url,
                    Title = blogImage.FileName, // Using fileName as default title
                    AltText = string.Empty,
                    Description = string.Empty
                };

                var metadataOptions = new CrudOperationOptions
                {
                    ConnectionString = _connectionString,
                    Data = metadata
                };

                // Store metadata in table storage
                await _crudFunctions.HandleCrudOperation<BlogImageMetadata>(
                    operation: Constants.Storage.Operations.Set,
                    entityType: Constants.Storage.EntityTypes.BlogImageMetadata,
                    options: metadataOptions);
            }
            catch (Exception metadataEx)
            {
                // Log but don't fail the whole operation
                _logger.LogWarning(metadataEx, "Failed to store blog image metadata: {Error}", metadataEx.Message);
            }

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(result.Items.First());
            return response;
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter($"{operation}_Error");
            _logger.LogError(ex, "Error uploading blog image: {Error}", ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }
    
    [Function("DeleteBlogImage")]
    public async Task<HttpResponseData> DeleteBlogImage(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "blog/posts/{postId}/images/{imageId}")] HttpRequestData req,
        string postId, string imageId)
    {
        const string operation = "DeleteBlogImage";
        _logger.LogInformation("Function Start: {Module} - {Operation}. PostId: {PostId}, ImageId: {ImageId}",
            Constants.Modules.Blog, operation, postId, imageId);

        try
        {
            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Filter = $"PartitionKey eq '{postId}' and RowKey eq '{imageId}'"
            };

            var result = await _crudFunctions.HandleCrudOperation<BlogImage>(
                operation: Constants.Storage.Operations.Delete,
                entityType: Constants.Storage.EntityTypes.BlogImages,
                options: options);

            if (!result.Items.Any())
            {
                return await CreateNotFoundResponse(req, "Blog image not found");
            }

            var response = req.CreateResponse(HttpStatusCode.NoContent);
            return response;
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter($"{operation}_Error");
            _logger.LogError(ex, "Error deleting blog image: {Error}", ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, Exception ex)
    {
        var response = req.CreateResponse(HttpStatusCode.InternalServerError);
        var error = new
        {
            Message = "An error occurred processing your request",
            Details = ex.Message,
            TraceId = System.Diagnostics.Activity.Current?.Id ?? "No trace ID available"
        };

        await response.WriteAsJsonAsync(error);
        return response;
    }

    private async Task<HttpResponseData> CreateNotFoundResponse(HttpRequestData req, string message)
    {
        var response = req.CreateResponse(HttpStatusCode.NotFound);
        await response.WriteAsJsonAsync(new { message });
        return response;
    }
}
