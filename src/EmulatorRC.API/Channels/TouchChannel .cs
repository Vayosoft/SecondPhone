using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Xml.Linq;
using EmulatorRC.API.Protos;

namespace EmulatorRC.API.Channels
{
    public sealed class TouchChannel : IDisposable
    {
        private readonly ConcurrentDictionary<string, Channel<TouchEvents>> _channels = new();
        private readonly ConcurrentDictionary<string, object> _locks = new();

        private readonly BoundedChannelOptions _options = new(1)
        {
            SingleWriter = true,
            SingleReader = true,
            AllowSynchronousContinuations = true,
            FullMode = BoundedChannelFullMode.DropOldest
        };

        public async ValueTask WriteAsync(string deviceId, TouchEvents request, CancellationToken cancellationToken = default)
        {
            if (_channels.TryGetValue(deviceId, out var channel))
            {
                await channel.Writer.WriteAsync(request, cancellationToken);
            }
        }

        public IAsyncEnumerable<TouchEvents> ReadAllAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            if (!_channels.TryGetValue(deviceId, out var channel))
            {
                lock (_locks.GetOrAdd(deviceId, s => new object()))
                {
                    if (!_channels.TryGetValue(deviceId, out channel))
                    {
                        channel = Channel.CreateBounded<TouchEvents>(_options);
                        _channels.TryAdd(deviceId, channel);
                    }
                }
            }

            return channel.Reader.ReadAllAsync(cancellationToken);
        }

        public void Dispose()
        {
            foreach (var channel in _channels)
                channel.Value.Writer.Complete();

            _channels.Clear();
        }
    }
}
