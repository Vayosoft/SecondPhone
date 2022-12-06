using EmulatorHub.Application.Services.IdentityProvider;
using Microsoft.AspNetCore.Mvc;

namespace EmulatorHub.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        [HttpGet("getToken")]
        public IActionResult GetToken()
        {
            return Ok(TokenUtils.GenerateToken("qwertyuiopasdfghjklzxcvbnm123456", TimeSpan.FromMinutes(60)));
        }
    }
}
