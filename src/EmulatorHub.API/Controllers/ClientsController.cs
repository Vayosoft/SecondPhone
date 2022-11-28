using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmulatorHub.API.Controllers
{
    [Route("api/clients")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        [HttpPost("token/set")]
        public async Task<IActionResult> SetPushToken(string token, [FromServices]HubDbContext db, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(token))
            {
                return Problem();
            }

            var client = await db.Clients
                .AsTracking()
                .Where(u => u.User.Id == 1)
                .FirstOrDefaultAsync(cancellationToken: cancellationToken);

            if (client == null)
            {
                return NotFound();
            }

            client.PushToken = token;

            await db.SaveChangesAsync(cancellationToken);
            return Ok();
        }
    }
}
