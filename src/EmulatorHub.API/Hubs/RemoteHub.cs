using Microsoft.AspNetCore.SignalR;

namespace EmulatorHub.API.Hubs
{
    public class RemoteHub : Hub
    {
        private readonly ILogger<RemoteHub> _logger;

        public RemoteHub(ILogger<RemoteHub> logger)
        {
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return base.OnDisconnectedAsync(exception);
        }

        public Task SendPush()
        {
            return Task.CompletedTask;
        }
    }
}
