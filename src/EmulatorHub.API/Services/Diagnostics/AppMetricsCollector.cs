using App.Metrics;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using EmulatorHub.Application.PushGateway.Channels;
using EmulatorHub.Application.PushGateway.Models;
using Vayosoft.Threading.Channels;
using Vayosoft.Threading.Channels.Models;
using static EmulatorHub.API.Services.Diagnostics.AppMetricsRegistry.Channels;
using static EmulatorHub.API.Services.Diagnostics.AppMetricsRegistry.Gauges;

namespace EmulatorHub.API.Services.Diagnostics
{
    public sealed class AppMetricsCollector : BackgroundService
    {
        private readonly HandlerChannel<PushMessage, MessageChannelHandler> _channel;
        private readonly PeriodicTimer _timer;
        private readonly IMetrics _metrics;
        private readonly ILogger<AppMetricsCollector> _logger;
        private readonly Process _process = Process.GetCurrentProcess();

        private static readonly MetricTags PushBroker = new("channel", "push_broker");

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

                Environment.Exit(1);
            }
        }
        private void CollectData()
        {
            if (_channel.GetSnapshot() is ChannelHandlerTelemetrySnapshot snapshot)
            {
                _metrics.Measure.Gauge.SetValue(Length, PushBroker, snapshot.HandlerTelemetrySnapshot.Length);
                _metrics.Measure.Gauge.SetValue(OperationCount, PushBroker, snapshot.HandlerTelemetrySnapshot.OperationCount);
                _metrics.Measure.Gauge.SetValue(OperationTime, PushBroker, snapshot.HandlerTelemetrySnapshot.MeasurementTimeMs);
                _metrics.Measure.Gauge.SetValue(DroppedItems, PushBroker, snapshot.DroppedItems);
            }

            ThreadPool.GetMaxThreads(out var maxWt, out _);
            ThreadPool.GetMinThreads(out var minWt, out _);
            ThreadPool.GetAvailableThreads(out var availableThreads, out _);

            _metrics.Measure.Gauge.SetValue(MaxThreads, maxWt);
            _metrics.Measure.Gauge.SetValue(MinThreads, minWt);
            _metrics.Measure.Gauge.SetValue(AvailableThreads, availableThreads);
            _metrics.Measure.Gauge.SetValue(WorkingThreads, _process.Threads.Count);
        }

        public override void Dispose()
        {
            _timer.Dispose();
        }
    }
}
