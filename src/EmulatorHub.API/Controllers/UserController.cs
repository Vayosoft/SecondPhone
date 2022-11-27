using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmulatorHub.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        [HttpPost("token/set")]
        public async Task<IActionResult> SetPushToken(string token, [FromServices]HubDbContext db, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(token))
            {
                return Problem();
            }

            var user = await db.Users.AsTracking().Where(u => u.Id == 1)
                .SingleOrDefaultAsync(cancellationToken: cancellationToken);

            if (user == null)
            {
                return NotFound();
            }

            user.PushToken = token;

            await db.SaveChangesAsync(cancellationToken);
            return Ok();
        }
    }
}
