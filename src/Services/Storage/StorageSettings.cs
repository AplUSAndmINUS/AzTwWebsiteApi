namespace AzTwWebsiteApi.Services.Storage
{
    public class StorageSettings
    {
        public string BlogPostsTableName { get; set; } = string.Empty;
        public string BlogCommentsTableName { get; set; } = string.Empty;
        public string BlogImagesContainerName { get; set; } = string.Empty;

        public static string TransformMockName(string name)
        {
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
            {
                return name.Replace("MOCK_", "").Replace("_", "");
            }
            return name;
        }
    }
}