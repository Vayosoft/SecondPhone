using System.Text;
using EmulatorRC.API.Extensions;
using EmulatorRC.API.Protos;
using EmulatorRC.Services;
using Google.Protobuf;
using Grpc.Core;

namespace EmulatorRC.API.Services
{
    public class ScreenService : Screener.ScreenerBase
    {
        private readonly ILogger<ScreenService> _logger;
        private readonly IEmulatorDataRepository _emulatorDataRepository;

        public ScreenService(ILogger<ScreenService> logger, IEmulatorDataRepository emulatorDataRepository)
        {
            _logger = logger;
            _emulatorDataRepository = emulatorDataRepository;
        }

        public override Task<ScreenReply> GetScreen(ScreenRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Saying hello to {Name}", request.Id);
            return Task.FromResult(new ScreenReply
            {
               Image = ByteString.CopyFrom(Encoding.UTF8.GetBytes("GetScreen"))
            });
        }

        public override async Task GetScreenStream(ScreenRequest request, IServerStreamWriter<ScreenReply> responseStream, ServerCallContext context)
        {
            foreach (var x in Enumerable.Range(1, 2))
            {
                await responseStream.WriteAsync(new ScreenReply
                {
                    Image = ByteString.CopyFrom(Encoding.UTF8.GetBytes("GetScreenStream"))
                });

                await Task.Delay(200);
            }
        }

        public override async Task GetScreen2Stream(IAsyncStreamReader<ScreenRequest> requestStream, IServerStreamWriter<ScreenReply> responseStream, ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            var deviceId = httpContext.Request.GetDeviceIdOrDefault();

            _logger.LogInformation("Connected for {deviceId}", deviceId);

            while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
            {
                var id = requestStream.Current.Id;
                Console.WriteLine("Stream request " + id);

                byte[]? bytes = null;
                if (deviceId is not null)
                {
                    bytes = _emulatorDataRepository.GetLastScreen(deviceId);
                }

                var response = new ScreenReply
                {
                    //Image = ByteString.CopyFrom(bytes ?? Array.Empty<byte>())
                    Image = UnsafeByteOperations.UnsafeWrap(bytes ?? Array.Empty<byte>())
                };

                await responseStream.WriteAsync(response);
            }
        }
    }
}
