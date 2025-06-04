using System;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Microsoft.Extensions.Logging;

namespace AzTwWebsiteApi.Services.Utils
{
    public class RetryPolicy
    {
        private readonly ILogger _logger;
        private readonly int _maxRetries;
        private readonly TimeSpan _initialDelay;
        private readonly TimeSpan _maxDelay;

        public RetryPolicy(ILogger logger, int maxRetries = 3, int initialDelayMs = 100, int maxDelayMs = 5000)
        {
            _logger = logger;
            _maxRetries = maxRetries;
            _initialDelay = TimeSpan.FromMilliseconds(initialDelayMs);
            _maxDelay = TimeSpan.FromMilliseconds(maxDelayMs);
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName)
        {
            var exceptions = new List<Exception>();
            var delay = _initialDelay;

            for (int i = 0; i <= _maxRetries; i++)
            {
                try
                {
                    return await operation();
                }
                catch (RequestFailedException ex) when (IsTransient(ex))
                {
                    if (i == _maxRetries) throw;
                    exceptions.Add(ex);
                    await HandleTransientException(ex, i, delay, operationName);
                    delay = CalculateNextDelay(delay);
                }
                catch (Exception ex) when (IsTransient(ex))
                {
                    if (i == _maxRetries) throw;
                    exceptions.Add(ex);
                    await HandleTransientException(ex, i, delay, operationName);
                    delay = CalculateNextDelay(delay);
                }
            }

            throw new AggregateException($"Failed to execute {operationName} after {_maxRetries} retries", exceptions);
        }

        private static bool IsTransient(RequestFailedException ex)
        {
            return ex.Status == 429 || // Too Many Requests
                   ex.Status == 503 || // Service Unavailable
                   ex.Status == 500 || // Internal Server Error
                   ex.Status == 502 || // Bad Gateway
                   ex.Status == 504;   // Gateway Timeout
        }

        private static bool IsTransient(Exception ex)
        {
            return ex is TimeoutException ||
                   ex is System.Net.Http.HttpRequestException ||
                   (ex is WebException webEx && IsTransientWebException(webEx));
        }

        private static bool IsTransientWebException(WebException ex)
        {
            return ex.Status == WebExceptionStatus.ConnectionClosed ||
                   ex.Status == WebExceptionStatus.Timeout ||
                   ex.Status == WebExceptionStatus.RequestCanceled;
        }

        private TimeSpan CalculateNextDelay(TimeSpan currentDelay)
        {
            var nextDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * 2);
            return nextDelay > _maxDelay ? _maxDelay : nextDelay;
        }

        private async Task HandleTransientException(Exception ex, int attemptNumber, TimeSpan delay, string operationName)
        {
            _logger.LogWarning(ex, 
                "Transient error on attempt {AttemptNumber} for operation {OperationName}. Retrying in {DelayMs}ms. Error: {Error}", 
                attemptNumber + 1, operationName, delay.TotalMilliseconds, ex.Message);
            
            await Task.Delay(delay);
        }
    }
}
