using Google.Protobuf;
using System.Threading.Channels;
using EmulatorRC.API.Protos;

namespace EmulatorRC.API.Services
{
    public class ScreenChannel : IDisposable
    {
        private readonly Channel<ScreenReply> _channel;

        public ScreenChannel()
        {
            var options = new BoundedChannelOptions(1)
            {
                SingleWriter = true,
                SingleReader = true,
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.DropOldest
            };
            _channel = Channel.CreateBounded<ScreenReply>(options);
        }

        public async ValueTask WriteAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            var screen = new ScreenReply
            {
                Id = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond).ToString(),
                Image = UnsafeByteOperations.UnsafeWrap(data)
            };
            await _channel.Writer.WriteAsync(screen, cancellationToken);
        } 
        
        public async ValueTask<ScreenReply> ReadAsync(CancellationToken cancellationToken = default)
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }

        public void Dispose()
        {
            _channel.Writer.Complete();
        }
    }
}
