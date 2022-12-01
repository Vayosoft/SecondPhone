using EmulatorHub.Domain.Entities;
using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vayosoft.Identity;
using Vayosoft.Persistence;

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

        //[Authorize]
        [HttpGet]
        public async Task<IActionResult> GetUsers(HubDbContext db)
        {
            return Ok(await db.Users.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> RegisterEmulator(string clientId, string deviceId,
            [FromServices] IUnitOfWork db)
        {
            var user = await db.FindAsync<UserEntity>(1);
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

                await db.CommitAsync();
                return Ok(device);
            }

            return NotFound();

        }
    }
}