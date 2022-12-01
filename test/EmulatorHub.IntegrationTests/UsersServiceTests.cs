using EmulatorHub.Application.Services;
using EmulatorHub.Application.Services.Commons;
using Microsoft.Extensions.DependencyInjection;

namespace EmulatorHub.IntegrationTests
{
    public class UsersServiceTests
    {
        private readonly UserService _userService;

        public UsersServiceTests()
        {
            _userService = Server.Host.Services.GetRequiredService<UserService>();
        }

        [Fact]
        public async Task Can_get_users()
        {
            var users = await _userService.GetUsersAsync();

            Assert.Collection(users, entity => {});
        }
    }
}
