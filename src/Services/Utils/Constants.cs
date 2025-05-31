namespace AzTwWebsiteApi.Services.Utils
{
    public static class Constants
    {
        public static class Modules
        {
            public const string Blog = "Blog";
            public const string Contact = "Contact";
            public const string Events = "Events";
            public const string GitHub = "GitHub";
            public const string Live = "Live";
            public const string Portfolio = "Portfolio";
            public const string Video = "Video";
        }

        public static class Storage
        {
            public enum StorageType
            {
                Blob,
                Table
            }

            public static class EntityTypes
            {
                // Table Storage entities
                public const string Artwork = "artwork";
                public const string Blog = "blog";
                public const string BlogComments = "blogcomments";
                public const string Books = "books";
                public const string Contact = "contact";
                public const string Events = "events";
                public const string GitHub = "github";
                public const string LiveStreamsTable = "livestreams";
                public const string Music = "music";
                public const string Portfolio = "portfolio";
                public const string PortfolioComments = "portfoliocomments";
                public const string VideoMetadata = "videometadata";

                // Blob Storage entities
                public const string ArtworkImages = "artwork-images";
                public const string BlogImages = "blog-images";
                public const string BooksImages = "books-images";
                public const string EventsData = "events-data";
                public const string LiveStreamsBlob = "livestreams";
                public const string PortfolioImages = "portfolio-images";
                public const string Video = "video";
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
                { EntityTypes.Artwork, StorageType.Table },
                { EntityTypes.Blog, StorageType.Table },
                { EntityTypes.BlogComments, StorageType.Table },
                { EntityTypes.Books, StorageType.Table },
                { EntityTypes.Contact, StorageType.Table },
                { EntityTypes.Events, StorageType.Table },
                { EntityTypes.GitHub, StorageType.Table },
                { EntityTypes.LiveStreamsTable, StorageType.Table },
                { EntityTypes.Music, StorageType.Table },
                { EntityTypes.Portfolio, StorageType.Table },
                { EntityTypes.PortfolioComments, StorageType.Table },
                { EntityTypes.VideoMetadata, StorageType.Table },

                // Blob Storage mappings
                { EntityTypes.ArtworkImages, StorageType.Blob },
                { EntityTypes.BlogImages, StorageType.Blob },
                { EntityTypes.BooksImages, StorageType.Blob },
                { EntityTypes.EventsData, StorageType.Blob },
                { EntityTypes.LiveStreamsBlob, StorageType.Blob },
                { EntityTypes.PortfolioImages, StorageType.Blob },
                { EntityTypes.Video, StorageType.Blob },
            };
        }
    }
}
