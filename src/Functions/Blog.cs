// C# app- GetBlogPostsFunction.cs
// Description: This file contains the Azure Functions for managing 
// blog posts, comments, and images. Endpoints listed below due to page brevity.

/** Endpoints: **/

/** BlogPosts: **/
// GetAllBlogPosts: api/blog/posts
// GetBlogPostById: api/blog/posts/{id}
// CreateBlogPost: api/blog/posts
// UpdateBlogPost: api/blog/posts/{id}
// DeleteBlogPost: api/blog/posts/{id}

/** BlogComments: **/
// GetBlogCommentsByPostId: api/blog/posts/{postId}/comments
// AddBlogComment: api/blog/posts/{postId}/comments
// UpdateBlogComment: api/blog/posts/{postId}/comments/{commentId}
// DeleteBlogComment: api/blog/posts/{postId}/comments/{commentId}

/** BlogImages: **/
// GetBlogImageById: api/blog/images/{id}
// UploadBlogImage: api/blog/images

using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Models.Blog;
using AzTwWebsiteApi.Services.Storage;
using AzTwWebsiteApi.Services.Utils;

namespace AzTwWebsiteApi.Functions;

public class BlogFunctions
{
    private readonly ILogger<BlogFunctions> _logger;
    private readonly HandleCrudFunctions _crudFunctions;
    private readonly IMetricsService _metrics;
    private readonly string _blogPostsTableName = StorageSettings.TransformMockName(Environment.GetEnvironmentVariable("BlogPostsTableName") ?? "mockblog");
    private readonly string _blogCommentsTableName = StorageSettings.TransformMockName(Environment.GetEnvironmentVariable("BlogCommentsTableName") ?? "mockblogcomments");
    private readonly string _blogImagesContainerName = StorageSettings.TransformMockName(Environment.GetEnvironmentVariable("BlogImagesContainerName") ?? "mock-blog-images");

    public BlogFunctions(
        ILogger<BlogFunctions> logger,
        HandleCrudFunctions crudFunctions,
        IMetricsService metrics)
    {
        _logger = logger;
        _crudFunctions = crudFunctions;
        _metrics = metrics;
        
        _logger.LogInformation("BlogFunctions initialized with settings:");
        _logger.LogInformation("Using blog comments table name: {TableName}", _blogCommentsTableName);
        _logger.LogInformation("Using blog posts table name: {TableName}", _blogPostsTableName);
        _logger.LogInformation("Using blog images container name: {ContainerName}", _blogImagesContainerName);
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

            // Call HandleCrudOperation with transformed table name
            var result = await _crudFunctions.HandleCrudOperation<BlogPost>(
                operation: Constants.Storage.Operations.Get,
                entityType: Constants.Storage.EntityTypes.Blog,  // Use the constant instead of transformed name
                pageSize: pageSize,
                continuationToken: continuationToken);

            _metrics.IncrementCounter($"{operation}_Success");
            _metrics.RecordValue($"{operation}_ResultCount", result.Count());

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
        _logger.LogInformation("Function Start: {Module} - GetBlogPostById. Id: {Id}", Constants.Modules.Blog, id);

        try
        {
            var result = await _crudFunctions.HandleCrudOperation<BlogPost>(
                operation: Constants.Storage.Operations.Get,
                entityType: Constants.Storage.EntityTypes.Blog,  // Use the constant instead of transformed name
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

            var result = await _crudFunctions.HandleCrudOperation(
                operation: Constants.Storage.Operations.Set,
                entityType: Constants.Storage.EntityTypes.Blog,  // Use the constant instead of transformed name
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

            var result = await _crudFunctions.HandleCrudOperation(
                operation: Constants.Storage.Operations.Update,
                entityType: Constants.Storage.EntityTypes.Blog,  // Use the constant instead of transformed name
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
            await _crudFunctions.HandleCrudOperation<BlogPost>(
                operation: Constants.Storage.Operations.Delete,
                entityType: Constants.Storage.EntityTypes.Blog,  // Use the constant instead of transformed name
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

    [Function("GetBlogCommentsByPostId")]
    public async Task<HttpResponseData> GetBlogCommentsByPostId(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blog/posts/{postId}/comments")] HttpRequestData req,
        string postId)
    {
        _logger.LogInformation("Function Start: {Module} - GetBlogCommentsByPostId. PostId: {PostId}", Constants.Modules.Blog, postId);
        try
        {
            var comments = await _crudFunctions.HandleCrudOperation<BlogComment>(
                operation: Constants.Storage.Operations.Get,
                entityType: _blogPostsTableName,
                filter: $"PartitionKey eq '{postId}'");

            if (!comments.Any())
            {
                return await CreateNotFoundResponse(req, "No comments found for this blog post");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(comments);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments for blog post {PostId}: {Error}", postId, ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    [Function("AddBlogComment")]
    public async Task<HttpResponseData> AddBlogComment(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog/posts/{postId}/comments")] HttpRequestData req,
        string postId)
    {
        _logger.LogInformation("Function Start: {Module} - AddBlogComment. PostId: {PostId}", Constants.Modules.Blog, postId);

        try
        {
            var blogComment = await JsonSerializer.DeserializeAsync<BlogComment>(req.Body);
            if (blogComment == null) throw new ArgumentNullException(nameof(blogComment));

            // Ensure required Table Storage properties are set
            blogComment.PartitionKey = postId;
            blogComment.RowKey = blogComment.Id;

            var comment = await _crudFunctions.HandleCrudOperation(
                operation: Constants.Storage.Operations.Set,
                entityType: _blogCommentsTableName,
                data: blogComment);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(comment.First());
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to blog post {PostId}: {Error}", postId, ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    [Function("UpdateBlogComment")]
    public async Task<HttpResponseData> UpdateBlogComment(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "blog/posts/{postId}/comments/{commentId}")] HttpRequestData req,
        string postId, string commentId)
    {
        _logger.LogInformation("Function Start: {Module} - UpdateBlogComment. PostId: {PostId}, CommentId: {CommentId}", Constants.Modules.Blog, postId, commentId);
        try
        {
            var blogComment = await JsonSerializer.DeserializeAsync<BlogComment>(req.Body);
            if (blogComment == null) return await CreateNotFoundResponse(req, "Comment not found");

            blogComment.PartitionKey = postId;
            blogComment.RowKey = commentId;

            var comment = await _crudFunctions.HandleCrudOperation(
                operation: Constants.Storage.Operations.Update,
                entityType: _blogCommentsTableName,
                data: blogComment);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(comment.First());
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating comment {CommentId} for blog post {PostId}: {Error}", commentId, postId, ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    [Function("DeleteBlogComment")]
    public async Task<HttpResponseData> DeleteBlogComment(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "blog/posts/{postId}/comments/{commentId}")] HttpRequestData req,
        string postId, string commentId)
    {
        _logger.LogInformation("Function Start: {Module} - DeleteBlogComment. PostId: {PostId}, CommentId: {CommentId}", Constants.Modules.Blog, postId, commentId);
        try
        {
            await _crudFunctions.HandleCrudOperation<BlogComment>(
                operation: Constants.Storage.Operations.Delete,
                entityType: _blogCommentsTableName,
                filter: $"PartitionKey eq '{postId}' and RowKey eq '{commentId}'");

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId} for blog post {PostId}: {Error}", commentId, postId, ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    [Function("GetBlogImageById")]
    public async Task<HttpResponseData> GetBlogImageById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blog/images/{id}")] HttpRequestData req,
        string id)
    {
        _logger.LogInformation("Function Start: {Module} - GetBlogImageById. Id: {Id}", Constants.Modules.Blog, id);

        try
        {
            var images = await _crudFunctions.HandleBlobOperation<BlogImage>(
                operation: Constants.Storage.Operations.Get,
                entityType: Constants.Storage.EntityTypes.BlogImages,
                blobName: id);
                
            var image = images.FirstOrDefault();

            if (image == null) return await CreateNotFoundResponse(req, "Blog image not found");

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(image);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blog image {Id}: {Error}", id, ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }

    [Function("UploadBlogImage")]
    public async Task<HttpResponseData> UploadBlogImage(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "blog/images")] HttpRequestData req)
    {
        _logger.LogInformation("Function Start: {Module} - UploadBlogImage", Constants.Modules.Blog);

        try
        {
            var blogImage = await JsonSerializer.DeserializeAsync<BlogImage>(req.Body);
            if (blogImage == null) throw new ArgumentNullException(nameof(blogImage));

            // Ensure required fields are set
            if (string.IsNullOrEmpty(blogImage.BlogImageId))
            {
                blogImage.BlogImageId = Guid.NewGuid().ToString();
            }

            // Set timestamps
            var now = DateTimeOffset.UtcNow;
            blogImage.CreatedAt = now;
            blogImage.LastModified = now;

            HttpResponseData response;

            try
            {
                // Store the image in blob storage
                var savedImage = await _crudFunctions.HandleBlobOperation<BlogImage>(
                    operation: Constants.Storage.Operations.Set,
                    entityType: Constants.Storage.EntityTypes.BlogImages,
                    data: blogImage,
                    blobName: blogImage.BlogImageId);

                // If the URL is provided, try to store metadata
                if (!string.IsNullOrEmpty(blogImage.Url))
                {
                    try
                    {
                        var metadata = new BlogImageMetadata
                        {
                            BlogImageMetadataId = Guid.NewGuid().ToString(),
                            BlogImageId = blogImage.BlogImageId,
                            BlobName = blogImage.BlobName,
                            ImageUrl = blogImage.Url,
                            Title = blogImage.BlogImageId, // Default to ID if no title provided
                            AltText = string.Empty,
                            Description = string.Empty
                        };

                        // Store metadata in table storage
                        await _crudFunctions.HandleCrudOperation<BlogImageMetadata>(
                            operation: Constants.Storage.Operations.Set,
                            entityType: Constants.Storage.EntityTypes.BlogImageMetadata,
                            data: metadata);
                    }
                    catch (Exception metadataEx)
                    {
                        // Log but don't fail the whole operation
                        _logger.LogWarning(metadataEx, "Failed to store blog image metadata: {Error}", metadataEx.Message);
                    }
                }

                response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(savedImage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading blog image: {Error}", ex.Message);
                return await CreateErrorResponse(req, ex);
            }

            return response;


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading blog image: {Error}", ex.Message);
            return await CreateErrorResponse(req, ex);
        }
    }
}