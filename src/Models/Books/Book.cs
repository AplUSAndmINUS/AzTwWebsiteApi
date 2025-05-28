namespace AzTwWebsiteApi.Models.Book

{
  public class BookPost
  {
    public string PartitionKey => AuthorId; // In BlogPost to optimize queries by author
    public string RowKey => Id; // Unique identifier for the post
    public required string Id { get; set; }
    public required string Title { get; set; }
    public DateTime PublishDate { get; set; }
    public DateTime LastModified { get; set; }
    public required string[] Tags { get; set; }
    public required string[] ImageUrls { get; set; }
    public required bool IsPublished { get; set; }
    public required string Summary { get; set; }
    public required string AuthorId { get; set; }
    public required string FeaturedImageUrl { get; set; } // URL of the featured image for the post
    public List<string> AuthorIds { get; set; } = new(); // List of author IDs for multi-author support
    public List<string> CategoryIds { get; set; } = new(); // Store only category IDs
  }
  public class BookImage
  {
    public required string Id { get; set; }
    public required string ImageUrl { get; set; }
    public required string Title { get; set; }
    public required string AltText { get; set; }
    public required string Description { get; set; }
  }
  public class BookCategory
  {
    public string PartitionKey => Id; // In BookComment to optimize queries
    public string RowKey => Id; // Unique identifier for the category
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Slug { get; set; } // URL-friendly version of the category name
    public List<string> PostIds { get; set; } = new(); // Efficient post-category linking
  }
}