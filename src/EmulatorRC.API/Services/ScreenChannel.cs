using System.Threading.Channels;
using EmulatorRC.API.Protos;
using EmulatorRC.Services;
using Google.Protobuf;

namespace EmulatorRC.API.Services
{
    public class ScreenChannel : IDisposable
    {
        private readonly Channel<ScreenReply> _channel;
        private readonly IEmulatorDataRepository _emulatorDataRepository;

        public ScreenChannel(IEmulatorDataRepository emulatorDataRepository)
        {
            _emulatorDataRepository = emulatorDataRepository;
            var options = new BoundedChannelOptions(1)
            {
                SingleWriter = true,
                SingleReader = true,
                AllowSynchronousContinuations = true,
                FullMode = BoundedChannelFullMode.DropOldest
            };
            _channel = Channel.CreateBounded<ScreenReply>(options);
        }

        public async ValueTask WriteAsync(string deviceId, UploadMessageRequest request, CancellationToken cancellationToken = default)
        {
            var screen = new ScreenReply
            {
                Id = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond).ToString(),
                Image = request.Image
            };
            await _channel.Writer.WriteAsync(screen, cancellationToken);

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

            return await _channel.Reader.ReadAsync(cancellationToken);
        }

        public void Dispose()
        {
            _channel.Writer.Complete();
        }
    }
}
