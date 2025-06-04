using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace AzTwWebsiteApi.Services.Utils
{
    public class CircuitBreaker
    {
        private readonly ILogger _logger;
        private readonly int _maxFailures;
        private readonly TimeSpan _resetTimeout;
        private int _failureCount;
        private DateTime _lastFailureTime;
        private readonly object _lock = new();
        private CircuitState _state;

        public enum CircuitState
        {
            Closed,      // Normal operation
            Open,        // Not allowing operations
            HalfOpen    // Testing if service is back
        }

        public CircuitBreaker(ILogger logger, int maxFailures = 3, int resetTimeoutSeconds = 60)
        {
            _logger = logger;
            _maxFailures = maxFailures;
            _resetTimeout = TimeSpan.FromSeconds(resetTimeoutSeconds);
            _state = CircuitState.Closed;
            _failureCount = 0;
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName)
        {
            await CheckCircuitState(operationName);

            try
            {
                var result = await operation();
                Success();
                return result;
            }
            catch (Exception ex)
            {
                return await HandleFailure(ex, operation, operationName);
            }
        }

        private async Task CheckCircuitState(string operationName)
        {
            lock (_lock)
            {
                if (_state == CircuitState.Open)
                {
                    if (DateTime.UtcNow - _lastFailureTime > _resetTimeout)
                    {
                        _logger.LogInformation(
                            "Circuit breaker for {OperationName} moving from Open to Half-Open state", 
                            operationName);
                        _state = CircuitState.HalfOpen;
                    }
                    else
                    {
                        throw new CircuitBreakerOpenException(
                            $"Circuit breaker is Open for {operationName}. Try again later.");
                    }
                }
            }
        }

        private void Success()
        {
            lock (_lock)
            {
                _failureCount = 0;
                _state = CircuitState.Closed;
            }
        }

        private async Task<T> HandleFailure<T>(Exception ex, Func<Task<T>> operation, string operationName)
        {
            lock (_lock)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;

                if (_state == CircuitState.HalfOpen || _failureCount >= _maxFailures)
                {
                    _state = CircuitState.Open;
                    _logger.LogError(ex,
                        "Circuit breaker for {OperationName} moved to Open state after {FailureCount} failures",
                        operationName, _failureCount);
                    throw new CircuitBreakerOpenException(
                        $"Circuit breaker opened for {operationName} after {_failureCount} failures", 
                        ex);
                }
            }

            throw ex;
        }
    }

    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
        public CircuitBreakerOpenException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}
