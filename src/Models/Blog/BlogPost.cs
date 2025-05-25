using System;
using System.Collections.Generic;

namespace BlogReaderFunction.Models.Blog
{
    public class BlogPost
    {
        public required string PartitionKey { get; set; } // e.g., category or author
        public required string RowKey { get; set; } // e.g., unique id
        public required string Name { get; set; }
        public required string Title { get; set; }
        public required string Author { get; set; }
        public required string Description { get; set; }
        public required string Content { get; set; }
        public required string Media { get; set; }
        public required string MediaDescription { get; set; }
        public required string Category { get; set; }
        public required string Metadata { get; set; } // Serialized JSON for tags, views, likes
        public required string Comments { get; set; } // Serialized JSON for comments
        public DateTime DatePublished { get; set; }
        public DateTime DateModified { get; set; }

        // Required by ITableEntity
        public required string ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}