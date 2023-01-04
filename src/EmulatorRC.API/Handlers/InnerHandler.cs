using EmulatorRC.API.Channels;
using EmulatorRC.Entities;
using Microsoft.AspNetCore.Connections;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.RegularExpressions;

namespace EmulatorRC.API.Handlers
{
    public sealed partial class InnerHandler : ConnectionHandler
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
                    SequencePosition consumed;

                    if (session == null)
                    {
                        consumed = ProcessHandshake(buffer, out session, out var w, out var h);
                        _ = await connection.Transport.Output.WriteAsync(CreateMockHeader(w, h), token);
                        _ = ReadFromChannelAsync(session.DeviceId, connection.Transport.Output, token);
                    }
                    else
                    {
                        consumed = ProcessCommand(buffer, out var cmd);
                        switch (cmd)
                        {
                            case Commands.GetBattery:
                            _ = await connection.Transport.Output.WriteAsync("\r\n\r\n100"u8.ToArray(), token);
                            break;
                        }
                    }

                    if (result.IsCompleted)
                    {
                        break;
                    }

                    connection.Transport.Input.AdvanceTo(consumed);
                }
            }
            catch (ConnectionResetException) { }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                _logger.LogError(e, "{connectionId} => {error}", connection.ConnectionId, e.Message);
            }
            finally
            {
                await connection.Transport.Input.CompleteAsync();
                await connection.Transport.Output.CompleteAsync();
            }

            _logger.LogInformation("{connectionId} disconnected", connection.ConnectionId);
        }

        public async Task ReadFromChannelAsync(string deviceId, PipeWriter output, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await foreach (var segment in _channel.ReadAllAsync(deviceId, token))
                    {
                        await output.WriteAsync(segment, token);
                    }

                    await Task.Delay(1000, token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                _logger.LogError(e, "ReadFromChannel => {error}\r\n", e.Message);
            }
        }

        private static ReadOnlySpan<byte> CommandPing => "CMD /v1/ping"u8;
        private static ReadOnlySpan<byte> CommandVideo => "CMD /v2/video.4?"u8;
        private static ReadOnlySpan<byte> GetBattery => "GET /battery"u8;

        private static SequencePosition ProcessCommand(ReadOnlySequence<byte> buffer, out Commands cmd)
        {
            var reader = new SequenceReader<byte>(buffer);

            if (reader.IsNext(CommandPing, true))
            {
                cmd = Commands.Ping;
            }
            else if (reader.IsNext(GetBattery, true))
            {
                cmd = Commands.GetBattery;
            }
            else
            {
                cmd = Commands.Undefined;
            }

            return reader.Position;
        }

        [GeneratedRegex("(\\d+)x(\\d+)&id=(\\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex HandshakeRegex();

        private static SequencePosition ProcessHandshake(ReadOnlySequence<byte> buffer, out DeviceSession session, out int width, out int height)
        {
            var reader = new SequenceReader<byte>(buffer);

            if (!reader.IsNext(CommandVideo, true)) throw new ApplicationException("Authentication failed");

            var str = Encoding.UTF8.GetString(reader.UnreadSequence);
            var m = HandshakeRegex().Match(str);

            if (!m.Success || m.Groups.Count < 4) throw new ApplicationException("Authorization failed");

            if (!int.TryParse(m.Groups[1].Value, out width) || !int.TryParse(m.Groups[2].Value, out height)) throw new ApplicationException("Authorization failed");
            
            session = new DeviceSession {DeviceId = m.Groups[3].Value, StreamType = "cam"};

            return reader.Position;
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

    internal enum Commands : byte
    {
        Undefined,
        Ping,
        GetBattery
    }
}
