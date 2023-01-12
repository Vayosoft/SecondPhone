using EmulatorHub.Commons.Domain.Entities;
using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vayosoft.Identity.Extensions;
using Vayosoft.Web.Identity.Authorization;

namespace EmulatorHub.API.Controllers
{
    [Produces("application/json")]
    [ProducesErrorResponseType(typeof(void))]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/emulators")]
    [PermissionAuthorization]
    public class EmulatorsController : ControllerBase
    {
        [ProducesResponseType(typeof(List<Emulator>), StatusCodes.Status200OK)]
        [HttpGet]
        public async Task<IActionResult> GetEmulators(HubDbContext db, CancellationToken cancellationToken)
        {
            var userId = HttpContext.User.Identity.GetUserId();

            return Ok(await db.Devices
                //.Include(d => d.Client)
                .Where(e => e.Client.User.Id == userId)
                .ToListAsync(cancellationToken: cancellationToken));
        }
    }
}
