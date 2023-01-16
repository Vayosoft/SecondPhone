using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using EmulatorHub.Application.PushGateway.Commands;
using Vayosoft.Commands;
using Vayosoft.Web.Controllers;

namespace EmulatorHub.API.Controllers
{
    [Route("api/messages")]
    public class MessagesController : ApiControllerBase
    {
        [HttpPost("push/send")]
        public async Task<IActionResult> SendPush(
            [Required][FromHeader(Name = "x-device-id")] string deviceId,
            [FromBody] dynamic payload,
            [FromServices] ICommandBus commandBus,
            CancellationToken cancellationToken)
        {
            var command = new SendPushMessage(deviceId, payload.ToString());
            return Result(await commandBus.Send(command, cancellationToken));
        }
    }
}
