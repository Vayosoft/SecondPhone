using EmulatorRC.API.Channels;
using EmulatorRC.API.Extensions;
using EmulatorRC.API.Protos;
using Grpc.Core;
using System.Runtime.CompilerServices;
using EmulatorRC.API.Services.Handlers;
using EmulatorRC.Services;

//https://learn.microsoft.com/ru-ru/aspnet/core/grpc/json-transcoding?view=aspnetcore-7.0
namespace EmulatorRC.API.Services
{
    public sealed class OuterService : ClientService.ClientServiceBase
    {
        private readonly IEmulatorDataRepository _deviceRepository;
        private readonly ILogger<OuterService> _logger;
        private readonly DeviceRpcHandler _deviceHandler;
        private readonly TouchChannel _touchEvents;
        private readonly DeviceInfoChannel _deviceInfo;
        private readonly IHostApplicationLifetime _lifeTime;

        public OuterService(
            DeviceRpcHandler deviceHandler,
            TouchChannel touchEvents, 
            DeviceInfoChannel deviceInfo,
            IHostApplicationLifetime lifeTime, IEmulatorDataRepository deviceRepository, ILogger<OuterService> logger)
        {
            _deviceHandler = deviceHandler;
            _touchEvents = touchEvents;
            _lifeTime = lifeTime;
            _deviceRepository = deviceRepository;
            _logger = logger;
            _deviceInfo = deviceInfo;
        }

        public override async Task<Ack> SendTouchEvents(IAsyncStreamReader<TouchEvents> requestStream, ServerCallContext context)
        {
            Handshake(context, out var clientId, out var cancellationSource);
            var cancellationToken = cancellationSource.Token;

            var device = await _deviceRepository.GetByClientIdAsync(clientId);
            if (device == null)
            {
                throw new RpcException(Status.DefaultCancelled, $"There are no device found for client {clientId}");
            }

            await foreach (var request in requestStream.ReadAllAsync(cancellationToken))
            {
                await _touchEvents.WriteAsync(device.Id, request, cancellationToken);
            }

            return new Ack();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Handshake(ServerCallContext context, out string clientId,
            out CancellationTokenSource cancellationSource)
        {
            clientId = context.GetClientId() ?? context.GetDeviceIdOrDefault("default")!;

            cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                context.CancellationToken, _lifeTime.ApplicationStopping);
        }

        public override async Task GetDeviceInfo(Syn request, IServerStreamWriter<DeviceInfo> responseStream, ServerCallContext context)
        {
            Handshake(context, out var clientId, out var cancellationSource);
            var cancellationToken = cancellationSource.Token;

            var device = await _deviceRepository.GetByClientIdAsync(clientId);
            if (device == null)
            {
                throw new RpcException(Status.DefaultCancelled, $"There are no device found for client {clientId}");
            }

            var lastDeviceInfo = _deviceInfo.ReadLastAsync(device.Id);
            if (lastDeviceInfo != null)
            {
                await responseStream.WriteAsync(lastDeviceInfo, cancellationToken);
            }

            await foreach (var data in _deviceInfo.ReadAllAsync(device.Id, cancellationToken))
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
            Handshake(context, out var clientId, out var cancellationSource);
            var cancellationToken = cancellationSource.Token;

            var device = await _deviceRepository.GetByClientIdAsync(clientId);
            if (device == null)
            {
                throw new RpcException(Status.DefaultCancelled, $"There are no device found for client {clientId}");
            }

            _ = _deviceHandler.WriteScreensToChannelAsync(device.Id, cancellationToken);

            await foreach (var request in requestStream.ReadAllAsync(cancellationToken))
            {
                var response = await _deviceHandler.ReadScreenFromChannelAsync(device.Id, cancellationToken);
                await responseStream.WriteAsync(response, cancellationToken);
            }
        }


        public override async Task GetAudio(Syn request, IServerStreamWriter<DeviceAudio> responseStream,
            ServerCallContext context)
        {
            Handshake(context, out var clientId, out var cancellationSource);
            var cancellationToken = cancellationSource.Token;

            var device = await _deviceRepository.GetByClientIdAsync(clientId);
            if (device == null)
            {
                throw new RpcException(Status.DefaultCancelled, $"There are no device found for client {clientId}");
            }

            await foreach (var deviceAudio in _deviceHandler.ReadAllAudioAsync(device.Id, cancellationToken))
            {
                await responseStream.WriteAsync(deviceAudio, cancellationToken);
            }
        }
    }
}
