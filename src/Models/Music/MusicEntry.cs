using Azure;
using Azure.Data.Tables;

namespace AzTwWebsiteApi.Models.Music
{
  public class MusicEntry : ITableEntity
  {
    public string PartitionKey { get => Artist; set => Artist = value; } // Fixed partition key for music entries
    public string RowKey
    {
      get => Id;
      set => Id = value;
    } // Unique identifier for the music entry
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required string Artist { get; set; }
    public required string Album { get; set; }
    public required string Genre { get; set; }
    public required DateTime ReleaseDate { get; set; }
    public required string[] ImageUrls { get; set; }
    public required string FeaturedImageUrl { get; set; }
    public required bool IsFeatured { get; set; }
  }

  public class MusicImage
  {
    public string PartitionKey => MusicEntryId; // In MusicImage to optimize queries
    public required string Id { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required string ImageUrl { get; set; }
    public required DateTime UploadDate { get; set; }
    public required string MusicEntryId { get; set; } // Reference to the music entry
    public required string AltText { get; set; }
    public string? Caption { get; set; }
    public required bool IsThumbnail { get; set; } // Indicates if this image is a thumbnail
  }

  public class MusicComment
  {
    public string PartitionKey => PostId; // Use PostId for grouping comments
      public string RowKey => Id; // Unique identifier for the comment
      public required string Id { get; set; }
      public required string PostId { get; set; }  // Reference to the music entry
      public required string Author { get; set; }
      public required string Content { get; set; }
      public DateTime PublishDate { get; set; }
      public bool IsApproved { get; set; } = false;
      public bool IsSpam { get; set; } = false;
      public DateTime LastModified { get; set; }
    }
}