using EmulatorRC.API.Channels;
using Microsoft.AspNetCore.Connections;

namespace EmulatorRC.API.Handlers.StreamReaders
{
    public sealed class MicStreamReader
    {
        private readonly StreamChannel _channel;

        public MicStreamReader(StreamChannel channel)
        {
            _channel = channel;
        }

        public async Task ReadAsync(ConnectionContext connection, AudioHandshake handshake, CancellationToken token)
        {
            var buf = new byte[] { (byte)'-', (byte)'@', (byte)'v', (byte)'0', (byte)'2', 2 };
            _ = await connection.Transport.Output.WriteAsync(buf, token);
            _ = _channel.ReadAllMicAsync(handshake.DeviceId, connection.Transport.Output, token);

            while (!token.IsCancellationRequested)
            {
                var result = await connection.Transport.Input.ReadAsync(token);
                var buffer = result.Buffer;

                var consumed = buffer.End;

                if (result.IsCompleted)
                {
                    break;
                }

                connection.Transport.Input.AdvanceTo(consumed);
            }
        }
    }
}
