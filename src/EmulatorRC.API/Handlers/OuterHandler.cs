using EmulatorRC.API.Channels;
using EmulatorRC.Entities;
using Microsoft.AspNetCore.Connections;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace EmulatorRC.API.Handlers
{
    public sealed class OuterHandler : ConnectionHandler
    {
        private readonly StreamChannel _channel;
        private readonly ILogger<OuterHandler> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public OuterHandler(
            StreamChannel channel,
            ILogger<OuterHandler> logger,
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

                HandshakeStatus status = default;
                Pipe channel = default;

                while (true)
                {
                    var result = await connection.Transport.Input.ReadAsync(token);
                    var buffer = result.Buffer;
                    var consumed = buffer.Start;

                    if (status == HandshakeStatus.Pending)
                    {
                        status = Handshake(ref buffer, out var session);
                        channel = status switch
                        {
                            HandshakeStatus.Successful => _channel.GetOrCreateChannel(session.DeviceId),
                            HandshakeStatus.Failed => throw new Exception("Authentication required"),

                            _ => channel
                        };
                    }

                    if (status == HandshakeStatus.Successful)
                    {
                        foreach (var segment in buffer)
                        {
                            await channel!.Writer.WriteAsync(segment, token);
                        }

                        consumed = buffer.End;
                    }
               
                    if (result.IsCompleted)
                    {
                        break;
                    }

                    connection.Transport.Input.AdvanceTo(consumed);
                }

                await connection.Transport.Input.CompleteAsync();
            }
            catch (ConnectionResetException) { }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                _logger.LogError(e, "{connectionId} => {error}", connection.ConnectionId, e.Message);
            }

            _logger.LogInformation("{connectionId} disconnected", connection.ConnectionId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private HandshakeStatus Handshake(ref ReadOnlySequence<byte> buffer, out DeviceSession session)
        {
            var reader = new SequenceReader<byte>(buffer);
            try
            {
                if (!reader.TryReadLittleEndian(out int length) || !reader.TryReadExact(length, out var header))
                {
                    session = null;
                    return HandshakeStatus.Pending;
                }

                Span<byte> payload = stackalloc byte[length];
                header.CopyTo(payload);

                session = JsonSerializer.Deserialize<DeviceSession>(payload);
                buffer = buffer.Slice(reader.Position);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Handshake => {error}\r\n", e.Message);

                session = null;
                return HandshakeStatus.Failed;
            }

            return HandshakeStatus.Successful;
        }

        public enum HandshakeStatus : byte
        {
            Pending,
            Successful,
            Failed
        }
    }
}
