using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Threading.Channels;
using Microsoft.AspNetCore.Connections;
using Newtonsoft.Json.Linq;

namespace EmulatorRC.API.Services
{
    public class ConnectionService : ConnectionHandler
    {
        private readonly BufferChannel _channel;
        private readonly ILogger<ConnectionService> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public ConnectionService(BufferChannel channel,
            ILogger<ConnectionService> logger,
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

                _ = _channel.ReadAsync("default", connection.Transport.Output, token);

                while (true)
                {
                    var result = await connection.Transport.Input.ReadAsync(token);
                    var buffer = result.Buffer;

                    foreach (var segment in buffer)
                    {
                        //await connection.Transport.Output.WriteAsync(segment, token);
                        await _channel.WriteAsync("default", segment, token);
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

    public sealed class BufferChannel
    {
        private readonly ConcurrentDictionary<string, Pipe> _channels = new();
        private readonly ConcurrentDictionary<string, object> _locks = new();

        public async Task ReadAsync(string deviceId, PipeWriter output, CancellationToken token)
        {
            while (true)
            {
                if (TryGetChannel(deviceId, out var channel))
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

        public ValueTask<FlushResult> WriteAsync(string deviceId, ReadOnlyMemory<byte> segment, CancellationToken token)
        {
            return GetOrCreateChannel(deviceId).Writer.WriteAsync(segment, token);
        }

        private bool TryGetChannel(string name, out Pipe channel)
        {
            return _channels.TryGetValue(name, out channel);
        }

        private Pipe GetOrCreateChannel(string name)
        {
            if (!_channels.TryGetValue(name, out var channel))
            {
                lock (_locks.GetOrAdd(name, s => new object()))
                {
                    if (!_channels.TryGetValue(name, out channel))
                    {
                        channel = new Pipe();
                        _channels.TryAdd(name, channel);
                    }
                }
            }

            return channel;
        }
    }
}
