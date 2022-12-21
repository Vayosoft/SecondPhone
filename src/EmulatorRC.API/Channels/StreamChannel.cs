using System.Collections.Concurrent;
using System.IO.Pipelines;

namespace EmulatorRC.API.Channels
{
    public sealed class StreamChannel
    {
        private readonly ConcurrentDictionary<string, Pipe> _channels = new();
        private readonly ConcurrentDictionary<string, object> _locks = new();

        public bool TryGetChannel(string name, out Pipe channel)
        {
            return _channels.TryGetValue(name, out channel);
        }

        public Pipe GetOrCreateChannel(string name)
        {
            if (!_channels.TryGetValue(name, out var channel))
            {
                lock (_locks.GetOrAdd(name, s => new object()))
                {
                    if (!_channels.TryGetValue(name, out channel))
                    {
                        channel = new Pipe();
                        _channels.TryAdd(name, channel);
                    }
                }
            }

            return channel;
        }
    }
}
