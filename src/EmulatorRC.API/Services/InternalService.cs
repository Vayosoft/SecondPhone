using System.Text.Json;
using System.Text.Json.Serialization;
using EmulatorRC.API.Channels;
using EmulatorRC.API.Extensions;
using EmulatorRC.API.Protos;
using Grpc.Core;

namespace EmulatorRC.API.Services
{
    public sealed class InternalService : DeviceService.DeviceServiceBase
    {
        private readonly ILogger<InternalService> _logger;
        private readonly ScreenChannel _screens;
        private readonly TouchChannel _toucheEvents;
        private readonly IHostApplicationLifetime _lifeTime;

        public InternalService(
            ILogger<InternalService> logger,
            ScreenChannel screens,
            TouchChannel toucheEvents,
            IHostApplicationLifetime lifeTime)
        {
            _logger = logger;
            _screens = screens;
            _toucheEvents = toucheEvents;
            _lifeTime = lifeTime;
        }

        public override Task<Ack> Ping(Syn request, ServerCallContext context)
        {
            return Task.FromResult(new Ack());
        }

        public override async Task GetTouchEvents(Syn request, IServerStreamWriter<TouchEvents> responseStream, ServerCallContext context)
        {
            Handshake(context, out var deviceId, out var cancellationSource);

            var cancellationToken = cancellationSource.Token;
            await foreach (var data in _toucheEvents.ReadAllAsync(deviceId, cancellationToken))
            {
                await responseStream.WriteAsync(data, cancellationToken);
            }
        }

        private void Handshake(ServerCallContext context, out string deviceId, out CancellationTokenSource cancellationSource)
        {
            deviceId = context.GetDeviceIdOrDefault("default")!;

            cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                context.CancellationToken, _lifeTime.ApplicationStopping);
        }

        public override Task<Ack> SendDeviceInfo(DeviceInfo request, ServerCallContext context)
        {
            return base.SendDeviceInfo(request, context);
        }

        public override async Task<Ack> UploadScreens(
            IAsyncStreamReader<DeviceScreen> requestStream,
            ServerCallContext context)
        {
            Handshake(context, out var deviceId, out var cancellationSource);
            
            var cancellationToken = cancellationSource.Token;
            await foreach (var request in requestStream.ReadAllAsync(cancellationToken))
            {
                await _screens.WriteAsync(deviceId, request, cancellationToken);
            }
            
            return new Ack();
        }
    }
}
