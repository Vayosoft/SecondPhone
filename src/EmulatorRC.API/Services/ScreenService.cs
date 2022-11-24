using EmulatorRC.API.Channels;
using EmulatorRC.API.Extensions;
using EmulatorRC.API.Protos;
using Grpc.Core;

//https://learn.microsoft.com/ru-ru/aspnet/core/grpc/json-transcoding?view=aspnetcore-7.0
namespace EmulatorRC.API.Services
{
    public class ScreenService : ClientService.ClientServiceBase
    {
        private readonly ILogger<ScreenService> _logger;
        private readonly ScreenChannel _screens;
        private readonly TouchChannel _touchEvents;
        private readonly IHostApplicationLifetime _lifeTime;

        public ScreenService(
            ILogger<ScreenService> logger,
            ScreenChannel screens, 
            TouchChannel touchEvents, 
            IHostApplicationLifetime lifeTime)
        {
            _logger = logger;
            _screens = screens;
            _touchEvents = touchEvents;
            _lifeTime = lifeTime;
        }

        public override async Task<Ack> SendTouchEvents(IAsyncStreamReader<TouchEvents> requestStream, ServerCallContext context)
        {
            var deviceId = context.GetDeviceIdOrDefault("default")!;

            _logger.LogInformation("TOUCH | DEV:[{deviceId}] Connected.", deviceId);

            var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                context.CancellationToken, _lifeTime.ApplicationStopping);

            try
            {
                await foreach (var request in requestStream.ReadAllAsync(cancellationSource.Token))
                {
                    await _touchEvents.WriteAsync(deviceId, request, cancellationSource.Token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError("TOUCH | {type}| {message}", ex.GetType(), ex.Message);
            }

            _logger.LogInformation("TOUCH | DEV:[{deviceId}] Stream closed.", deviceId);

            return new Ack();
        }

        public override Task GetDeviceInfo(Syn request, IServerStreamWriter<DeviceInfo> responseStream, ServerCallContext context)
        {
            return base.GetDeviceInfo(request, responseStream, context);
        }

        //[Authorize]
        public override async Task GetScreens(
            IAsyncStreamReader<ScreenRequest> requestStream,
            IServerStreamWriter<DeviceScreen> responseStream,
            ServerCallContext context)
        {
            var deviceId = context.GetDeviceIdOrDefault("default")!;
            var clientId = context.GetClientId();

            _logger.LogInformation("SCREEN | CLIENT:[{clientId}] Connected for device: {deviceId}.", clientId, deviceId);

            var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                context.CancellationToken, _lifeTime.ApplicationStopping);

            try
            {
                //if (!_channel.Subscribe(clientId, deviceId))
                //    throw new RpcException(new Status(StatusCode.Internal, "Subscription failed."));

                await foreach (var request in requestStream.ReadAllAsync(cancellationSource.Token))
                {
                    var response = await _screens.ReadAsync(deviceId, request.Id, cancellationSource.Token);
                    await responseStream.WriteAsync(response, cancellationSource.Token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError("SCREEN | {type}| {message}", ex.GetType(), ex.Message);
            }
            finally
            {
                //_channel.Unsubscribe(clientId, deviceId);

                _logger.LogInformation("SCREEN | CLIENT:[{clientId}] Stream closed.", clientId);
            }
        }
    }
}
