using EmulatorHub.Commons.Domain.Entities;
using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vayosoft.Identity;
using Vayosoft.Persistence;
using Vayosoft.Web.Identity.Authorization;

namespace EmulatorHub.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private ILogger<Emulator> _logger;

        public UsersController(ILogger<Emulator> logger)
        {
            _logger = logger;
        }

        [PermissionAuthorization]
        [HttpGet]
        public async Task<IActionResult> GetUsers(HubDbContext db, CancellationToken cancellationToken)
        {
            return Ok(await db.Users.ToArrayAsync(cancellationToken: cancellationToken));
        }

        [HttpPost("/register")]
        public async Task<IActionResult> RegisterEmulator(string clientId, string deviceId,
            [FromServices] IUnitOfWork db, CancellationToken cancellationToken)
        {
            var user = await db.FindAsync<UserEntity>(1, cancellationToken);
            if (user is { } item)
            {
                var client = new MobileClient
                {
                    Id = clientId,
                    User = user,
                    ProviderId = user.ProviderId
                };
                db.Add(client);

                var device = new Emulator
                {
                    Id = deviceId,
                    Client = client,
                    ProviderId = user.ProviderId,
                };
                db.Add(device);

                await db.CommitAsync(cancellationToken);
                return Ok(device);
            }

            return NotFound();

        }
    }
}