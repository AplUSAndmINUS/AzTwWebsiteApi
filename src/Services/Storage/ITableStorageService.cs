using AzTwWebsiteApi;

namespace AzTwWebsiteApi.Services.Storage
{
    public interface ITableBlogStorageService
    {
        Task<IEnumerable<BlogPost>> GetBlogPostsAsync();
        // Task<BlogPost> GetBlogPostAsync(string id);
        // Task SetBlogPostAsync(BlogPost post);
    }
}