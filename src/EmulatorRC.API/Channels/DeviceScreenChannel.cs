using System.Collections.Concurrent;
using EmulatorRC.API.Protos;

namespace EmulatorRC.API.Channels
{
    public sealed class DeviceScreenChannel : ChannelBase<DeviceScreen>
    {
        private readonly ConcurrentDictionary<string, DeviceScreen> _lastScreens = new();

        public async ValueTask WriteAsync(string deviceId, DeviceScreen request, CancellationToken cancellationToken = default)
        {
            request.Id = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond).ToString();

            if (TryGetChannel(deviceId, out var channel))
            {
                await channel.Writer.WriteAsync(request, cancellationToken);
            }

            _lastScreens[deviceId] = request;
        }

        public ValueTask<DeviceScreen> ReadAsync(string deviceId, string imageId, CancellationToken cancellationToken = default)
        {
            return string.IsNullOrEmpty(imageId) 
                ? ValueTask.FromResult(_lastScreens.TryGetValue(deviceId, out var screen) ? screen : null) 
                : GetOrCreateChannel(deviceId).Reader.ReadAsync(cancellationToken);
        }
    }
}
