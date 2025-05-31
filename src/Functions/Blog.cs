// C# app- GetBlogPostsFunction.cs
// Endpoints: 
// GetAllBlogPosts: api/blog/posts
// GetBlogPostById: api/blog/posts/{id}
// SetBlogPost: api/blog/posts
// UpdateBlogPost: api/blog/posts/{id}
// DeleteBlogPost: api/blog/posts/{id}

using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Models.Blog;
using AzTwWebsiteApi.Services.Utils;

namespace AzTwWebsiteApi.Functions;

public class BlogFunctions
{
    private readonly ILogger<BlogFunctions> _logger;
    private const string StorageServiceName = Constants.Storage.EntityTypes.Blog;

    public BlogFunctions(ILogger<BlogFunctions> logger)
    {
        _logger = logger;
    }

    // Get all blog posts
    [Function("GetAllBlogPosts")]
    public async Task<HttpResponseData> GetAllBlogPosts(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blog/posts")] HttpRequestData req)
    {
        _logger.LogInformation("Function Start: {Module} - GetAllBlogPosts", Constants.Modules.Blog);

        try
        {
            // Parse query parameters for paging
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            int pageSize = int.TryParse(query["pageSize"], out var size) ? size : 25;
            string? continuationToken = query["continuationToken"];

            // Call HandleCrudOperation
            var posts = await HandleCrudFunctions.HandleCrudOperation<BlogPost>(
                operation: "get",        // What you want to do
                entityType: "blog",      // Tells it to use Table Storage
                pageSize: pageSize,      // Optional: for paging
                continuationToken: continuationToken  // Optional: for paging
            );

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(posts);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blog posts");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ex.Message });
            return response;
        }
    }

    [Function("GetBlogPostById")]
    public async Task<HttpResponseData> GetBlogPostById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blog/posts/{id}")] HttpRequestData req,
        string id)
    {
        _logger.LogInformation("Function Start: {Module} - GetBlogPostById. Id: {Id}", Constants.Modules.Blog, id);

        try
        {
            var result = await HandleCrudFunctions.HandleCrudOperation<BlogPost>(
                "get",
                StorageServiceName,
                filter: $"PartitionKey eq '{id}' and RowKey eq '{id}'");

            if (!result.Any())
            {
                return await CreateNotFoundResponse(req, $"Blog post with ID {id} not found");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result.First());
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetBlogPostById: {Error}", ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    [Function("SetBlogPost")]
    public async Task<HttpResponseData> SetBlogPost(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog/posts")] HttpRequestData req)
    {
        _logger.LogInformation("Function Start: {Module} - SetBlogPost", Constants.Modules.Blog);

        try
        {
            var blogPost = await JsonSerializer.DeserializeAsync<BlogPost>(req.Body);
            if (blogPost == null) throw new ArgumentNullException(nameof(blogPost));

            blogPost.PartitionKey = blogPost.Id;
            blogPost.RowKey = blogPost.Id;

            var result = await HandleCrudFunctions.HandleCrudOperation<BlogPost>(
                "set",
                StorageServiceName,
                data: blogPost);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SetBlogPost: {Error}", ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    [Function("UpdateBlogPost")]
    public async Task<HttpResponseData> UpdateBlogPost(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "blog/posts/{id}")] HttpRequestData req,
        string id)
    {
        _logger.LogInformation("Function Start: {Module} - UpdateBlogPost. Id: {Id}", Constants.Modules.Blog, id);

        try
        {
            var blogPost = await JsonSerializer.DeserializeAsync<BlogPost>(req.Body);
            if (blogPost == null) throw new ArgumentNullException(nameof(blogPost));

            blogPost.PartitionKey = id;
            blogPost.RowKey = id;
            blogPost.Id = id;

            var result = await HandleCrudFunctions.HandleCrudOperation<BlogPost>(
                "update",
                StorageServiceName,
                data: blogPost);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateBlogPost: {Error}", ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    [Function("DeleteBlogPost")]
    public async Task<HttpResponseData> DeleteBlogPost(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "blog/posts/{id}")] HttpRequestData req,
        string id)
    {
        _logger.LogInformation("Function Start: {Module} - DeleteBlogPost. Id: {Id}", Constants.Modules.Blog, id);

        try
        {
            await HandleCrudFunctions.HandleCrudOperation<BlogPost>(
                "delete",
                StorageServiceName,
                filter: $"PartitionKey eq '{id}' and RowKey eq '{id}'");

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteBlogPost: {Error}", ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, Exception ex)
    {
        var response = req.CreateResponse(HttpStatusCode.InternalServerError);
        await response.WriteAsJsonAsync(new
        {
            error = "An error occurred while processing the request",
            message = ex.Message
        });
        return response;
    }

    private async Task<HttpResponseData> CreateNotFoundResponse(HttpRequestData req, string message)
    {
        var response = req.CreateResponse(HttpStatusCode.NotFound);
        await response.WriteAsJsonAsync(new { message });
        return response;
    }
}