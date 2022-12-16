using System.Collections.Concurrent;
using System.Threading.Channels;
using EmulatorRC.API.Model.Bridge.TCP.Servers;

namespace EmulatorRC.API.Channels
{
    public sealed class TcpStreamChannel : IDisposable
    {
        private readonly ConcurrentDictionary<string, Channel<TcpData>> _channels = new();
        private readonly ConcurrentDictionary<string, object> _locks = new();
        private readonly object _deletingLock = new();

        private readonly BoundedChannelOptions _options = new(1000)
        {
            SingleWriter = true,
            SingleReader = true,
            AllowSynchronousContinuations = true,
            FullMode = BoundedChannelFullMode.DropOldest
        };

        public async ValueTask WriteAsync(string streamId, TcpData data, CancellationToken cancellationToken = default)
        {
            await this[streamId].Writer.WriteAsync(data, cancellationToken);
        }

        public bool Write(string streamId, TcpData data)
        {
            return this[streamId].Writer.TryWrite(data);
        }

        public IAsyncEnumerable<TcpData> ReadAllAsync(string streamId, CancellationToken cancellationToken = default)
        {
            return this[streamId].Reader.ReadAllAsync(cancellationToken);
        }

        private Channel<TcpData> this[string streamId]
        {
            get
            {
                if (_channels.TryGetValue(streamId, out var channel)) return channel;
                lock (_locks.GetOrAdd(streamId, _ => new object()))
                {
                    if (!_channels.TryGetValue(streamId, out channel))
                    {
                        channel = Channel.CreateBounded<TcpData>(_options);
                        _channels.TryAdd(streamId, channel);
                    }
                }

                return channel;
            }
        }


        public bool RegisterChannel(string streamId)
        {
            return this[streamId] != null;
        }

        public bool TryGetChannel(string streamId, out Channel<TcpData> channel)
        {
            return _channels.TryGetValue(streamId, out channel);
        }

        public bool IsChannelExists(string streamId)
        {
            return _channels.ContainsKey(streamId);
        }

        public void DisposeChannel(string streamId)
        {
            lock (_deletingLock)
            {
                if (_channels.TryRemove(streamId, out var channel))
                    channel.Writer.Complete();
            }
        }

        public void Dispose()
        {
            foreach (var channel in _channels)
                channel.Value.Writer.Complete();

            _channels.Clear();
        }
    }
}
