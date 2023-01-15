using EmulatorHub.Application.Commons.Services;
using EmulatorHub.Application.Commons.Services.Commons;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace EmulatorHub.IntegrationTests
{
    public class MessageTests
    {
        private readonly MessageService _userService;
        private readonly ITestOutputHelper _logger;

        public MessageTests(ITestOutputHelper logger)
        {
            _logger = logger;
            _userService = Server.Host.Services.GetRequiredService<MessageService>();
        }

        [Fact]
        public async Task Can_Send_PushMessage()
        {
           var result = await _userService.SendPushMessage("device_123", new TestPushMessage(Guid.NewGuid().ToString(), "test"));

            _logger.WriteLine(result);
        }

        public record TestPushMessage(string Id, string Text);

    }
}
