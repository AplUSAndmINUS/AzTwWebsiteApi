using System;
using System.Collections.Generic;

namespace BlogReaderFunction.Models.GitHub
{
    public class GitHubData
    {
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
        public string ProfileUrl { get; set; }
        public string Bio { get; set; }
        public DateTime LastUpdated { get; set; }

        // Repository Information
        public List<Repository> FeaturedRepositories { get; set; }
        public int TotalRepositories { get; set; }
        public int TotalStars { get; set; }
        public int TotalForks { get; set; }

        // Contribution Statistics
        public int TotalContributions { get; set; }
        public Dictionary<string, int> ContributionsByDate { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }

        // Languages and Technologies
        public Dictionary<string, int> TopLanguages { get; set; }
        public List<string> Technologies { get; set; }

        // Recent Activity
        public List<GitHubActivity> RecentActivities { get; set; }
    }

    public class Repository
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string MainLanguage { get; set; }
        public int Stars { get; set; }
        public int Forks { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsFork { get; set; }
        public string[] Topics { get; set; }
        public Dictionary<string, int> LanguageBreakdown { get; set; }
    }

    public class GitHubActivity
    {
        public string Type { get; set; }  // Push, PullRequest, Issue, etc.
        public string RepoName { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string Url { get; set; }
    }
}