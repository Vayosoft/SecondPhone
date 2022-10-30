using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace EmulatorRC.API.Hubs
{
    public sealed class ImagesHub : Hub
    {
        private readonly ILogger<ImagesHub> _logger;
        public static readonly ConcurrentDictionary<string, HashSet<string>> Devices = new();

        public ImagesHub(ILogger<ImagesHub> logger)
        {
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            try
            {
                var deviceId = Context.GetHttpContext()?.Request.Headers["X-DEVICE-ID"].FirstOrDefault() ?? null;
                if (deviceId is not null)
                {
                    var connectionId = Context.ConnectionId;

                    if (!Devices.TryGetValue(deviceId, out _))
                    {
                        Devices.TryAdd(deviceId, new HashSet<string>());
                    }

                    _logger.LogInformation("ADDED IE {deviceId} > {connectionId}", deviceId, connectionId);

                    Devices[deviceId].Add(connectionId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var deviceId = Context.GetHttpContext()?.Request.Headers["X-DEVICE-ID"].FirstOrDefault() ?? null;
                if (deviceId is not null)
                {
                    var connectionId = Context.ConnectionId;

                    if (Devices.TryGetValue(deviceId, out var clientIds))
                    {
                        _logger.LogInformation("REMOVED IE {deviceId} > {connectionId}", deviceId, connectionId);

                        clientIds.Remove(connectionId);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
            
            return base.OnDisconnectedAsync(exception);
        }
    }
}

