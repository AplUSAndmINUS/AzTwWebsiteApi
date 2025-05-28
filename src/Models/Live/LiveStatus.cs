using System;

namespace AzTwWebsiteApi.Models.Live
{
    public class LiveStatus
    {
        public required string Id { get; set; }
        public required bool IsLive { get; set; }
        public required string Platform { get; set; }  // Twitch, YouTube, etc.
        public required string StreamTitle { get; set; }
        public required string StreamUrl { get; set; }
        public required DateTime? StartTime { get; set; }
        public required int? ViewerCount { get; set; }
        public required string Game { get; set; }
        public required string Category { get; set; }
        public required string ThumbnailUrl { get; set; }
        public required DateTime LastUpdated { get; set; }
        public required string[] Tags { get; set; }
    }
}