using Azure;
using Azure.Data.Tables;

namespace AzTwWebsiteApi.Models.Blog
{
    public class BlogPost : ITableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); // Unique identifier for the blog post
        public string PartitionKey { get; set; } = "BlogPosts";
        public string? RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty; // Reference to Blob Storage
        public string AuthorId { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public ETag ETag { get; set; }
    }
    public class BlogComment : ITableEntity
    {
        public string PartitionKey => BlogPostId; // Use Comment ID for grouping
        public string RowKey => Id; // Unique identifier for the comment
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public required string Id { get; set; }
        public required string BlogPostId { get; set; } // Reference to the blog post
        public required string AuthorId { get; set; } // Reference to the author
        public required string Content { get; set; }
        public DateTime PublishDate { get; set; }
        public DateTime LastModified { get; set; }
        public required bool IsApproved { get; set; } = false;
        public required bool IsSpam { get; set; } = false;
        string ITableEntity.PartitionKey { get => PartitionKey; set => throw new NotImplementedException(); }
        string ITableEntity.RowKey { get => RowKey; set => throw new NotImplementedException(); }
    }

    public class BlogImage
    {
        public string BlogImageId { get; set; } = string.Empty; // Unique identifier for the image
        public required string BlogPostId { get; set; } // Image identified with the blog post
        public string PartitionKey => "BlogImage"; // In BlogComment to optimize queries
        public string RowKey => BlogImageId; // Unique identifier for the image
        public required string BlobName { get; set; } // Name of the blob in storage
        public required string Url { get; set; } // URL to access the image
        public required DateTimeOffset CreatedAt { get; set; }
        public required DateTimeOffset LastModified { get; set; }
    }

  public class BlogImageMetadata : ITableEntity
  {
      public string PartitionKey => "BlogImageMetadata"; // In BlogComment to optimize queries
      public string RowKey => BlogImageMetadataId; // Unique identifier for the image metadata
      public ETag ETag { get; set; }
      public DateTimeOffset? Timestamp { get; set; }
      public required string BlogImageMetadataId { get; set; }
      public required string BlogImageId { get; set; } // Image identified with the metadata
      public required string BlobName { get; set; } // Name of the blob in storage
      public required string ImageUrl { get; set; }
      public required string Title { get; set; }
      public required string AltText { get; set; }
      public required string Description { get; set; }
      string ITableEntity.PartitionKey { get => PartitionKey; set => throw new NotImplementedException(); }
      string ITableEntity.RowKey { get => RowKey; set => throw new NotImplementedException(); }
  }
    public class BlogCategory
    {
        public static string PartitionKey => "BlogCategory"; // In BlogComment to optimize queries
        public string RowKey => Id; // Unique identifier for the category
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Slug { get; set; } // URL-friendly version of the category name
        public List<string> PostIds { get; set; } = new(); // Efficient post-category linking

    }
}
