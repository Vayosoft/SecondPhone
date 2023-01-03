using System.Collections.Concurrent;
using System.IO.Pipelines;

namespace EmulatorRC.API.Channels
{
    public sealed class StreamChannel
    {
        private readonly ILogger<StreamChannel> _logger;

        public StreamChannel(ILogger<StreamChannel> logger)
        {
            _logger = logger;
        }

        private readonly ConcurrentDictionary<string, Pipe> _channels = new();
        private readonly ConcurrentDictionary<string, object> _locks = new();

        public bool TryGetChannelReader(string name, out PipeReader reader)
        {
            reader = _channels.TryGetValue(name, out var channel) ? channel.Reader : null;
            return reader != null;
        }

        public PipeWriter GetOrCreateChannelWriter(string name)
        {
            if (_channels.TryGetValue(name, out var channel)) return channel.Writer;

            lock (_locks.GetOrAdd(name, _ => new object()))
            {
                if (!_channels.TryGetValue(name, out channel))
                {
                    channel = new Pipe();
                    _channels.TryAdd(name, channel);
                }
            }

            return channel.Writer;
        }

        public bool RemoveChannel(string name, out Pipe channel)
        {
            return _channels.TryRemove(name, out channel);
        }
    }
}
