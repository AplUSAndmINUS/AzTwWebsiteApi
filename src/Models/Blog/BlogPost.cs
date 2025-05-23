using System;

namespace BlogReaderFunction.Models.Blog
{
    public class BlogPost
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Author { get; set; }
        public DateTime PublishDate { get; set; }
        public DateTime LastModified { get; set; }
        public string[] Tags { get; set; }
        public string[] ImageUrls { get; set; }
        public bool IsPublished { get; set; }
        public string Slug { get; set; }
        public string Summary { get; set; }
    }
}