using BenchmarkDotNet.Attributes;
using EmulatorRC.Entities;
using System.Buffers;
using EmulatorRC.UnitTests;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class HandshakeBenchmarks
    {
        private ReadOnlySequence<byte> _buffer;

        [GlobalSetup]
        public void Setup()
        {
            _buffer = new ReadOnlySequence<byte>("CMD /v2/video.4?640x480&id=default"u8.ToArray());
        }

        [Benchmark]
        public DeviceSession Regex()
        {
            return HandshakeTests.ParseByRegex(ref _buffer);
        }

        [Benchmark]
        public DeviceSession Regex_Generated()
        {
           return HandshakeTests.ParseByRegexGenerated(ref _buffer);
        }

        [Benchmark]
        public DeviceSession ParseByReader()
        {
            return HandshakeTests.ParseByReader(ref _buffer);
        }
    }
}
