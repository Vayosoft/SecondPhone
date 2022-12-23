﻿using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EmulatorRC.Entities;

namespace EmulatorRC.UnitTests
{
    public class HandshakeTests
    {
        [Fact]
        public void EmulatorHandshake()
        {
            const string handshake = "CMD /v2/video.4?640x480&id=default";
            var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(handshake));
            var session = ParseInnerHeader(ref buffer);

            Assert.Equal("default", session.DeviceId);
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
                    throw new OperationCanceledException("Not authenticated");

                if (!int.TryParse(m.Groups[1].Value, out var w) || !int.TryParse(m.Groups[2].Value, out var h))
                    throw new OperationCanceledException("Not authenticated");

                var deviceId = m.Groups[3].Value;

                if (w == 0 || h == 0 || string.IsNullOrEmpty(deviceId))
                    throw new OperationCanceledException("Not authenticated");

                return new DeviceSession { DeviceId = deviceId, StreamType = "cam" };
            }

            throw new OperationCanceledException("Not authenticated");
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

            Array.Resize(ref header, 4 + handshake.Length);
            Array.Copy(handshake, 0, header, 4, handshake.Length);

            var buffer = new ReadOnlySequence<byte>(header);
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
    }
}
