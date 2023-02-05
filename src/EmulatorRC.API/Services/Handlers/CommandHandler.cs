using ErrorOr;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using EmulatorRC.Commons;
using System.Buffers;
using EmulatorRC.API.Model.Commands;

namespace EmulatorRC.API.Services.Handlers
{
    public abstract class CommandHandler
    {
        protected readonly ILogger<CommandHandler> Logger;

        protected CommandHandler(ILogger<CommandHandler> logger)
        {
            Logger = logger;
        }

        protected static ErrorOr<bool> InitChannel(ConcurrentDictionary<string, IDuplexPipe> channels, string name, IDuplexPipe pipe)
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

        protected static async Task RemoveChannelAsync(ConcurrentDictionary<string, IDuplexPipe> channels, string name)
        {
            if (channels.TryRemove(name, out var channel))
            {
                await channel.Output.CompleteAsync();
            }
        }

        protected async Task WriteAsync(ConcurrentDictionary<string, IDuplexPipe> channels, string name, IDuplexPipe pipe,
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
                        Logger.LogWarning(e, "{ChannelType} => {ChannelName} - {Error}", callerType, name, e.Message);
                    }
                }

                if (result.IsCompleted)
                {
                    break;
                }

                reader.AdvanceTo(buffer.End);
            }
        }

        protected async Task ReadAsync(ConcurrentDictionary<string, IDuplexPipe> channels, string name, IDuplexPipe pipe,
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

                        await Task.Delay(1000, cancellationToken);
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
                    Logger.LogError("{ChannelType} => {ChannelName} - {Error}", callerType, name, ch.FirstError.Description);
                }
            }
            finally
            {
                await RemoveChannelAsync(channels, name);
            }
        }
    }

    public abstract class CameraCommandHandler : CommandHandler
    {
        protected CameraCommandHandler(ILogger<CommandHandler> logger)
            : base(logger) { }

        protected abstract ConcurrentDictionary<string, IDuplexPipe> Channels { get; }

        public Task WriteAsync(CameraCommand command, IDuplexPipe pipe, CancellationToken cancellationToken) =>
            WriteAsync(Channels, command.DeviceId, pipe, cancellationToken: cancellationToken);

        public async Task ReadAsync(CameraCommand command, IDuplexPipe pipe, CancellationToken cancellationToken)
        {
            _ = await pipe.Output.WriteAsync(CreateMockHeader(command.Width, command.Height), cancellationToken);

            var initChannel = InitChannel(Channels, command.DeviceId, pipe);
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
                            default:
                                await Task.Delay(1000, cancellationToken);
                                consumed = buffer.End;
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
                    Logger.LogError("Camera => {ChannelName} - {Error}", command.DeviceId, initChannel.FirstError.Description);
                }
            }
            finally
            {
                await RemoveChannelAsync(Channels, command.DeviceId);
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
                reader.AdvanceToEnd();
                cmd = Commands.Undefined;
            }

            return reader.Position;
        }

        private static byte[] CreateMockHeader(int width, int height)
        {
            const int some1 = 0x21; //25,
            const int some2 = 0x307fe8f5;

            var byteBuffer = new List<byte>(9)
            {
                //0x02, 0x80 - 640
                (byte) (width >> 8 & 255),
                (byte) (width & 255),
                //0x01, 0xe0 - 480
                (byte)(height >> 8 & 255),
                (byte)(height & 255),
                //0x21 - 33
                some1 & 255,
                //0xf5, 0xe8, 0x7f, 0x30
                some2 & 255,
                some2 >> 8 & 255,
                some2 >> 16 & 255,
                some2 >> 24 & 255
            };

            return byteBuffer.ToArray();
        }

        internal enum Commands : byte
        {
            Undefined,
            Ping,
            GetBattery
        }
    }

    public sealed class CameraFrontCommandHandler : CameraCommandHandler
    {
        private static readonly ConcurrentDictionary<string, IDuplexPipe> Pipes = new();

        public CameraFrontCommandHandler(ILogger<CommandHandler> logger) : base(logger) {}

        protected override ConcurrentDictionary<string, IDuplexPipe> Channels => Pipes;
    }

    public sealed class CameraRearCommandHandler : CameraCommandHandler
    {
        private static readonly ConcurrentDictionary<string, IDuplexPipe> Pipes = new();

        public CameraRearCommandHandler(ILogger<CommandHandler> logger) : base(logger) { }

        protected override ConcurrentDictionary<string, IDuplexPipe> Channels => Pipes;
    }


    public sealed class MicrophoneCommandHandler : CommandHandler
    {
        private static readonly ConcurrentDictionary<string, IDuplexPipe> Channels = new();
        private readonly byte[] _header = { (byte)'-', (byte)'@', (byte)'v', (byte)'0', (byte)'2', 2 };

        public MicrophoneCommandHandler(ILogger<CommandHandler> logger)
            : base(logger) { }

        public Task WriteAsync(AudioCommand command, IDuplexPipe pipe, CancellationToken cancellationToken) =>
            WriteAsync(Channels, command.DeviceId, pipe, cancellationToken: cancellationToken);

        public async Task ReadAsync(AudioCommand command, IDuplexPipe pipe, CancellationToken cancellationToken)
        {
            _ = await pipe.Output.WriteAsync(_header, cancellationToken);
            await ReadAsync(Channels, command.DeviceId, pipe, cancellationToken: cancellationToken);
        }
    }
}
