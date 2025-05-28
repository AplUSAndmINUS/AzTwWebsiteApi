using System;

namespace AzTwWebsiteApi.Models.Video
{
    public class Video
    {
        public required string Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string Url { get; set; }
        public required string Platform { get; set; }  // YouTube, Twitch, etc.
        public required string ThumbnailUrl { get; set; }
        public required DateTime PublishDate { get; set; }
        public required string Duration { get; set; }
        public required string[] Tags { get; set; }
        public required string Category { get; set; }
        public required bool IsFeatured { get; set; }
        public required string PlaylistId { get; set; }
        public required string ExternalId { get; set; }  // YouTube ID, etc.
    }
}