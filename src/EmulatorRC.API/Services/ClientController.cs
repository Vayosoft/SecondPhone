using EmulatorRC.API.Model.Commands;
using EmulatorRC.API.Services.Handlers;
using EmulatorRC.Entities;
using EmulatorRC.Services;
using Grpc.Core;
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
        private readonly IEmulatorDataRepository _deviceRepository;

        private const int MaxStackLength = 128;
        private const int MaxHeaderLength = 1024;

        public ClientController(
            IServiceProvider services,
            ILogger<ClientController> logger,
            IHostApplicationLifetime lifetime, IEmulatorDataRepository deviceRepository)
        {
            _services = services;
            _logger = logger;
            _lifetime = lifetime;
            _deviceRepository = deviceRepository;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogInformation("TCP Connection {ConnectionId} Client connected", connection.ConnectionId);
            }

            var cts = CancellationTokenSource.CreateLinkedTokenSource(
                connection.ConnectionClosed, _lifetime.ApplicationStopping);
            var cancellationToken = cts.Token;

            try
            {
                var command = await GetCommandRequestAsync(connection.Transport, cancellationToken);

                if (string.IsNullOrEmpty(command.DeviceId))
                {
                    throw new ApplicationException("Bad request. ClientId is null");
                }

                var device = await _deviceRepository.GetByClientIdAsync(command.DeviceId);
                if (device == null)
                {
                    throw new ApplicationException("Authentication failed." +
                                                   $" There are no device found for client {command.DeviceId}");
                }

                command = command with { DeviceId = device.Id };

                switch (command)
                {
                    case CameraFrontCommand frontCamCommand:
                        _logger.LogInformation("TCP Connection {ConnectionId}. Client {DeviceId} Front camera (Write)", connection.ConnectionId, command.DeviceId);

                        var frontCameraHandler = _services.GetRequiredService<CameraFrontCommandHandler>();
                        await frontCameraHandler.WriteAsync(frontCamCommand, connection.Transport, cancellationToken);
                        break;
                    case CameraRearCommand rearCamCommand:
                        _logger.LogInformation("TCP Connection {ConnectionId}. Client {DeviceId} Rear camera (Write)", connection.ConnectionId, command.DeviceId);

                        var rearCameraHandler = _services.GetRequiredService<CameraRearCommandHandler>();
                        await rearCameraHandler.WriteAsync(rearCamCommand, connection.Transport, cancellationToken);
                        break;
                    case AudioCommand audioCommand:
                        _logger.LogInformation("TCP Connection {ConnectionId}. Client {DeviceId} Mic (Write)", connection.ConnectionId, command.DeviceId);

                        var micHandler = _services.GetRequiredService<MicrophoneCommandHandler>();
                        await micHandler.WriteAsync(audioCommand, connection.Transport, cancellationToken);
                        break;
                }
            }
            catch (ConnectionResetException) { }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                _logger.LogError(e, "TCP Connection {ConnectionId} Client error occurred: {Error}", connection.ConnectionId, e.Message);
            }
            finally
            {
                await connection.Transport.Input.CompleteAsync();
                await connection.Transport.Output.CompleteAsync();

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogInformation("TCP Connection {ConnectionId} Client disconnected",
                        connection.ConnectionId);
                }
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
                    throw new ApplicationException("Handshake failed." +
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
                    "cam" or "cam.front" => new CameraFrontCommand { DeviceId = session.DeviceId },
                    "cam.rear" => new CameraRearCommand { DeviceId = session.DeviceId },
                    "mic" => new AudioCommand(session.DeviceId),
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
