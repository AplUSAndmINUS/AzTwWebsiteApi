using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Azure.Data.Tables;
using AzTwWebsiteApi.Services.Blog;
using AzTwWebsiteApi.Services.Storage;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddLogging();
        services.ConfigureFunctionsApplicationInsights();
        services.AddHttpClient();

        // Register your services for dependency injection
        services.AddScoped<IBlogService, BlogService>();
        services.AddScoped<ITableStorageService, TableStorageService>();
        services.AddScoped<IBlobStorageService, BlobStorageService>();

        // Configure Azure Table Storage
        services.AddSingleton(sp =>
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            return new TableServiceClient(connectionString);
        });
    })
    .Build();

host.Run();
