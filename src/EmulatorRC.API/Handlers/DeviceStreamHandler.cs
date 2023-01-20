using EmulatorRC.API.Channels;
using Microsoft.AspNetCore.Connections;
using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;

namespace EmulatorRC.API.Handlers
{
    public sealed partial class DeviceStreamHandler : ConnectionHandler
    {
        private readonly DeviceController _controller;
        private readonly ILogger<DeviceStreamHandler> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public DeviceStreamHandler(
            DeviceController controller,
            ILogger<DeviceStreamHandler> logger,
            IHostApplicationLifetime lifetime)
        {
            _controller = controller;
            _logger = logger;
            _lifetime = lifetime;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            _logger.LogInformation("TCP (Device) {ConnectionId} connected", connection.ConnectionId);

            try
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(
                    connection.ConnectionClosed, _lifetime.ApplicationStopping);
                var cancellationToken = cts.Token;

                var request = await _controller.GetRequestAsync(connection.Transport, cancellationToken);

                //todo authentication
                if (string.IsNullOrEmpty(request.DeviceId))
                {
                    throw new ApplicationException("Authentication failed");
                }

                await _controller.ExecuteAsync(request, connection.Transport, cancellationToken);
            }
            catch (ConnectionResetException) { }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                _logger.LogError(e, "TCP (Device) {ConnectionId} => {Error}", connection.ConnectionId, e.Message);
            }
            finally
            {
                await connection.Transport.Input.CompleteAsync();
                await connection.Transport.Output.CompleteAsync();

                _logger.LogInformation("TCP (Device) {ConnectionId} disconnected", connection.ConnectionId);
            }
        }
    }
  
    internal enum Commands : byte
    {
        Undefined,
        Ping,
        GetBattery
    }

    public record Handshake(string DeviceId);
    public record SpeakerHandshake(string DeviceId) : Handshake(DeviceId);
    public record AudioHandshake(string DeviceId) : Handshake(DeviceId);
    public record VideoHandshake(string DeviceId) : Handshake(DeviceId)
    {
        public int Width { get; init; }
        public int Height { get; init; }
    }

}
