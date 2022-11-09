using EmulatorRC.API.Extensions;
using EmulatorRC.API.Hubs;
using EmulatorRC.API.Protos;
using EmulatorRC.Services;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Channels;
using static System.Net.Mime.MediaTypeNames;

namespace EmulatorRC.API.Services
{
    public class UploaderService : Uploader.UploaderBase
    {
        private readonly ILogger<UploaderService> _logger;
        private readonly IEmulatorDataRepository _emulatorDataRepository;
        private readonly IHubContext<ImagesHub> _imageHub;
        private readonly ScreenChannel _channel;

        public UploaderService(ILogger<UploaderService> logger, IEmulatorDataRepository emulatorDataRepository, IHubContext<ImagesHub> imageHub, ScreenChannel channel)
        {
            _logger = logger;
            _emulatorDataRepository = emulatorDataRepository;
            _imageHub = imageHub;
            _channel = channel;
        }

        public override async Task<Ack> UploadMessage(IAsyncStreamReader<UploadMessageRequest> requestStream, ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            var deviceId = httpContext.Request.GetDeviceIdOrDefault("default")!;

            try
            {
                while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
                {
                    //var image = requestStream.Current.Image.ToByteArray();

                    //if (_logger.IsEnabled(LogLevel.Debug))
                    //{
                    //    _logger.LogDebug("Screen uploaded => {length} bytes", image.Length);
                    //}

                    //var imageId = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond).ToString();

                    //_emulatorDataRepository.SetScreen(deviceId, imageId, image);
                    //_emulatorDataRepository.SetLastScreenId(deviceId, imageId);
                    //_emulatorDataRepository.SetLastScreen(deviceId, new Screen(imageId, image));

                    await _channel.WriteAsync(requestStream.Current);

                    //if (ImagesHub.Devices.TryGetValue(deviceId, out var clientIds) && clientIds.Count > 0)
                    //{
                    //    await _imageHub.Clients.Clients(clientIds.ToArray()).SendAsync("ImageMessage", imageId);
                    //}
                }
            }
            catch (RpcException ex) // when (ex.StatusCode == StatusCode.Cancelled)
            {
                _logger.LogWarning(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            _logger.LogInformation("Upload stream closed for {deviceId}", deviceId);

            return new Ack();
        }
    }
}
