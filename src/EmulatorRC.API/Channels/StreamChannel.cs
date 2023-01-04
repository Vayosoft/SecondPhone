using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace EmulatorRC.API.Channels
{
    public sealed class StreamChannel
    {
        private readonly ILogger<StreamChannel> _logger;

        public StreamChannel(ILogger<StreamChannel> logger)
        {
            _logger = logger;
        }

        private static readonly ConcurrentDictionary<string, Pipe> Channels = new();
        private readonly ConcurrentDictionary<string, object> _locks = new();

        public bool TryGetChannelReader(string name, out ChannelReader reader)
        {
            reader = Channels.TryGetValue(name, out var channel) ? new ChannelReader(name, channel.Reader) : default;
            return reader != default;
        }

        public ChannelWriter GetOrCreateChannelWriter(string name)
        {
            if (!Channels.TryGetValue(name, out var channel))
            {
                lock (_locks.GetOrAdd(name, _ => new object()))
                {
                    if (!Channels.TryGetValue(name, out channel))
                    {
                        channel = new Pipe();
                        Channels.TryAdd(name, channel);
                    }
                }
            }

            return new ChannelWriter(name, channel.Writer);
        }

        public readonly struct ChannelReader : IAsyncDisposable, IEquatable<ChannelReader>
        {
            private readonly string _key;
            private readonly PipeReader _reader;

            public ChannelReader(string key, PipeReader reader)
            {
                _key = key;
                _reader = reader;
            }

            public async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
            {
                while (true)
                {
                    var result = await _reader.ReadAsync(cancellationToken);
                    var buffer = result.Buffer;

                    foreach (var segment in buffer)
                    {
                        yield return segment;
                    }

                    if (result.IsCompleted)
                    {
                        break;
                    }

                    _reader.AdvanceTo(buffer.End);
                }
            }

            public async ValueTask DisposeAsync()
            {
                await _reader.CompleteAsync();
            }

            public bool Equals(ChannelReader other)
            {
                return _key == other._key && Equals(_reader, other._reader);
            }

            public override bool Equals(object obj)
            {
                return obj is ChannelReader other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(_key, _reader);
            }

            public static bool operator ==(ChannelReader lch, ChannelReader rch)
            {
                return lch.Equals(rch);
            }
            public static bool operator !=(ChannelReader lch, ChannelReader rch)
            {
                return !lch.Equals(rch);
            }
        }

        public readonly struct ChannelWriter : IAsyncDisposable, IEquatable<ChannelWriter>
        {
            private readonly string _key;
            private readonly PipeWriter _writer;

            public ChannelWriter(string key, PipeWriter writer)
            {
                _key = key;
                _writer = writer;
            }

            public async ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
            {
                await _writer.WriteAsync(data, cancellationToken);
            }

            public async ValueTask DisposeAsync()
            {
                if (Channels.TryRemove(_key, out var channel))
                {
                    await channel.Writer.CompleteAsync();
                }
            }

            public bool Equals(ChannelWriter other)
            {
                return _key == other._key && Equals(_writer, other._writer);
            }

            public override bool Equals(object obj)
            {
                return obj is ChannelWriter other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(_key, _writer);
            }

            public static bool operator ==(ChannelWriter lch, ChannelWriter rch)
            {
                return lch.Equals(rch);
            }
            public static bool operator !=(ChannelWriter lch, ChannelWriter rch)
            {
                return !lch.Equals(rch);
            }
        }
    }
}
