using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vayosoft.Commands;
using Vayosoft.Identity;
using Vayosoft.Identity.Extensions;
using Vayosoft.Identity.Providers;
using Vayosoft.Identity.Security;
using Vayosoft.Persistence;
using Vayosoft.Persistence.Commands;
using Vayosoft.Persistence.Extensions;
using Vayosoft.Web.Identity.Authorization;

namespace EmulatorHub.API.Controllers
{
    [Route("api/providers")]
    [ApiController]
    public class ProvidersController : ControllerBase
    {
        private readonly ICommandBus commandBus;
        private readonly ILinqProvider linqProvider;
        private readonly IUserContext userContext;

        public ProvidersController(ICommandBus commandBus, ILinqProvider linqProvider, IUserContext userContext)
        {
            this.commandBus = commandBus;
            this.linqProvider = linqProvider;
            this.userContext = userContext;
        }

        [HttpGet]
        [PermissionAuthorization("PROVIDER", SecurityPermissions.View)]
        public async Task<IActionResult> Get(CancellationToken token)
        {
            await userContext.LoadContextAsync();
            long? providerId = !userContext.IsSupervisor
                ? userContext.User.Identity?.GetProviderId() ?? 0
                : null;
            return Ok(await linqProvider
                .WhereIf<ProviderEntity>(providerId != null, p => p.Parent == providerId || p.Id == providerId)
                .ToListAsync(token));
        }

        [HttpPost("set")]
        [PermissionAuthorization("PROVIDER", SecurityPermissions.Add | SecurityPermissions.Edit)]
        public async Task<IActionResult> PostSet([FromBody] ProviderEntity entity, CancellationToken token) {
            entity.Parent = !userContext.IsSupervisor
                ? userContext.User.Identity?.GetProviderId() ?? 0
                : default;
            var command = new CreateOrUpdateCommand<ProviderEntity>(entity);
            await commandBus.Send(command, token);
            return Ok();
        }

        [HttpPost("delete")]
        [PermissionAuthorization("PROVIDER", SecurityPermissions.Delete)]
        public async Task<IActionResult> PostDelete([FromBody] ProviderEntity entity, CancellationToken token) {
            var command = new DeleteCommand<ProviderEntity>(entity);
            await commandBus.Send(command, token);
            return Ok();
        }
    }
}
