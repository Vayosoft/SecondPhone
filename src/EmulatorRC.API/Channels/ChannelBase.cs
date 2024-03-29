﻿using System.Collections.Concurrent;
using System.Threading.Channels;

namespace EmulatorRC.API.Channels
{
    public abstract class ChannelBase<T> : IDisposable
    {
        private readonly ConcurrentDictionary<string, Channel<T>> _channels = new();
        private readonly ConcurrentDictionary<string, object> _locks = new();

        private readonly BoundedChannelOptions _options = new(1)
        {
            SingleWriter = true,
            SingleReader = true,
            AllowSynchronousContinuations = true,
            FullMode = BoundedChannelFullMode.DropOldest
        };

        private bool _disposed;

        protected bool TryGetChannel(string name, out Channel<T> channel)
        {
            return _channels.TryGetValue(name, out channel);
        }

        protected Channel<T> GetOrCreateChannel(string name)
        {
            if (!_channels.TryGetValue(name, out var channel))
            {
                lock (_locks.GetOrAdd(name, s => new object()))
                {
                    if (!_channels.TryGetValue(name, out channel))
                    {
                        channel = Channel.CreateBounded<T>(_options);
                        _channels.TryAdd(name, channel);
                    }
                }
            }

            return channel;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (var channel in _channels)
                        channel.Value.Writer.Complete();

                    _channels.Clear();
                }
                _disposed = true;
            }
        }
    }
}
