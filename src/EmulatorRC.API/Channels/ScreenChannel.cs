using System.Collections.Concurrent;
using System.Threading.Channels;
using EmulatorRC.API.Protos;
using EmulatorRC.Services;
using Google.Protobuf;

namespace EmulatorRC.API.Channels
{
    public sealed class ScreenChannel : IDisposable
    {
        private readonly IEmulatorDataRepository _emulatorDataRepository;
        private readonly ConcurrentDictionary<string, Channel<DeviceScreen>> _channels = new();
        private readonly ConcurrentDictionary<string, object> _locks = new();

        private readonly BoundedChannelOptions _options = new(1000)
        {
            SingleWriter = true,
            SingleReader = true,
            AllowSynchronousContinuations = true,
            FullMode = BoundedChannelFullMode.DropOldest
        };

        public ScreenChannel(IEmulatorDataRepository emulatorDataRepository)
        {
            _emulatorDataRepository = emulatorDataRepository;
        }

        public async ValueTask WriteAsync(string deviceId, DeviceScreen request, CancellationToken cancellationToken = default)
        {
            request.Id = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond).ToString();

            if (_channels.TryGetValue(deviceId, out var channel))
            {
                await channel.Writer.WriteAsync(request, cancellationToken);
            }

            _emulatorDataRepository.SetLastScreen(deviceId, new Screen(request.Id, request.Image.ToByteArray()));
        }

        public async ValueTask<DeviceScreen> ReadAsync(string deviceId, string imageId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(imageId))
            {
                var screen = _emulatorDataRepository.GetLastScreen(deviceId);
                if (screen != null)
                {
                    return new DeviceScreen
                    {
                        Id = screen.Id,
                        Image = UnsafeByteOperations.UnsafeWrap(screen.Image)
                    };
                }
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

            return await channel.Reader.ReadAsync(cancellationToken);
        }

        public void Dispose()
        {
            foreach (var channel in _channels)
                channel.Value.Writer.Complete();

            _channels.Clear();
        }
    }
}
