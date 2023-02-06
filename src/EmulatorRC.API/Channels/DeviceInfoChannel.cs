using System.Collections.Concurrent;
using EmulatorRC.API.Protos;

namespace EmulatorRC.API.Channels
{
    public sealed class DeviceInfoChannel : ChannelBase<DeviceInfo>
    {
        private readonly ConcurrentDictionary<string, DeviceInfo> _data = new();

        public async ValueTask WriteAsync(string deviceId, DeviceInfo request, CancellationToken cancellationToken = default)
        {
            if (TryGetChannel(deviceId, out var channel))
            {
                await channel.Writer.WriteAsync(request, cancellationToken);
            }

            _data[deviceId] = request;
        }

        public DeviceInfo ReadLastAsync(string deviceId)
        {
            _data.TryGetValue(deviceId, out var lastData);
            return lastData;
        }

        public IAsyncEnumerable<DeviceInfo> ReadAllAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            return GetOrCreateChannel(deviceId).Reader.ReadAllAsync(cancellationToken);
        }
    }
}
