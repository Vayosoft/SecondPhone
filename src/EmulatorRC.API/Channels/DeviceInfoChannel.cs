using EmulatorRC.API.Protos;

namespace EmulatorRC.API.Channels
{
    public sealed class DeviceInfoChannel : ChannelBase<DeviceInfo>
    {
        public async ValueTask WriteAsync(string deviceId, DeviceInfo request, CancellationToken cancellationToken = default)
        {
            if (TryGetChannel(deviceId, out var channel))
            {
                await channel.Writer.WriteAsync(request, cancellationToken);
            }
        }

        public IAsyncEnumerable<DeviceInfo> ReadAllAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            return GetOrCreateChannel(deviceId).Reader.ReadAllAsync(cancellationToken);
        }
    }
}
