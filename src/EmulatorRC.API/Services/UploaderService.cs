using EmulatorRC.API.Extensions;
using EmulatorRC.API.Protos;
using Grpc.Core;

namespace EmulatorRC.API.Services
{
    public class UploaderService : Uploader.UploaderBase
    {
        private readonly ILogger<UploaderService> _logger;
        private readonly ScreenChannel _channel;
        private readonly IHostApplicationLifetime _lifeTime;

        public UploaderService(
            ILogger<UploaderService> logger,
            ScreenChannel channel,
            IHostApplicationLifetime lifeTime)
        {
            _logger = logger;
            _channel = channel;
            _lifeTime = lifeTime;
        }

        public override async Task<Ack> UploadMessage(
            IAsyncStreamReader<UploadMessageRequest> requestStream,
            ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            var deviceId = httpContext.Request.GetDeviceIdOrDefault("default")!;

            _logger.LogInformation("DEV:[{deviceId}] Connected.", deviceId);

            var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                context.CancellationToken, _lifeTime.ApplicationStopping);

            try
            {
                await foreach(var request in requestStream.ReadAllAsync(cancellationSource.Token))
                {
                    await _channel.WriteAsync(deviceId, request, cancellationSource.Token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError("{type}| {message}", ex.GetType(), ex.Message);
            }

            _logger.LogInformation("DEV:[{deviceId}] Stream closed.", deviceId);

            return new Ack();
        }
    }
}
