using EmulatorRC.API.Extensions;
using EmulatorRC.API.Hubs;
using EmulatorRC.API.Protos;
using EmulatorRC.Services;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;

namespace EmulatorRC.API.Services
{
    public class UploaderService : Uploader.UploaderBase
    {
        private readonly ILogger<UploaderService> _logger;
        private readonly IEmulatorDataRepository _emulatorDataRepository;
        private readonly IHubContext<ImagesHub> _imageHub;

        public UploaderService(ILogger<UploaderService> logger, IEmulatorDataRepository emulatorDataRepository, IHubContext<ImagesHub> imageHub)
        {
            _logger = logger;
            _emulatorDataRepository = emulatorDataRepository;
            _imageHub = imageHub;
        }

        public override async Task<Ack> UploadMessage(IAsyncStreamReader<UploadMessageRequest> requestStream, ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            var deviceId = httpContext.Request.GetDeviceIdOrDefault("default")!;

            try
            {
                while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
                {
                    var image = requestStream.Current.Image.ToByteArray();

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Screen uploaded => {0} bytes", image.Length);
                    }

                    var imageId = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond).ToString();

                    _emulatorDataRepository.SetScreen(deviceId, imageId, image);
                    _emulatorDataRepository.SetLastScreenId(deviceId, imageId);
                    _emulatorDataRepository.SetLastScreen(deviceId, new Screen(imageId, image));

                    if (ImagesHub.Devices.TryGetValue(deviceId, out var clientIds) && clientIds.Count > 0)
                    {
                        await _imageHub.Clients.Clients(clientIds.ToArray()).SendAsync("ImageMessage", imageId);
                    }
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled) { /*ignored*/ }

            return new Ack();
        }
    }
}
