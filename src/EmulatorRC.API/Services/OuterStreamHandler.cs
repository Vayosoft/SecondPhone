using EmulatorRC.API.Channels;
using EmulatorRC.Entities;
using Microsoft.AspNetCore.Connections;
using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;

namespace EmulatorRC.API.Services
{
    public class OuterStreamHandler : ConnectionHandler
    {
        private readonly StreamChannel _channel;
        private readonly ILogger<OuterStreamHandler> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public OuterStreamHandler(
            StreamChannel channel,
            ILogger<OuterStreamHandler> logger,
            IHostApplicationLifetime lifetime)
        {
            _channel = channel;
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

                _ = ReadAsync("default", connection.Transport.Output, token);
                await WriteAsync("default", connection.Transport.Input, token);
            }
            catch (Exception e)
            {
                _logger.LogError("{connectionId} => {error}", connection.ConnectionId, e.Message);
            }

            _logger.LogInformation("{connectionId} disconnected", connection.ConnectionId);
        }

        public async Task ReadAsync(string deviceId, PipeWriter output, CancellationToken token)
        {
            while (true)
            {
                if (_channel.TryGetChannel(deviceId, out var channel))
                {
                    var result = await channel.Reader.ReadAsync(token);
                    var buffer = result.Buffer;

                    foreach (var segment in buffer)
                    {
                        await output.WriteAsync(segment, token);
                    }

                    if (result.IsCompleted)
                    {
                        break;
                    }

                    channel.Reader.AdvanceTo(buffer.End);
                }
                else
                {
                    await Task.Delay(1000, token);
                }
            }
        }

        private async Task WriteAsync(string deviceId, PipeReader input, CancellationToken token)
        {
            DeviceSession session = null;

            while (true)
            {
                var result = await input.ReadAsync(token);
                var buffer = result.Buffer;
                session ??= Handshake(ref buffer);

                foreach (var segment in buffer)
                {
                    await _channel.GetOrCreateChannel(deviceId).Writer.WriteAsync(segment, token);
                }

                if (result.IsCompleted)
                {
                    break;
                }

                input.AdvanceTo(buffer.End);
            }

            await input.CompleteAsync();
        }

        private static DeviceSession Handshake(ref ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);

            if (reader.TryReadExact(4, out var header))
            {
                var length = BitConverter.ToInt32(header.FirstSpan);
                if (reader.TryReadExact(length, out var handshake))
                {
                    var deviceSession = JsonSerializer.Deserialize<DeviceSession>(handshake.FirstSpan);
                    buffer = buffer.Slice(4 + length);

                    return deviceSession;
                }
            }

            throw new OperationCanceledException("Not authenticated");
        }
    }
}
