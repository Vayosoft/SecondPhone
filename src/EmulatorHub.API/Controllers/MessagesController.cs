using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Vayosoft.PushBrokers;

namespace EmulatorHub.API.Controllers
{
    [Route("api/messages")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        [HttpPost("push/send")]
        public async Task<IActionResult> SendPush(
            [FromBody] dynamic payload,
            [FromServices] HubDbContext db,
            [FromServices] ILogger<MessagesController> logger,
            [FromServices] PushBrokerFactory pushFactory,
            CancellationToken cancellationToken)
        {
            var message = JObject.Parse(payload.ToString());

            var client = await db.Clients.Where(u => u.User.Id == 1)
                .FirstOrDefaultAsync(cancellationToken: cancellationToken);

            if (client == null || string.IsNullOrEmpty(client.PushToken))
                return NotFound();

            logger.LogInformation($"Sending push message...\r\n{payload}");

            pushFactory
                .GetFor("Android")
                .Send(client.PushToken, message);

            return Ok();
        }
    }
}
