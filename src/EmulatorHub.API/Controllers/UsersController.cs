using System.ComponentModel.DataAnnotations;
using EmulatorHub.Commons.Domain.Entities;
using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Vayosoft.Caching;
using Vayosoft.Identity;
using Vayosoft.Identity.Extensions;
using Vayosoft.Persistence;
using Vayosoft.Persistence.Criterias;
using Vayosoft.Web.Identity.Authorization;

namespace EmulatorHub.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [PermissionAuthorization]
    public class UsersController : ControllerBase
    {
        private ILogger<Emulator> _logger;

        public UsersController(ILogger<Emulator> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(HubDbContext db, CancellationToken cancellationToken)
        {
            return Ok(await db.Users.ToArrayAsync(cancellationToken: cancellationToken));
        }

        [HttpPost("/register-device")]
        public async Task<IActionResult> RegisterEmulator([FromBody] RegisterDevice model,
            [FromServices] IUnitOfWork db, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var userId = HttpContext.User.Identity.GetUserId();

            var user = await db.FindAsync<UserEntity>(userId, cancellationToken);
            if (user is { } item)
            {
                var client = await db.FindAsync<MobileClient>(model.ClientId, cancellationToken);
                if (client is null)
                {
                    client = new MobileClient
                    {
                        Id = model.ClientId,
                        User = user,
                        ProviderId = user.ProviderId
                    };
                    db.Add(client);
                }

                var device = await db.FindAsync<Emulator>(model.DeviceId, cancellationToken);
                if (device is null)
                {
                    device = new Emulator
                    {
                        Id = model.DeviceId,
                        Client = client,
                        ProviderId = user.ProviderId,
                    };
                    db.Add(device);
                }
                else if(device.Client.Id != model.ClientId)
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

    public record RegisterDevice
    {
        [Required]
        public string ClientId { get; set; }
        [Required]
        public string DeviceId { get; set; }
    };
}