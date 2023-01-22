using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.RegularExpressions;
using EmulatorRC.API.Model.Commands;
using EmulatorRC.API.Services.Handlers;
using Microsoft.AspNetCore.Connections;

namespace EmulatorRC.API.Services
{
    public partial class DeviceController : ConnectionHandler
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<DeviceController> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public DeviceController(
            IServiceProvider services,
            ILogger<DeviceController> logger,
            IHostApplicationLifetime lifetime)
        {
            _services = services;
            _logger = logger;
            _lifetime = lifetime;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            _logger.LogInformation("TCP (Device) {ConnectionId} connected", connection.ConnectionId);

            var cts = CancellationTokenSource.CreateLinkedTokenSource(
                connection.ConnectionClosed, _lifetime.ApplicationStopping);
            var cancellationToken = cts.Token;

            try
            {
                var command = await GetCommandRequestAsync(connection.Transport, cancellationToken);

                //todo authentication
                if (string.IsNullOrEmpty(command.DeviceId))
                {
                    throw new ApplicationException("Authentication failed");
                }

                switch (command)
                {
                    case VideoCommand videoCommand:
                        _logger.LogInformation("TCP (Device) {ConnectionId} => Camera [Read]", connection.ConnectionId);

                        var cameraHandler = _services.GetRequiredService<CameraCommandHandler>();
                        await cameraHandler.ReadAsync(videoCommand, connection.Transport, cancellationToken);
                        break;
                    case AudioCommand audioCommand:
                        _logger.LogInformation("TCP (Device) {ConnectionId} => Mic [Read]", connection.ConnectionId);

                        var micHandler = _services.GetRequiredService<MicrophoneCommandHandler>();
                        await micHandler.ReadAsync(audioCommand, connection.Transport, cancellationToken);
                        break;
                    case SpeakerCommand speakerCommand:
                        _logger.LogInformation("TCP (Device) {ConnectionId} => Speaker [Write]", connection.ConnectionId);

                        var speakerHandler = _services.GetRequiredService<SpeakerCommandHandler>();
                        await speakerHandler.WriteAsync(speakerCommand, connection.Transport, cancellationToken);
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

        public async Task<CommandRequest> GetCommandRequestAsync(IDuplexPipe pipe, CancellationToken cancellationToken)
        {
            CommandRequest command;
            while (true)
            {
                var result = await pipe.Input.ReadAsync(cancellationToken);
                var buffer = result.Buffer;

                var consumed = ProcessHandshake(buffer, out command);

                pipe.Input.AdvanceTo(consumed);

                break;
            }

            return command;
        }

        private static ReadOnlySpan<byte> CommandSpeaker => "CMD /v1/sound?"u8;

        //DroidCam
        private static ReadOnlySpan<byte> CommandVideo => "CMD /v2/video.4?"u8; //cam
        private static ReadOnlySpan<byte> CommandAudio => "CMD /v2/audio"u8; //mic

        [GeneratedRegex("id=(\\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex HandshakeRegex();

        [GeneratedRegex("(\\d+)x(\\d+)&id=(\\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex VideoHandshakeRegex();

        private static SequencePosition ProcessHandshake(ReadOnlySequence<byte> buffer, out CommandRequest command)
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

                command = new VideoCommand(m.Groups[3].Value)
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

                command = new SpeakerCommand(m.Groups[1].Value);
            }
            else if (reader.IsNext(CommandAudio, true))
            {
                //var str = Encoding.UTF8.GetString(reader.UnreadSequence);
                //var m = HandshakeRegex().Match(str);

                //if (!m.Success || m.Groups.Count < 2)
                //    throw new ApplicationException($"Handshake failed (Mic) => {str}");

                //command = new AudioHandshake(m.Groups[1].Value);
                command = new AudioCommand("default");
            }
            else
            {
                var str = Encoding.UTF8.GetString(reader.UnreadSequence);
                throw new ApplicationException($"Handshake failed => {str}");
            }

            return reader.Position;
        }
    }
}
