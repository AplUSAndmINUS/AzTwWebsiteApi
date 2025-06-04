using AzTwWebsiteApi.Models.Blog;
using AzTwWebsiteApi.Services.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzTwWebsiteApi.Services.Blog
{
    public interface IBlogService
    {
        // Query operations
        Task<(IEnumerable<BlogPost> Posts, string? ContinuationToken)> GetBlogPostsAsync(
            int pageSize = 25,
            string? continuationToken = null);
        Task<BlogPost?> GetBlogPostAsync(string id);
        Task<BlogPost?> GetBlogPostWithImageAsync(string id, IBlobStorageService<BlogImage> blobStorageService);
        
        // Write operations
        Task<BlogPost> CreateBlogPostAsync(BlogPost post);
        Task<BlogPost> UpdateBlogPostAsync(string id, BlogPost post);
        Task DeleteBlogPostAsync(string id);
        
        // Status operations
        Task<BlogPost> PublishBlogPostAsync(string id);
        Task<BlogPost> UnpublishBlogPostAsync(string id);
        Task<BlogPost> ArchiveBlogPostAsync(string id);
        
        // Image operations
        Task<BlogImage> UploadBlogImageAsync(
            string postId,
            BlogImage image,
            byte[] imageData,
            IBlobStorageService<BlogImage> blobStorageService);
        Task<BlogImage?> GetBlogImageAsync(
            string imageId,
            IBlobStorageService<BlogImage> blobStorageService);
        Task DeleteBlogImageAsync(
            string imageId,
            IBlobStorageService<BlogImage> blobStorageService);
    }
}