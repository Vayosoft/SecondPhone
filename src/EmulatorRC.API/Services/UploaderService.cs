using System.Collections.Concurrent;
using EmulatorRC.API.Extensions;
using EmulatorRC.API.Protos;
using Grpc.Core;

namespace EmulatorRC.API.Services
{
    public class UploaderService : Uploader.UploaderBase
    {   
        public static readonly ConcurrentDictionary<string, object> Devices = new();

        private readonly ILogger<UploaderService> _logger;
        private readonly ScreenChannel _channel;

        public UploaderService(ILogger<UploaderService> logger, ScreenChannel channel)
        {
            _logger = logger;
            _channel = channel;
        }

        public override async Task<Ack> UploadMessage(IAsyncStreamReader<UploadMessageRequest> requestStream, ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            var deviceId = httpContext.Request.GetDeviceIdOrDefault("default")!;

            AddDevice(deviceId);
            _logger.LogInformation("DEV:[{deviceId}] Connected.", deviceId);

            try
            {
                await foreach(var request in requestStream.ReadAllAsync(context.CancellationToken))
                {
                    await _channel.WriteAsync(request);
                }
            }
            catch (RpcException ex) // when (ex.StatusCode == StatusCode.Cancelled)
            {
                _logger.LogWarning("UploaderService| {status} {message}", ex.StatusCode, ex.Message);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError("UploaderService| {type}| {message}", ex.GetType(), ex.Message);
            }

            RemoveDevice(deviceId);
            _logger.LogInformation("DEV:[{deviceId}] Stream closed.", deviceId);

            return new Ack();
        }

        public void AddDevice(string name)
        {
            if (Devices.Any())
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists, "Device already exists"));
            }

            Devices.TryAdd(name, string.Empty);
        }

        public void RemoveDevice(string name) => Devices.TryRemove(name, out var device);
    }
}
