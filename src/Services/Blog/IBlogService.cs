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
    }
}