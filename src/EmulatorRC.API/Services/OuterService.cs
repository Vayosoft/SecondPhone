using EmulatorRC.API.Channels;
using EmulatorRC.API.Extensions;
using EmulatorRC.API.Protos;
using Grpc.Core;
using System.Linq;

//https://learn.microsoft.com/ru-ru/aspnet/core/grpc/json-transcoding?view=aspnetcore-7.0
namespace EmulatorRC.API.Services
{
    public sealed class OuterService : ClientService.ClientServiceBase
    {
        private readonly DeviceScreenChannel _screens;
        private readonly TouchChannel _touchEvents;
        private readonly DeviceInfoChannel _deviceInfo;
        private readonly IHostApplicationLifetime _lifeTime;

        public OuterService(
            DeviceScreenChannel screens, 
            TouchChannel touchEvents, 
            DeviceInfoChannel deviceInfo,
            IHostApplicationLifetime lifeTime)
        {
            _screens = screens;
            _touchEvents = touchEvents;
            _lifeTime = lifeTime;
            _deviceInfo = deviceInfo;
        }

        public override async Task<Ack> SendTouchEvents(IAsyncStreamReader<TouchEvents> requestStream, ServerCallContext context)
        {
            Handshake(context, out var deviceId, out var clientId, out var cancellationSource);

            var cancellationToken = cancellationSource.Token;
            await foreach (var request in requestStream.ReadAllAsync(cancellationToken))
            {
                await _touchEvents.WriteAsync(deviceId, request, cancellationToken);
            }

            return new Ack();
        }

        private void Handshake(ServerCallContext context, out string deviceId, out string clientId,
            out CancellationTokenSource cancellationSource)
        {
            deviceId = context.GetDeviceIdOrDefault("default")!;
            clientId = context.GetClientId();

            cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                context.CancellationToken, _lifeTime.ApplicationStopping);
        }

        public override async Task GetDeviceInfo(Syn request, IServerStreamWriter<DeviceInfo> responseStream, ServerCallContext context)
        {
            Handshake(context, out var deviceId, out var clientId, out var cancellationSource);

            var cancellationToken = cancellationSource.Token;
            await foreach (var data in _deviceInfo.ReadAllAsync(deviceId, cancellationToken))
            {
                await responseStream.WriteAsync(data, cancellationToken);
            }
        }

        //[Authorize]
        public override async Task GetScreens(
            IAsyncStreamReader<ScreenRequest> requestStream,
            IServerStreamWriter<DeviceScreen> responseStream,
            ServerCallContext context)
        {
            Handshake(context, out var deviceId, out var clientId, out var cancellationSource);

            //if (!_channel.Subscribe(clientId, deviceId))
            //    throw new RpcException(new Status(StatusCode.Internal, "Subscription failed."));
            var cancellationToken = cancellationSource.Token;
            await foreach (var request in requestStream.ReadAllAsync(cancellationToken))
            {
                var response = await _screens.ReadAsync(deviceId, request.Id, cancellationToken);
                await responseStream.WriteAsync(response, cancellationToken);
            }
        }
    }
}
