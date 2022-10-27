using Microsoft.AspNetCore.SignalR;

namespace EmulatorRC.API.Hubs
{
    public class ImagesHub : Hub
    {
        public static Dictionary<string, HashSet<string>> Devices = new Dictionary<string, HashSet<string>>();


        public override Task OnConnectedAsync()
        {
            try
            {
                string? deviceId = Context.GetHttpContext()?.Request.Headers["X-DEVICE-ID"].FirstOrDefault() ?? null;
                if (deviceId != null)
                {
                    string connectionId = Context.ConnectionId;

                    HashSet<string>? clientIds;
                    if (!Devices.TryGetValue(deviceId, out clientIds))
                    {
                        Devices.Add(deviceId, new HashSet<string>());
                    }
                    Console.WriteLine($"ADDED IE {deviceId} > {connectionId}");
                    Devices[deviceId].Add(connectionId);
                }
            }
            catch (Exception e)
            {
                string s = e.Message;
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {

            string? deviceId = Context.GetHttpContext()?.Request.Headers["X-DEVICE-ID"].FirstOrDefault() ?? null;
            if (deviceId != null)
            {
                string connectionId = Context.ConnectionId;

                HashSet<string>? clientIds;
                if (Devices.TryGetValue(deviceId, out clientIds))
                {
                    Console.WriteLine($"REMOVED IE {deviceId} > {connectionId}");
                    clientIds.Remove(connectionId);
                }
            }
            return base.OnDisconnectedAsync(exception);
        }

        public ImagesHub()
        {
        }

        /*
        public async Task ImageMessage(String action)
        {
            await Clients.All.SendAsync("ImageMessage", action);
        }*/

    }
}

