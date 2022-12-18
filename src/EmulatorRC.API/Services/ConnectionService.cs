using System.Text;
using Microsoft.AspNetCore.Connections;

namespace EmulatorRC.API.Services
{
    public class ConnectionService : ConnectionHandler
    {
        private readonly ILogger<ConnectionService> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public ConnectionService(ILogger<ConnectionService> logger, IHostApplicationLifetime lifetime)
        {
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
                var cancellationToken = cts.Token;

                while (true)
                {
                    var result = await connection.Transport.Input.ReadAsync(cancellationToken);
                    var buffer = result.Buffer;

                    foreach (var segment in buffer)
                    {
                        _logger.LogInformation("{data}", Encoding.UTF8.GetString(segment.ToArray()));
                        await connection.Transport.Output.WriteAsync(segment, cancellationToken);
                    }

                    if (result.IsCompleted)
                    {
                        break;
                    }

                    connection.Transport.Input.AdvanceTo(buffer.End);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("{connectionId} => {error}", connection.ConnectionId, e.Message);
            }

            _logger.LogInformation("{connectionId} disconnected", connection.ConnectionId);
        }
    }
}
