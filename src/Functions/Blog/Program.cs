using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using AzTwWebsiteApi.Models.Blog;
using AzTwWebsiteApi.Services.Blog;
using AzTwWebsiteApi.Services.Storage;

var host = new HostBuilder()
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

        // Configure Azure Table Storage with connection string
        services.AddSingleton<ITableStorageService<BlogPost>>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<TableStorageService<BlogPost>>>();
            var tableName = Environment.GetEnvironmentVariable("BlogPostsTableName") ?? "blogposts";
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") 
                ?? throw new ArgumentNullException("AzureWebJobsStorage connection string is not set");
            return new TableStorageService<BlogPost>(connectionString, tableName, logger);
        });
    })
    .Build();

host.Run();
