using System.ComponentModel.DataAnnotations;
using EmulatorHub.API.Model;
using EmulatorHub.Domain.Commons.Entities;
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
    [Route("api/clients")]
    [PermissionAuthorization]
    public class ClientsController : ControllerBase
    {
        [ProducesResponseType(typeof(List<MobileClient>), StatusCodes.Status200OK)]
        [HttpGet]
        public async Task<IActionResult> GetClients(HubDbContext db, CancellationToken cancellationToken)
        {
            var userId = HttpContext.User.Identity.GetUserId();

            var clients = await db.Clients.Where(c => c.User.Id == userId)
                .ToListAsync(cancellationToken: cancellationToken);

            return Ok(clients);
        }

        [HttpPost("set-token")]
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
            var userId = HttpContext.User.Identity.GetUserId();
            var client = await db.Clients
                .AsTracking()
                .Where(c => c.User.Id == userId && c.Id == clientId)
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
