using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AzTwWebsiteApi.Services.Storage;
using AzTwWebsiteApi.Models.Blog;
using AzTwWebsiteApi.Utils;
using Azure.Identity;
// using AzTwWebsiteApi.Services.Email;

// get the standard mock tables and blog storage account names
string blogCommentsTableName = GetEnvironmentVariable("MOCK_BLOG_COMMENTS_TABLE_NAME");
string blogPostsTableName = GetEnvironmentVariable("MOCK_BLOG_POSTS_TABLE_NAME");
string blogImagesContainerName = GetEnvironmentVariable("MOCK_BLOG_IMAGES_CONTAINER_NAME");

public static string GetEnvironmentVariable(string key)
{
  string mockValue = Environment.GetEnvironmentVariable(key);
  // If running in production, transform the mock value
  if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "production" && mockValue != null)
  {
    return mockValue.Replace("MOCK", "").Replace("_", ""); // Adjust based on naming conventions
  }
  return mockValue ?? throw new ArgumentNullException($"Environment variable '{key}' is not set.");
}

public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
  // Register Blob and Table Storage with Managed Identity
  var credential = new DefaultAzureCredential();
  services.AddSingleton<TokenCredential>(credential);

  services.AddSingleton<IBlobStorageService>(provider =>
  new BlobStorageService(credential, blogImagesContainerName));

  services.AddSingleton<ITableStorageService>(provider =>
  new TableStorageService(credential, blogPostsTableName)); 
  services.AddSingleton<ITableStorageService>(provider =>
  new TableStorageService(credential, blogCommentsTableName)); 

  services.AddSingleton<IBlogPostTableService>(provider =>
    new TableStorageService(credential, blogPostsTableName));
  services.AddSingleton<IBlogCommentsTableService>(provider =>
    new TableStorageService(credential, blogCommentsTableName));


  // TODO: Register Email Sending
  // services.Configure<SendGridSettings>(configuration.GetSection("SendGridSettings"));
  // services.AddSingleton<IEmailService, SendGridEmailService>();
  // services.AddHttpClient<IEmailService, SendGridEmailService>()
  // .SetHandlerLifetime(TimeSpan.FromMinutes(5))
  // .AddPolicyHandler(GetRetryPolicy());

  // Additional configurations (logging, telemetry, etc.)
  services.AddLogging(loggingBuilder =>
  {
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
    loggingBuilder.AddApplicationInsights(configuration["ApplicationInsights:InstrumentationKey"]);
  });
  services.AddApplicationInsightsTelemetry();
}