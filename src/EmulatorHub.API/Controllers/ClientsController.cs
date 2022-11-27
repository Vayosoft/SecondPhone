using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmulatorHub.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        public async Task<IActionResult> GetDevices(HubDbContext db)
        {
            return Ok(await db.Devices.Where(d => d.Client.Id == 1).ToListAsync());
        }
    }
}
