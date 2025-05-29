using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace AzTwWebsiteApi.Utils
{
    public static class LoggingExtensions
    {
        private const string CorrelationIdKey = "CorrelationId";
        private const string EnvironmentKey = "Environment";
        private const string ComponentKey = "Component";
        private const string OperationKey = "Operation";
        private const string ModuleKey = "Module";

        public static ILogger WithContext(this ILogger logger, 
            string module,
            string component, 
            string operation, 
            string? correlationId = null)
        {
            logger.BeginScope(new[] 
            {
                new KeyValuePair<string, object>(ModuleKey, module),
                new KeyValuePair<string, object>(ComponentKey, component),
                new KeyValuePair<string, object>(OperationKey, operation),
                new KeyValuePair<string, object>(CorrelationIdKey, correlationId ?? Guid.NewGuid().ToString()),
                new KeyValuePair<string, object>(EnvironmentKey, Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Development")
            });
            return logger;
        }

        public static void LogFunctionStart(this ILogger logger, string module, string functionName, string? correlationId = null)
        {
            logger.WithContext(module, "Function", $"{functionName}.Start", correlationId)
                .LogInformation("[{Module}] Function {FunctionName} execution started", module, functionName);
        }

        public static void LogFunctionComplete(this ILogger logger, string module, string functionName, string? correlationId = null)
        {
            logger.WithContext(module, "Function", $"{functionName}.Complete", correlationId)
                .LogInformation("[{Module}] Function {FunctionName} execution completed successfully", module, functionName);
        }

        public static void LogFunctionError(this ILogger logger, string module, string functionName, Exception ex, string? correlationId = null)
        {
            logger.WithContext(module, "Function", $"{functionName}.Error", correlationId)
                .LogError(ex, "[{Module}] Function {FunctionName} execution failed: {ErrorMessage}", module, functionName, ex.Message);
        }
    }
}
