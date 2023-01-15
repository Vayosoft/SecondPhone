using EmulatorHub.Domain.Commons.Entities;
using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vayosoft.Identity;
using Vayosoft.Web.Identity.Authorization;

namespace EmulatorHub.API.Controllers
{
    [Produces("application/json")]
    [ProducesErrorResponseType(typeof(void))]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/emulators")]
    [PermissionAuthorization(UserType.Administrator)]
    public class EmulatorsController : ControllerBase
    {
        [ProducesResponseType(typeof(List<Emulator>), StatusCodes.Status200OK)]
        [HttpGet]
        public async Task<IActionResult> GetEmulators(HubDbContext db, CancellationToken cancellationToken)
        {
            return Ok(await db.Devices
                .ToListAsync(cancellationToken: cancellationToken));
        }
    }
}
