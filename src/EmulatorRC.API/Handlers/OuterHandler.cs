using EmulatorRC.API.Channels;
using EmulatorRC.Entities;
using Microsoft.AspNetCore.Connections;
using System.Buffers;
using System.IO.Pipelines;
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

                    switch (status)
                    {
                        case HandshakeStatus.Successful:
                            {
                                foreach (var segment in buffer)
                                {
                                    await channel!.Writer.WriteAsync(segment, token);
                                }

                                break;
                            }
                        case HandshakeStatus.Pending:
                            status = Handshake(ref buffer, out var session);
                            channel = status switch
                            {
                                HandshakeStatus.Successful => _channel.GetOrCreateChannel(session.DeviceId),
                                HandshakeStatus.Failed => throw new Exception("Authentication required"),

                                _ => channel
                            };

                            break;
                    }

                    if (result.IsCompleted)
                    {
                        break;
                    }

                    connection.Transport.Input.AdvanceTo(status != HandshakeStatus.Pending ? buffer.End : buffer.Start);
                }

                await connection.Transport.Input.CompleteAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{connectionId} => {error}", connection.ConnectionId, e.Message);
            }

            _logger.LogInformation("{connectionId} disconnected", connection.ConnectionId);
        }

        private HandshakeStatus Handshake(ref ReadOnlySequence<byte> buffer, out DeviceSession session)
        {
            try
            {
                if (buffer.IsSingleSegment)
                {
                    var span = buffer.FirstSpan;
                    var length = BitConverter.ToInt32(span[..4]);

                    if (span.Length < length + 4)
                    {
                        session = null;
                        return HandshakeStatus.Pending;
                    }

                    session = JsonSerializer.Deserialize<DeviceSession>(span.Slice(4, length));
                    buffer = buffer.Slice(4 + length);
                }
                else
                {
                    var reader = new SequenceReader<byte>(buffer);

                    if (!reader.TryReadExact(4, out var header))
                    {
                        session = null;
                        return HandshakeStatus.Pending;
                    }

                    var length = BitConverter.ToInt32(header.FirstSpan);
                    if (!reader.TryReadExact(length, out var handshake))
                    {
                        session = null;
                        return HandshakeStatus.Pending;
                    }

                    session = JsonSerializer.Deserialize<DeviceSession>(handshake.FirstSpan);
                    buffer = buffer.Slice(4 + length);
                }
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
