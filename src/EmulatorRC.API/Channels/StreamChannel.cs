using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using ErrorOr;

namespace EmulatorRC.API.Channels
{
    public sealed class StreamChannel
    {
        private readonly ConcurrentDictionary<string, Pipe> _channels = new();
        private readonly ConcurrentDictionary<string, object> _locks = new();

        public ErrorOr<PipeWriter> GetOrCreateWriter(string name)
        {
            if (_channels.TryGetValue(name, out var channel))
                return Error.Conflict("Channel is busy");

            lock (_locks.GetOrAdd(name, new object()))
            {
                if (_channels.TryGetValue(name, out channel))
                    return Error.Conflict("Channel is busy");

                channel = new Pipe();
                _channels[name] = channel;

                _locks.TryRemove(name, out _);
            }

            return channel.Writer;
        }

        public async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadAllAsync(string name, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (!_channels.TryGetValue(name, out var channel)) yield break;

            var reader = channel.Reader;
            try
            {
                while (true)
                {
                    var result = await reader.ReadAsync(cancellationToken);
                    var buffer = result.Buffer;

                    foreach (var segment in buffer)
                    {
                        yield return segment;
                    }

                    if (result.IsCompleted)
                    {
                        break;
                    }

                    reader.AdvanceTo(buffer.End);
                }
            }
            finally
            {
                await reader.CompleteAsync();
            }
        }

        public async Task RemoveWriterAsync(string name)
        {
            if (_channels.TryRemove(name, out var channel))
            {
                await channel.Writer.CompleteAsync();
            }
        }
    }
}
