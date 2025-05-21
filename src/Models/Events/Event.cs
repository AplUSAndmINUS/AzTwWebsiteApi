using System;

namespace BlogReaderFunction.Models.Events
{
    public class Event
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Location { get; set; }
        public string VirtualLink { get; set; }
        public string Type { get; set; }  // Conference, Meetup, Workshop, etc.
        public string Status { get; set; } // Upcoming, Ongoing, Completed, Cancelled
        public string[] Tags { get; set; }
        public string ImageUrl { get; set; }
        public string RegistrationUrl { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsFeatured { get; set; }
    }
}