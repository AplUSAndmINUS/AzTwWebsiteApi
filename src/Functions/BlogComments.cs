using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Models.Blog;
using AzTwWebsiteApi.Services.Storage;
using AzTwWebsiteApi.Services.Utils;

namespace AzTwWebsiteApi.Functions;

public class BlogCommentFunctions
{
    private readonly ILogger<BlogCommentFunctions> _logger;
    private readonly HandleCrudFunctions _crudFunctions;
    private readonly IMetricsService _metrics;
    private readonly string _blogCommentsTableName;
    private readonly string _connectionString;

    public BlogCommentFunctions(
        ILogger<BlogCommentFunctions> logger,
        HandleCrudFunctions crudFunctions,
        IMetricsService metrics)
    {
        _logger = logger;
        _crudFunctions = crudFunctions;
        _metrics = metrics;
        
        _connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") 
            ?? throw new ArgumentNullException("AzureWebJobsStorage connection string is not set");
        _blogCommentsTableName = StorageSettings.TransformMockName(
            Environment.GetEnvironmentVariable("BlogCommentsTableName") ?? "mockblogcomments");
        
        _logger.LogInformation("BlogCommentFunctions initialized with settings:");
        _logger.LogInformation("Using blog comments table name: {TableName}", _blogCommentsTableName);
    }

    [Function("GetBlogCommentsByPostId")]
    public async Task<HttpResponseData> GetBlogCommentsByPostId(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blog/posts/{postId}/comments")] HttpRequestData req,
        string postId)
    {
        const string operation = "GetBlogCommentsByPostId";
        _logger.LogInformation("Function Start: {Module} - {Operation}. PostId: {PostId}", 
            Constants.Modules.Blog, operation, postId);
        
        try
        {
            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Filter = $"PartitionKey eq '{postId}'"
            };

            var result = await _crudFunctions.HandleCrudOperation<BlogComment>(
                operation: Constants.Storage.Operations.Get,
                entityType: Constants.Storage.EntityTypes.BlogComments,
                options: options);

            if (!result.Items.Any())
            {
                return await CreateNotFoundResponse(req, "No comments found for this blog post");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result.Items);
            return response;
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter($"{operation}_Error");
            _logger.LogError(ex, "Error getting comments for blog post {PostId}: {Error}", postId, ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    [Function("AddBlogComment")]
    public async Task<HttpResponseData> AddBlogComment(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog/posts/{postId}/comments")] HttpRequestData req,
        string postId)
    {
        const string operation = "AddBlogComment";
        _logger.LogInformation("Function Start: {Module} - {Operation}. PostId: {PostId}", 
            Constants.Modules.Blog, operation, postId);

        try
        {
            var blogComment = await JsonSerializer.DeserializeAsync<BlogComment>(req.Body);
            if (blogComment == null) throw new ArgumentNullException(nameof(blogComment));

            // Ensure required Table Storage properties are set
            blogComment.PartitionKey = postId;
            blogComment.RowKey = blogComment.Id;

            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Data = blogComment
            };

            var result = await _crudFunctions.HandleCrudOperation<BlogComment>(
                operation: Constants.Storage.Operations.Set,
                entityType: Constants.Storage.EntityTypes.BlogComments,
                options: options);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(result.Items.First());
            return response;
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter($"{operation}_Error");
            _logger.LogError(ex, "Error adding comment to blog post {PostId}: {Error}", postId, ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    [Function("UpdateBlogComment")]
    public async Task<HttpResponseData> UpdateBlogComment(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "blog/posts/{postId}/comments/{commentId}")] HttpRequestData req,
        string postId, string commentId)
    {
        const string operation = "UpdateBlogComment";
        _logger.LogInformation("Function Start: {Module} - {Operation}. PostId: {PostId}, CommentId: {CommentId}", 
            Constants.Modules.Blog, operation, postId, commentId);
            
        try
        {
            var blogComment = await JsonSerializer.DeserializeAsync<BlogComment>(req.Body);
            if (blogComment == null) return await CreateNotFoundResponse(req, "Comment not found");

            blogComment.PartitionKey = postId;
            blogComment.RowKey = commentId;

            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Data = blogComment
            };

            var result = await _crudFunctions.HandleCrudOperation<BlogComment>(
                operation: Constants.Storage.Operations.Update,
                entityType: Constants.Storage.EntityTypes.BlogComments,
                options: options);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result.Items.First());
            return response;
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter($"{operation}_Error");
            _logger.LogError(ex, "Error updating comment {CommentId} for blog post {PostId}: {Error}", commentId, postId, ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    [Function("DeleteBlogComment")]
    public async Task<HttpResponseData> DeleteBlogComment(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "blog/posts/{postId}/comments/{commentId}")] HttpRequestData req,
        string postId, string commentId)
    {
        const string operation = "DeleteBlogComment";
        _logger.LogInformation("Function Start: {Module} - {Operation}. PostId: {PostId}, CommentId: {CommentId}", 
            Constants.Modules.Blog, operation, postId, commentId);
            
        try
        {
            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Filter = $"PartitionKey eq '{postId}' and RowKey eq '{commentId}'"
            };

            await _crudFunctions.HandleCrudOperation<BlogComment>(
                operation: Constants.Storage.Operations.Delete,
                entityType: Constants.Storage.EntityTypes.BlogComments,
                options: options);

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter($"{operation}_Error");
            _logger.LogError(ex, "Error deleting comment {CommentId} for blog post {PostId}: {Error}", commentId, postId, ex.Message);
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
