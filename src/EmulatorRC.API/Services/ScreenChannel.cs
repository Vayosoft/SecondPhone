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
        private readonly ConcurrentDictionary<string, Dictionary<string ,Channel<ScreenReply>>> _channels = new();

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

            if (_channels.TryGetValue(deviceId, out var channels))
            {
                foreach (var channel in channels.Values) 
                    await channel.Writer.WriteAsync(screen, cancellationToken);
            }

            _emulatorDataRepository.SetLastScreen(deviceId, new Screen(screen.Id, screen.Image.ToByteArray()));
        } 
        
        public async ValueTask<ScreenReply> ReadAsync(string clientId, string deviceId, string imageId, CancellationToken cancellationToken = default)
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

            return await GetChannel(clientId, deviceId).Reader.ReadAsync(cancellationToken);
        }

        private Channel<ScreenReply> GetChannel(string clientId, string deviceId)
        {
            if (!_channels.TryGetValue(deviceId, out var channels))
                throw new Exception("Please subscribe the channel first.");

            if (channels.TryGetValue(clientId, out var channel)) return channel;

            channel = Channel.CreateBounded<ScreenReply>(_options);
            channels.Add(clientId, channel);

            return channel;

        }

        public bool Subscribe(string clientId, string deviceId)
        {
            if (!_channels.TryGetValue(deviceId, out var channels))
            {
                return _channels.TryAdd(deviceId, new Dictionary<string, Channel<ScreenReply>>
                {
                    { clientId, Channel.CreateBounded<ScreenReply>(_options) }
                });
            }

            if (!channels.ContainsKey(clientId))
            {
                return channels.TryAdd(clientId, Channel.CreateBounded<ScreenReply>(_options));
            }

            return true;
        }

        public void Unsubscribe(string clientId, string deviceId)
        {
            if (!_channels.TryGetValue(deviceId, out var channels)) return;

            if(channels.ContainsKey(clientId))
                channels.Remove(clientId);
        }

        public void Dispose()
        {
            foreach (var channelList in _channels.Values)
            {
                foreach (var channel in channelList.Values)
                    channel.Writer.Complete();
            }
            _channels.Clear();
        }
    }
}
