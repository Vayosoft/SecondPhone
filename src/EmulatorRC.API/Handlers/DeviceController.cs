using System.Buffers;
using System.IO.Pipelines;
using System.Text.RegularExpressions;
using System.Text;

namespace EmulatorRC.API.Handlers
{
    public partial class DeviceController
    {
        public IServiceProvider Services { get; }

        public DeviceController(IServiceProvider services)
        {
            Services = services;
        }

        public async Task<DeviceRequest> GetRequestAsync(IDuplexPipe pipe, CancellationToken cancellationToken)
        {
            DeviceRequest handshake;
            while (true)
            {
                var result = await pipe.Input.ReadAsync(cancellationToken);
                var buffer = result.Buffer;

                var consumed = ProcessHandshake(buffer, out handshake);

                pipe.Input.AdvanceTo(consumed);

                break;
            }

            return handshake;
        }

        public async Task ExecuteAsync(DeviceRequest request, IDuplexPipe pipe, CancellationToken cancellationToken)
        {
            switch (request)
            {
                case VideoDeviceRequest videoRequest:
                    var cameraHandler = Services.GetRequiredService<CameraHandler>();
                    await cameraHandler.ReadAsync(videoRequest, pipe, cancellationToken);
                    break;
                case AudioDeviceRequest audioRequest:
                    var micHandler = Services.GetRequiredService<MicrophoneHandler>();
                    await micHandler.ReadAsync(audioRequest, pipe, cancellationToken);
                    break;
                case SpeakerDeviceRequest speakerRequest:
                    var speakerHandler = Services.GetRequiredService<SpeakerHandler>();
                    await speakerHandler.WriteAsync(speakerRequest, pipe, cancellationToken);
                    break;
            }
        }

        private static ReadOnlySpan<byte> CommandSpeaker => "CMD /v1/sound?"u8;

        //DroidCam
        private static ReadOnlySpan<byte> CommandVideo => "CMD /v2/video.4?"u8; //cam
        private static ReadOnlySpan<byte> CommandAudio => "CMD /v2/audio"u8; //mic

        [GeneratedRegex("id=(\\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex HandshakeRegex();

        [GeneratedRegex("(\\d+)x(\\d+)&id=(\\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex VideoHandshakeRegex();

        private static SequencePosition ProcessHandshake(ReadOnlySequence<byte> buffer, out DeviceRequest command)
        {
            var reader = new SequenceReader<byte>(buffer);

            if (reader.IsNext(CommandVideo, true))
            {
                var str = Encoding.UTF8.GetString(reader.UnreadSequence);
                var m = VideoHandshakeRegex().Match(str);

                if (!m.Success || m.Groups.Count < 4)
                    throw new ApplicationException($"Handshake failed (Camera) => {str}");

                if (!int.TryParse(m.Groups[1].Value, out var width) || !int.TryParse(m.Groups[2].Value, out var height))
                    throw new ApplicationException($"Handshake failed (Camera) => {str}");

                command = new VideoDeviceRequest(m.Groups[3].Value)
                {
                    Width = width,
                    Height = height,
                };
            }
            else if (reader.IsNext(CommandSpeaker, true))
            {
                var str = Encoding.UTF8.GetString(reader.UnreadSequence);
                var m = HandshakeRegex().Match(str);

                if (!m.Success || m.Groups.Count < 2)
                    throw new ApplicationException($"Handshake failed (Speaker) => {str}");

                command = new SpeakerDeviceRequest(m.Groups[1].Value);
            }
            else if (reader.IsNext(CommandAudio, true))
            {
                //var str = Encoding.UTF8.GetString(reader.UnreadSequence);
                //var m = HandshakeRegex().Match(str);

                //if (!m.Success || m.Groups.Count < 2)
                //    throw new ApplicationException($"Handshake failed (Mic) => {str}");

                //command = new AudioHandshake(m.Groups[1].Value);
                command = new AudioDeviceRequest("default");
            }
            else
            {
                var str = Encoding.UTF8.GetString(reader.UnreadSequence);
                throw new ApplicationException($"Handshake failed => {str}");
            }

            return reader.Position;
        }
    }
}
