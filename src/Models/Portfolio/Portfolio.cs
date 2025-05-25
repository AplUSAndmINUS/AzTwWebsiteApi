using System;

namespace AzTwWebsiteApi.Models.Portfolio
{
    public class PortfolioPost
    {
        public string PartitionKey => CategoryIds.FirstOrDefault() ?? AuthorId; // Use the first category ID for partitioning
        public string RowKey => Id;
        public required string Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string Technologies { get; set; }
        public required string ProjectUrl { get; set; }
        public string? GitHubUrl { get; set; }
        public required DateTime StartDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public required string[] ImageUrls { get; set; }
        public required string FeaturedImageUrl { get; set; } // URL of the featured image for the post
        public required bool IsFeatured { get; set; }
        public required string AuthorId { get; set; } // Reference to the author
        public required string Status { get; set; }
        public required string[] Tags { get; set; }
        public List<string> CommentIds { get; set; } = new(); // Efficient category-post linkage
        public List<string> ImageIds { get; set; } = new(); // Stores only IDs for faster lookups
        public List<string> CategoryIds { get; set; } = new(); // Store only category IDs
    }
    public class PortfolioComment
    {
        public string PartitionKey => PostId; // Use Comment ID for grouping
        public string RowKey => Id; // Unique identifier for the comment
        public required string Id { get; set; }
        public required string PostId { get; set; } // Reference to the portfolio post
        public required string AuthorId { get; set; } // Reference to the author
        public required string Content { get; set; }
        public DateTime PublishDate { get; set; }
        public required bool IsApproved { get; set; } = false;
        public required bool IsSpam { get; set; } = false;
        public DateTime LastModified { get; set; }
    }
    public class PortfolioImage
    {
        public string PartitionKey => PortfolioPostId; // In PortfolioComment to optimize queries
        public required string Id { get; set; }
        public required string FileName { get; set; }
        public required string ContentType { get; set; }
        public required string ImageUrl { get; set; }
        public required DateTime UploadDate { get; set; }
        public required string PortfolioPostId { get; set; }
        public required string AltText { get; set; }
        public required string Caption { get; set; }
        public required bool IsThumbnail { get; set; }
    }
    public class PortfolioCategory
    {
        public string PartitionKey => "PortfolioCategory"; // In PortfolioComment to optimize queries
        public string RowKey => Id; // Unique identifier for the category
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Slug { get; set; } // URL-friendly version of the category name
        public List<string> PostIds { get; set; } = new(); // Efficient post-category linking
    }
}