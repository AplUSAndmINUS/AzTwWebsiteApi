using System;

namespace BlogReaderFunction.Models.Portfolio
{
    public class PortfolioPost
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Technologies { get; set; }
        public string ProjectUrl { get; set; }
        public string GitHubUrl { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string[] ImageUrls { get; set; }
        public bool IsFeatured { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public string[] Tags { get; set; }
    }
}