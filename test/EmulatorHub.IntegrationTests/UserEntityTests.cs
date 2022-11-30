using EmulatorHub.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Vayosoft.Identity;
using Vayosoft.Testing;
using Xunit.Abstractions;

namespace EmulatorHub.IntegrationTests
{
    public class UserEntityTests : IClassFixture<DatabaseFixture>
    {
        private readonly ILogger<UserEntityTests> _logger;

        public UserEntityTests(DatabaseFixture fixture, ITestOutputHelper testOutputHelper)
        {
            Fixture = fixture;
            Fixture.Configure(options =>
            {
                options.LoggerFactory = LoggerFactory
                    .Create(builder => builder.AddProvider(new XUnitLoggerProvider(testOutputHelper)));
            });
            Fixture.Initialize();

            _logger = XUnitLogger.CreateLogger<UserEntityTests>(testOutputHelper);
        }

        public DatabaseFixture Fixture { get; }

        [Theory]
        [InlineData("support")]
        public void CreateUsers(string username)
        {
            var user = new UserEntity($"{username}@vayosoft.com")
            {
                Email = $"{username}@vayosoft.com",
                PasswordHash = "VBbXzW7xlaD3YiqcVrVehA==",
                Phone = "0500000000",
                Registered = DateTime.UtcNow
            };

            using var context = Fixture.CreateContext();
            context.Users.Add(user);
            context.SaveChanges();

            _logger.LogInformation("userId: {UserId}", user.Id);

            user.Id.Should().BeGreaterThan(0);
        }
    }
}