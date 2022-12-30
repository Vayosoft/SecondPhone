using App.Metrics;
using EmulatorRC.API.Model.Diagnostics;
using Microsoft.Extensions.Options;
using static EmulatorRC.API.Services.Diagnostics.AppMetricsRegistry.Gauges;

namespace EmulatorRC.API.Services.Diagnostics
{
    public sealed class AppMetricsCollector : BackgroundService
    {
        private readonly PeriodicTimer _timer;
        private readonly IMetrics _metrics;
        private readonly ILogger<AppMetricsCollector> _logger;

        public AppMetricsCollector(IMetrics metrics, IOptions<CollectorOptions> options, ILogger<AppMetricsCollector> logger)
        {
            _metrics = metrics;
            _logger = logger;
            _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(options.Value.CollectIntervalMilliseconds));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (await _timer.WaitForNextTickAsync(stoppingToken))
                {
                    CollectData();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                _logger.LogError("An error occurred. {message}", e.Message);

                Environment.Exit(1);
            }
        }

        private void CollectData()
        {
            ThreadPool.GetMaxThreads(out var maxWt, out _);
            ThreadPool.GetMinThreads(out var minWt, out _);
            ThreadPool.GetAvailableThreads(out var workerThreads, out _);

            _metrics.Measure.Gauge.SetValue(MaxThreads, maxWt);
            _metrics.Measure.Gauge.SetValue(MinThreads, minWt);
            _metrics.Measure.Gauge.SetValue(AvailableThreads, workerThreads);
        }

        public override void Dispose()
        {
            _timer.Dispose();
        }
    }
}
