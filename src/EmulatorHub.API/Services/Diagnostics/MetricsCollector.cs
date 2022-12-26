using EmulatorHub.PushBroker.Application.Channels;
using EmulatorHub.PushBroker.Application.Models;
using Vayosoft.Threading.Channels;
using Vayosoft.Threading.Channels.Diagnostics;
using Vayosoft.Threading.Channels.Models;
using Vayosoft.Utilities;

namespace EmulatorHub.API.Services.Diagnostics
{
    public class MetricsCollector : BackgroundService
    {
        private readonly HandlerChannel<PushMessage, MessageChannelHandler> _channel;
        private readonly ILogger<MetricsCollector> _logger;

        public MetricsCollector(HandlerChannel<PushMessage, MessageChannelHandler> channel, ILogger<MetricsCollector> logger)
        {
            _channel = channel;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
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
                        Channels = new Dictionary<string, ChannelHandlerTelemetrySnapshot>(1) {{"PushBroker", (ChannelHandlerTelemetrySnapshot)snapshot} }
                    };

                    _logger.LogInformation("[snapshot]\r\n{measurements}", measurements.ToJson());

                    await Task.Delay(TimeSpan.FromMinutes(1), token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);

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
    }
}
