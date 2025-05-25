using System;
using System.Collections.Generic;

namespace BlogReaderFunction.Models.Blog
{
    public class BlogPost : ITableEntity
    {
        public string PartitionKey { get; set; } // e.g., category or author
        public string RowKey { get; set; } // e.g., unique id
        public string Name { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public string Media { get; set; }
        public string MediaDescription { get; set; }
        public string Category { get; set; }
        public string Metadata { get; set; } // Serialized JSON for tags, views, likes
        public string Comments { get; set; } // Serialized JSON for comments
        public DateTime DatePublished { get; set; }
        public DateTime DateModified { get; set; }

        // Required by ITableEntity
        public string ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}