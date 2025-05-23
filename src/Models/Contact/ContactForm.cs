using System;

namespace BlogReaderFunction.Models.Contact
{
    public class ContactForm
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string Status { get; set; }  // New, Read, Replied, Archived
        public string? CompanyName { get; set; }
        public string? Phone { get; set; }
        public string Category { get; set; }  // General, Business, Support, etc.
        public bool IsUrgent { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}