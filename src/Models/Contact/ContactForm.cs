using System;

namespace AzTwWebsiteApi.Models.Contact
{
    public class ContactForm
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Subject { get; set; }
        public required string Message { get; set; }
        public required DateTime SubmissionDate { get; set; }
        public required string Status { get; set; }  // New, Read, Replied, Archived
        public string? CompanyName { get; set; }
        public string? Phone { get; set; }
        public required string Category { get; set; }  // General, Business, Support, etc.
        public required bool IsUrgent { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}