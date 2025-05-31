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

        public static class Storage
        {
            public enum StorageType
            {
                Table,
                Blob,
                Queue,
                File
            }

            public static class EntityTypes
            {
                // Table Storage entities
                public const string Blog = "blog";
                public const string Books = "books";
                public const string Contact = "contact";
                public const string Events = "events";
                public const string GitHub = "github";
                public const string LiveStreams = "livestreams";
                public const string Music = "music";
                public const string Portfolio = "portfolio";
                public const string BlogComments = "blogcomments";
                public const string PortfolioComments = "portfoliocomments";

                // Blob Storage entities
                public const string BlogImages = "blogimages";
                public const string BooksImages = "booksimages";
                public const string EventsData = "eventsdata";
                public const string LiveStreamsImages = "livestreams-images";
                public const string PortfolioImages = "portfolioimages";
                public const string Video = "video";
                public const string ArtworkImages = "artworkimages";
            }

            public static class Operations
            {
                public const string Get = "get";
                public const string Set = "set";
                public const string Update = "update";
                public const string Delete = "delete";
                public const string List = "list";
            }

            public static readonly Dictionary<string, StorageType> EntityStorageTypes = new()
            {
                // Table Storage mappings
                { EntityTypes.Blog, StorageType.Table },
                { EntityTypes.Books, StorageType.Table },
                { EntityTypes.Contact, StorageType.Table },
                { EntityTypes.Events, StorageType.Table },
                { EntityTypes.GitHub, StorageType.Table },
                { EntityTypes.LiveStreams, StorageType.Table },
                { EntityTypes.Music, StorageType.Table },
                { EntityTypes.Portfolio, StorageType.Table },
                { EntityTypes.BlogComments, StorageType.Table },
                { EntityTypes.PortfolioComments, StorageType.Table },

                // Blob Storage mappings
                { EntityTypes.BlogImages, StorageType.Blob },
                { EntityTypes.BooksImages, StorageType.Blob },
                { EntityTypes.EventsData, StorageType.Blob },
                { EntityTypes.LiveStreamsImages, StorageType.Blob },
                { EntityTypes.PortfolioImages, StorageType.Blob },
                { EntityTypes.Video, StorageType.Blob },
                { EntityTypes.ArtworkImages, StorageType.Blob }
            };
        }
    }
}
