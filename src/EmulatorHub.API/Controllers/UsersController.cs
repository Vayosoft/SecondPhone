using EmulatorHub.Application.Administration.Specifications;
using Microsoft.AspNetCore.Mvc;
using Vayosoft.Commands;
using Vayosoft.Commons.Models.Pagination;
using Vayosoft.Identity;
using Vayosoft.Identity.Extensions;
using Vayosoft.Identity.Security.Commands;
using Vayosoft.Identity.Security.Models;
using Vayosoft.Identity.Security;
using Vayosoft.Persistence.Commands;
using Vayosoft.Persistence.Queries;
using Vayosoft.Queries;
using Vayosoft.Web.Controllers;
using Vayosoft.Web.Identity.Authorization;
using Vayosoft.Web.Model;
using Vayosoft.Utilities;

namespace EmulatorHub.API.Controllers
{
    [Produces("application/json")]
    [ProducesResponseType(typeof(HttpErrorWrapper), StatusCodes.Status401Unauthorized)]
    [ProducesErrorResponseType(typeof(void))]
    [ApiVersion("1.0")]
    [Route("api/users")]
    public class UsersController : ApiControllerBase
    {
        private readonly ICommandBus commandBus;
        private readonly IQueryBus queryBus;
        private readonly IUserContext userContext;

        public UsersController(ICommandBus commandBus, IQueryBus queryBus, IUserContext userContext)
        {
            this.commandBus = commandBus;
            this.queryBus = queryBus;
            this.userContext = userContext;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<UserEntityDto>), StatusCodes.Status200OK)]
        [PermissionAuthorization("USER", SecurityPermissions.View)]
        public async Task<IActionResult> Get(int page, int size, string searchTerm = null, CancellationToken token = default)
        {
            await userContext.LoadContextAsync();
            var providerId = !userContext.IsSupervisor
                ? Guard.NotNull(userContext.User.Identity?.GetProviderId())
                : null;
            var spec = new UserSpec(page, size, providerId, searchTerm);
            var query = new SpecificationQuery<UserSpec, IPagedEnumerable<UserEntityDto>>(spec);

            return Paged(await queryBus.Send(query, token), size);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UserEntityDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [PermissionAuthorization("USER", SecurityPermissions.View)]
        public async Task<IActionResult> GetById(ulong id, CancellationToken token)
        {
            var query = new SingleQuery<UserEntityDto>(id);
            UserEntityDto result;
            if ((result = await queryBus.Send(query, token)) is null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        [HttpPost("set")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [PermissionAuthorization("USER", SecurityPermissions.Add | SecurityPermissions.Edit)]
        public async Task<IActionResult> Post([FromBody] SaveUser command, CancellationToken token)
        {
            await commandBus.Send(command, token);
            return Ok();
        }

        [HttpPost("delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [PermissionAuthorization("USER", SecurityPermissions.Delete)]
        public async Task<IActionResult> PostDelete([FromBody] UserEntityDto entity, CancellationToken token)
        {
            var command = new DeleteCommand<UserEntity>(new UserEntity(entity.Username) { Id = entity.Id });
            await commandBus.Send(command, token);
            return Ok();
        }

        
    }
}