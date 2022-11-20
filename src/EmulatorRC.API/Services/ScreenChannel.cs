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

        public async ValueTask WriteAsync(string deviceId, UploadMessageRequest request, CancellationToken cancellationToken = default)
        {
            var screen = new ScreenReply
            {
                Id = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond).ToString(),
                Image = request.Image
            };
            await GetChannel(deviceId).Writer.WriteAsync(screen, cancellationToken);

            _emulatorDataRepository.SetLastScreen(deviceId, new Screen(screen.Id, screen.Image.ToByteArray()));
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
                        Image = UnsafeByteOperations.UnsafeWrap(screen.Image),
                    };
                }
            }

            return await GetChannel(deviceId).Reader.ReadAsync(cancellationToken);
        }

        private Channel<ScreenReply> GetChannel(string key)
        {
            if (!_channels.TryGetValue(key, out var channel))
            {
                channel = Channel.CreateBounded<ScreenReply>(_options);
                _channels.TryAdd(key, channel);
            }

            return channel;
        }

        public void Dispose()
        {
            foreach (var channel in _channels.Values)
            {
                channel.Writer.Complete();
            }
            _channels.Clear();
        }
    }
}
