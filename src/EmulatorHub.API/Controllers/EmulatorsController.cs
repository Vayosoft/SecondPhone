using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmulatorHub.API.Controllers
{
    [ApiController]
    [Route("api/emulators")]
    public class EmulatorsController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetEmulators(HubDbContext db)
        {
            var emulators = await db.Devices.ToListAsync();
            return Ok(emulators);
        }
    }
}
