﻿using EmulatorRC.API.Channels;
using Microsoft.AspNetCore.Connections;
using System.IO.Pipelines;

namespace EmulatorRC.API.Handlers
{
    public sealed class SpeakerStreamHandler
    {
        private readonly StreamChannel _channel;
        private readonly ILogger _logger;

        public SpeakerStreamHandler(StreamChannel channel, ILogger logger)
        {
            _channel = channel;
            _logger = logger;
        }

        public async Task HandleOuterAsync(ConnectionContext connection, SpeakerHandshake handshake, CancellationToken token)
        {
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
                    await foreach (var segment in _channel.ReadAllCameraAsync(deviceId, token))
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

        public async Task HandleInnerAsync(ConnectionContext connection, SpeakerHandshake handshake, CancellationToken token)
        {
            var channelWriter = _channel.GetOrCreateSpeakerWriter(handshake.DeviceId);
            if (!channelWriter.IsError)
            {
                var writer = channelWriter.Value;

                while (!token.IsCancellationRequested)
                {
                    var result = await connection.Transport.Input.ReadAsync(token);
                    var buffer = result.Buffer;

                    foreach (var segment in buffer)
                    {
                        await writer!.WriteAsync(segment, token);
                    }

                    if (result.IsCompleted)
                    {
                        break;
                    }

                    connection.Transport.Input.AdvanceTo(buffer.End);
                }
            }
            else
            {
                _logger.LogError("{ConnectionId} => {Error}", connection.ConnectionId, channelWriter.FirstError.Description);
            }
        }
    }
}
