using AzTwWebsiteApi.Services.Utils;

namespace AzTwWebsiteApi.Functions;

public static class GetStorageService
{
    public static string GetStorageServiceName(string storageService)
    {
        if (string.IsNullOrEmpty(storageService))
        {
            throw new ArgumentNullException(nameof(storageService), "No storage service specified");
        }

        if (!Constants.Storage.EntityStorageTypes.TryGetValue(storageService, out var storageType))
        {
            var validTypes = string.Join(", ", Constants.Storage.EntityStorageTypes.Keys);
            throw new ArgumentException($"Unknown entity type: {storageService}. Valid types are: {validTypes}");
        }

        // Just return the service name as-is - any transformations should happen in Program.cs
        return storageService;
    }
}