using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Models.Blog;
using AzTwWebsiteApi.Services.Storage;
using AzTwWebsiteApi.Services.Utils;

namespace AzTwWebsiteApi.Services.Blog
{
    public class BlogService : IBlogService
    {
        private readonly ITableStorageService<BlogPost> _tableStorage;
        private readonly ILogger<BlogService> _logger;
        private readonly IMetricsService _metrics;
        private readonly CircuitBreaker _circuitBreaker;
        private readonly RetryPolicy _retryPolicy;

        public BlogService(
            ITableStorageService<BlogPost> tableStorage,
            ILogger<BlogService> logger,
            IMetricsService metrics)
        {
            _tableStorage = tableStorage;
            _logger = logger;
            _metrics = metrics;
            _circuitBreaker = new CircuitBreaker(logger);
            _retryPolicy = new RetryPolicy(logger);
        }

        public async Task<(IEnumerable<BlogPost> Posts, string? ContinuationToken)> GetBlogPostsAsync(
            int pageSize = 25,
            string? continuationToken = null)
        {
            const string operation = "GetBlogPosts";
            using var timer = new OperationTimer(operation, _metrics);

            try
            {
                _logger.LogInformation("Retrieving blog posts. PageSize: {PageSize}, ContinuationToken: {ContinuationToken}",
                    pageSize, continuationToken);

                var result = await ExecuteWithResilience(async () =>
                {
                    var filter = $"Status eq '{Constants.Blog.Status.Published}'";
                    var pagedResult = await _tableStorage.GetPagedResultsAsync(pageSize, continuationToken, filter);
                    _metrics.RecordValue($"{operation}_Count", pagedResult.Items.Count());
                    return pagedResult;
                }, operation);

                return (result.Items, result.ContinuationToken);
            }
            catch (Exception ex)
            {
                _metrics.IncrementCounter($"{operation}_Error");
                _logger.LogError(ex, "Error retrieving blog posts");
                throw;
            }
        }

        public async Task<BlogPost?> GetBlogPostAsync(string id)
        {
            const string operation = "GetBlogPost";
            using var timer = new OperationTimer(operation, _metrics);

            try
            {
                return await ExecuteWithResilience(async () =>
                {
                    return await _tableStorage.GetEntityAsync(id, id);
                }, operation);
            }
            catch (Exception ex)
            {
                _metrics.IncrementCounter($"{operation}_Error");
                _logger.LogError(ex, "Error retrieving blog post {Id}", id);
                throw;
            }
        }

        public async Task<BlogPost?> GetBlogPostWithImageAsync(
            string id,
            IBlobStorageService<BlogImage> blobStorageService)
        {
            const string operation = "GetBlogPostWithImage";
            using var timer = new OperationTimer(operation, _metrics);

            try
            {
                return await ExecuteWithResilience(async () =>
                {
                    var blogPost = await _tableStorage.GetEntityAsync(id, id);
                    if (blogPost == null) return null;

                    if (!string.IsNullOrEmpty(blogPost.ImageUrl))
                    {
                        var (image, metadata) = await blobStorageService.GetBlobWithMetadataAsync(blogPost.ImageUrl);
                        if (image != null)
                        {
                            blogPost.ImageUrl = image.Url;
                        }
                    }

                    return blogPost;
                }, operation);
            }
            catch (Exception ex)
            {
                _metrics.IncrementCounter($"{operation}_Error");
                _logger.LogError(ex, "Error retrieving blog post with image {Id}", id);
                throw;
            }
        }

        public async Task<BlogPost> CreateBlogPostAsync(BlogPost post)
        {
            const string operation = "CreateBlogPost";
            using var timer = new OperationTimer(operation, _metrics);

            try
            {
                ValidateBlogPost(post);
                
                return await ExecuteWithResilience(async () =>
                {
                    post.Status = Constants.Blog.Status.Draft;
                    post.PublishDate = DateTime.UtcNow;
                    post.LastModified = DateTime.UtcNow;
                    
                    return await _tableStorage.AddEntityAsync(post);
                }, operation);
            }
            catch (Exception ex)
            {
                _metrics.IncrementCounter($"{operation}_Error");
                _logger.LogError(ex, "Error creating blog post");
                throw;
            }
        }

        public async Task<BlogPost> UpdateBlogPostAsync(string id, BlogPost post)
        {
            const string operation = "UpdateBlogPost";
            using var timer = new OperationTimer(operation, _metrics);

            try
            {
                ValidateBlogPost(post);
                
                return await ExecuteWithResilience(async () =>
                {
                    var existing = await _tableStorage.GetEntityAsync(id, id)
                        ?? throw new InvalidOperationException($"Blog post {id} not found");

                    post.Id = id;
                    post.PartitionKey = id;
                    post.RowKey = id;
                    post.Status = existing.Status;
                    post.LastModified = DateTime.UtcNow;
                    
                    await _tableStorage.UpdateEntityAsync(post);
                    return post;
                }, operation);
            }
            catch (Exception ex)
            {
                _metrics.IncrementCounter($"{operation}_Error");
                _logger.LogError(ex, "Error updating blog post {Id}", id);
                throw;
            }
        }

        public async Task DeleteBlogPostAsync(string id)
        {
            const string operation = "DeleteBlogPost";
            using var timer = new OperationTimer(operation, _metrics);

            try
            {
                await ExecuteWithResilience(async () =>
                {
                    await _tableStorage.DeleteEntityAsync(id, id);
                    return true;
                }, operation);
            }
            catch (Exception ex)
            {
                _metrics.IncrementCounter($"{operation}_Error");
                _logger.LogError(ex, "Error deleting blog post {Id}", id);
                throw;
            }
        }

        public async Task<BlogPost> PublishBlogPostAsync(string id)
        {
            return await UpdateBlogStatusAsync(id, Constants.Blog.Status.Published);
        }

        public async Task<BlogPost> UnpublishBlogPostAsync(string id)
        {
            return await UpdateBlogStatusAsync(id, Constants.Blog.Status.Draft);
        }

        public async Task<BlogPost> ArchiveBlogPostAsync(string id)
        {
            return await UpdateBlogStatusAsync(id, Constants.Blog.Status.Archived);
        }

        public async Task<BlogImage> UploadBlogImageAsync(
            string postId,
            BlogImage image,
            byte[] imageData,
            IBlobStorageService<BlogImage> blobStorageService)
        {
            const string operation = "UploadBlogImage";
            using var timer = new OperationTimer(operation, _metrics);

            try
            {
                ValidateBlogImage(image, imageData);

                return await ExecuteWithResilience(async () =>
                {
                    var metadata = new Dictionary<string, string>
                    {
                        { Constants.Storage.MetadataKeys.ContentType, image.MimeType },
                        { Constants.Storage.MetadataKeys.CreatedDate, DateTime.UtcNow.ToString("O") },
                        { Constants.Storage.MetadataKeys.ModifiedDate, DateTime.UtcNow.ToString("O") }
                    };

                    await blobStorageService.UploadBlobWithMetadataAsync(
                        image.BlobName,
                        image,
                        metadata,
                        image.MimeType);

                    return image;
                }, operation);
            }
            catch (Exception ex)
            {
                _metrics.IncrementCounter($"{operation}_Error");
                _logger.LogError(ex, "Error uploading blog image for post {PostId}", postId);
                throw;
            }
        }

        public async Task<BlogImage?> GetBlogImageAsync(
            string imageId,
            IBlobStorageService<BlogImage> blobStorageService)
        {
            const string operation = "GetBlogImage";
            using var timer = new OperationTimer(operation, _metrics);

            try
            {
                return await ExecuteWithResilience(async () =>
                {
                    var (image, metadata) = await blobStorageService.GetBlobWithMetadataAsync(imageId);
                    return image;
                }, operation);
            }
            catch (Exception ex)
            {
                _metrics.IncrementCounter($"{operation}_Error");
                _logger.LogError(ex, "Error retrieving blog image {ImageId}", imageId);
                throw;
            }
        }

        public async Task DeleteBlogImageAsync(
            string imageId,
            IBlobStorageService<BlogImage> blobStorageService)
        {
            const string operation = "DeleteBlogImage";
            using var timer = new OperationTimer(operation, _metrics);

            try
            {
                await ExecuteWithResilience(async () =>
                {
                    await blobStorageService.DeleteBlobAsync(imageId);
                    return true;
                }, operation);
            }
            catch (Exception ex)
            {
                _metrics.IncrementCounter($"{operation}_Error");
                _logger.LogError(ex, "Error deleting blog image {ImageId}", imageId);
                throw;
            }
        }

        private async Task<BlogPost> UpdateBlogStatusAsync(string id, string status)
        {
            const string operation = "UpdateBlogStatus";
            using var timer = new OperationTimer(operation, _metrics);

            try
            {
                return await ExecuteWithResilience(async () =>
                {
                    var post = await _tableStorage.GetEntityAsync(id, id)
                        ?? throw new InvalidOperationException($"Blog post {id} not found");

                    post.Status = status;
                    post.LastModified = DateTime.UtcNow;
                    
                    await _tableStorage.UpdateEntityAsync(post);
                    return post;
                }, operation);
            }
            catch (Exception ex)
            {
                _metrics.IncrementCounter($"{operation}_Error");
                _logger.LogError(ex, "Error updating blog status to {Status} for {Id}", status, id);
                throw;
            }
        }

        private async Task<T> ExecuteWithResilience<T>(Func<Task<T>> operation, string operationName)
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                var result = await _retryPolicy.ExecuteAsync(async () =>
                {
                    var value = await operation();
                    _metrics.IncrementCounter($"{operationName}_Success");
                    return value;
                }, operationName);

                return result;
            }, operationName);
        }

        private static void ValidateBlogPost(BlogPost post)
        {
            if (string.IsNullOrWhiteSpace(post.Title))
                throw new ArgumentException("Blog post title is required");
            
            if (string.IsNullOrWhiteSpace(post.Content))
                throw new ArgumentException("Blog post content is required");
        }

        private static void ValidateBlogImage(BlogImage image, byte[] imageData)
        {
            if (string.IsNullOrWhiteSpace(image.BlobName))
                throw new ArgumentException("Image blob name is required");

            if (string.IsNullOrWhiteSpace(image.MimeType))
                throw new ArgumentException("Image MIME type is required");

            if (!Constants.Blog.ImageTypes.AllowedMimeTypes.Contains(image.MimeType))
                throw new ArgumentException($"Invalid image type. Allowed types: {string.Join(", ", Constants.Blog.ImageTypes.AllowedMimeTypes)}");

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!Constants.Blog.ImageTypes.AllowedExtensions.Contains(extension))
                throw new ArgumentException($"Invalid file extension. Allowed extensions: {string.Join(", ", Constants.Blog.ImageTypes.AllowedExtensions)}");

            var fileSizeInMb = imageData.Length / (1024.0 * 1024.0);
            if (fileSizeInMb > Constants.Blog.ImageTypes.MaxFileSizeInMb)
                throw new ArgumentException($"Image size exceeds maximum allowed size of {Constants.Blog.ImageTypes.MaxFileSizeInMb}MB");
        }
    }
}