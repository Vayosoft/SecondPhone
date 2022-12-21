using EmulatorRC.API.Channels;
using EmulatorRC.Entities;
using Microsoft.AspNetCore.Connections;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.RegularExpressions;

namespace EmulatorRC.API.Services
{
    public class InnerStreamHandler : ConnectionHandler
    {
        private readonly StreamChannel _channel;
        private readonly ILogger<InnerStreamHandler> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public InnerStreamHandler(
            StreamChannel channel,
            ILogger<InnerStreamHandler> logger,
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
                await OnReceiveAsync(connection.Transport.Input, token);
            }
            catch (Exception e)
            {
                _logger.LogError("{connectionId} => {error}", connection.ConnectionId, e.Message, e.StackTrace);
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

        private static async Task OnReceiveAsync(PipeReader input, CancellationToken token)
        {
            DeviceSession session = null;

            while (!token.IsCancellationRequested)
            {
                var result = await input.ReadAsync(token);
                var buffer = result.Buffer;

                session ??= Handshake(ref buffer);

                if (result.IsCompleted)
                {
                    break;
                }

                input.AdvanceTo(buffer.End);
            }
        }

        private const RegexOptions RegexOptions = System.Text.RegularExpressions.RegexOptions.Compiled |
                                                  System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                                                  System.Text.RegularExpressions.RegexOptions.Singleline;
        private static DeviceSession Handshake(ref ReadOnlySequence<byte> buffer)
        {
            var payload = Encoding.UTF8.GetString(buffer.First.Span);
            if (payload.StartsWith("CMD /v2/video.4?"))
            {
                var s = payload.Split("?")[1];

                var m = Regex.Match(s, "(\\d+)x(\\d+)&id=(\\w+)", RegexOptions);
                if (!m.Success || m.Groups.Count < 4)
                    throw new OperationCanceledException("Not authenticated");

                if (!int.TryParse(m.Groups[1].Value, out var w) || !int.TryParse(m.Groups[2].Value, out var h))
                    throw new OperationCanceledException("Not authenticated");

                var deviceId = m.Groups[3].Value;

                if (w == 0 || h == 0 || string.IsNullOrEmpty(deviceId))
                    throw new OperationCanceledException("Not authenticated");

                buffer = buffer.Slice(buffer.End);

                return new DeviceSession { DeviceId = deviceId, StreamType = "cam" };
            }

            throw new OperationCanceledException("Not authenticated");
        }
    }
}
