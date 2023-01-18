using EmulatorRC.API.Channels;
using Microsoft.AspNetCore.Connections;
using System.IO.Pipelines;

namespace EmulatorRC.API.Handlers
{
    public sealed class MicStreamReader
    {
        private readonly StreamChannel _channel;
        private readonly ILogger _logger;

        public MicStreamReader(StreamChannel channel, ILogger logger)
        {
            _channel = channel;
            _logger = logger;
        }

        public async Task ReadAsync(ConnectionContext connection, AudioHandshake handshake, CancellationToken token)
        {
            var buf = new byte[]{(byte)'-', (byte)'@', (byte)'v', (byte)'0', (byte)'2', 2};
            _ = await connection.Transport.Output.WriteAsync(buf, token);
            _ = ReadFromChannelAsync(handshake.DeviceId, connection.Transport.Output, token);

            while (!token.IsCancellationRequested)
            {
                var result = await connection.Transport.Input.ReadAsync(token);
                var buffer = result.Buffer;

                var consumed = buffer.End;
                
                if (result.IsCompleted)
                {
                    break;
                }

                connection.Transport.Input.AdvanceTo(consumed);
            }
        }

        private async Task ReadFromChannelAsync(string deviceId, PipeWriter output, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await foreach (var segment in _channel.ReadAllMicAsync(deviceId, token))
                    {
                        await output.WriteAsync(segment, token);
                    }

                    await Task.Delay(1000, token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                _logger.LogError(e, "ReadFromChannel => {Error}\r\n", e.Message);
            }
        }
    }
}
