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
            var result = await HandleCrudFunctions.HandleCrudOperation<BlogPost>(
                operation: Constants.Storage.Operations.Get,
                entityType: Constants.Storage.EntityTypes.Blog,
                pageSize: pageSize,
                continuationToken: continuationToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blog posts: {Error}", ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    // Get a single blog post by ID
    [Function("GetBlogPostById")]
    public async Task<HttpResponseData> GetBlogPostById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blog/posts/{id}")] HttpRequestData req,
        string id)
    {
        _logger.LogInformation("Function Start: {Module} - GetBlogPostById. Id: {Id}", Constants.Modules.Blog, id);

        try
        {
            var result = await HandleCrudFunctions.HandleCrudOperation<BlogPost>(
                operation: Constants.Storage.Operations.Get,
                entityType: Constants.Storage.EntityTypes.Blog,
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
            _logger.LogError(ex, "Error getting blog post {Id}: {Error}", id, ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    // Create a new blog post
    [Function("CreateBlogPost")]
    public async Task<HttpResponseData> CreateBlogPost(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog/posts")] HttpRequestData req)
    {
        _logger.LogInformation("Function Start: {Module} - CreateBlogPost", Constants.Modules.Blog);

        try
        {
            var blogPost = await JsonSerializer.DeserializeAsync<BlogPost>(req.Body);
            if (blogPost == null) throw new ArgumentNullException(nameof(blogPost));

            // Ensure required Table Storage properties are set
            blogPost.PartitionKey = blogPost.Id;
            blogPost.RowKey = blogPost.Id;

            var result = await HandleCrudFunctions.HandleCrudOperation(
                operation: Constants.Storage.Operations.Set,
                entityType: Constants.Storage.EntityTypes.Blog,
                data: blogPost);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(result.First());
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating blog post: {Error}", ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    // Update an existing blog post
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

            var result = await HandleCrudFunctions.HandleCrudOperation(
                operation: Constants.Storage.Operations.Update,
                entityType: Constants.Storage.EntityTypes.Blog,
                data: blogPost);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result.First());
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating blog post {Id}: {Error}", id, ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    // Delete a blog post
    [Function("DeleteBlogPost")]
    public async Task<HttpResponseData> DeleteBlogPost(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "blog/posts/{id}")] HttpRequestData req,
        string id)
    {
        _logger.LogInformation("Function Start: {Module} - DeleteBlogPost. Id: {Id}", Constants.Modules.Blog, id);

        try
        {
            await HandleCrudFunctions.HandleCrudOperation<BlogPost>(
                operation: Constants.Storage.Operations.Delete,
                entityType: Constants.Storage.EntityTypes.Blog,
                filter: $"PartitionKey eq '{id}' and RowKey eq '{id}'");

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blog post {Id}: {Error}", id, ex.Message);
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