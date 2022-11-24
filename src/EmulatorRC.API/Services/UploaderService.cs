using EmulatorRC.API.Channels;
using EmulatorRC.API.Extensions;
using EmulatorRC.API.Protos;
using Grpc.Core;

namespace EmulatorRC.API.Services
{
    public class UploaderService : DeviceService.DeviceServiceBase
    {
        private readonly ILogger<UploaderService> _logger;
        private readonly ScreenChannel _screens;
        private readonly TouchChannel _toucheEvents;
        private readonly IHostApplicationLifetime _lifeTime;

        public UploaderService(
            ILogger<UploaderService> logger,
            ScreenChannel screens,
            TouchChannel toucheEvents,
            IHostApplicationLifetime lifeTime)
        {
            _logger = logger;
            _screens = screens;
            _toucheEvents = toucheEvents;
            _lifeTime = lifeTime;
        }

        public override async Task GetTouchEvents(Syn request, IServerStreamWriter<TouchEvents> responseStream, ServerCallContext context)
        {
            var deviceId = context.GetDeviceIdOrDefault("default")!;
            var clientId = context.GetClientId();

            _logger.LogInformation("TOUCH | CLIENT:[{clientId}] Connected for device: {deviceId}.", clientId, deviceId);

            var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                context.CancellationToken, _lifeTime.ApplicationStopping);

            try
            {
                await foreach (var data in _toucheEvents.ReadAllAsync(deviceId, cancellationSource.Token))
                {
                    await responseStream.WriteAsync(data, cancellationSource.Token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError("TOUCH | {type}| {message}", ex.GetType(), ex.Message);
            }
            finally
            {
                _logger.LogInformation("TOUCH | CLIENT:[{clientId}] Stream closed.", clientId);
            }
        }

        public override Task<Ack> SendDeviceInfo(DeviceInfo request, ServerCallContext context)
        {
            return base.SendDeviceInfo(request, context);
        }

        public override async Task<Ack> UploadScreens(
            IAsyncStreamReader<DeviceScreen> requestStream,
            ServerCallContext context)
        {
            var deviceId = context.GetDeviceIdOrDefault("default")!;

            _logger.LogInformation("SCREEN | DEV:[{deviceId}] Connected.", deviceId);

            var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                context.CancellationToken, _lifeTime.ApplicationStopping);

            try
            {
                await foreach(var request in requestStream.ReadAllAsync(cancellationSource.Token))
                {
                    await _screens.WriteAsync(deviceId, request, cancellationSource.Token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError("SCREEN | {type}| {message}", ex.GetType(), ex.Message);
            }

            _logger.LogInformation("SCREEN | DEV:[{deviceId}] Stream closed.", deviceId);

            return new Ack();
        }
    }
}
