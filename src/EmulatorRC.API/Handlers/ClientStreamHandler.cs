using EmulatorRC.API.Channels;
using EmulatorRC.Entities;
using Microsoft.AspNetCore.Connections;
using System.Buffers;
using System.Text;
using System.Text.Json;
using EmulatorRC.API.Handlers.StreamReaders;

namespace EmulatorRC.API.Handlers
{
    public sealed class ClientStreamHandler : ConnectionHandler
    {
        private readonly StreamChannel _channel;
        private readonly ILogger<ClientStreamHandler> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        private const int MaxStackLength = 128;
        private const int MaxHeaderLength = 1024;

        public ClientStreamHandler(
            StreamChannel channel,
            ILogger<ClientStreamHandler> logger,
            IHostApplicationLifetime lifetime)
        {
            _channel = channel;
            _logger = logger;
            _lifetime = lifetime;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            _logger.LogInformation("TCP (Client) {ConnectionId} connected", connection.ConnectionId);

            try
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(
                    connection.ConnectionClosed, _lifetime.ApplicationStopping);
                var cancellationToken = cts.Token;

                Handshake handshake = null;
                HandshakeStatus status = default;
                while (status != HandshakeStatus.Successful)
                {
                    var result = await connection.Transport.Input.ReadAsync(cancellationToken);
                    var buffer = result.Buffer;

                    var consumed = ProcessHandshake(ref buffer, out status, out handshake);
                    switch (status)
                    {
                        case HandshakeStatus.Successful:
                        {
                            //todo authentication
                            if (string.IsNullOrEmpty(handshake.DeviceId))
                            {
                                _logger.LogError("TCP (Client) {ConnectionId} => Authentication failed", connection.ConnectionId);
                                return;
                            }
                            break;
                        }
                        case HandshakeStatus.Failed:

                            _logger.LogError("TCP (Client) {ConnectionId} => Handshake failed. {EndPoint}. Length: {BufferLength}. Hex: {Hex}. UTF8: {UTF8}",
                                connection.ConnectionId, connection.RemoteEndPoint, buffer.Length,
                                Convert.ToHexString(buffer.ToArray()), Encoding.UTF8.GetString(buffer));
                            return;
                    }

                    if (result.IsCompleted)
                    {
                        break;
                    }

                    connection.Transport.Input.AdvanceTo(consumed);

                    if(status == HandshakeStatus.Successful) break;
                }

                switch (handshake)
                {
                    case VideoHandshake videoHandshake:
                        _logger.LogInformation("TCP (Client) {ConnectionId} => Camera [Write]", connection.ConnectionId);
                        await _channel.WriterCameraAsync(videoHandshake.DeviceId, connection, cancellationToken);
                        break;
                    case AudioHandshake audioHandshake:
                        _logger.LogInformation("TCP (Client) {ConnectionId} => Mic [Write]", connection.ConnectionId);
                        await _channel.WriterMicAsync(audioHandshake.DeviceId, connection, cancellationToken);
                        break;
                    case SpeakerHandshake speakerHandshake:
                        _logger.LogInformation("TCP (Client) {ConnectionId} => Speaker [Read]", connection.ConnectionId);
                        var speakerStreamHandler = new SpeakerStreamReader(_channel);
                        await speakerStreamHandler.ReadAsync(connection, speakerHandshake, cancellationToken);
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

        private SequencePosition ProcessHandshake(ref ReadOnlySequence<byte> buffer, out HandshakeStatus status, out Handshake command)
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
                    "cam" => new VideoHandshake(session.DeviceId),
                    "mic" => new AudioHandshake(session.DeviceId),
                    "sound" => new SpeakerHandshake(session.DeviceId),
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
