using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using EmulatorRC.API.Handlers;
using System.Threading;
using EmulatorRC.Commons;
using ErrorOr;
using Microsoft.AspNetCore.Connections;
using System.Xml.Linq;

namespace EmulatorRC.API.Channels
{
    public sealed class StreamChannel
    {
        private readonly ILogger<StreamChannel> _logger;
        private readonly ConcurrentDictionary<string, Pipe> _camera = new();
        private readonly ConcurrentDictionary<string, Pipe> _mic = new();
        private readonly ConcurrentDictionary<string, Pipe> _speaker = new();

        public StreamChannel(ILogger<StreamChannel> logger)
        {
            _logger = logger;
        }

        public async Task WriteSpeakerAsync(string name, ConnectionContext connection, CancellationToken token)
        {
            var channelWriter = GetOrCreateWriter(_camera, name);
            try
            {
                if (!channelWriter.IsError)
                {
                    var writer = channelWriter.Value;

                    while (!token.IsCancellationRequested)
                    {
                        var result = await connection.Transport.Input.ReadAsync(token);
                        var buffer = result.Buffer;

                        foreach (var segment in buffer)
                        {
                            await writer!.WriteAsync(segment, token);
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
                    _logger.LogError("{ConnectionId} => {Error}", connection.ConnectionId, channelWriter.FirstError.Description);
                }
            }
            finally
            {
                await RemoveSpeakerWriterAsync(name);
            }
        }

        public async Task WriterCameraAsync(string name, ConnectionContext connection, CancellationToken cancellationToken)
        {
            var channelWriter = GetOrCreateWriter(_camera, name);
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
                    _logger.LogError("{ConnectionId} => {Error}", connection.ConnectionId, channelWriter.FirstError.Description);
                }
            }
            finally
            {
                await RemoveCameraWriterAsync(name);
            }
        }

        public async Task WriterMicAsync(string name, ConnectionContext connection, CancellationToken cancellationToken)
        {
            var channelWriter = GetOrCreateWriter(_mic, name);
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
                    _logger.LogError("{ConnectionId} => {Error}", connection.ConnectionId, channelWriter.FirstError.Description);
                }
            }
            finally
            {
                await RemoveMicWriterAsync(name);
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

        public IAsyncEnumerable<ReadOnlyMemory<byte>> ReadAllMicAsync(string name, CancellationToken cancellationToken) =>
            ReadAllAsync(_mic, name, cancellationToken);
        public IAsyncEnumerable<ReadOnlyMemory<byte>> ReadAllCameraAsync(string name, CancellationToken cancellationToken) =>
            ReadAllAsync(_camera, name, cancellationToken);
        public IAsyncEnumerable<ReadOnlyMemory<byte>> ReadAllSpeakerAsync(string name, CancellationToken cancellationToken) =>
            ReadAllAsync(_camera, name, cancellationToken);

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
            RemoveWriterAsync(_camera, name);
        public Task RemoveSpeakerWriterAsync(string name) =>
            RemoveWriterAsync(_speaker, name);
        public Task RemoveMicWriterAsync(string name) =>
            RemoveWriterAsync(_mic, name);
        private static async Task RemoveWriterAsync(ConcurrentDictionary<string, Pipe> channels, string name)
        {
            if (channels.TryRemove(name, out var channel))
            {
                await channel.Writer.CompleteAsync();
            }
        }
    }
}
