using System;

namespace AzTwWebsiteApi.Models.Blog
{
    public class BlogImage
    {
        public required string Id { get; set; }
        public required string FileName { get; set; }
        public required string ContentType { get; set; }
        public required long FileSize { get; set; }
        public required string Url { get; set; }
        public DateTime UploadDate { get; set; }
        public required string BlogPostId { get; set; }
        public required string AltText { get; set; }
        public string Caption { get; set; } = string.Empty;
    }
}