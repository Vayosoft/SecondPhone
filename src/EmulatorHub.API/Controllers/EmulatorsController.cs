using EmulatorHub.Domain.Commons.Entities;
using Microsoft.AspNetCore.Mvc;
using Vayosoft.Identity;
using Vayosoft.Identity.Security;
using Vayosoft.Persistence;
using Vayosoft.Web.Identity.Authorization;
using EmulatorHub.Application.Administration.Commands;
using EmulatorHub.Application.Administration.Models;
using Vayosoft.Persistence.Criterias;
using Vayosoft.Utilities;
using EmulatorHub.Application.Administration.Specifications;
using Vayosoft.Commons.Models.Pagination;
using Vayosoft.Persistence.Queries;
using Vayosoft.Queries;
using Vayosoft.Web.Controllers;
using Vayosoft.Web.Model;

namespace EmulatorHub.API.Controllers
{
    [Produces("application/json")]
    [ProducesErrorResponseType(typeof(void))]
    [ApiVersion("1.0")]
    [Route("api/emulators")]
    public class EmulatorsController : ApiControllerBase
    {
        private readonly IQueryBus queryBus;

        public EmulatorsController(IQueryBus queryBus)
        {
            this.queryBus = queryBus;
        }

        [ProducesResponseType(typeof(PagedResponse<Emulator>), StatusCodes.Status200OK)]
        [PermissionAuthorization("DEVICE", SecurityPermissions.View)]
        [HttpGet]
        public async Task<IActionResult> Get(int page, int size, string searchTerm = null, CancellationToken token = default)
        {
            var spec = new EmulatorSpec(page, size, searchTerm);
            var query = new SpecificationQuery<EmulatorSpec, IPagedEnumerable<EmulatorDto>>(spec);

            return Page(await queryBus.Send(query, token), size);
        }

        [PermissionAuthorization("DEVICE", SecurityPermissions.Edit)]
        [HttpPost("/register-device")]
        public async Task<IActionResult> RegisterEmulator([FromBody] RegisterDevice command,
            [FromServices] IUoW store, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await store.FindAsync(new Criteria<ApplicationUser>(u => u.Phone == command.PhoneNumber), cancellationToken);
            if (user is { } userEntity)
            {
                var deviceId = string.IsNullOrEmpty(command.DeviceId) ? GuidUtils.GetStringFromGuid(GuidGenerator.New()) : command.DeviceId;
                MobileClient client;
                var device = await store.FindAsync<Emulator>(command.DeviceId, cancellationToken);
                if (device is null)
                {
                    client = new MobileClient
                    {
                        Id = deviceId,
                        User = user,
                        Name = command.Name ?? deviceId,
                        ProviderId = user.ProviderId
                    };
                    store.Add(client);

                    device = new Emulator
                    {
                        Id = deviceId,
                        Client = client,
                        Name = command.Name ?? deviceId,
                        ProviderId = user.ProviderId,
                    };
                    store.Add(device);
                }
                else
                {
                    client = await store.FindAsync<MobileClient>(deviceId, cancellationToken);
                    if (client is null)
                    {
                        client = new MobileClient
                        {
                            Id = deviceId,
                            User = user,
                            Name = command.Name ?? deviceId,
                            ProviderId = user.ProviderId
                        };
                        store.Add(client);

                    }
                    else if (client.User.Id != user.Id)
                    {
                        client.User = user;
                        store.Update(client);
                    }
                }

                await store.CommitAsync(cancellationToken);
                return Ok(new EmulatorDto { Id = device.Id, Name = device.Name });
            }

            return NotFound(command.PhoneNumber);
        }
    }
}
