using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AzTwWebsiteApi.Models.Blog;

namespace AzTwWebsiteApi.Services.Storage
{
    public interface ITableBlogStorageService
    {
        Task<BlogPost> GetBlogPostAsync(string id);
        Task<IEnumerable<BlogPost>> GetBlogPostsAsync();
        Task<BlogImage> GetBlogImageAsync(string id);
        Task SetBlogPostAsync(BlogPost post);
        Task SetBlogImageAsync(string id, Stream imageStream, string contentType, Dictionary<string, string> metadata);
    }
}