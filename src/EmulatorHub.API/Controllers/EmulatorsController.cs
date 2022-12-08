using EmulatorHub.Domain.Entities;
using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmulatorHub.API.Controllers
{
    [Produces("application/json")]
    [ProducesErrorResponseType(typeof(void))]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/emulators")]
    public class EmulatorsController : ControllerBase
    {
        [ProducesResponseType(typeof(List<Emulator>), StatusCodes.Status200OK)]
        [HttpGet]
        public async Task<IActionResult> GetEmulators(HubDbContext db)
        {
            return Ok(await db.Devices
                .Include(d => d.Client)
                .ToListAsync());
        }
    }
}
