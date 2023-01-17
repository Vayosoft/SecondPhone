using System.ComponentModel.DataAnnotations;
using EmulatorHub.Application.ClientContext.Commands;
using EmulatorHub.Application.ClientContext.Models;
using EmulatorHub.Domain.Commons.Entities;
using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vayosoft.Commons;
using Vayosoft.Identity.Extensions;
using Vayosoft.Persistence.Extensions;
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
        private readonly HubDbContext _store;
        private readonly IProjector _projector;

        public ClientsController(HubDbContext store, IProjector projector)
        {
            _store = store;
            _projector = projector;
        }

        [ProducesResponseType(typeof(List<MobileClientDto>), StatusCodes.Status200OK)]
        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var userId = HttpContext.User.Identity.GetUserId();
            var clients = await _store.Clients
                .Where(c => c.User.Id == userId)
                .Project<MobileClient, MobileClientDto>(_projector)
                .ToListAsync(cancellationToken: cancellationToken);

            return Ok(clients);
        }

        [ProducesResponseType(typeof(MobileClientDto), StatusCodes.Status200OK)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            var userId = HttpContext.User.Identity.GetUserId();
            var client = await _store.Clients
                .Where(e => e.Id == id && e.User.Id == userId)
                .Project<MobileClient, MobileClientDto>(_projector)
                .SingleOrDefaultAsync(cancellationToken: cancellationToken);

            if (client == null)
            {
                return NotFound(id);
            }

            return Ok(client);
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update(
            [Required][FromHeader(Name = "x-client-id")] string clientId,
            [FromBody]UpdateMobileClient command, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                ModelState.AddModelError(nameof(clientId), "ClientId was not provided.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = HttpContext.User.Identity.GetUserId();
            var client = await _store.Clients
                .AsTracking()
                .Where(e => e.Id == clientId && e.User.Id == userId)
                .SingleOrDefaultAsync(cancellationToken: cancellationToken);

            if (client == null)
            {
                return NotFound();
            }

            client.Name = command.Name;
            await _store.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        [HttpPost("set-token")]
        public async Task<IActionResult> SetPushToken(
            [Required][FromHeader(Name = "x-client-id")] string clientId, 
            [FromBody]SetPushToken command,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                ModelState.AddModelError(nameof(clientId), "ClientId was not provided.");
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }
            var userId = HttpContext.User.Identity.GetUserId();
            var client = await _store.Clients
                .AsTracking()
                .Where(c => c.User.Id == userId && c.Id == clientId)
                .SingleOrDefaultAsync(cancellationToken: cancellationToken);

            if (client == null)
            {
                return NotFound();
            }

            client.PushToken = command.Token;

            await _store.SaveChangesAsync(cancellationToken);
            return Ok();
        }
    }
}
