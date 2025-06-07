using System;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzTwWebsiteApi.Functions.Utils
{
    public static class FunctionRegistrationHelper
    {
        public static void VerifyFunctionDiscovery(IHost host)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            
            var assembly = Assembly.GetExecutingAssembly();
            var functionTypes = assembly.GetTypes()
                .Where(type => type.GetMethods().Any(method => method.GetCustomAttribute<FunctionAttribute>() != null))
                .ToList();
            
            logger.LogInformation("Found {Count} classes containing Function attributes:", functionTypes.Count);
            foreach (var type in functionTypes)
            {
                var methods = type.GetMethods()
                    .Where(m => m.GetCustomAttribute<FunctionAttribute>() != null)
                    .ToList();
                
                logger.LogInformation("  - {Type} has {Count} functions:", type.Name, methods.Count);
                foreach (var method in methods)
                {
                    var attr = method.GetCustomAttribute<FunctionAttribute>();
                    logger.LogInformation("    * {FunctionName}", attr?.Name ?? "Unknown");
                }
            }
        }
    }
}
