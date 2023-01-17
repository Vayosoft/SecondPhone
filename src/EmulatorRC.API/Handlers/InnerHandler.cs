using EmulatorRC.API.Channels;
using Microsoft.AspNetCore.Connections;
using System.Buffers;
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
            _logger.LogInformation("{ConnectionId} connected", connection.ConnectionId);

            Handshake handshake = null;
            try
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(
                    connection.ConnectionClosed, _lifetime.ApplicationStopping);
                var cancellationToken = cts.Token;

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
                        var cameraStreamHandler = new CameraStreamHandler(_channel, _logger);
                        await cameraStreamHandler.HandleInnerAsync(connection, videoHandshake, cancellationToken);
                        break;
                    case SpeakerHandshake speakerHandshake:
                        var speakerStreamHandler = new SpeakerStreamHandler(_channel, _logger);
                        await speakerStreamHandler.HandleInnerAsync(connection, speakerHandshake, cancellationToken);
                        break;
                }
            }
            catch (ConnectionResetException) { }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                _logger.LogError(e, "{ConnectionId} => {Error}", connection.ConnectionId, e.Message);
            }
            finally
            {
                if (handshake != default)
                {
                    switch (handshake)
                    {
                        case SpeakerHandshake videoHandshake:
                            await _channel.RemoveSpeakerWriterAsync(videoHandshake.DeviceId);
                            break;
                    }

                }

                await connection.Transport.Input.CompleteAsync();
                await connection.Transport.Output.CompleteAsync();
            }

            _logger.LogInformation("{ConnectionId} disconnected", connection.ConnectionId);
        }


        private static ReadOnlySpan<byte> CommandSpeaker => "CMD /v1/sound?"u8;

        //DroidCam
        private static ReadOnlySpan<byte> CommandVideo => "CMD /v2/video.4?"u8; //cam
        private static ReadOnlySpan<byte> CommandAudio => "CMD /v2/audio?"u8; //mic


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
                    throw new ApplicationException("Handshake failed");

                if (!int.TryParse(m.Groups[1].Value, out var width) || !int.TryParse(m.Groups[2].Value, out var height))
                    throw new ApplicationException("Handshake failed");

                command = new VideoHandshake(width, height)
                {
                    DeviceId = m.Groups[3].Value
                };
            }
            else if (reader.IsNext(CommandSpeaker, true))
            {
                var str = Encoding.UTF8.GetString(reader.UnreadSequence);
                var m = HandshakeRegex().Match(str);

                if (!m.Success || m.Groups.Count < 2)
                    throw new ApplicationException("Handshake failed");

                command = new SpeakerHandshake
                {
                    DeviceId = m.Groups[3].Value
                };
            }
            else
            {
                throw new ApplicationException("Handshake failed");
            }

            return reader.Position;
        }
    }

    internal enum Commands : byte
    {
        Undefined,
        Ping,
        GetBattery
    }

    public record Handshake
    {
        public required string DeviceId { get; init; }
    }

    public record VideoHandshake(int Width, int Height) : Handshake;
    public record SpeakerHandshake : Handshake;
}
