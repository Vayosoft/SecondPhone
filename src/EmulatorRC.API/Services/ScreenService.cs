using EmulatorRC.API.Extensions;
using EmulatorRC.API.Protos;
using Grpc.Core;

//https://learn.microsoft.com/ru-ru/aspnet/core/grpc/json-transcoding?view=aspnetcore-7.0
namespace EmulatorRC.API.Services
{
    public class ScreenService : ClientService.ClientServiceBase
    {
        private readonly ILogger<ScreenService> _logger;
        private readonly ScreenChannel _channel;
        private readonly IHostApplicationLifetime _lifeTime;

        public ScreenService(
            ILogger<ScreenService> logger,
            ScreenChannel channel, 
            IHostApplicationLifetime lifeTime)
        {
            _logger = logger;
            _channel = channel;
            _lifeTime = lifeTime;
        }

        //[Authorize]
        public override async Task GetScreens(
            IAsyncStreamReader<ScreenRequest> requestStream,
            IServerStreamWriter<DeviceScreen> responseStream,
            ServerCallContext context)
        {
            var deviceId = context.GetDeviceIdOrDefault("default")!;
            var clientId = context.GetClientId();

            _logger.LogInformation("CLIENT:[{clientId}] Connected for device: {deviceId}.", clientId, deviceId);

            var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                context.CancellationToken, _lifeTime.ApplicationStopping);

            try
            {
                //if (!_channel.Subscribe(clientId, deviceId))
                //    throw new RpcException(new Status(StatusCode.Internal, "Subscription failed."));

                await foreach (var request in requestStream.ReadAllAsync(cancellationSource.Token))
                {
                    var response = await _channel.ReadAsync(deviceId, request.Id, cancellationSource.Token);
                    await responseStream.WriteAsync(response, cancellationSource.Token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError("{type}| {message}", ex.GetType(), ex.Message);
            }
            finally
            {
                //_channel.Unsubscribe(clientId, deviceId);

                _logger.LogInformation("CLIENT:[{clientId}] Stream closed.", clientId);
            }
        }
    }
}
