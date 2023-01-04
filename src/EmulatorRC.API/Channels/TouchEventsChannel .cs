using EmulatorRC.API.Protos;

namespace EmulatorRC.API.Channels
{
    public sealed class TouchChannel : ChannelBase<TouchEvents>
    {
        public TouchChannel() : base(10) { }

        public async ValueTask WriteAsync(string deviceId, TouchEvents request, CancellationToken cancellationToken = default)
        {
            if (TryGetChannel(deviceId, out var channel))
            {
                await channel.Writer.WriteAsync(request, cancellationToken);
            }
        }

        public IAsyncEnumerable<TouchEvents> ReadAllAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            return GetOrCreateChannel(deviceId).Reader.ReadAllAsync(cancellationToken);
        }
    }
}