namespace AzTwWebsiteApi.Models.Blog

{
    public class BlogPost
    {
        public string PartitionKey => AuthorId; // In BlogPost to optimize queries by author
        public string RowKey => Id; // Unique identifier for the post
        public required string Id { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public DateTime PublishDate { get; set; }
        public DateTime LastModified { get; set; }
        public required string[] Tags { get; set; }
        public required string[] ImageUrls { get; set; }
        public required bool IsPublished { get; set; }
        public required string Summary { get; set; }
        public string AuthorId { get; set; }
        public required string FeaturedImageUrl { get; set; } // URL of the featured image for the post
        public List<string> CommentIds { get; set; } = new(); // Efficient category-post linkage
        public List<string> ImageIds { get; set; } = new(); // Stores only IDs for faster lookups
        public List<string> CategoryIds { get; set; } = new(); // Store only category IDs
    }
    public class BlogComment
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
    public class BlogImage
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
