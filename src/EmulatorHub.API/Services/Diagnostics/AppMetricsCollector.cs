﻿using App.Metrics;
using EmulatorHub.API.Model.Diagnostics;
using EmulatorHub.PushBroker.Application.Channels;
using EmulatorHub.PushBroker.Application.Models;
using Microsoft.Extensions.Options;
using Vayosoft.Threading.Channels;
using Vayosoft.Threading.Channels.Models;
using static EmulatorHub.API.Services.Diagnostics.AppMetricsRegistry.Channels;

namespace EmulatorHub.API.Services.Diagnostics
{
    public sealed class AppMetricsCollector : BackgroundService
    {
        private readonly HandlerChannel<PushMessage, MessageChannelHandler> _channel;
        private readonly PeriodicTimer _timer;
        private readonly IMetrics _metrics;
        private readonly ILogger<AppMetricsCollector> _logger;

        public const string PushBroker = "push_broker";

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
            var snapshot = (ChannelHandlerTelemetrySnapshot)_channel.GetSnapshot();
            
            _metrics.Measure.Gauge.SetValue(Length(PushBroker), snapshot.HandlerTelemetrySnapshot.Length);
            _metrics.Measure.Gauge.SetValue(OperationCount(PushBroker), snapshot.HandlerTelemetrySnapshot.OperationCount);
            _metrics.Measure.Gauge.SetValue(OperationTime(PushBroker), snapshot.HandlerTelemetrySnapshot.MeasurementTimeMs);
        }

        public override void Dispose()
        {
            _timer.Dispose();
        }
    }
}
