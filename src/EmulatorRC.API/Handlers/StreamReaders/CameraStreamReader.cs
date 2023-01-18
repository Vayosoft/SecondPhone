﻿using EmulatorRC.API.Channels;
using Microsoft.AspNetCore.Connections;
using System.Buffers;

namespace EmulatorRC.API.Handlers.StreamReaders
{
    public sealed class CameraStreamReader
    {
        private readonly StreamChannel _channel;

        public CameraStreamReader(StreamChannel channel)
        {
            _channel = channel;
        }

        private static ReadOnlySpan<byte> CommandPing => "CMD /v1/ping"u8;
        private static ReadOnlySpan<byte> GetBattery => "GET /battery"u8;

        public async Task ReadAsync(ConnectionContext connection, VideoHandshake videoHandshake, CancellationToken token)
        {
            _ = await connection.Transport.Output.WriteAsync(CreateMockHeader(videoHandshake.Width, videoHandshake.Height), token);
            _ = _channel.ReadAllCameraAsync(videoHandshake.DeviceId, connection.Transport.Output, token);

            while (!token.IsCancellationRequested)
            {
                var result = await connection.Transport.Input.ReadAsync(token);
                var buffer = result.Buffer;

                var consumed = ProcessCommand(buffer, out var cmd);
                switch (cmd)
                {
                    case Commands.GetBattery:
                        _ = await connection.Transport.Output.WriteAsync("\r\n\r\n100"u8.ToArray(), token);
                        break;
                }

                if (result.IsCompleted)
                {
                    break;
                }

                connection.Transport.Input.AdvanceTo(consumed);
            }
        }

        private static SequencePosition ProcessCommand(ReadOnlySequence<byte> buffer, out Commands cmd)
        {
            var reader = new SequenceReader<byte>(buffer);

            if (reader.IsNext(CommandPing, true))
            {
                cmd = Commands.Ping;
            }
            else if (reader.IsNext(GetBattery, true))
            {
                cmd = Commands.GetBattery;
            }
            else
            {
                cmd = Commands.Undefined;
            }

            return reader.Position;
        }

        private static byte[] CreateMockHeader(int width, int height)
        {
            const int some1 = 0x21; //25,
            const int some2 = 0x307fe8f5;

            var byteBuffer = new List<byte>(9)
            {
                //0x02, 0x80 - 640
                (byte) (width >> 8 & 255),
                (byte) (width & 255),
                //0x01, 0xe0 - 480
                (byte)(height >> 8 & 255),
                (byte)(height & 255),
                //0x21 - 33
                some1 & 255,
                //0xf5, 0xe8, 0x7f, 0x30
                some2 & 255,
                some2 >> 8 & 255,
                some2 >> 16 & 255,
                some2 >> 24 & 255
            };

            return byteBuffer.ToArray();
        }
    }
}