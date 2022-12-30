using System.Collections.Concurrent;
using System.IO.Pipelines;

namespace EmulatorRC.API.Channels
{
    public sealed class StreamChannel
    {
        private readonly ConcurrentDictionary<string, Pipe> _channels = new();
 
        public bool TryGetChannel(string name, out Pipe channel)
        {
            return _channels.TryGetValue(name, out channel);
        }

        public Pipe GetOrCreateChannel(string name)
        {
            return _channels.GetOrAdd(name, _ => new Pipe());
        }

        public bool RemoveChannel(string name, out Pipe channel)
        {
            return _channels.TryRemove(name, out channel);
        }
    }
}
