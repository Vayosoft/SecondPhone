using EmulatorRC.API.Services.Sessions;
using Microsoft.AspNetCore.Connections;

namespace EmulatorRC.API.Services
{
    public class StreamService : ConnectionHandler
    {
        private readonly StreamSession _session;
        private readonly ILogger<StreamService> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public StreamService(
            StreamSession session,
            ILogger<StreamService> logger,
            IHostApplicationLifetime lifetime)
        {
            _session = session;
            _logger = logger;
            _lifetime = lifetime;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            _logger.LogInformation("{connectionId} connected", connection.ConnectionId);

            try
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(
                    connection.ConnectionClosed, _lifetime.ApplicationStopping);
                var token = cts.Token;

                _ = _session.ReadAsync("default", connection.Transport.Output, token);
                await _session.WriteAsync("default", connection.Transport.Input, token);
            }
            catch (Exception e)
            {
                _logger.LogError("{connectionId} => {error}", connection.ConnectionId, e.Message);
            }

            _logger.LogInformation("{connectionId} disconnected", connection.ConnectionId);
        }
    }
}
