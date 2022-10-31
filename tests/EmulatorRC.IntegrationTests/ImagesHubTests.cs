using Microsoft.AspNetCore.SignalR.Client;
using Xunit.Abstractions;

namespace EmulatorRC.IntegrationTests
{
    public class ImagesHubTests
    {
        private readonly ITestOutputHelper _helper;

        public ImagesHubTests(ITestOutputHelper helper)
        {
            _helper = helper;
        }

        [Fact]
        public async Task SignalTransfer()
        {
            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/zub")
                .Build();

            var counter = 0;
            connection.On<byte[]>("OnGetLastScreen", async data =>
            {
                _helper.WriteLine("[{0}]Got data '{1}'", counter++, BitConverter.ToString(data));
                await connection.InvokeAsync("GetLastScreen");
            });

            await connection.StartAsync();
            await connection.InvokeAsync("GetLastScreen");

            await Task.Delay(1000);
        }
    }
}
