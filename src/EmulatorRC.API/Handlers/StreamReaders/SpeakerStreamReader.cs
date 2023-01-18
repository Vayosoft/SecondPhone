using EmulatorRC.API.Channels;
using Microsoft.AspNetCore.Connections;

namespace EmulatorRC.API.Handlers.StreamReaders
{
    public sealed class SpeakerStreamReader
    {
        private readonly StreamChannel _channel;

        public SpeakerStreamReader(StreamChannel channel)
        {
            _channel = channel;
        }

        public async Task ReadAsync(ConnectionContext connection, SpeakerHandshake handshake, CancellationToken token)
        {
            _ = _channel.ReadAllSpeakerAsync(handshake.DeviceId, connection.Transport.Output, token);

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
