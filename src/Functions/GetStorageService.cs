{
  public class GetStorageService
  {
    public static string GetStorageServiceName(string storageService)
    {
      if (string.IsNullOrEmpty(storageService))
      {
        throw new Error('No storage service specified');
      }

      switch (storageService.ToLowerInvariant())
      {
        case "artwork":
        case "blog":
        case "blogcomments":
        case "books":
        case "contact":
        case "events":
        case "github":
        case "livestreams":
        case "music":
        case "portfolio":
        case "portfoliocomments":
          return TableStorageService.GetTableName(storageService);
        case "artworkimages":
        case "blogimages":
        case "booksimages":
        case "eventsdata":
        case "livestreams-images":
        case "portfolioimages":
        case "video":
          return BlobStorageService.GetBlobContainerName(storageService);
        default:
          throw new Error('Unsupported storage service: ' + storageService);
      }
    }
  }
}