using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Channels;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class LockBenchmarks
    {
        private readonly ConcurrentDictionary<string, Lazy<Channel<int>>> _channelsLazy = new();
        private readonly ConcurrentDictionary<string, Channel<int>> _channels = new();
        private readonly ConcurrentDictionary<string, object> _locks = new();

        private readonly BoundedChannelOptions _options = new(1)
        {
            SingleWriter = true,
            SingleReader = true,
            AllowSynchronousContinuations = true,
            FullMode = BoundedChannelFullMode.DropOldest
        };

        private readonly Random _random = new Random();
     
        [Benchmark]
        public Channel<int> One()
        {
            return GetOrCreateChannel("default");
        }

        [Benchmark]
        public Channel<int> One_Lazy()
        {
            return GetOrCreateChannel_Lazy("default");
        }

        [Benchmark]
        public Channel<int> Many()
        {
            return GetOrCreateChannel(_random.Next(100).ToString());
        }

        [Benchmark]
        public Channel<int> Many_Lazy()
        {
            return GetOrCreateChannel_Lazy(_random.Next(100).ToString());
        }

        private Channel<int> GetOrCreateChannel_Lazy(string name)
        {
            return _channelsLazy.GetOrAdd(name, _ => 
                new Lazy<Channel<int>>(() =>
                    Channel.CreateBounded<int>(_options))).Value;
        }

        private Channel<int> GetOrCreateChannel(string name)
        {
            if (_channels.TryGetValue(name, out var channel)) return channel;
            lock (_locks.GetOrAdd(name, _ => new object()))
            {
                if (_channels.TryGetValue(name, out channel)) return channel;
                channel = Channel.CreateBounded<int>(_options);
                _channels.TryAdd(name, channel);
            }

            return channel;
        }
    }
}
