using EmulatorHub.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Vayosoft.Persistence;
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
            [FromServices] IUnitOfWork db,
            [FromServices] ILogger<MessagesController> logger,
            [FromServices] PushBrokerFactory pushFactory,
            CancellationToken cancellationToken)
        {
            var message = JObject.Parse(payload.ToString());

            var user = await db.FindAsync<UserEntity>(1, cancellationToken);
            if (user == null || string.IsNullOrEmpty(user.PushToken))
                return NotFound();

            logger.LogInformation($"Sending push message...\r\n{payload}");

            pushFactory
                .GetFor("Android")
                .Send(user.PushToken, message);

            return Ok();
        }
    }
}
