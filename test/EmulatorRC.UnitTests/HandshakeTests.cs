using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EmulatorRC.Entities;
using static System.Net.Mime.MediaTypeNames;

namespace EmulatorRC.UnitTests
{
    public partial class HandshakeTests
    {
        [Fact]
        public void EmulatorHandshake()
        {
            const string handshake = "CMD /v2/video.4?640x480&id=default";
            var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(handshake));
            var session = ParseInnerHeader2(ref buffer);

            Assert.Equal("default", session.DeviceId);
        }

        private static ReadOnlySpan<byte> CommandPing => "CMD /v1/ping"u8;
        private static ReadOnlySpan<byte> CommandVideo => "CMD /v2/video.4?"u8;
        private static ReadOnlySpan<byte> GetBattery => "GET /battery"u8;
    
        [GeneratedRegex("(\\d+)x(\\d+)&id=(\\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex HandshakeRegex();
        private static DeviceSession ParseInnerHeader3(ref ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);

            if (reader.IsNext(CommandVideo, true))
            {
                var str = Encoding.UTF8.GetString(reader.UnreadSequence);
                var m = HandshakeRegex().Match(str);
                if (!m.Success || m.Groups.Count < 4)
                {
                    throw new Exception("Authorization required");
                }
                var width = m.Groups[1].Value;
                var height = m.Groups[2].Value;
                var deviceId = m.Groups[3].Value;

                return new DeviceSession { DeviceId = deviceId };
            }

            return null;
        }

        public static DeviceSession ParseInnerHeader2(ref ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);

            if (reader.IsNext(CommandVideo, true))
            {
                if (!reader.TryReadTo(out ReadOnlySpan<byte> widthSpan, (byte)'x') ||
                    !Utf8Parser.TryParse(widthSpan, out int width, out var widthConsumed) ||
                    !reader.TryReadTo(out ReadOnlySpan<byte> heightSpan, (byte)'&') ||
                    !Utf8Parser.TryParse(heightSpan, out int height, out var heightConsumed) ||
                    !reader.TryReadTo(out ReadOnlySpan<byte> _, "id="u8))
                {
                    throw new Exception("Authorization required");
                }

                var deviceId = Encoding.UTF8.GetString(reader.UnreadSequence);

                return new DeviceSession { DeviceId = deviceId };
            }

            return null;
        }

        private const RegexOptions RegexOptions = System.Text.RegularExpressions.RegexOptions.Compiled |
                                                  System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                                                  System.Text.RegularExpressions.RegexOptions.Singleline;

        public static DeviceSession ParseInnerHeader(ref ReadOnlySequence<byte> buffer)
        {
            var payload = Encoding.UTF8.GetString(buffer.First.Span);
            if (payload.StartsWith("CMD /v2/video.4?"))
            {
                var s = payload.Split("?")[1];

                var m = Regex.Match(s, "(\\d+)x(\\d+)&id=(\\w+)", RegexOptions);
                if (!m.Success || m.Groups.Count < 4)
                    throw new Exception("Not authenticated");

                if (!int.TryParse(m.Groups[1].Value, out var w) || !int.TryParse(m.Groups[2].Value, out var h))
                    throw new Exception("Not authenticated");

                var deviceId = m.Groups[3].Value;

                if (w == 0 || h == 0 || string.IsNullOrEmpty(deviceId))
                    throw new Exception("Not authenticated");

                return new DeviceSession { DeviceId = deviceId, StreamType = "cam" };
            }

            throw new Exception("Not authenticated");
        }

        [Fact]
        public void ClientHandshake()
        {
            var handshake = JsonSerializer.SerializeToUtf8Bytes(
                new DeviceSession
                {
                    DeviceId = "default",
                    StreamType = "cam",
                });

            var header = BitConverter.GetBytes(handshake.Length);

            //Array.Resize(ref header, 4 + handshake.Length);
            //Array.Copy(handshake, 0, header, 4, handshake.Length);
            //var buffer = new ReadOnlySequence<byte>(header);

            var firstSegment = new MemorySegment<byte>(header);
            var lastSegment = firstSegment.Append(handshake);
            var buffer = new ReadOnlySequence<byte>(firstSegment, 0, lastSegment, lastSegment.Memory.Length);

            var status = ParseOuterHeader(ref buffer, out var session);

            Assert.Equal(HandshakeStatus.Successful, status);
        }

        private static HandshakeStatus ParseOuterHeader(ref ReadOnlySequence<byte> buffer, out DeviceSession session)
        {
            var reader = new SequenceReader<byte>(buffer);

            if (!reader.TryReadLittleEndian(out int length) || !reader.TryReadExact(length, out var header))
            {
                session = null;
                return HandshakeStatus.Pending;
            }

            Span<byte> payload = stackalloc byte[length];
            header.CopyTo(payload);
            
            session = JsonSerializer.Deserialize<DeviceSession>(payload);
            buffer = buffer.Slice(reader.Position);

            return HandshakeStatus.Successful;
        }

        public enum HandshakeStatus : byte
        {
            Pending,
            Successful,
            Failed
        }

        public class MemorySegment<T> : ReadOnlySequenceSegment<T>
        {
            public MemorySegment(ReadOnlyMemory<T> memory)
            {
                Memory = memory;
            }

            public MemorySegment<T> Append(ReadOnlyMemory<T> memory)
            {
                var segment = new MemorySegment<T>(memory)
                {
                    RunningIndex = RunningIndex + memory.Length
                };

                Next = segment;

                return segment;
            }
        }
    }
}
