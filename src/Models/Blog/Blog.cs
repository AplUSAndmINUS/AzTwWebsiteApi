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


    public class BlogImage
    {
        public string BlogImageId { get; set; } = string.Empty; // Unique identifier for the image
        public string BlogPostId { get; set; } = string.Empty; // Image identified with the blog post
        public string PartitionKey => "BlogImage";
        public string RowKey => BlogImageId;
        public string BlobName { get; set; } = string.Empty; // Name of the blob in storage
        public string Url { get; set; } = string.Empty; // URL to access the image
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset LastModified { get; set; }

        // Add a public parameterless constructor
        public BlogImage() { }
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
