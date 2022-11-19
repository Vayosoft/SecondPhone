using EmulatorHub.Application.Services.Tokens;
using Microsoft.AspNetCore.Mvc;

namespace EmulatorHub.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        [HttpGet("getToken")]
        public IActionResult GetToken()
        {
            return Ok(TokenUtils.GenerateToken("qwertyuiopasdfghjklzxcvbnm123456", TimeSpan.FromMinutes(60)));
        }
    }
}