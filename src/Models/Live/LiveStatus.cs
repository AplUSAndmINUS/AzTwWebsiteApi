using System;

namespace AzTwWebsiteApi.Models.Live
{
    public class LiveStatus
    {
        public string Id { get; set; }
        public bool IsLive { get; set; }
        public string Platform { get; set; }  // Twitch, YouTube, etc.
        public string StreamTitle { get; set; }
        public string StreamUrl { get; set; }
        public DateTime? StartTime { get; set; }
        public int? ViewerCount { get; set; }
        public string Game { get; set; }
        public string Category { get; set; }
        public string ThumbnailUrl { get; set; }
        public DateTime LastUpdated { get; set; }
        public string[] Tags { get; set; }
    }
}