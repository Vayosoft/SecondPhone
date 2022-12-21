using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;
using EmulatorRC.API.Channels;
using EmulatorRC.Entities;

namespace EmulatorRC.API.Services.Sessions
{
    public sealed class StreamSession
    {
        private readonly StreamChannel _channel;

        public StreamSession(StreamChannel channel)
        {
            _channel = channel;
        }

        public async Task ReadAsync(string deviceId, PipeWriter output, CancellationToken token)
        {
            while (true)
            {
                if (_channel.TryGetChannel(deviceId, out var channel))
                {
                    var result = await channel.Reader.ReadAsync(token);
                    var buffer = result.Buffer;

                    foreach (var segment in buffer)
                    {
                        await output.WriteAsync(segment, token);
                    }

                    if (result.IsCompleted)
                    {
                        break;
                    }

                    channel.Reader.AdvanceTo(buffer.End);
                }
                else
                {
                    await Task.Delay(1000, token);
                }
            }
        }

        public async Task WriteAsync(string deviceId, PipeReader input, CancellationToken token)
        {
            DeviceSession session = null;

            while (true)
            {
                var result = await input.ReadAsync(token);
                var buffer = result.Buffer;

                session ??= Handshake(ref buffer);

                foreach (var segment in buffer)
                {
                    await _channel.GetOrCreateChannel(deviceId).Writer.WriteAsync(segment, token);
                }

                if (result.IsCompleted)
                {
                    break;
                }

                input.AdvanceTo(buffer.End);
            }

            await input.CompleteAsync();
        }

        private static DeviceSession Handshake(ref ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);

            if (reader.TryReadExact(4, out var header))
            {
                var length = BitConverter.ToInt32(header.FirstSpan);
                if (reader.TryReadExact(length, out var handshake))
                {
                    var deviceSession = JsonSerializer.Deserialize<DeviceSession>(handshake.FirstSpan);
                    buffer = buffer.Slice(4 + length);

                    return deviceSession;
                }
            }

            throw new OperationCanceledException("Not authenticated");
        }
    }
}
