using System;
using System.Collections.Generic;

namespace BlogReaderFunction.Models.Blog
{
  public class BlogPostComments
  {
    public required string PostId { get; set; } // The ID of the blog post this comment belongs to
    public required string CommentId { get; set; } // Unique identifier for the comment
    public required string Author { get; set; } // Author of the comment
    public required string Content { get; set; } // Content of the comment
    public DateTime DatePublished { get; set; } // Date when the comment was published
    public required int Likes { get; set; } // Number of likes for the comment
    public required string[] Replies { get; set; } // Array of comment IDs that are replies to this comment 
    // Additional properties can be added as needed, such as likes, replies, etc.
  }
}