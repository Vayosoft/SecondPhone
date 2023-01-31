using EmulatorRC.API.Channels;
using EmulatorRC.API.Extensions;
using EmulatorRC.API.Protos;
using Grpc.Core;
using ImageMagick.Formats;
using ImageMagick;
using System.Runtime.CompilerServices;
using Google.Protobuf;
using Microsoft.IO;

//https://learn.microsoft.com/ru-ru/aspnet/core/grpc/json-transcoding?view=aspnetcore-7.0
namespace EmulatorRC.API.Services
{
    public sealed class OuterService : ClientService.ClientServiceBase
    {
        //private readonly DeviceScreenChannel _screens;
        private readonly EmulatorController.EmulatorControllerClient _emulatorClient;
        private readonly TouchChannel _touchEvents;
        private readonly DeviceInfoChannel _deviceInfo;
        private readonly IHostApplicationLifetime _lifeTime;

        public OuterService(
            //DeviceScreenChannel screens,
            EmulatorController.EmulatorControllerClient emulatorClient,
            TouchChannel touchEvents, 
            DeviceInfoChannel deviceInfo,
            IHostApplicationLifetime lifeTime)
        {
            //_screens = screens;
            _emulatorClient = emulatorClient;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        //public override async Task GetScreens(
        //    IAsyncStreamReader<ScreenRequest> requestStream,
        //    IServerStreamWriter<DeviceScreen> responseStream,
        //    ServerCallContext context)
        //{
        //    Handshake(context, out var deviceId, out var clientId, out var cancellationSource);

        //    //if (!_channel.Subscribe(clientId, deviceId))
        //    //    throw new RpcException(new Status(StatusCode.Internal, "Subscription failed."));
        //    var cancellationToken = cancellationSource.Token;
        //    await foreach (var request in requestStream.ReadAllAsync(cancellationToken))
        //    {
        //        var response = await _screens.ReadAsync(deviceId, request.Id, cancellationToken);
        //        await responseStream.WriteAsync(response, cancellationToken);
        //    }
        //}

        private static readonly ImageFormat ImageFormat = new() { Format = ImageFormat.Types.ImgFormat.Png, Width = 480, Height = 720 };
        private static readonly CallOptions CallOptions = new();
        public override async Task GetScreens(
            IAsyncStreamReader<ScreenRequest> requestStream,
            IServerStreamWriter<DeviceScreen> responseStream,
            ServerCallContext context)
        {
            Handshake(context, out var deviceId, out var clientId, out var cancellationSource);

            var cancellationToken = cancellationSource.Token;

            await foreach (var request in requestStream.ReadAllAsync(cancellationToken))
            {
                var response = await _emulatorClient.getScreenshotAsync(ImageFormat, CallOptions);
                await responseStream.WriteAsync(GetScreen(response.Image_.Span), cancellationToken);
            }
        }

        private static readonly JpegWriteDefines JpegWriteDefines = new() { OptimizeCoding = true };
        //private static readonly RecyclableMemoryStreamManager RecyclableMemoryStreamManager = new();
        private static DeviceScreen GetScreen(ReadOnlySpan<byte> imageSpan)
        {
            using var image = new MagickImage(imageSpan);
            image.Format = image.Format;
            image.Quality = 30;

            var data = image.ToByteArray(JpegWriteDefines).AsSpan();
            var deviceScreen = new DeviceScreen
            {
                Image = ByteString.CopyFrom(data),
            };
            return deviceScreen;
        }

        private static readonly AudioFormat AudioFormat = new AudioFormat
        {
            Channels = AudioFormat.Types.Channels.Stereo,
            Format = AudioFormat.Types.SampleFormat.AudFmtS16,
            Mode = AudioFormat.Types.DeliveryMode.ModeRealTime,
            SamplingRate = 8000 //44100
        };

        public override async Task GetAudio(Syn request, IServerStreamWriter<DeviceAudio> responseStream,
            ServerCallContext context)
        {
            Handshake(context, out var deviceId, out var clientId, out var cancellationSource);

            var cancellationToken = cancellationSource.Token;

            using var audioStream = _emulatorClient.streamAudio(AudioFormat, CallOptions);
            await foreach (var sample in audioStream.ResponseStream.ReadAllAsync(cancellationToken))
            {
                var deviceAudio = new DeviceAudio
                {
                    Audio = sample.Audio,
                    Timestamp = sample.Timestamp
                };
                await responseStream.WriteAsync(deviceAudio, cancellationToken);
            }
        }
    }
}
