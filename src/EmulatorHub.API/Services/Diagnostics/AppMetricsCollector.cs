using App.Metrics;
using EmulatorHub.API.Model.Diagnostics;
using EmulatorHub.PushBroker.Application.Channels;
using EmulatorHub.PushBroker.Application.Models;
using Microsoft.Extensions.Options;
using Vayosoft.Threading.Channels;
using Vayosoft.Threading.Channels.Models;
using Vayosoft.Utilities;

namespace EmulatorHub.API.Services.Diagnostics
{
    public sealed class AppMetricsCollector : BackgroundService
    {
        private readonly HandlerChannel<PushMessage, MessageChannelHandler> _channel;
        private readonly PeriodicTimer _timer;
        private readonly IMetrics _metrics;
        private readonly ILogger<AppMetricsCollector> _logger;

        public AppMetricsCollector(IMetrics metrics,
            IOptions<CollectorOptions> options,
            HandlerChannel<PushMessage, MessageChannelHandler> channel,
            ILogger<AppMetricsCollector> logger)
        {
            _metrics = metrics;
            _channel = channel;
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

                // Terminates this process and returns an exit code to the operating system.
                // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
                // performs one of two scenarios:
                // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
                // 2. When set to "StopHost": will cleanly stop the host, and log errors.
                //
                // In order for the Windows Service Management system to leverage configured
                // recovery options, we need to terminate the process with a non-zero exit code.
                Environment.Exit(1);
            }
        }
        private void CollectData()
        {
            var time = DateTime.Now;
            var snapshot = _channel.GetSnapshot();
            var measurements = new Measurements
            {
                SnapshotTime = new SnapshotTime
                {
                    From = time.AddMinutes(-1),
                    To = time
                },
                Channels = new Dictionary<string, ChannelHandlerTelemetrySnapshot>(1)
                    {{"PushBroker", (ChannelHandlerTelemetrySnapshot)snapshot} }
            };

            _logger.LogInformation("[snapshot]\r\n{measurements}", measurements.ToJson());
        }

        public override void Dispose()
        {
            _timer.Dispose();
        }
    }
}
