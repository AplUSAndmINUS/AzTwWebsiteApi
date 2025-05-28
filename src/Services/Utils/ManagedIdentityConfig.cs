using Azure.Core;
using Microsoft.Extensions.Configuration;

namespace AzTwWebsiteApi.Utils
{
    public static class ManagedIdentityConfig
    {
        public static TokenCredential GetCredential(IConfiguration configuration)
        {
            // In local development, we use DefaultAzureCredential which will use Visual Studio or VS Code credentials
            // In Azure, it will use the managed identity
            return new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = configuration["ManagedIdentityClientId"]
            });
        }
    }
}