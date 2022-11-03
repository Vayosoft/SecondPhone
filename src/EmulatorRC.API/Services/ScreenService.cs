using EmulatorRC.API.Protos;
using Google.Protobuf;
using Grpc.Core;
using LanguageExt;

namespace EmulatorRC.API.Services
{
    public class ScreenService : Screener.ScreenerBase
    {
        private readonly ILogger<ScreenService> _logger;

        public ScreenService(ILogger<ScreenService> logger)
        {
            _logger = logger;
        }

        public override Task<ScreenReply> GetScreen(ScreenRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Saying hello to {Name}", request.Id);
            return Task.FromResult(new ScreenReply
            {
               Image = ByteString.CopyFrom(Array.Empty<byte>())
            });
        }

        public override async Task GetScreenStream(ScreenRequest request, IServerStreamWriter<ScreenReply> responseStream, ServerCallContext context)
        {
            foreach (var x in Enumerable.Range(1, 10))
            {
                await responseStream.WriteAsync(new ScreenReply
                {
                    Image = ByteString.CopyFrom(Array.Empty<byte>())
                });

                await Task.Delay(200);
            }
        }
    }
}
