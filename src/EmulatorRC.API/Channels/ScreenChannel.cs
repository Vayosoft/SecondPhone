using System.Collections.Concurrent;
using System.Threading.Channels;
using EmulatorRC.API.Protos;

namespace EmulatorRC.API.Channels
{
    public sealed class ScreenChannel : IDisposable
    {
        private readonly ConcurrentDictionary<string, Channel<DeviceScreen>> _channels = new();
        private readonly ConcurrentDictionary<string, object> _locks = new();
        private readonly ConcurrentDictionary<string, DeviceScreen> _lastScreens = new();

        private readonly BoundedChannelOptions _options = new(1)
        {
            SingleWriter = true,
            SingleReader = true,
            AllowSynchronousContinuations = true,
            FullMode = BoundedChannelFullMode.DropOldest
        };

        public async ValueTask WriteAsync(string deviceId, DeviceScreen request, CancellationToken cancellationToken = default)
        {
            request.Id = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond).ToString();

            if (_channels.TryGetValue(deviceId, out var channel))
            {
                await channel.Writer.WriteAsync(request, cancellationToken);
            }

            _lastScreens[deviceId] = request;
        }

        public ValueTask<DeviceScreen> ReadAsync(string deviceId, string imageId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(imageId))
            {
                return ValueTask.FromResult(_lastScreens.TryGetValue(deviceId, out var screen) ? screen : null);
            }

            if (!_channels.TryGetValue(deviceId, out var channel))
            {
                lock (_locks.GetOrAdd(deviceId, s => new object()))
                {
                    if (!_channels.TryGetValue(deviceId, out channel))
                    {
                        channel = Channel.CreateBounded<DeviceScreen>(_options);
                        _channels.TryAdd(deviceId, channel);
                    }
                }
            }

            return channel.Reader.ReadAsync(cancellationToken);
        }

        public void Dispose()
        {
            foreach (var channel in _channels)
                channel.Value.Writer.Complete();

            _channels.Clear();
        }
    }
}
