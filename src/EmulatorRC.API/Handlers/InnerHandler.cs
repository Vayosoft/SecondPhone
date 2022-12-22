using EmulatorRC.API.Channels;
using EmulatorRC.Entities;
using Microsoft.AspNetCore.Connections;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.RegularExpressions;

namespace EmulatorRC.API.Handlers
{
    public sealed class InnerHandler : ConnectionHandler
    {
        private readonly StreamChannel _channel;
        private readonly ILogger<InnerHandler> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public InnerHandler(
            StreamChannel channel,
            ILogger<InnerHandler> logger,
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

                DeviceSession session = null;

                while (!token.IsCancellationRequested)
                {
                    var result = await connection.Transport.Input.ReadAsync(token);
                    var buffer = result.Buffer;

                    if (session == null)
                    {
                        session = Handshake(ref buffer, connection.Transport.Output);
                        _ = ReadFromChannelAsync(session.DeviceId, connection.Transport.Output, token);
                    }

                    if (result.IsCompleted)
                    {
                        break;
                    }

                    connection.Transport.Input.AdvanceTo(buffer.End);
                }
            }
            catch (ConnectionResetException) { }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                _logger.LogError(e, "{connectionId} => {error}", connection.ConnectionId, e.Message);
            }

            _logger.LogInformation("{connectionId} disconnected", connection.ConnectionId);
        }

        public async Task ReadFromChannelAsync(string deviceId, PipeWriter output, CancellationToken token)
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

        private const RegexOptions RegexOptions =
            System.Text.RegularExpressions.RegexOptions.Compiled |
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.Singleline;
        private static DeviceSession Handshake(ref ReadOnlySequence<byte> buffer, PipeWriter output)
        {
            var payload = Encoding.UTF8.GetString(buffer.First.Span);
            if (payload.StartsWith("CMD /v2/video.4?"))
            {
                var s = payload.Split("?")[1];

                var m = Regex.Match(s, "(\\d+)x(\\d+)&id=(\\w+)", RegexOptions);
                if (!m.Success || m.Groups.Count < 4)
                    throw new Exception("Authentication required");

                if (!int.TryParse(m.Groups[1].Value, out var w) || !int.TryParse(m.Groups[2].Value, out var h))
                    throw new Exception("Authentication required");

                var deviceId = m.Groups[3].Value;

                if (w == 0 || h == 0 || string.IsNullOrEmpty(deviceId))
                    throw new Exception("Authentication required");

                buffer = buffer.Slice(buffer.End);

                output.WriteAsync(CreateMockHeader(w, h));

                return new DeviceSession { DeviceId = deviceId, StreamType = "cam" };
            }

            throw new Exception("Authentication required");
        }

        private static byte[] CreateMockHeader(int width, int height)
        {
            const int some1 = 0x21; //25,
            const int some2 = 0x307fe8f5;

            var byteBuffer = new List<byte>(9)
            {
                //0x02, 0x80 - 640
                (byte) (width >> 8 & 255),
                (byte) (width & 255),
                //0x01, 0xe0 - 480
                (byte)(height >> 8 & 255),
                (byte)(height & 255),
                //0x21 - 33
                some1 & 255,
                //0xf5, 0xe8, 0x7f, 0x30
                some2 & 255,
                some2 >> 8 & 255,
                some2 >> 16 & 255,
                some2 >> 24 & 255
            };

            return byteBuffer.ToArray();
        }
    }
}
