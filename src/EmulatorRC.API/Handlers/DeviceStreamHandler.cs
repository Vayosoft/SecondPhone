using EmulatorRC.API.Channels;
using Microsoft.AspNetCore.Connections;
using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;

namespace EmulatorRC.API.Handlers
{
    public sealed partial class DeviceStreamHandler : ConnectionHandler
    {
        private readonly StreamChannel _channel;
        private readonly ILogger<DeviceStreamHandler> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public DeviceStreamHandler(
            StreamChannel channel,
            ILogger<DeviceStreamHandler> logger,
            IHostApplicationLifetime lifetime)
        {
            _channel = channel;
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

                Handshake handshake;
                while (true)
                {
                    var result = await connection.Transport.Input.ReadAsync(cancellationToken);
                    var buffer = result.Buffer;

                    var consumed = ProcessHandshake(buffer, out handshake);

                    //todo authentication
                    if (string.IsNullOrEmpty(handshake.DeviceId))
                    {
                        throw new ApplicationException("Authentication failed");
                    }

                    connection.Transport.Input.AdvanceTo(consumed);

                    break;
                }

                switch (handshake)
                {
                    case VideoHandshake videoHandshake:
                        _logger.LogInformation("TCP (Device) {ConnectionId} => Camera [Read]", connection.ConnectionId);
                        _ = await connection.Transport.Output.WriteAsync(CreateMockHeader(videoHandshake.Width, videoHandshake.Height), cancellationToken);
                        await _channel.ReadCameraAsync(videoHandshake.DeviceId, connection.Transport, cancellationToken);
                        break;
                    case AudioHandshake audioHandshake:
                        _logger.LogInformation("TCP (Device) {ConnectionId} => Mic [Read]", connection.ConnectionId);
                        _ = await connection.Transport.Output.WriteAsync(_header, cancellationToken);
                        await _channel.ReadMicAsync(audioHandshake.DeviceId, connection.Transport, cancellationToken);
                        break;
                    case SpeakerHandshake speakerHandshake:
                        _logger.LogInformation("TCP (Device) {ConnectionId} => Speaker [Write]", connection.ConnectionId);
                        await _channel.WriteSpeakerAsync(speakerHandshake.DeviceId, connection.Transport, cancellationToken);
                        break;
                }
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

        private static ReadOnlySpan<byte> CommandSpeaker => "CMD /v1/sound?"u8;

        //DroidCam
        private static ReadOnlySpan<byte> CommandVideo => "CMD /v2/video.4?"u8; //cam
        private static ReadOnlySpan<byte> CommandAudio => "CMD /v2/audio"u8; //mic


        [GeneratedRegex("id=(\\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex HandshakeRegex();

        [GeneratedRegex("(\\d+)x(\\d+)&id=(\\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex VideoHandshakeRegex();

        private static SequencePosition ProcessHandshake(ReadOnlySequence<byte> buffer, out Handshake command)
        {
            var reader = new SequenceReader<byte>(buffer);
            
            if (reader.IsNext(CommandVideo, true))
            {
                var str = Encoding.UTF8.GetString(reader.UnreadSequence);
                var m = VideoHandshakeRegex().Match(str);

                if (!m.Success || m.Groups.Count < 4)
                    throw new ApplicationException($"Handshake failed (Camera) => {str}");

                if (!int.TryParse(m.Groups[1].Value, out var width) || !int.TryParse(m.Groups[2].Value, out var height))
                    throw new ApplicationException($"Handshake failed (Camera) => {str}");

                command = new VideoHandshake(m.Groups[3].Value)
                {
                    Width = width,
                    Height = height,
                };
            }
            else if (reader.IsNext(CommandSpeaker, true))
            {
                var str = Encoding.UTF8.GetString(reader.UnreadSequence);
                var m = HandshakeRegex().Match(str);

                if (!m.Success || m.Groups.Count < 2)
                    throw new ApplicationException($"Handshake failed (Speaker) => {str}");

                command = new SpeakerHandshake(m.Groups[1].Value);
            }
            else if (reader.IsNext(CommandAudio, true))
            {
                //var str = Encoding.UTF8.GetString(reader.UnreadSequence);
                //var m = HandshakeRegex().Match(str);

                //if (!m.Success || m.Groups.Count < 2)
                //    throw new ApplicationException($"Handshake failed (Mic) => {str}");

                //command = new AudioHandshake(m.Groups[1].Value);
                command = new AudioHandshake("default");
            }
            else
            {
                var str = Encoding.UTF8.GetString(reader.UnreadSequence);
                throw new ApplicationException($"Handshake failed => { str }");
            }

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

        private readonly byte[] _header = { (byte)'-', (byte)'@', (byte)'v', (byte)'0', (byte)'2', 2 };
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
        public int Height { get; init; } }

}
