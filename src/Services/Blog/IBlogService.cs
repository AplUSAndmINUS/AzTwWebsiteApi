using AzTwWebsiteApi.Models.Blog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzTwWebsiteApi.Services.Blog
{
    public interface IBlogService
    {
        Task<(IEnumerable<BlogPost> Posts, string? ContinuationToken)> GetBlogPostsAsync(
            int pageSize = 25,
            string? continuationToken = null);
    // Task<BlogPost> GetBlogPostByIdAsync(string id);
    // Task<BlogPost> CreateBlogPostAsync(BlogPost post);
    // Task<BlogPost> UpdateBlogPostAsync(string id, BlogPost post);
    // Task DeleteBlogPostAsync(string id);

    // Task<IEnumerable<BlogImage>> GetBlogImagesAsync();
    // Task<BlogImage> GetBlogImageByIdAsync(string id);
    // Task<BlogImage> UploadBlogImageAsync(BlogImage image);
    // Task DeleteBlogImageAsync(string id);

    // Task<IEnumerable<BlogComment>> GetCommentsByPostIdAsync(string postId);
    // Task<BlogComment> AddCommentToPostAsync(string postId, BlogComment comment);
    // Task<BlogComment> UpdateCommentAsync(string postId, string commentId, BlogComment comment);
    // Task DeleteCommentAsync(string postId, string commentId);
  }
}