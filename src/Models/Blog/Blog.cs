using Azure;
using Azure.Data.Tables;
using Azure.Data.Blobs;

namespace AzTwWebsiteApi.Models.Blog
{
    public class BlogPost : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty; // AuthorId
        public string RowKey { get; set; } = string.Empty; // Post Id
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime PublishDate { get; set; }
        public DateTime LastModified { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string[] ImageUrls { get; set; } = Array.Empty<string>();
        public bool IsPublished { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public string FeaturedImageUrl { get; set; } = string.Empty;
        public List<string> CommentIds { get; set; } = new();
        public List<string> ImageIds { get; set; } = new();
        public List<string> CategoryIds { get; set; } = new();
    }
    public class BlogComment : ITableEntity
    {
        public string PartitionKey => PostId; // Use Comment ID for grouping
        public string RowKey => Id; // Unique identifier for the comment
        public required string Id { get; set; }
        public required string PostId { get; set; } // Reference to the blog post
        public required string AuthorId { get; set; } // Reference to the author
        public required string Content { get; set; }
        public DateTime PublishDate { get; set; }
        public required bool IsApproved { get; set; } = false;
        public required bool IsSpam { get; set; } = false;
        public DateTime LastModified { get; set; }
    }
    public class BlogImage : IBlobEntity
    {
        public string PartitionKey => "BlogImage"; // In BlogComment to optimize queries
        public string RowKey => Id; // Unique identifier for the image
        public required string BlobName { get; set; } // Name of the blob in storage
        public required string Url { get; set; } // URL to access the image
        public required DateTimeOffset CreatedAt { get; set; }
        public required DateTimeOffset LastModified { get; set; }
    }
    public class BlogImageMetadata : ITableEntity
    {
        public required string Id { get; set; }
        public required string ImageUrl { get; set; }
        public required string Title { get; set; }
        public required string AltText { get; set; }
        public required string Description { get; set; }
    }
    public class BlogCategory
    {
        public string PartitionKey => "BlogCategory"; // In BlogComment to optimize queries
        public string RowKey => Id; // Unique identifier for the category
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Slug { get; set; } // URL-friendly version of the category name
        public List<string> PostIds { get; set; } = new(); // Efficient post-category linking

    }
}
