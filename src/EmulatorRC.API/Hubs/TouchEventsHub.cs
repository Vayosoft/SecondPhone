using EmulatorRC.API.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace EmulatorRC.API.Hubs
{
    public class TouchEventsHub : Hub
    {
        private readonly ILogger<TouchEventsHub> _logger;
        public static readonly Dictionary<string, HashSet<string>> Devices = new();
        public TouchEventsHub(ILogger<TouchEventsHub> logger)
        {
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            try
            {
                var deviceId = Context.GetHttpContext()?.GetDeviceIdOrDefault();
                if (deviceId != null)
                {
                    var connectionId = Context.ConnectionId;

                    if (!Devices.TryGetValue(deviceId, out _))
                    {
                        Devices.Add(deviceId, new HashSet<string>());
                    }
                    _logger.LogInformation("ADDED TE {deviceId} > {connectionId}", deviceId, connectionId);

                    Devices[deviceId].Add(connectionId);
                }
            }catch(Exception e)
            {
                _logger.LogError(e.Message);
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var deviceId = Context.GetHttpContext()?.GetDeviceIdOrDefault();
                if (deviceId != null)
                {
                    var connectionId = Context.ConnectionId;

                    if (Devices.TryGetValue(deviceId, out var clientIds))
                    {
                        _logger.LogInformation("REMOVED TE {deviceId} > {connectionId}", deviceId, connectionId);

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

        public async Task SendMessage(string deviceId, string message)
        {
            if (Devices.TryGetValue(deviceId, out var clientIds) && clientIds.Count > 0)
            {
                await Clients.Clients(clientIds.ToArray()).SendAsync("ReceiveMessage", deviceId, message);
            }
        }
    }
}

