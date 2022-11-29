using System.ComponentModel.DataAnnotations;
using EmulatorHub.API.Model;
using EmulatorHub.Infrastructure.Persistence;
using LanguageExt.Pipes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmulatorHub.API.Controllers
{
    [Route("api/clients")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        [HttpPost("token/set")]
        public async Task<IActionResult> SetPushToken(
            [Required][FromHeader(Name = "x-client-id")] string clientId, 
            [FromBody]SetTokenViewModel request,
            [FromServices]HubDbContext db,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                ModelState.AddModelError(nameof(clientId), "ClientId has not provided.");
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }
            
            var client = await db.Clients
                .AsTracking()
                .Where(c => c.Id == clientId)
                .FirstOrDefaultAsync(cancellationToken: cancellationToken);

            if (client == null)
            {
                return NotFound();
            }

            client.PushToken = request.Token;

            await db.SaveChangesAsync(cancellationToken);
            return Ok();
        }
    }
}
