using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using EmulatorRC.Entities;

namespace EmulatorRC.UnitTests
{
    public class HandshakeTests
    {
        [Fact]
        public void Handshake()
        {
            const string handshake = "CMD /v2/video.4?640x480&id=default";
            var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(handshake));
            var session = ParseInnerHeader(ref buffer);

            Assert.Equal("default", session.DeviceId);
        }

        private const RegexOptions RegexOptions = System.Text.RegularExpressions.RegexOptions.Compiled | 
                                                  System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                                                  System.Text.RegularExpressions.RegexOptions.Singleline;

        public DeviceSession ParseInnerHeader(ref ReadOnlySequence<byte> buffer)
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
    }
}
