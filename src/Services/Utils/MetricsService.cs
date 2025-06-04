using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AzTwWebsiteApi.Services.Utils
{
    public interface IMetricsService
    {
        void RecordDuration(string operation, TimeSpan duration);
        void IncrementCounter(string metric);
        void RecordValue(string metric, double value);
        IDictionary<string, MetricStats> GetMetrics();
    }

    public class MetricsService : IMetricsService
    {
        private readonly ILogger<MetricsService> _logger;
        private readonly ConcurrentDictionary<string, MetricStats> _metrics = new();

        public MetricsService(ILogger<MetricsService> logger)
        {
            _logger = logger;
        }

        public void RecordDuration(string operation, TimeSpan duration)
        {
            var stats = _metrics.GetOrAdd(operation, _ => new MetricStats());
            stats.AddDuration(duration);
            
            _logger.LogInformation(
                "{Operation} completed in {DurationMs}ms (Avg: {AvgMs}ms)",
                operation, duration.TotalMilliseconds, stats.AverageDuration.TotalMilliseconds);
        }

        public void IncrementCounter(string metric)
        {
            var stats = _metrics.GetOrAdd(metric, _ => new MetricStats());
            stats.IncrementCount();
            
            _logger.LogDebug("{Metric} count: {Count}", metric, stats.Count);
        }

        public void RecordValue(string metric, double value)
        {
            var stats = _metrics.GetOrAdd(metric, _ => new MetricStats());
            stats.AddValue(value);
            
            _logger.LogDebug(
                "{Metric} value: {Value} (Avg: {Average})",
                metric, value, stats.AverageValue);
        }

        public IDictionary<string, MetricStats> GetMetrics()
        {
            return _metrics;
        }
    }

    public class MetricStats
    {
        private long _count;
        private double _sum;
        private readonly ConcurrentQueue<TimeSpan> _durations = new();
        private readonly int _maxSamples = 100;

        public long Count => _count;
        public TimeSpan AverageDuration => _durations.Any() 
            ? TimeSpan.FromTicks((long)_durations.Average(d => d.Ticks)) 
            : TimeSpan.Zero;
        public double AverageValue => _count > 0 ? _sum / _count : 0;

        public void IncrementCount()
        {
            Interlocked.Increment(ref _count);
        }

        public void AddValue(double value)
        {
            Interlocked.Increment(ref _count);
            Interlocked.Exchange(ref _sum, _sum + value);
        }

        public void AddDuration(TimeSpan duration)
        {
            _durations.Enqueue(duration);
            while (_durations.Count > _maxSamples)
            {
                _durations.TryDequeue(out _);
            }
        }
    }

    public class OperationTimer : IDisposable
    {
        private readonly string _operation;
        private readonly IMetricsService _metrics;
        private readonly Stopwatch _stopwatch;

        public OperationTimer(string operation, IMetricsService metrics)
        {
            _operation = operation;
            _metrics = metrics;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _metrics.RecordDuration(_operation, _stopwatch.Elapsed);
        }
    }
}
