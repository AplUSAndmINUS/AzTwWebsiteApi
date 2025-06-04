using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Models.Blog;
using AzTwWebsiteApi.Services.Storage;
using AzTwWebsiteApi.Services.Utils;

namespace AzTwWebsiteApi.Functions;

public class BlogPostFunctions
{
    private readonly ILogger<BlogPostFunctions> _logger;
    private readonly HandleCrudFunctions _crudFunctions;
    private readonly IMetricsService _metrics;
    private readonly string _blogPostsTableName;
    private readonly string _connectionString;

    public BlogPostFunctions(
        ILogger<BlogPostFunctions> logger,
        HandleCrudFunctions crudFunctions,
        IMetricsService metrics)
    {
        _logger = logger;
        _crudFunctions = crudFunctions;
        _metrics = metrics;
        
        _connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") 
            ?? throw new ArgumentNullException("AzureWebJobsStorage connection string is not set");
        _blogPostsTableName = StorageSettings.TransformMockName(
            Environment.GetEnvironmentVariable("BlogPostsTableName") ?? "mockblog");
        
        _logger.LogInformation("BlogPostFunctions initialized with settings:");
        _logger.LogInformation("Using blog posts table name: {TableName}", _blogPostsTableName);
    }

    // Get all blog posts
    [Function("GetAllBlogPosts")]
    public async Task<HttpResponseData> GetAllBlogPosts(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blog/posts")] HttpRequestData req)
    {
        const string operation = "GetAllBlogPosts";
        using var timer = new OperationTimer(operation, _metrics);
        _logger.LogInformation("Function Start: {Module} - {Operation}", Constants.Modules.Blog, operation);

        try
        {
            // Parse query parameters for paging
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            int pageSize = int.TryParse(query["pageSize"], out var size) ? size : 25;
            string? continuationToken = query["continuationToken"];

            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                PageSize = pageSize,
                ContinuationToken = continuationToken,
                Filter = $"Status eq '{Constants.Blog.Status.Published}'"
            };

            // Call HandleCrudOperation with transformed table name
            var result = await _crudFunctions.HandleCrudOperation<BlogPost>(
                operation: Constants.Storage.Operations.GetPaged,
                entityType: Constants.Storage.EntityTypes.Blog,
                options: options);

            _metrics.IncrementCounter($"{operation}_Success");
            _metrics.RecordValue($"{operation}_ResultCount", result.Items.Count);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter($"{operation}_Error");
            _logger.LogError(ex, "Error in {Operation}: {Error}", operation, ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    // Get a single blog post by ID
    [Function("GetBlogPostById")]
    public async Task<HttpResponseData> GetBlogPostById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blog/posts/{id}")] HttpRequestData req,
        string id)
    {
        const string operation = "GetBlogPostById";
        _logger.LogInformation("Function Start: {Module} - {Operation}. Id: {Id}", 
            Constants.Modules.Blog, operation, id);

        try
        {
            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Filter = $"PartitionKey eq '{id}' and RowKey eq '{id}'"
            };

            var result = await _crudFunctions.HandleCrudOperation<BlogPost>(
                operation: Constants.Storage.Operations.Get,
                entityType: Constants.Storage.EntityTypes.Blog,
                options: options);

            if (!result.Items.Any())
            {
                return await CreateNotFoundResponse(req, $"Blog post with ID {id} not found");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result.Items.First());
            return response;
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter($"{operation}_Error");
            _logger.LogError(ex, "Error getting blog post {Id}: {Error}", id, ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    // Create a new blog post
    [Function("CreateBlogPost")]
    public async Task<HttpResponseData> CreateBlogPost(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog/posts")] HttpRequestData req)
    {
        const string operation = "CreateBlogPost";
        _logger.LogInformation("Function Start: {Module} - {Operation}", Constants.Modules.Blog, operation);

        try
        {
            var blogPost = await JsonSerializer.DeserializeAsync<BlogPost>(req.Body);
            if (blogPost == null) throw new ArgumentNullException(nameof(blogPost));

            // Ensure required Table Storage properties are set
            blogPost.PartitionKey = blogPost.Id;
            blogPost.RowKey = blogPost.Id;
            blogPost.Status = Constants.Blog.Status.Draft;

            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Data = blogPost
            };

            var result = await _crudFunctions.HandleCrudOperation<BlogPost>(
                operation: Constants.Storage.Operations.Set,
                entityType: Constants.Storage.EntityTypes.Blog,
                options: options);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(result.Items.First());
            return response;
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter($"{operation}_Error");
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
        const string operation = "UpdateBlogPost";
        _logger.LogInformation("Function Start: {Module} - {Operation}. Id: {Id}", 
            Constants.Modules.Blog, operation, id);

        try
        {
            var blogPost = await JsonSerializer.DeserializeAsync<BlogPost>(req.Body);
            if (blogPost == null) throw new ArgumentNullException(nameof(blogPost));

            blogPost.PartitionKey = id;
            blogPost.RowKey = id;
            blogPost.Id = id;

            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Data = blogPost
            };

            var result = await _crudFunctions.HandleCrudOperation<BlogPost>(
                operation: Constants.Storage.Operations.Update,
                entityType: Constants.Storage.EntityTypes.Blog,
                options: options);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result.Items.First());
            return response;
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter($"{operation}_Error");
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
        const string operation = "DeleteBlogPost";
        _logger.LogInformation("Function Start: {Module} - {Operation}. Id: {Id}", 
            Constants.Modules.Blog, operation, id);

        try
        {
            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Filter = $"PartitionKey eq '{id}' and RowKey eq '{id}'"
            };

            var result = await _crudFunctions.HandleCrudOperation<BlogPost>(
                operation: Constants.Storage.Operations.Delete,
                entityType: Constants.Storage.EntityTypes.Blog,
                options: options);

            var response = req.CreateResponse(HttpStatusCode.NoContent);
            return response;
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter($"{operation}_Error");
            _logger.LogError(ex, "Error deleting blog post {Id}: {Error}", id, ex.Message);
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
