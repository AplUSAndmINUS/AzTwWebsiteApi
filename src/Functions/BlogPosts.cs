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
        
        // Get the already-transformed table name from the environment
        _blogPostsTableName = Environment.GetEnvironmentVariable("BlogPostsTableName") ?? "mockblog";
        
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
            // Parse query parameters for paging and filtering
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            int pageSize = int.TryParse(query["pageSize"], out var size) ? size : 25;
            string? continuationToken = query["continuationToken"];
            string? status = query["status"];

            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                PageSize = pageSize,
                ContinuationToken = continuationToken,
                Filter = !string.IsNullOrEmpty(status) ? $"Status eq '{status}'" : null // Only filter by status if provided
            };

            // Log the query details for debugging
            _logger.LogInformation("Querying blog posts with settings: TableName={TableName}, PageSize={PageSize}, Filter={Filter}", 
                _blogPostsTableName, pageSize, options.Filter ?? "No filter");

            // Call HandleCrudOperation with transformed table name
            var result = await _crudFunctions.HandleCrudOperation<BlogPost>(
                operation: Constants.Storage.Operations.GetPaged,
                entityType: _blogPostsTableName,
                options: options);

            _metrics.IncrementCounter($"{operation}_Success");
            _metrics.RecordValue($"{operation}_ResultCount", result.Items.Count);

            _logger.LogInformation("Found {Count} blog posts", result.Items.Count);
            if (result.Items.Count > 0)
            {
                _logger.LogInformation("First blog post: Id={Id}, Title={Title}, Status={Status}", 
                    result.Items[0].Id, result.Items[0].Title, result.Items[0].Status);
            }

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
                entityType: _blogPostsTableName,
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
            string requestBody;
            using (var reader = new StreamReader(req.Body))
            {
                requestBody = await reader.ReadToEndAsync();
                _logger.LogInformation("Received request body: {Body}", requestBody);
            }

            using var jsonDoc = JsonDocument.Parse(requestBody);
            var element = jsonDoc.RootElement;

            string TryGetPropertyCaseInsensitive(JsonElement elem, string propertyName, string defaultValue = "")
            {
                foreach (var property in elem.EnumerateObject())
                {
                    _logger.LogInformation("Checking property: {PropertyName} against {SearchName}", 
                        property.Name, propertyName);
                    if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        var value = property.Value.GetString() ?? defaultValue;
                        _logger.LogInformation("Found match. Value: {Value}", value);
                        return value;
                    }
                }
                _logger.LogInformation("No match found for {PropertyName}, using default: {Default}", 
                    propertyName, defaultValue);
                return defaultValue;
            }

            DateTime? TryGetDateTimeCaseInsensitive(JsonElement elem, string propertyName)
            {
                foreach (var property in elem.EnumerateObject())
                {
                    if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            return property.Value.GetDateTime();
                        }
                        catch
                        {
                            return null;
                        }
                    }
                }
                return null;
            }

            // Create new blog post with provided values or defaults
            var id = TryGetPropertyCaseInsensitive(element, "id", Guid.NewGuid().ToString());
            var title = TryGetPropertyCaseInsensitive(element, "title");
            var content = TryGetPropertyCaseInsensitive(element, "content");
            var authorId = TryGetPropertyCaseInsensitive(element, "author");
            var status = TryGetPropertyCaseInsensitive(element, "status", Constants.Blog.Status.Draft);

            _logger.LogInformation("Extracted values: Id={Id}, Title={Title}, Author={Author}, Status={Status}",
                id, title, authorId, status);

            var blogPost = new BlogPost
            {
                Id = id,
                PartitionKey = id,
                RowKey = id,
                Title = title,
                Content = content,
                AuthorId = authorId,
                ImageUrl = TryGetPropertyCaseInsensitive(element, "imageUrl"),
                Tags = TryGetPropertyCaseInsensitive(element, "tags"),
                Status = status,
                PublishDate = TryGetDateTimeCaseInsensitive(element, "publishedDate") ?? DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _logger.LogInformation("Created blog post object: {BlogPost}", 
                JsonSerializer.Serialize(blogPost));

            var options = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Data = blogPost
            };

            var result = await _crudFunctions.HandleCrudOperation<BlogPost>(
                operation: Constants.Storage.Operations.Set,
                entityType: _blogPostsTableName,
                options: options);

            _logger.LogInformation("Operation result: {Result}", 
                JsonSerializer.Serialize(result.Items.First()));

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(result.Items.First());
            return response;
        }
        catch (JsonException ex)
        {
            _metrics.IncrementCounter($"{operation}_Error");
            _logger.LogError(ex, "Invalid JSON format: {Error}", ex.Message);
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(new { error = "Invalid JSON format", details = ex.Message });
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
            // First, try to get the existing post
            var getOptions = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Filter = $"RowKey eq '{id}'"
            };

            var existingResult = await _crudFunctions.HandleCrudOperation<BlogPost>(
                operation: Constants.Storage.Operations.Get,
                entityType: _blogPostsTableName,
                options: getOptions);

            if (!existingResult.Items.Any())
            {
                _logger.LogWarning("Blog post with ID {Id} not found", id);
                return await CreateNotFoundResponse(req, $"Blog post with ID {id} not found");
            }

            var existingPost = existingResult.Items.First();
            _logger.LogInformation("Found existing post: {Post}", JsonSerializer.Serialize(existingPost));

            // Read and log the update request
            string requestBody;
            using (var reader = new StreamReader(req.Body))
            {
                requestBody = await reader.ReadToEndAsync();
                _logger.LogInformation("Received update request body: {Body}", requestBody);
            }

            using var jsonDoc = JsonDocument.Parse(requestBody);
            var element = jsonDoc.RootElement;

            // Create a new blog post with all the existing values
            var updatedPost = new BlogPost
            {
                Id = existingPost.Id,
                PartitionKey = existingPost.PartitionKey,
                RowKey = existingPost.RowKey,
                Title = existingPost.Title,
                Content = existingPost.Content,
                AuthorId = existingPost.AuthorId,
                ImageUrl = existingPost.ImageUrl,
                Tags = existingPost.Tags,
                Status = existingPost.Status,
                PublishDate = existingPost.PublishDate,
                LastModified = DateTime.UtcNow
            };

            // Track which fields are being updated
            var updatedFields = new List<string>();

            // Update only the fields that are present in the request
            foreach (var property in element.EnumerateObject())
            {
                var propertyName = property.Name.ToLowerInvariant();
                _logger.LogInformation("Processing update for field: {PropertyName}", propertyName);

                switch (propertyName)
                {
                    case "title":
                        updatedPost.Title = property.Value.GetString() ?? existingPost.Title;
                        updatedFields.Add("Title");
                        break;
                    case "content":
                        updatedPost.Content = property.Value.GetString() ?? existingPost.Content;
                        updatedFields.Add("Content");
                        break;
                    case "author":
                        updatedPost.AuthorId = property.Value.GetString() ?? existingPost.AuthorId;
                        updatedFields.Add("AuthorId");
                        break;
                    case "imageurl":
                        updatedPost.ImageUrl = property.Value.GetString() ?? existingPost.ImageUrl;
                        updatedFields.Add("ImageUrl");
                        break;
                    case "tags":
                        updatedPost.Tags = property.Value.GetString() ?? existingPost.Tags;
                        updatedFields.Add("Tags");
                        break;
                    case "status":
                        updatedPost.Status = property.Value.GetString() ?? existingPost.Status;
                        updatedFields.Add("Status");
                        break;
                    case "publisheddate":
                        if (property.Value.TryGetDateTime(out var date))
                        {
                            updatedPost.PublishDate = date;
                            updatedFields.Add("PublishDate");
                        }
                        break;
                }
            }

            _logger.LogInformation("Fields being updated: {Fields}", string.Join(", ", updatedFields));
            _logger.LogInformation("Updated post state before storage: {UpdatedPost}", 
                JsonSerializer.Serialize(updatedPost));

            var updateOptions = new CrudOperationOptions
            {
                ConnectionString = _connectionString,
                Data = updatedPost
            };

            _logger.LogInformation("Calling HandleCrudOperation with Update operation");
            var result = await _crudFunctions.HandleCrudOperation<BlogPost>(
                operation: Constants.Storage.Operations.Update,  // Make sure we use the Update operation
                entityType: _blogPostsTableName,
                options: updateOptions);

            if (!result.Items.Any())
            {
                _logger.LogError("Update operation did not return any items");
                throw new InvalidOperationException("Update operation failed to return the updated entity");
            }

            var updatedResult = result.Items.First();
            _logger.LogInformation("Update result: {Result}", JsonSerializer.Serialize(updatedResult));

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(updatedResult);
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
                entityType: _blogPostsTableName,
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
