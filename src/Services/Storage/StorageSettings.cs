namespace AzTwWebsiteApi.Services.Storage
{
    public class StorageSettings
    {
        public string BlogPostsTableName { get; set; } = string.Empty;
        public string BlogCommentsTableName { get; set; } = string.Empty;
        public string BlogImagesContainerName { get; set; } = string.Empty;

        public static string TransformMockName(string name)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLowerInvariant();
            
            // Only remove "mock" prefix in production
            if (environment == "production" || environment == "prod")
            {
                return name.StartsWith("mock", StringComparison.OrdinalIgnoreCase) 
                    ? name[4..]
                    : name.StartsWith("mock-", StringComparison.OrdinalIgnoreCase) || name.StartsWith("mock_", StringComparison.OrdinalIgnoreCase)
                    ? name[5..] 
                    : name;
            }
            
            // For dev/test environments, keep the original name with "mock" or "mock-" prefix
            return name;
        }
    }
}