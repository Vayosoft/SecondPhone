using System.Collections.Concurrent;
using System.Threading.Channels;
using EmulatorRC.API.Protos;
using EmulatorRC.Services;
using Google.Protobuf;

namespace EmulatorRC.API.Services
{
    public class ScreenChannel : IDisposable
    {
        private readonly IEmulatorDataRepository _emulatorDataRepository;
        private readonly ConcurrentDictionary<string, Channel<ScreenReply>> _channels = new();

        private readonly BoundedChannelOptions _options = new(1)
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
            var screen = new ScreenReply
            {
                Id = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond).ToString(),
                DeviceScreen = request
            };

            if (_channels.TryGetValue(deviceId, out var channel))
            {
                await channel.Writer.WriteAsync(screen, cancellationToken);
            }

            _emulatorDataRepository.SetLastScreen(deviceId, new Screen(screen.Id, screen.DeviceScreen.Image.ToByteArray()));
        }

        public async ValueTask<ScreenReply> ReadAsync(string deviceId, string imageId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(imageId))
            {
                var screen = _emulatorDataRepository.GetLastScreen(deviceId);
                if (screen != null)
                {
                    return new ScreenReply
                    {
                        Id = screen.Id,
                        DeviceScreen = new DeviceScreen
                        {
                            Image = UnsafeByteOperations.UnsafeWrap(screen.Image)
                        },
                    };
                }
            }

            if (!_channels.TryGetValue(deviceId, out var channel))
            {
                channel = Channel.CreateBounded<ScreenReply>(_options);
                _channels.TryAdd(deviceId, channel);
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
