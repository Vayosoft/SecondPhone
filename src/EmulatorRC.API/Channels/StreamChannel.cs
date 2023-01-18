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

        public StreamChannel(ILogger<StreamChannel> logger)
        {
            _logger = logger;
        }

        public Task WriteSpeakerAsync(string name, ConnectionContext connection, CancellationToken cancellationToken) =>
            WriterAsync(_speakerDeviceToClient, name, connection, cancellationToken);

        public Task WriterCameraAsync(string name, ConnectionContext connection, CancellationToken cancellationToken) =>
            WriterAsync(_cameraClientToDevice, name, connection, cancellationToken);

        public Task WriterMicAsync(string name, ConnectionContext connection, CancellationToken cancellationToken) => 
            WriterAsync(_micClientToDevice, name, connection, cancellationToken);

        private async Task WriterAsync(ConcurrentDictionary<string, Pipe> channels, string name, ConnectionContext connection, CancellationToken cancellationToken)
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

                        foreach (var segment in buffer)
                        {
                            await writer!.WriteAsync(segment, cancellationToken);
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
                    _logger.LogError("WriterAsync {ConnectionId} => {Error}", connection.ConnectionId, channelWriter.FirstError.Description);
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
                return Error.Conflict("Channel is busy");

            using (Locker.GetLockerByName(name).Lock())
            {
                if (channels.TryGetValue(name, out channel))
                    return Error.Conflict("Channel is busy");

                channel = new Pipe();
                channels[name] = channel;
            }

            return channel.Writer;
        }

        public Task ReadAllMicAsync(string name, PipeWriter output, CancellationToken cancellationToken) =>
            ReadAllAsync(_micClientToDevice, name, output, cancellationToken);
        public Task ReadAllCameraAsync(string name, PipeWriter output, CancellationToken cancellationToken) =>
            ReadAllAsync(_cameraClientToDevice, name, output, cancellationToken);
        public Task ReadAllSpeakerAsync(string name, PipeWriter output, CancellationToken cancellationToken) =>
            ReadAllAsync(_cameraClientToDevice, name, output, cancellationToken);

        private async Task ReadAllAsync(ConcurrentDictionary<string, Pipe> channels, string name, PipeWriter output, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await foreach (var segment in ReadAllAsync(channels, name, token))
                    {
                        await output.WriteAsync(segment, token);
                    }

                    await Task.Delay(1000, token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                _logger.LogError(e, "ReadAllAsync => {Error}", e.Message);
            }
        }

        public IAsyncEnumerable<ReadOnlyMemory<byte>> ReadAllMicAsync(string name, CancellationToken cancellationToken) =>
            ReadAllAsync(_micClientToDevice, name, cancellationToken);
        public IAsyncEnumerable<ReadOnlyMemory<byte>> ReadAllCameraAsync(string name, CancellationToken cancellationToken) =>
            ReadAllAsync(_cameraClientToDevice, name, cancellationToken);
        public IAsyncEnumerable<ReadOnlyMemory<byte>> ReadAllSpeakerAsync(string name, CancellationToken cancellationToken) =>
            ReadAllAsync(_cameraClientToDevice, name, cancellationToken);

        private static async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadAllAsync(ConcurrentDictionary<string, Pipe> channels, string name,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (!channels.TryGetValue(name, out var channel)) yield break;

            var reader = channel.Reader;
            try
            {
                while (true)
                {
                    var result = await reader.ReadAsync(cancellationToken);
                    var buffer = result.Buffer;

                    foreach (var segment in buffer)
                    {
                        yield return segment;
                    }

                    if (result.IsCompleted)
                    {
                        break;
                    }

                    reader.AdvanceTo(buffer.End);
                }
            }
            finally
            {
                await reader.CompleteAsync();
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
