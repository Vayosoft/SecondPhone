using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using EmulatorHub.PushBroker.Application.Commands;
using Vayosoft.Commands;
using Vayosoft.PushBrokers;

namespace EmulatorHub.API.Controllers
{
    [Route("api/messages")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        [HttpPost("push/send")]
        public async Task<IActionResult> SendPush(
            [Required][FromHeader(Name = "x-device-id")] string deviceId,
            [FromBody] dynamic payload,
            [FromServices] HubDbContext db,
            [FromServices] ILogger<MessagesController> logger,
            [FromServices] PushBrokerFactory pushFactory,
            [FromServices] ICommandBus commandBus,
            CancellationToken cancellationToken)
        {
            //var result = await commandBus.Send(new SendPushMessage(deviceId, payload.ToString()), cancellationToken);
            //return Ok();

            if (string.IsNullOrEmpty(deviceId))
            {
                ModelState.AddModelError(nameof(deviceId), "DeviceId has not provided.");
            }
            string data = payload.ToString();
            if (string.IsNullOrEmpty(data))
            {
                ModelState.AddModelError(nameof(data), "Payload is empty.");
            }
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var device = await db
                .Devices
                .Include(d => d.Client)
                .Where(d => d.Id == deviceId)
                .FirstOrDefaultAsync(cancellationToken: cancellationToken);

            if (device == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(device.Client.PushToken))
            {
                return Problem("The client has not token.");
            }

            logger.LogInformation("Sending push message...\r\n{data}", data);

            pushFactory
                .GetFor("Android")
                .Send(device.Client.PushToken, JObject.Parse(data!));

            return Ok();
        }
    }
}
