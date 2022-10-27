using Microsoft.AspNetCore.SignalR;

namespace EmulatorRC.API.Hubs
{
    public class TouchEventsHub : Hub
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
                    Console.WriteLine($"ADDED TE {deviceId} > {connectionId}");
                    Devices[deviceId].Add(connectionId);
                }
            }catch(Exception e)
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
                    Console.WriteLine($"REMOVED TE {deviceId} > {connectionId}");
                    clientIds.Remove(connectionId);
                }
            }
            return base.OnDisconnectedAsync(exception);
        }
        public TouchEventsHub()
        {
        }

        public async Task SendMessage(string deviceId, string message)
        {
            HashSet<string>? clientIds;
            if (Devices.TryGetValue(deviceId, out clientIds) && clientIds.Count > 0)
            {
                await Clients.Clients(clientIds.ToArray()).SendAsync("ReceiveMessage", deviceId, message);
            }
        }



    }
}

