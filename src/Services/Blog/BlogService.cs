// C# functions- BlogService.cs
// Endpoints: 

// *BLOG POSTS*
// api/blog/posts
// api/blog/posts/{id}
// api/blog/posts/{id}/update

// *BLOG IMAGES*
// api/blog/images
// api/blog/images/{id}                             
// api/blog/images/{id}/delete
// api/blog/images/{id}/update

// *BLOG COMMENTS*`
// api/blog/posts/{id}/comments
// api/blog/posts/{id}/comments/{commentId}
// api/blog/posts/{id}/comments/{commentId}/delete
// api/blog/posts/{id}/comments/{commentId}/update
// api/blog/posts/{id}/comments/{commentId}/reactions
// api/blog/posts/{id}/comments/{commentId}/replies
// api/blog/posts/{id}/comments/{commentId}/replies/{replyId}
// api/blog/posts/{id}/comments/{commentId}/replies/{replyId}/reactions

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.Data.Tables;
using AzTwWebsiteApi.Utils;
using AzTwWebsiteApi.Models.Blog;
using AzTwWebsiteApi.Services.Storage;
using AzTwWebsiteApi.Services.Blog;

namespace AzTwWebsiteApi.Functions.Blog
{
    public class BlogService
    {
        private readonly ILogger<BlogService> _logger;
        private readonly ITableStorageService _tableStorageService;
        private readonly IBlobStorageService _blobStorageService;

        public BlogService(ILogger<BlogService> logger, 
                           ITableStorageService tableStorageService, 
                           IBlobStorageService blobStorageService)
        {
            _logger = logger;
            _tableStorageService = tableStorageService;
            _blobStorageService = blobStorageService;
        }

        // Add methods to handle blog posts, images, and comments here
    }
}