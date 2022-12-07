using EmulatorRC.API.Channels;
using EmulatorRC.API.Extensions;
using EmulatorRC.API.Protos;
using Grpc.Core;

//https://learn.microsoft.com/ru-ru/aspnet/core/grpc/json-transcoding?view=aspnetcore-7.0
namespace EmulatorRC.API.Services
{
    public sealed class OuterService : ClientService.ClientServiceBase
    {
        private readonly ILogger<OuterService> _logger;
        private readonly ScreenChannel _screens;
        private readonly TouchChannel _touchEvents;
        private readonly IHostApplicationLifetime _lifeTime;

        public OuterService(
            ILogger<OuterService> logger,
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
            Handshake(context, out var deviceId, out var clientId, out var cancellationSource);

            try
            {
                await foreach (var request in requestStream.ReadAllAsync(cancellationSource.Token))
                {
                    await _touchEvents.WriteAsync(deviceId, request, cancellationSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("{action} | CLIENT:[{deviceId}] Cancelled by client.",
                    context.Method, deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError("{action} | {type}| {message}", 
                    context.Method, ex.GetType(), ex.Message);
            }

            _logger.LogInformation("{action} | CLIENT:[{deviceId}] Stream closed.",
                context.Method, deviceId);

            return new Ack();
        }

        private void Handshake(ServerCallContext context, out string deviceId, out string clientId,
            out CancellationTokenSource cancellationSource)
        {
            deviceId = context.GetDeviceIdOrDefault("default")!;
            clientId = context.GetClientId();

            _logger.LogInformation("{action} | CLIENT:[{deviceId}] Connected for device.",
                context.Method, deviceId);

            cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                context.CancellationToken, _lifeTime.ApplicationStopping);
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
            Handshake(context, out var deviceId, out var clientId, out var cancellationSource);

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
            catch (OperationCanceledException ex)
            {
                _logger.LogError("{action} | {type}| {message}",
                    context.Method, ex.GetType(), ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError("{action} | {type}| CLIENT:[{deviceId}] {message}", 
                    context.Method, ex.GetType(), deviceId, ex.Message);
            }
            finally
            {
                //_channel.Unsubscribe(clientId, deviceId);

                _logger.LogInformation("{action} | CLIENT:[{deviceId}] Stream closed.",
                    context.Method, deviceId);
            }
        }
    }
}
