﻿using System.Runtime.CompilerServices;
using EmulatorRC.API.Protos;
using Google.Protobuf;
using ImageMagick.Formats;
using ImageMagick;
using Grpc.Core;
using EmulatorRC.API.Channels;

namespace EmulatorRC.API.Services.Handlers
{
    public sealed class DeviceHandler : ChannelBase<DeviceScreen>
    {
        private static readonly CallOptions CallOptions = new();
        private static readonly ImageFormat ImageFormat = new()
        {
            Format = ImageFormat.Types.ImgFormat.Png,
            Width = 480,
            Height = 720
        };

        public async IAsyncEnumerable<DeviceAudio> ReadAllAudioAsync(EmulatorController.EmulatorControllerClient emulatorClient, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var audioStream = emulatorClient.streamAudio(AudioFormat, CallOptions);
            await foreach (var sample in audioStream.ResponseStream.ReadAllAsync(cancellationToken))
            {
                var deviceAudio = new DeviceAudio
                {
                    Audio = sample.Audio,
                    Timestamp = sample.Timestamp
                };
                yield return deviceAudio;
            }
        }

        private static readonly AudioFormat AudioFormat = new()
        {
            Channels = AudioFormat.Types.Channels.Stereo,
            Format = AudioFormat.Types.SampleFormat.AudFmtS16,
            Mode = AudioFormat.Types.DeliveryMode.ModeRealTime,
            SamplingRate = 8000
        };

        public ValueTask<DeviceScreen> ReadScreenFromChannelAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            return GetOrCreateChannel(deviceId).Reader.ReadAsync(cancellationToken);
        }

        public async Task WriteScreensToChannelAsync(string deviceId, EmulatorController.EmulatorControllerClient emulatorClient, CancellationToken cancellationToken = default)
        {
            using var res = emulatorClient.streamScreenshot(ImageFormat, CallOptions);
            await foreach (var response in res.ResponseStream.ReadAllAsync(cancellationToken))
            {
                if (!TryGetChannel(deviceId, out var channel)) continue;

                var deviceScreen = new DeviceScreen
                {
                    Image = UnsafeByteOperations.UnsafeWrap(PrepareImage(response.Image_.Span)),
                    //Image = ByteString.FromStream(stream)
                };
                await channel.Writer.WriteAsync(deviceScreen, cancellationToken);
            }
        }

        private static readonly JpegWriteDefines JpegWriteDefines = new()
        {
            OptimizeCoding = true
        };

        //private static readonly RecyclableMemoryStreamManager RecyclableMemoryStreamManager = new();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Memory<byte> PrepareImage(ReadOnlySpan<byte> imageSpan)
        {
            using var image = new MagickImage(imageSpan);
            image.Format = image.Format;
            image.Quality = 50;

            //using var stream = RecyclableMemoryStreamManager.GetStream();
            //image.Write(stream);

            return image.ToByteArray(JpegWriteDefines).AsMemory();
        }
    }
}
