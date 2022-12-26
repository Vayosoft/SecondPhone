using System.Collections.Concurrent;
using System.Threading.Channels;

namespace EmulatorRC.API.Channels
{
    public sealed class TcpStreamChannel : IDisposable
    {
        private readonly ConcurrentDictionary<string, Channel<byte[]>> _channels = new();
        private readonly ConcurrentDictionary<string, object> _locks = new();
        private readonly object _deletingLock = new();

        private readonly BoundedChannelOptions _options = new(5000)
        {
            SingleWriter = true,
            SingleReader = true,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.DropOldest
        };

        public async ValueTask WriteAsync(string streamId, byte[] data, CancellationToken cancellationToken = default)
        {
            await this[streamId].Writer.WriteAsync(data, cancellationToken);
        }

        public bool Write(string streamId, byte[] data)
        {
            return this[streamId].Writer.TryWrite(data);
        }

        public IAsyncEnumerable<byte[]> ReadAllAsync(string streamId, CancellationToken cancellationToken = default)
        {
            return this[streamId].Reader.ReadAllAsync(cancellationToken);
        }

        private Channel<byte[]> this[string streamId]
        {
            get
            {
                if (_channels.TryGetValue(streamId, out var channel)) return channel;
                lock (_locks.GetOrAdd(streamId, _ => new object()))
                {
                    if (!_channels.TryGetValue(streamId, out channel))
                    {
                        channel = Channel.CreateBounded<byte[]>(_options);
                        _channels.TryAdd(streamId, channel);
                    }
                }

                return channel;
            }
        }

        public async Task Consume(string streamId, Action<byte[]> onDataReceived,  CancellationToken cancellationToken = default)
        {
            try
            {
                var channelReader = this[streamId].Reader;

                while (await channelReader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    try
                    {
                        if (channelReader.TryRead(out var item))
                        {
                            // var item = await _channelReader.ReadAsync(_globalCancellationToken).ConfigureAwait(false);

                            onDataReceived(item);
                            //ThreadPool.QueueUserWorkItem(o => { _consumeAction.Invoke(item, _cts.Token); });
                        }
                    }
                    catch (ChannelClosedException)
                    {
                    }
                    catch (OperationCanceledException)
                    {
                        // Logger?.Trace($"[{WorkerName}]: task cancel");
                    }
                    catch (Exception exception)
                    {
                        // Logger?.Warning($"[{WorkerName}]: Exception occurred: {exception}");
                    }
                }

                // Logger?.Trace($"[{WorkerName}]: Shutdown gracefully");
            }
            catch (OperationCanceledException)
            {
                // Logger?.Trace($"[{WorkerName}]: Shutdown due to cancel");
            }
            catch (Exception e)
            {
                // Logger?.Warning($"[{WorkerName}]: Shutdown error: {e.Message}");
            }
        }


        public bool RegisterChannel(string streamId)
        {
            return this[streamId] != null;
        }

        public bool TryGetChannel(string streamId, out Channel<byte[]> channel)
        {
            return _channels.TryGetValue(streamId, out channel);
        }

        public bool IsChannelExists(string streamId)
        {
            return _channels.ContainsKey(streamId);
        }

        public void DisposeChannel(string streamId)
        {
            lock (_deletingLock)
            {
                if (_channels.TryRemove(streamId, out var channel))
                    channel.Writer.Complete();
            }
        }

        public void Dispose()
        {
            foreach (var channel in _channels)
                channel.Value.Writer.Complete();

            _channels.Clear();
        }
    }
}
