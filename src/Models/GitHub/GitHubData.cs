using System;
using System.Collections.Generic;

namespace AzTwWebsiteApi.Models.GitHub
{
    public class GitHubData
    {
        public required string Username { get; set; }
        public required string AvatarUrl { get; set; }
        public required string ProfileUrl { get; set; }
        public required string Bio { get; set; }
        public required DateTime LastUpdated { get; set; }

        // Repository Information
        public required List<Repository> FeaturedRepositories { get; set; }
        public required int TotalRepositories { get; set; }
        public required int TotalStars { get; set; }
        public required int TotalForks { get; set; }

        // Contribution Statistics
        public required int TotalContributions { get; set; }
        public required Dictionary<string, int> ContributionsByDate { get; set; }
        public required int CurrentStreak { get; set; }
        public required int LongestStreak { get; set; }

        // Languages and Technologies
        public required Dictionary<string, int> TopLanguages { get; set; }
        public required List<string> Technologies { get; set; }

        // Recent Activity
        public required List<GitHubActivity> RecentActivities { get; set; }
    }

    public class Repository
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Url { get; set; }
        public required string MainLanguage { get; set; }
        public required int Stars { get; set; }
        public required int Forks { get; set; }
        public required DateTime LastUpdated { get; set; }
        public required bool IsFork { get; set; }
        public required string[] Topics { get; set; }
        public required Dictionary<string, int> LanguageBreakdown { get; set; }
    }

    public class GitHubActivity
    {
        public required string Type { get; set; }  // Push, PullRequest, Issue, etc.
        public required string RepoName { get; set; }
        public required string Description { get; set; }
        public required DateTime Timestamp { get; set; }
        public required string Url { get; set; }
    }
}