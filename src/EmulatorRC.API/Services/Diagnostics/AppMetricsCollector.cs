using App.Metrics;
using EmulatorRC.API.Model.Diagnostics;
using Microsoft.Extensions.Options;

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

        }

        public override void Dispose()
        {
            _timer.Dispose();
        }
    }
}
