namespace AzTwWebsiteApi.Models.Author
public class Author
{
  public required string Id { get; set; }
  public required string Name { get; set; }
  public required string Email { get; set; }
  public required string Username { get; set; } // Unique username for the author
  public required string DisplayName { get; set; } // Name displayed on the website
  public required string AvatarUrl { get; set; } // URL to the author's avatar image
  public required string Bio { get; set; } // Short bio of the author
  public required string Website { get; set; } // URL to the author's personal or professional website
}