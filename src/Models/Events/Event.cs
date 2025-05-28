using System;

namespace AzTwWebsiteApi.Models.Events
{
    public class Event
    {
        public required string Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required DateTime StartDate { get; set; }
        public required DateTime EndDate { get; set; }
        public required string Location { get; set; }
        public required string VirtualLink { get; set; }
        public required string Type { get; set; }  // Conference, Meetup, Workshop, etc.
        public required string Status { get; set; } // Upcoming, Ongoing, Completed, Cancelled
        public required string[] Tags { get; set; }
        public required string ImageUrl { get; set; }
        public required string RegistrationUrl { get; set; }
        public required bool IsVirtual { get; set; }
        public required bool IsFeatured { get; set; }
    }
}