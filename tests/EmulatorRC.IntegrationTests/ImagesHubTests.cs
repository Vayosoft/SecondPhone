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
            connection.On<ScreenMessage>("OnGetScreen", async message =>
            {
                _helper.WriteLine("[{0}] DATA => '{1}'", counter++, BitConverter.ToString(message.Image));
                await connection.InvokeAsync("GetScreen");
            });

            await connection.StartAsync();
            await connection.InvokeAsync("GetScreen");

            await Task.Delay(1000);
        }
    }

    public class ScreenMessage
    {
        public string Id { get; set; } = null!;
        public byte[] Image { get; init; } = null!;
    }
}
