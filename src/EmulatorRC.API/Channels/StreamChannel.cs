using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using EmulatorRC.API.Handlers;
using EmulatorRC.Commons;
using ErrorOr;
using System.Buffers;

namespace EmulatorRC.API.Channels
{
    public sealed class StreamChannel
    {
        private readonly ILogger<StreamChannel> _logger;

        private static readonly ConcurrentDictionary<string, IDuplexPipe> Camera = new();
        private static readonly ConcurrentDictionary<string, IDuplexPipe> Mic = new();
        private static readonly ConcurrentDictionary<string, IDuplexPipe> Speaker = new();

        public StreamChannel(ILogger<StreamChannel> logger)
        {
            _logger = logger;
        }

        public async Task ReadCameraAsync(string name, IDuplexPipe pipe, CancellationToken cancellationToken)
        {
            var initChannel = InitChannel(Camera, name, pipe);
            try
            {
                if (!initChannel.IsError)
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var result = await pipe.Input.ReadAsync(cancellationToken);
                        var buffer = result.Buffer;

                        var consumed = ProcessCommand(buffer, out var cmd);
                        switch (cmd)
                        {
                            case Commands.GetBattery:
                                _ = await pipe.Output.WriteAsync("\r\n\r\n100"u8.ToArray(), cancellationToken);
                                break;
                        }

                        if (result.IsCompleted)
                        {
                            break;
                        }

                        pipe.Input.AdvanceTo(consumed);
                    }
                }
                else
                {
                    _logger.LogError("Camera => {ChannelName} - {Error}", name, initChannel.FirstError.Description);
                }
            }
            finally
            {
                await RemoveChannelAsync(Camera, name);
            }
        }

        private static ReadOnlySpan<byte> CommandPing => "CMD /v1/ping"u8;
        private static ReadOnlySpan<byte> GetBattery => "GET /battery"u8;

        private static SequencePosition ProcessCommand(ReadOnlySequence<byte> buffer, out Commands cmd)
        {
            var reader = new SequenceReader<byte>(buffer);

            if (reader.IsNext(CommandPing, true))
            {
                cmd = Commands.Ping;
            }
            else if (reader.IsNext(GetBattery, true))
            {
                cmd = Commands.GetBattery;
            }
            else
            {
                cmd = Commands.Undefined;
            }

            return reader.Position;
        }

        public Task ReadMicAsync(string name, IDuplexPipe pipe, CancellationToken cancellationToken) =>
            ReadAsync(Mic, name, pipe, cancellationToken: cancellationToken);
        public Task ReadSpeakerAsync(string name, IDuplexPipe pipe, CancellationToken cancellationToken) =>
            ReadAsync(Speaker, name, pipe, cancellationToken: cancellationToken);

        private async Task ReadAsync(ConcurrentDictionary<string, IDuplexPipe> channels, string name, IDuplexPipe pipe,
            [CallerMemberName] string callerType = "", CancellationToken cancellationToken = default)
        {
            var ch = InitChannel(channels, name, pipe);
            try
            {
                if (!ch.IsError)
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var result = await pipe.Input.ReadAsync(cancellationToken);
                        var buffer = result.Buffer;

                        var consumed = buffer.End;

                        if (result.IsCompleted)
                        {
                            break;
                        }

                        pipe.Input.AdvanceTo(consumed);
                    }
                }
                else
                {
                    _logger.LogError("{ChannelType} => {ChannelName} - {Error}", callerType, name, ch.FirstError.Description);
                }
            }
            finally
            {
                await RemoveChannelAsync(channels, name);
            }
        }

        private static ErrorOr<bool> InitChannel(ConcurrentDictionary<string, IDuplexPipe> channels, string name, IDuplexPipe pipe)
        {
            if (channels.TryGetValue(name, out _))
                return Error.Conflict(description: "Channel is busy");

            using (Locker.GetLockerByName(name).Lock())
            {
                if (channels.TryGetValue(name, out _))
                    return Error.Conflict(description: "Channel is busy");

                channels[name] = pipe;
            }

            return true;
        }

        private static async Task RemoveChannelAsync(ConcurrentDictionary<string, IDuplexPipe> channels, string name)
        {
            if (channels.TryRemove(name, out var channel))
            {
                await channel.Output.CompleteAsync();
            }
        }

        public Task WriteCameraAsync(string name, IDuplexPipe pipe, CancellationToken cancellationToken) =>
            WriteAsync(Camera, name, pipe, cancellationToken: cancellationToken);
        public Task WriteMicAsync(string name, IDuplexPipe pipe, CancellationToken cancellationToken) =>
            WriteAsync(Mic, name, pipe, cancellationToken: cancellationToken);

        public Task WriteSpeakerAsync(string name, IDuplexPipe pipe, CancellationToken cancellationToken) =>
            WriteAsync(Speaker, name, pipe, cancellationToken: cancellationToken);

        private async Task WriteAsync(ConcurrentDictionary<string, IDuplexPipe> channels, string name, IDuplexPipe pipe,
            [CallerMemberName] string callerType = "", CancellationToken cancellationToken = default)
        {
            var reader = pipe.Input;
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken);
                var buffer = result.Buffer;

                if (channels.TryGetValue(name, out var channel))
                {
                    try
                    {
                        foreach (var segment in buffer)
                        {
                            await channel.Output.WriteAsync(segment, cancellationToken);
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "{ChannelType} => {ChannelName} - {Error}", callerType, name, e.Message);
                    }
                }

                if (result.IsCompleted)
                {
                    break;
                }

                reader.AdvanceTo(buffer.End);
            }
        }
    }
}
