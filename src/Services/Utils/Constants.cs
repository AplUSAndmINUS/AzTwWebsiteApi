namespace AzTwWebsiteApi.Services.Utils
{
    public static class Constants
    {
        public static class Modules
        {
            public const string Blog = "Blog";
            public const string Portfolio = "Portfolio";
            public const string Events = "Events";
            public const string Contact = "Contact";
            public const string GitHub = "GitHub";
            public const string Video = "Video";
            public const string Live = "Live";
        }

        public static class BlobContainers
        {
            public const string ArtworkImages = "artwork-images";
            public const string BlogImages = "blog-images";
            public const string PortfolioImages = "portfolio-images";
            public const string EventsData = "events-data";
            public const string GitHub = "github-data";
            public const string LiveStreams = "livestreams";
            public const string Videos = "videos";
        }
        public static class TableNames
        {
            public const string Artwork = "artwork";
            public const string BlogPosts = "blog";
            public const string BlogComments = "blogcomments";
            public const string Books = "books";
            public const string Contact = "contact";
            public const string Events = "events";
            public const string GitHubRepositories = "github";
            public const string PortfolioItems = "portfolio";
            public const string PortfolioComments = "portfoliocomments";
            public const string LiveStreams = "livestreams";
            public const string Music = "music";
        }

        public static class Functions
        {
            public const string GetBlogPosts = "GetBlogPosts";
            public const string GetBlogPost = "GetBlogPostById";
            public const string SetBlogPosts = "SetBlogPosts";
            public const string GetBlogImages = "GetBlogImages";
            public const string GetBlogImage = "GetBlogImageById";
            public const string SetBlogImage = "SetBlogImage";
        }
    }
}
