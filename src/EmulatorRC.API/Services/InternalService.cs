﻿using EmulatorRC.API.Channels;
using EmulatorRC.API.Extensions;
using EmulatorRC.API.Protos;
using Grpc.Core;

namespace EmulatorRC.API.Services
{
    public sealed class InternalService : DeviceService.DeviceServiceBase
    {
        private readonly DeviceScreenChannel _screens;
        private readonly TouchChannel _touchEvents;
        private readonly DeviceInfoChannel _deviceInfo;
        private readonly IHostApplicationLifetime _lifeTime;

        public InternalService(
            DeviceScreenChannel screens,
            TouchChannel touchEvents,
            DeviceInfoChannel deviceInfo,
            IHostApplicationLifetime lifeTime)
        {
            _screens = screens;
            _touchEvents = touchEvents;
            _deviceInfo = deviceInfo;
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
            await foreach (var data in _touchEvents.ReadAllAsync(deviceId, cancellationToken))
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

        public override async Task<Ack> SendDeviceInfo(DeviceInfo request, ServerCallContext context)
        {
            Handshake(context, out var deviceId, out var cancellationSource);

            await _deviceInfo.WriteAsync(deviceId, request, cancellationSource.Token);

            return new Ack();
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
