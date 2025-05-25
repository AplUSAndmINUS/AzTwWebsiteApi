namespace AzTwWebsiteApi.Utils
{
    public static class Constants
    {
        public static class Modules
        {
            public const string Blog = "Blog";
            public const string Portfolio = "Portfolio";
            public const string Events = "Events";
            public const string GitHub = "GitHub";
            public const string Video = "Video";
            public const string Live = "Live";
            public const string Contact = "Contact";
        }

        public static class BlobContainers
        {
            public const string BlogPosts = "blog-posts";
            public const string BlogImages = "blog-images";
            public const string PortfolioPosts = "portfolio-posts";
            public const string PortfolioImages = "portfolio-images";
            public const string Events = "events";
            public const string GitHub = "github-data";
        }

        public static class Functions
        {
            public const string GetBlogPosts = "GetBlogPosts";
            public const string SetBlogPosts = "SetBlogPosts";
            public const string GetBlogImage = "GetBlogImage";
            public const string SetBlogImage = "SetBlogImage";
        }
    }
}
