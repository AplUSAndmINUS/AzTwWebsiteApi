using Azure;
using Azure.Data.Tables;

namespace AzTwWebsiteApi.Models.Blog
{
    public class BlogPost : ITableEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); // Unique identifier for the blog post
        public string PartitionKey { get; set; } // Initialize in the constructor
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty; // Reference to Blob Storage
        public string AuthorId { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public DateTime PublishDate { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Draft"; // Draft, Published, Archived
        public bool IsPublished => Status == "Published";
        public ETag ETag { get; set; }

        public BlogPost()
        {
            PartitionKey = Id; // Initialize PartitionKey in the constructor
        }
    }

    public class BlogComment : ITableEntity
    {
        public string PartitionKey { get; set; } // Initialize in the constructor
        public string RowKey { get; set; } = Guid.NewGuid().ToString(); // Unique identifier for the comment

        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string BlogPostId { get; set; } = string.Empty; // Reference to the blog post
        public string AuthorId { get; set; } = string.Empty; // Reference to the author
        public string Content { get; set; } = string.Empty;

        public DateTime PublishDate { get; set; }
        public DateTime LastModified { get; set; }

        public bool IsApproved { get; set; } = false;
        public bool IsSpam { get; set; } = false;

        public BlogComment()
        {
            PartitionKey = BlogPostId; // Initialize PartitionKey in the constructor
        }
    }


    public class BlogImage : ITableEntity
    {
        public string BlogImageId { get; set; } = Guid.NewGuid().ToString();
        public string BlogPostId { get; set; } = string.Empty;
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public string BlobName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileName { get; set; } = string.Empty;
        public ETag ETag { get; set; }

        public BlogImage()
        {
            PartitionKey = "BlogImage";
            RowKey = BlogImageId;
        }
    }

  public class BlogImageMetadata : ITableEntity
  {
      public string PartitionKey { get; set; } = "BlogImageMetadata"; // Fixed value for all blog image metadata
      public string RowKey { get; set; } = Guid.NewGuid().ToString(); // Auto-generated if not set
      public ETag ETag { get; set; }
      public DateTimeOffset? Timestamp { get; set; }
      public string BlogImageMetadataId { get; set; } = Guid.NewGuid().ToString();
      public string BlogImageId { get; set; } = string.Empty; // Image identified with the metadata
      public string BlobName { get; set; } = string.Empty; // Name of the blob in storage
      public string ImageUrl { get; set; } = string.Empty;
      public string Title { get; set; } = string.Empty;
      public string AltText { get; set; } = string.Empty;
      public string Description { get; set; } = string.Empty;
  }
    public class BlogCategory
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); // Unique identifier for the category
        public string PartitionKey { get; set; } // Initialize in the constructor
        public string RowKey => Id; // Unique identifier for the category
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty; // URL-friendly version of the category name
        private List<string> _postIds = new();
        public string PostIdsSerialized
        {
            get => string.Join(",", _postIds); // Convert list to comma-separated string
            set => _postIds = value.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(); // Convert back to list
        }

        public BlogCategory()
        {
            Slug = string.IsNullOrWhiteSpace(Slug) ? Guid.NewGuid().ToString() : Slug;
            PartitionKey = $"Category-{Uri.EscapeDataString(Slug)}";
        }
    }
}
