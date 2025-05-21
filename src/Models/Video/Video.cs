using System;

namespace BlogReaderFunction.Models.Video
{
    public class Video
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string Platform { get; set; }  // YouTube, Twitch, etc.
        public string ThumbnailUrl { get; set; }
        public DateTime PublishDate { get; set; }
        public string Duration { get; set; }
        public string[] Tags { get; set; }
        public string Category { get; set; }
        public bool IsFeatured { get; set; }
        public string PlaylistId { get; set; }
        public string ExternalId { get; set; }  // YouTube ID, etc.
    }
}