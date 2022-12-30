using System.Collections.Concurrent;
using System.Threading.Channels;

namespace EmulatorRC.API.Channels
{
    public abstract class ChannelBase<T> : IDisposable
    {
        private readonly ConcurrentDictionary<string, Lazy<Channel<T>>> _channels = new();
    
        private readonly BoundedChannelOptions _options = new(1)
        {
            SingleWriter = true,
            SingleReader = true,
            AllowSynchronousContinuations = true,
            FullMode = BoundedChannelFullMode.DropOldest
        };

        private bool _disposed;

        protected bool TryGetChannel(string name, out Channel<T> channel)
        {
            var result = _channels.TryGetValue(name, out var lazy);
            channel = result ? lazy.Value : null;
            return result;
        }

        protected Channel<T> GetOrCreateChannel(string name)
        {
            return _channels.GetOrAdd(name, _ => 
                new Lazy<Channel<T>>(() => 
                    Channel.CreateBounded<T>(_options))).Value;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                foreach (var channel in _channels.Values)
                    channel.Value.Writer.Complete();

                _channels.Clear();
            }
            _disposed = true;
        }
    }
}
