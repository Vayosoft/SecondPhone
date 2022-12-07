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

        public override async Task GetTouchEvents(Syn request, IServerStreamWriter<TouchEvents> responseStream, ServerCallContext context)
        {
            Handshake(context, out var deviceId, out var cancellationSource);

            try
            {
                var cancellationToken = cancellationSource.Token;
                await foreach (var data in _toucheEvents.ReadAllAsync(deviceId, cancellationToken))
                {
                    await responseStream.WriteAsync(data, cancellationToken);
                }

                _logger.LogInformation("{action} | EMULATOR:[{deviceId}] Stream closed.",
                    context.Method, deviceId);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("{action} | EMULATOR:[{deviceId}] Cancelled by emulator.",
                    context.Method, deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError("{action} | EMULATOR:[{deviceId}] Exception: {type} {message}",
                    context.Method, deviceId, ex.GetType(), ex.Message);
            }
        }

        private void Handshake(ServerCallContext context, out string deviceId, out CancellationTokenSource cancellationSource)
        {
            deviceId = context.GetDeviceIdOrDefault("default")!;

            _logger.LogInformation("{action} | EMULATOR:[{deviceId}] Connected.", context.Method, deviceId);

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

            try
            {
                var cancellationToken = cancellationSource.Token;
                await foreach (var request in requestStream.ReadAllAsync(cancellationToken))
                {
                    await _screens.WriteAsync(deviceId, request, cancellationToken);
                }

                _logger.LogInformation("{action} | EMULATOR:[{deviceId}] Stream closed.",
                    context.Method, deviceId);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("{action} | EMULATOR:[{deviceId}] Stream cancelled.",
                    context.Method, deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError("{action} | EMULATOR:[{deviceId}] Exception: {type} {message}",
                    context.Method, deviceId, ex.GetType(), ex.Message);
            }

            return new Ack();
        }
    }
}
