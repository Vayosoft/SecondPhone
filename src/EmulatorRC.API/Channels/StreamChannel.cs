using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using EmulatorRC.Commons;
using ErrorOr;
using Microsoft.AspNetCore.Connections;

namespace EmulatorRC.API.Channels
{
    public sealed class StreamChannel
    {
        private readonly ILogger<StreamChannel> _logger;

        private readonly ConcurrentDictionary<string, Pipe> _cameraClientToDevice = new();
        private readonly ConcurrentDictionary<string, Pipe> _micClientToDevice = new();
        private readonly ConcurrentDictionary<string, Pipe> _speakerDeviceToClient = new();

        private static readonly ConcurrentDictionary<string, bool> Camera = new();
        private static readonly ConcurrentDictionary<string, bool> Mic = new();
        private static readonly ConcurrentDictionary<string, bool> Speaker = new();

        public StreamChannel(ILogger<StreamChannel> logger)
        {
            _logger = logger;
        }

        public Task WriteSpeakerAsync(string name, ConnectionContext connection, CancellationToken cancellationToken) =>
            WriterAsync(_speakerDeviceToClient, Speaker, name, connection, cancellationToken: cancellationToken);

        public Task WriterCameraAsync(string name, ConnectionContext connection, CancellationToken cancellationToken) =>
            WriterAsync(_cameraClientToDevice, Camera, name, connection, cancellationToken: cancellationToken);

        public Task WriterMicAsync(string name, ConnectionContext connection, CancellationToken cancellationToken) => 
            WriterAsync(_micClientToDevice, Mic, name, connection, cancellationToken: cancellationToken);

        private async Task WriterAsync(ConcurrentDictionary<string, Pipe> channels, ConcurrentDictionary<string, bool> readers, string name, ConnectionContext connection,
            [CallerMemberName] string callerType = "", CancellationToken cancellationToken = default)
        {
            var channelWriter = GetOrCreateWriter(channels, name);
            try
            {
                if (!channelWriter.IsError)
                {
                    var writer = channelWriter.Value;

                    while (true)
                    {
                        var result = await connection.Transport.Input.ReadAsync(cancellationToken);
                        var buffer = result.Buffer;

                        if (readers.ContainsKey(name))
                        {
                            foreach (var segment in buffer)
                            {
                                await writer!.WriteAsync(segment, cancellationToken);
                            }
                        }

                        if (result.IsCompleted)
                        {
                            break;
                        }

                        connection.Transport.Input.AdvanceTo(buffer.End);
                    }
                }
                else
                {
                    _logger.LogError("{ConnectionId} {ChannelType} => {ChannelName} - {Error}",
                        connection.ConnectionId, callerType, name, channelWriter.FirstError.Description);
                }
            }
            finally
            {
                await RemoveWriterAsync(channels, name);
            }
        }

        private static ErrorOr<PipeWriter> GetOrCreateWriter(ConcurrentDictionary<string, Pipe> channels, string name)
        {
            if (channels.TryGetValue(name, out var channel))
                return Error.Conflict(description: "Channel is busy");

            using (Locker.GetLockerByName(name).Lock())
            {
                if (channels.TryGetValue(name, out channel))
                    return Error.Conflict(description: "Channel is busy");

                channel = new Pipe();
                channels[name] = channel;
            }

            return channel.Writer;
        }

        public Task ReadAllMicAsync(string name, PipeWriter output, CancellationToken cancellationToken) =>
            ReadAllAsync(_micClientToDevice, Mic, name, output, cancellationToken: cancellationToken);
        public Task ReadAllCameraAsync(string name, PipeWriter output, CancellationToken cancellationToken) =>
            ReadAllAsync(_cameraClientToDevice, Camera, name, output, cancellationToken: cancellationToken);
        public Task ReadAllSpeakerAsync(string name, PipeWriter output, CancellationToken cancellationToken) =>
            ReadAllAsync(_speakerDeviceToClient, Speaker, name, output, cancellationToken: cancellationToken);

        private async Task ReadAllAsync(ConcurrentDictionary<string, Pipe> channels, ConcurrentDictionary<string, bool> readers, string name, PipeWriter output,
            [CallerMemberName] string callerType = "", CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                readers.TryAdd(name, true);
                try
                {
                    await foreach (var segment in ReadAllAsync(channels, name, cancellationToken))
                    {
                        await output.WriteAsync(segment, cancellationToken);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "{ChannelType} => {ChannelName} - {Error}", callerType, name, e.Message);
                }
                finally
                {
                    readers.TryRemove(name, out _);
                }

                await Task.Delay(1000, cancellationToken);
            }
        }

        public IAsyncEnumerable<ReadOnlyMemory<byte>> ReadAllMicAsync(string name, CancellationToken cancellationToken) =>
            ReadAllAsync(_micClientToDevice, name, cancellationToken);
        public IAsyncEnumerable<ReadOnlyMemory<byte>> ReadAllCameraAsync(string name, CancellationToken cancellationToken) =>
            ReadAllAsync(_cameraClientToDevice, name, cancellationToken);
        public IAsyncEnumerable<ReadOnlyMemory<byte>> ReadAllSpeakerAsync(string name, CancellationToken cancellationToken) =>
            ReadAllAsync(_speakerDeviceToClient, name, cancellationToken);

        private static async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadAllAsync(ConcurrentDictionary<string, Pipe> channels, string name,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (!channels.TryGetValue(name, out var channel)) yield break;
            var reader = channel.Reader;
            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken);
                var buffer = result.Buffer;
                try
                {
                    if (result.IsCompleted)
                    {
                        yield break;
                    }

                    foreach (var segment in buffer)
                    {
                        yield return segment;
                    }
                }
                finally
                {
                    reader.AdvanceTo(buffer.End);
                }
            }
        }

        public Task RemoveCameraWriterAsync(string name) =>
            RemoveWriterAsync(_cameraClientToDevice, name);
        public Task RemoveSpeakerWriterAsync(string name) =>
            RemoveWriterAsync(_speakerDeviceToClient, name);
        public Task RemoveMicWriterAsync(string name) =>
            RemoveWriterAsync(_micClientToDevice, name);
        private static async Task RemoveWriterAsync(ConcurrentDictionary<string, Pipe> channels, string name)
        {
            if (channels.TryRemove(name, out var channel))
            {
                await channel.Writer.CompleteAsync();
            }
        }
    }
}
