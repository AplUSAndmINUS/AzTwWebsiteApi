using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AzTwWebsiteApi.Services.Storage;
using AzTwWebsiteApi.Models.Blog;

namespace AzTwWebsiteApi.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzTwWebsiteServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration settings with proper binding
        services.Configure<StorageSettings>(options => 
        {
            configuration.GetSection("Storage").Bind(options);
            // Set default values if not provided in configuration
            options.BlogPostsTableName = StorageSettings.TransformMockName(
                options.BlogPostsTableName ?? "MOCK_BLOG_POSTS");
            options.BlogCommentsTableName = StorageSettings.TransformMockName(
                options.BlogCommentsTableName ?? "MOCK_BLOG_COMMENTS");
            options.BlogImagesContainerName = StorageSettings.TransformMockName(
                options.BlogImagesContainerName ?? "MOCK_BLOG_IMAGES");
        });
        
        // Register TableStorageService for BlogPost
        services.AddScoped<ITableStorageService<BlogPost>>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<TableStorageService<BlogPost>>>();
            var settings = provider.GetRequiredService<IOptions<StorageSettings>>().Value;
            var connectionString = configuration.GetConnectionString("AzureWebJobsStorage") 
                ?? throw new InvalidOperationException("Storage connection string not found");
            
            return new TableStorageService<BlogPost>(
                connectionString,
                settings.BlogPostsTableName,
                logger);
        });

        return services;
    }
}