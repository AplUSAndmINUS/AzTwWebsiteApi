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

        return storageType switch
        {
            Constants.Storage.StorageType.Table => storageService,  // Table name is same as service name
            Constants.Storage.StorageType.Blob => storageService,   // Container name is same as service name
            _ => throw new ArgumentException($"Unsupported storage type: {storageType}")
        };
    }
}