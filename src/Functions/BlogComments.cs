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
            // Get pagination parameters from query
            var pageSize = GetQueryParamAsInt(req, "pageSize", 25);
            var showUnapproved = GetQueryParamAsBool(req, "showUnapproved", false);

            var filter = $"PartitionKey eq '{postId}'";
            if (!showUnapproved)
            {
                filter += " and IsApproved eq true and IsSpam eq false";
            }

            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Filter = filter
            };

            var result = await _crudFunctions.HandleCrudOperation<BlogComment>(
                operation: Constants.Storage.Operations.Get,
                entityType: _blogCommentsTableName,
                options: options);

            if (!result.Items.Any())
            {
                return await CreateNotFoundResponse(req, "No comments found for this blog post");
            }

            // Sort comments by publish date
            var sortedComments = result.Items.OrderByDescending(c => c.PublishDate);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                Items = sortedComments,
                PageSize = pageSize,
                TotalItems = result.Items.Count()
            });
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
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var blogComment = await JsonSerializer.DeserializeAsync<BlogComment>(req.Body, jsonOptions);
            if (blogComment == null) throw new ArgumentNullException(nameof(blogComment));

            _logger.LogInformation("Received comment: {@BlogComment}", blogComment);

            // Validate comment
            await ValidateBlogComment(blogComment);

            // Initialize the comment properly
            blogComment.Initialize(postId);

            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Data = blogComment
            };

            var result = await _crudFunctions.HandleCrudOperation<BlogComment>(
                operation: Constants.Storage.Operations.Set,
                entityType: _blogCommentsTableName,
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

            // Validate comment
            await ValidateBlogComment(blogComment);

            blogComment.PartitionKey = postId;
            blogComment.RowKey = commentId;
            blogComment.LastModified = DateTime.UtcNow;

            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Data = blogComment
            };

            var result = await _crudFunctions.HandleCrudOperation<BlogComment>(
                operation: Constants.Storage.Operations.Update,
                entityType: _blogCommentsTableName,
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
                entityType: _blogCommentsTableName,
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

    [Function("ApproveComment")]
    public async Task<HttpResponseData> ApproveComment(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog/posts/{postId}/comments/{commentId}/approve")] HttpRequestData req,
        string postId, string commentId)
    {
        const string operation = "ApproveComment";
        _logger.LogInformation("Function Start: {Module} - {Operation}. PostId: {PostId}, CommentId: {CommentId}", 
            Constants.Modules.Blog, operation, postId, commentId);
            
        try
        {
            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Filter = $"PartitionKey eq '{postId}' and RowKey eq '{commentId}'"
            };

            var result = await _crudFunctions.HandleCrudOperation<BlogComment>(
                operation: Constants.Storage.Operations.Get,
                entityType: _blogCommentsTableName,
                options: options);

            var comment = result.Items.FirstOrDefault();
            if (comment == null) return await CreateNotFoundResponse(req, "Comment not found");

            comment.IsApproved = true;
            comment.IsSpam = false;
            comment.LastModified = DateTime.UtcNow;

            options.Data = comment;
            result = await _crudFunctions.HandleCrudOperation<BlogComment>(
                operation: Constants.Storage.Operations.Update,
                entityType: _blogCommentsTableName,
                options: options);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result.Items.First());
            return response;
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter($"{operation}_Error");
            _logger.LogError(ex, "Error approving comment {CommentId} for blog post {PostId}: {Error}", commentId, postId, ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    [Function("MarkAsSpam")]
    public async Task<HttpResponseData> MarkAsSpam(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog/posts/{postId}/comments/{commentId}/spam")] HttpRequestData req,
        string postId, string commentId)
    {
        const string operation = "MarkAsSpam";
        _logger.LogInformation("Function Start: {Module} - {Operation}. PostId: {PostId}, CommentId: {CommentId}", 
            Constants.Modules.Blog, operation, postId, commentId);
            
        try
        {
            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Filter = $"PartitionKey eq '{postId}' and RowKey eq '{commentId}'"
            };

            var result = await _crudFunctions.HandleCrudOperation<BlogComment>(
                operation: Constants.Storage.Operations.Get,
                entityType: _blogCommentsTableName,
                options: options);

            var comment = result.Items.FirstOrDefault();
            if (comment == null) return await CreateNotFoundResponse(req, "Comment not found");

            comment.IsSpam = true;
            comment.IsApproved = false;
            comment.IsLiked = false;
            comment.LastModified = DateTime.UtcNow;

            options.Data = comment;
            result = await _crudFunctions.HandleCrudOperation<BlogComment>(
                operation: Constants.Storage.Operations.Update,
                entityType: _blogCommentsTableName,
                options: options);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result.Items.First());
            return response;
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter($"{operation}_Error");
            _logger.LogError(ex, "Error marking comment {CommentId} as spam for blog post {PostId}: {Error}", commentId, postId, ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    [Function("MarkAsLiked")]
    public async Task<HttpResponseData> MarkAsLiked(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog/posts/{postId}/comments/{commentId}/like")] HttpRequestData req,
        string postId, string commentId)
    {
        const string operation = "MarkAsLiked";
        _logger.LogInformation("Function Start: {Module} - {Operation}. PostId: {PostId}, CommentId: {CommentId}",
            Constants.Modules.Blog, operation, postId, commentId);

        try
        {
            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Filter = $"PartitionKey eq '{postId}' and RowKey eq '{commentId}'"
            };

            var result = await _crudFunctions.HandleCrudOperation<BlogComment>(
                operation: Constants.Storage.Operations.Get,
                entityType: _blogCommentsTableName,
                options: options);

            var comment = result.Items.FirstOrDefault();
            if (comment == null) return await CreateNotFoundResponse(req, "Comment not found");

            comment.IsLiked = !comment.IsLiked; // Toggle like status
            comment.LastModified = DateTime.UtcNow;

            options.Data = comment;
            result = await _crudFunctions.HandleCrudOperation<BlogComment>(
                operation: Constants.Storage.Operations.Update,
                entityType: _blogCommentsTableName,
                options: options);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result.Items.First());
            return response;
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter($"{operation}_Error");
            _logger.LogError(ex, "Error liking comment {CommentId} for blog post {PostId}: {Error}", commentId, postId, ex.Message);
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

    private static int GetQueryParamAsInt(HttpRequestData req, string paramName, int defaultValue)
    {
        var queryDictionary = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var value = queryDictionary[paramName];
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    private static bool GetQueryParamAsBool(HttpRequestData req, string paramName, bool defaultValue)
    {
        var queryDictionary = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var value = queryDictionary[paramName];
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    private static Task ValidateBlogComment(BlogComment comment)
    {
        if (string.IsNullOrWhiteSpace(comment.Content))
            throw new ArgumentException("Comment content is required");

        if (string.IsNullOrWhiteSpace(comment.AuthorName))
            throw new ArgumentException("Author name is required");

        if (!string.IsNullOrWhiteSpace(comment.EmailAddress) && 
            !System.Text.RegularExpressions.Regex.IsMatch(comment.EmailAddress, 
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            throw new ArgumentException("Invalid email address format");
        }

        return Task.CompletedTask;
    }
}
