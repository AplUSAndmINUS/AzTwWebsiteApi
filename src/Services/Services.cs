using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AzTwWebsiteApi.Services.Storage;
using AzTwWebsiteApi.Models.Blog;
using AzTwWebsiteApi.Utils;

namespace AzTwWebsiteApi.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzTwWebsiteServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration settings
        services.Configure<StorageSettings>(configuration.GetSection("Storage"));
        
        // Register Azure credential
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = false,
            ExcludeManagedIdentityCredential = false,
            ExcludeSharedTokenCacheCredential = true,
            ExcludeVisualStudioCredential = true,
            ExcludeVisualStudioCodeCredential = true,
            ExcludeAzureCliCredential = true,
            ExcludeInteractiveBrowserCredential = true
        });
        services.AddSingleton<TokenCredential>(credential);

        // Register storage services
        services.AddSingleton<ITableStorageService>(provider =>
        {
            var settings = configuration.GetSection("Storage").Get<StorageSettings>()
                ?? throw new InvalidOperationException("Storage settings are not configured");
            return new TableStorageService(
                credential,
                settings.BlogPostsTableName);
        });

        // Register blob storage when needed
        if (!string.IsNullOrEmpty(configuration["Storage:BlogImagesContainerName"]))
        {
            services.AddSingleton<IBlobStorageService>(provider =>
            {
                var settings = configuration.GetSection("Storage").Get<StorageSettings>()
                    ?? throw new InvalidOperationException("Storage settings are not configured");
                return new BlobStorageService(
                    credential,
                    settings.BlogImagesContainerName);
            });
        }

        // Configure logging and telemetry
        ConfigureLogging(services, configuration);

        return services;
    }

    private static void ConfigureLogging(IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            
            var connectionString = configuration["ApplicationInsights:ConnectionString"];
            if (!string.IsNullOrEmpty(connectionString))
            {
                builder.AddApplicationInsights(config =>
                {
                    config.ConnectionString = connectionString;
                });
            }
        });

        services.AddApplicationInsightsTelemetry(options =>
        {
            options.EnableAdaptiveSampling = true;
            options.EnableQuickPulseMetricStream = true;
        });
    }
}

public class StorageSettings
{
    public string BlogPostsTableName { get; set; } = string.Empty;
    public string BlogCommentsTableName { get; set; } = string.Empty;
    public string BlogImagesContainerName { get; set; } = string.Empty;

    public static string TransformMockName(string name)
    {
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
        {
            return name.Replace("MOCK_", "").Replace("_", "");
        }
        return name;
    }
}