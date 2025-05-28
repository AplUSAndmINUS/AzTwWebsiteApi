using Azure;
using Azure.Data.Tables;

namespace AzTwWebsiteApi.Models.Author
{
  public class Author : ITableEntity
  {
    public string PartitionKey { get; set; } = "Author"; // Fixed partition key for authors
    public string RowKey { get; set; } = string.Empty; // Unique identifier for the author
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Author properties
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Username { get; set; } // Unique username for the author
    public required string DisplayName { get; set; } // Name displayed on the website
    public required string AvatarUrl { get; set; } // URL to the author's avatar image
    public required string Bio { get; set; } // Short bio of the author
    public required string Website { get; set; } // URL to the author's personal or professional website
  }
}