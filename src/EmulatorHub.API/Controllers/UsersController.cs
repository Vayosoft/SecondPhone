using EmulatorHub.Commons.Domain.Entities;
using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Vayosoft.Caching;
using Vayosoft.Identity;
using Vayosoft.Persistence;
using Vayosoft.Persistence.Criterias;
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
                var client = await db.FindAsync<MobileClient>(clientId, cancellationToken);
                if (client is null)
                {
                    client = new MobileClient
                    {
                        Id = clientId,
                        User = user,
                        ProviderId = user.ProviderId
                    };
                    db.Add(client);
                }

                var device = await db.FindAsync<Emulator>(deviceId, cancellationToken);
                if (device is null)
                {
                    device = new Emulator
                    {
                        Id = deviceId,
                        Client = client,
                        ProviderId = user.ProviderId,
                    };
                    db.Add(device);
                }
                else if(device.Client.Id != clientId)
                {
                    device.Client = client;
                    db.Update(device);
                }
                
                await db.CommitAsync(cancellationToken);
                return Ok(device);
            }

            return NotFound();

        }
    }
}