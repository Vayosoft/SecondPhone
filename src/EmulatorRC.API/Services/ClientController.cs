using EmulatorRC.API.Model.Commands;
using EmulatorRC.API.Services.Handlers;
using EmulatorRC.Entities;
using Microsoft.AspNetCore.Connections;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;

namespace EmulatorRC.API.Services
{
    public sealed class ClientController : ConnectionHandler
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ClientController> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        private const int MaxStackLength = 128;
        private const int MaxHeaderLength = 1024;

        public ClientController(
            IServiceProvider services,
            ILogger<ClientController> logger,
            IHostApplicationLifetime lifetime)
        {
            _services = services;
            _logger = logger;
            _lifetime = lifetime;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            _logger.LogInformation("TCP (Client) {ConnectionId} connected", connection.ConnectionId);

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
                        _logger.LogInformation("TCP (Client) {ConnectionId} => Camera [Write]", connection.ConnectionId);

                        var cameraHandler = _services.GetRequiredService<CameraCommandHandler>();
                        await cameraHandler.WriteAsync(videoCommand, connection.Transport, cancellationToken);
                        break;
                    case AudioCommand audioCommand:
                        _logger.LogInformation("TCP (Client) {ConnectionId} => Mic [Write]", connection.ConnectionId);

                        var micHandler = _services.GetRequiredService<MicrophoneCommandHandler>();
                        await micHandler.WriteAsync(audioCommand, connection.Transport, cancellationToken);
                        break;
                    case SpeakerCommand speakerCommand:
                        _logger.LogInformation("TCP (Client) {ConnectionId} => Speaker [Read]", connection.ConnectionId);

                        var speakerHandler = _services.GetRequiredService<SpeakerCommandHandler>();
                        await speakerHandler.ReadAsync(speakerCommand, connection.Transport, cancellationToken);
                        break;
                }
            }
            catch (ConnectionResetException) { }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                _logger.LogError(e, "TCP (Client) {ConnectionId} => {Error}", connection.ConnectionId, e.Message);
            }
            finally
            {
                await connection.Transport.Input.CompleteAsync();
                await connection.Transport.Output.CompleteAsync();

                _logger.LogInformation("TCP (Client) {ConnectionId} disconnected", connection.ConnectionId);
            }
        }

        public async Task<CommandRequest> GetCommandRequestAsync(IDuplexPipe pipe, CancellationToken cancellationToken)
        {
            CommandRequest command = null;
            HandshakeStatus status = default;
            while (status != HandshakeStatus.Successful)
            {
                var result = await pipe.Input.ReadAsync(cancellationToken);
                var buffer = result.Buffer;

                var consumed = ProcessHandshake(ref buffer, out status, out command);

                if (result.IsCompleted)
                {
                    break;
                }

                pipe.Input.AdvanceTo(consumed);

                if (status == HandshakeStatus.Failed)
                    throw new ApplicationException($"Handshake failed." +
                                                   $" Length: {buffer.Length}." +
                                                   $" Hex: {Convert.ToHexString(buffer.ToArray())}." +
                                                   $" UTF8: {Encoding.UTF8.GetString(buffer)}");

                if (status == HandshakeStatus.Successful) break;
            }

            return command;
        }

        private SequencePosition ProcessHandshake(ref ReadOnlySequence<byte> buffer, out HandshakeStatus status, out CommandRequest command)
        {
            var reader = new SequenceReader<byte>(buffer);
            try
            {
                if (!reader.TryReadLittleEndian(out int length) || length > MaxHeaderLength)
                {
                    command = null;
                    status = HandshakeStatus.Failed;
                    return reader.Position;
                }


                if (!reader.TryReadExact(length, out var header))
                {
                    //todo security issue
                    command = null;
                    status = HandshakeStatus.Pending;
                    return buffer.Start;
                }

                DeviceSession session;
                if (length < MaxStackLength)
                {
                    Span<byte> payload = stackalloc byte[length];
                    header.CopyTo(payload);
                    session = JsonSerializer.Deserialize(payload,
                        DeviceSessionJsonContext.Default.DeviceSession);
                }
                else
                {
                    var payload = ArrayPool<byte>.Shared.Rent(length);

                    try
                    {
                        header.CopyTo(payload);
                        session = JsonSerializer.Deserialize(payload.AsSpan()[..length],
                            DeviceSessionJsonContext.Default.DeviceSession);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(payload);
                    }
                }

                command = session.StreamType switch
                {
                    "cam" => new VideoCommand(session.DeviceId),
                    "mic" => new AudioCommand(session.DeviceId),
                    "sound" => new SpeakerCommand(session.DeviceId),
                    _ => throw new ArgumentOutOfRangeException(session.StreamType)
                };
                status = HandshakeStatus.Successful;
                return reader.Position;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Handshake failed. => {Error}\r\n", e.Message);

                command = null;
                status = HandshakeStatus.Failed;
                return reader.Position;
            }
        }

        public enum HandshakeStatus : byte
        {
            Pending,
            Successful,
            Failed
        }
    }
}
