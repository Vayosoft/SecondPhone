using EmulatorHub.Application.Services;
using EmulatorHub.Application.Services.Commons;
using Microsoft.Extensions.DependencyInjection;

namespace EmulatorHub.IntegrationTests
{
    public class EmulatorTests
    {
        private readonly EmulatorService _userService;

        public EmulatorTests()
        {
            _userService = Server.Host.Services.GetRequiredService<EmulatorService>();
        }

        [Fact]
        public async Task Can_get_users()
        {
            var users = await _userService.GetEmulatorsAsync();

            Assert.Collection(users, entity => {});
        }
    }
}
