
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Http; // Ensure HTTP extension is loaded
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Models.Blog;
using AzTwWebsiteApi.Services.Blog;
using AzTwWebsiteApi.Services.Storage;
using AzTwWebsiteApi.Functions;
using AzTwWebsiteApi.Services.Utils;
using System.Reflection;
using System.Threading.Tasks; // For async Main
using System; // For Console
using AzTwWebsiteApi.Functions.Utils;

// Simplified entry point for .NET isolated functions
var host = new HostBuilder()
    // Use the default worker configuration with explicit registration to ensure Function discovery
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.SetBasePath(context.HostingEnvironment.ContentRootPath)
              .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        // Configure logging without Application Insights for local development
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddHttpClient();

        // Add our custom services
        services.AddScoped<IBlogService, BlogService>();
        services.AddScoped<HandleCrudFunctions>();

        // Add utility services
        services.AddSingleton<IMetricsService, MetricsService>();

        // Register our split blog function classes
        services.AddScoped<BlogPostFunctions>();
        services.AddScoped<BlogCommentFunctions>();
        services.AddScoped<BlogImageFunctions>();

        // Configure storage services
        var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
            ?? throw new ArgumentNullException("AzureWebJobsStorage connection string is not set");

        // Configure Blog-related services
        ConfigureBlogServices(services, storageConnectionString);

         // Configure Application Insights telemetry for the worker service
    services.AddApplicationInsightsTelemetryWorkerService();
    })
    
    .Build();

// Add this to properly log more information about the host startup
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting host with functions: BlogPostFunctions, BlogCommentFunctions, BlogImageFunctions");

// Verify that functions are properly discovered
FunctionRegistrationHelper.VerifyFunctionDiscovery(host);

// Run the host
await host.RunAsync();

void ConfigureBlogServices(IServiceCollection services, string storageConnectionString)
{
    // Configure Table Storage services
    services.AddSingleton<ITableStorageService<BlogPost>>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<TableStorageService<BlogPost>>>();
        var metrics = sp.GetRequiredService<IMetricsService>();
        var rawTableName = Environment.GetEnvironmentVariable("BlogPostsTableName") ?? "mockblog";
        var tableName = StorageSettings.TransformMockName(rawTableName);
        
        logger.LogInformation("Configuring BlogPost TableStorageService with table name: {TableName}", tableName);
        return new TableStorageService<BlogPost>(
            connectionString: storageConnectionString,
            tableName: tableName,
            logger: logger,
            metrics: metrics);
    });

    // Configure Blob Storage services for blog images
    services.AddSingleton<IBlobStorageService<BlogImage>>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<BlobStorageService<BlogImage>>>();
        var metrics = sp.GetRequiredService<IMetricsService>();
        var rawContainerName = Environment.GetEnvironmentVariable("BlogImagesContainerName") ?? "mock-blog-images";
        var containerName = StorageSettings.TransformMockName(rawContainerName);
        
        logger.LogInformation("Configuring BlogImage BlobStorageService with container name: {ContainerName}", containerName);
        return new BlobStorageService<BlogImage>(
            connectionString: storageConnectionString,
            containerName: containerName,
            logger: logger,
            metrics: metrics);
    });

    // Configure Table Storage for comments
    services.AddSingleton<ITableStorageService<BlogComment>>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<TableStorageService<BlogComment>>>();
        var metrics = sp.GetRequiredService<IMetricsService>();
        var rawTableName = Environment.GetEnvironmentVariable("BlogCommentsTableName") ?? "mockblogcomments";
        var tableName = StorageSettings.TransformMockName(rawTableName);
        
        logger.LogInformation("Configuring BlogComment TableStorageService with table name: {TableName}", tableName);
        return new TableStorageService<BlogComment>(
            connectionString: storageConnectionString,
            tableName: tableName,
            logger: logger,
            metrics: metrics);
    });
}

// Run the host synchronously to properly initialize all components
host.Run();