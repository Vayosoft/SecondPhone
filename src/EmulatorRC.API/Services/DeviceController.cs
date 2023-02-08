using System.Buffers;
using System.IO.Pipelines;
using System.Reflection.PortableExecutable;
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
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("TCP Connection {ConnectionId} Device connected", connection.ConnectionId);
            }

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
                    case CameraFrontCommand frontCamCommand:
                        _logger.LogInformation("TCP Connection {ConnectionId}. Device {DeviceId} Front camera (Read)", connection.ConnectionId, command.DeviceId);

                        var frontCameraHandler = _services.GetRequiredService<CameraFrontCommandHandler>();
                        await frontCameraHandler.ReadAsync(frontCamCommand, connection.Transport, cancellationToken);
                        break;
                    case CameraRearCommand rearCamCommand:
                        _logger.LogInformation("TCP Connection {ConnectionId}. Device {DeviceId} Rear camera (Read)", connection.ConnectionId, command.DeviceId);

                        var rearCameraHandler = _services.GetRequiredService<CameraRearCommandHandler>();
                        await rearCameraHandler.ReadAsync(rearCamCommand, connection.Transport, cancellationToken);
                        break;
                    case AudioCommand audioCommand:
                        _logger.LogInformation("TCP Connection {ConnectionId}. Device {DeviceId} Mic (Read)", connection.ConnectionId, command.DeviceId);

                        var micHandler = _services.GetRequiredService<MicrophoneCommandHandler>();
                        await micHandler.ReadAsync(audioCommand, connection.Transport, cancellationToken);
                        break;
                }
            }
            catch (ConnectionResetException) { }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                _logger.LogError(e, "TCP Connection {ConnectionId} Device error occurred: {Error}", connection.ConnectionId, e.Message);
            }
            finally
            {
                await connection.Transport.Input.CompleteAsync();
                await connection.Transport.Output.CompleteAsync();

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("TCP Connection {ConnectionId} Device disconnected", connection.ConnectionId);
                }

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

        //DroidCam
        private static ReadOnlySpan<byte> CommandVideoFront => "CMD /v2/video.4?"u8; // front cam
        private static ReadOnlySpan<byte> CommandVideoRear => "CMD /v2/video.1?"u8; // rear cam
        private static ReadOnlySpan<byte> CommandAudio => "CMD /v2/audio"u8; //mic

        [GeneratedRegex("id=(\\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex HandshakeRegex();

        [GeneratedRegex("(\\d+)x(\\d+)&id=(\\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex VideoHandshakeRegex();

        private static SequencePosition ProcessHandshake(ReadOnlySequence<byte> buffer, out CommandRequest command)
        {
            var reader = new SequenceReader<byte>(buffer);

            if (reader.IsNext(CommandVideoFront, true))
            {
                command = GetCameraCommand<CameraFrontCommand>(Encoding.UTF8.GetString(reader.UnreadSequence));
            }
            else if (reader.IsNext(CommandVideoRear, true))
            {
                command = GetCameraCommand<CameraRearCommand>(Encoding.UTF8.GetString(reader.UnreadSequence));
            }
            else if (reader.IsNext(CommandAudio, true))
            {
                var str = Encoding.UTF8.GetString(reader.UnreadSequence);
                var m = HandshakeRegex().Match(str);

                if (!m.Success || m.Groups.Count < 2)
                    throw new ApplicationException($"Handshake failed (Mic) => {str}");

                command = new AudioCommand(m.Groups[1].Value);
            }
            else
            {
                var str = Encoding.UTF8.GetString(reader.UnreadSequence);
                throw new ApplicationException($"Handshake failed => {str}");
            }

            reader.AdvanceToEnd();

            return reader.Position;
        }

        private static T GetCameraCommand<T>(string str) where T : CameraCommand, new()
        {
            var m = VideoHandshakeRegex().Match(str);

            if (!m.Success || m.Groups.Count < 4)
                throw new ApplicationException($"Handshake failed ({typeof(T).Name}) => {str}");

            if (!int.TryParse(m.Groups[1].Value, out var width) || !int.TryParse(m.Groups[2].Value, out var height))
                throw new ApplicationException($"Handshake failed ({typeof(T).Name}) => {str}");

            return new T
            {
                Width = width,
                Height = height,
                DeviceId = m.Groups[3].Value
             };
        }

    }
}
