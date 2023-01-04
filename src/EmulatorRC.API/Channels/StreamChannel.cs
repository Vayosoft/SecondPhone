using System.Collections.Concurrent;
using System.IO.Pipelines;

namespace EmulatorRC.API.Channels
{
    public sealed class StreamChannel
    {
        private readonly ConcurrentDictionary<string, Pipe> _channels = new();
        private readonly ConcurrentDictionary<string, object> _locks = new();

        public bool TryGetChannelReader(string name, out PipeReader reader)
        {
            reader = _channels.TryGetValue(name, out var channel) ? channel.Reader : default;
            return reader != default;
        }

        public PipeWriter GetOrCreateChannelWriter(string name)
        {
            if (!_channels.TryGetValue(name, out var channel))
            {
                lock (_locks.GetOrAdd(name, _ => new object()))
                {
                    if (!_channels.TryGetValue(name, out channel))
                    {
                        channel = new Pipe();
                        _channels.TryAdd(name, channel);
                    }
                }
            }

            return channel.Writer;
        }

        public async Task CloseWriterAsync(string name)
        {
            if (_channels.TryRemove(name, out var channel))
            {
                await channel.Writer.CompleteAsync();
            }
        }
    }
}
