using Azure.Data.Tables;

public class TableStorageService : ITableStorageService
{
  private readonly TableClient _tableClient;

  public TableStorageService(IConfiguration configuration, ILogger<TableStorageService> logger)
  {
    string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
    _tableClient = new TableClient(connectionString, "mockblog");
    // Prod version
    // _tableClient = new TableClient(connectionString, "blog");
  }

  public async Task<BlogPost> GetBlogPostAsync(string partitionKey, string rowKey)
  {
    try
    {
      // Ensure the table exists
      await _tableClient.CreateIfNotExistsAsync();

      // Retrieve a specific entity from the table
      var response = await _tableClient.GetEntityAsync<BlogPost>(partitionKey, rowKey);
      return response.Value;
    }
    catch (RequestFailedException ex) when (ex.Status == 404)
    {
      // Handle not found error
      throw new Exception($"Entity with PartitionKey '{partitionKey}' and RowKey '{rowKey}' not found.", ex);
    }
    catch (Exception ex)
    {
      // Handle other exceptions
      throw new Exception("An error occurred while retrieving the blog post.", ex);
    }
  }

  // Implement methods to interact with Azure Table Storage
  public async Task<IEnumerable<BlogPost>> GetBlogPostsAsync()
  {
    try
    {
      // Ensure the table exists
      await _tableClient.CreateIfNotExistsAsync();

      // Retrieve all entities from the table
      var queryResults = _tableClient.Query<BlogPost>();
      return await Task.FromResult(queryResults);
    }
    catch (Exception ex)
    {
      // Handle exceptions
      throw new Exception("An error occurred while retrieving blog posts.", ex);
    }
  }
}