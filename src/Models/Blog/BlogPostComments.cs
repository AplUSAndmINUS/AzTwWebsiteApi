using System;
using System.Collections.Generic;

namespace BlogReaderFunction.Models.Blog
{
    public class BlogPostComments
    {
        public string PostId { get; set; } // The ID of the blog post this comment belongs to
        public string CommentId { get; set; } // Unique identifier for the comment
        public string Author { get; set; } // Author of the comment
        public string Content { get; set; } // Content of the comment
        public DateTime DatePublished { get; set; } // Date when the comment was published

        // Additional properties can be added as needed, such as likes, replies, etc.
    }
}