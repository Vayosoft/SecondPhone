using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using EmulatorRC.Commons;
using ErrorOr;

namespace EmulatorRC.API.Channels
{
    public sealed class StreamChannel
    {
        private readonly ConcurrentDictionary<string, Pipe> _camera = new();
        private readonly ConcurrentDictionary<string, Pipe> _speaker = new();

        public ErrorOr<PipeWriter> GetOrCreateCameraWriter(string name) => GetOrCreateWriter(_camera, name);
        public ErrorOr<PipeWriter> GetOrCreateSpeakerWriter(string name) => GetOrCreateWriter(_speaker, name);

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
            RemoveWriterAsync(_camera, name);
        private static async Task RemoveWriterAsync(ConcurrentDictionary<string, Pipe> channels, string name)
        {
            if (channels.TryRemove(name, out var channel))
            {
                await channel.Writer.CompleteAsync();
            }
        }
    }
}
