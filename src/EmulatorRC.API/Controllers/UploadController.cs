using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EmulatorRC.API.Hubs;
using EmulatorRC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using EmulatorRC.API.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace EmulatorRC.API.Controllers
{

    [Route("api/[Controller]")]
    public class UploadController : ApiControllerBase
    {

        private readonly IHubContext<ImagesHub> _imageHub;
        private readonly IEmulatorDataRepository _emulatorDataRepository;

        public UploadController(IHubContext<ImagesHub> imageHub, IEmulatorDataRepository emulatorDataRepository)
        {
            _imageHub = imageHub;
            _emulatorDataRepository = emulatorDataRepository;

        }

        [HttpPost, DisableRequestSizeLimit]
        public async Task<IActionResult> PostAsync()
        {
            var deviceId = Request.GetDeviceIdOrDefault("DEFAULT")!;
            var imageId = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond).ToString();

            using (var memoryStream = new MemoryStream())
            {
                await Request.Body.CopyToAsync(memoryStream);
                var array = memoryStream.ToArray();

                _emulatorDataRepository.SetScreen(deviceId, imageId, array);
                _emulatorDataRepository.SetLastScreenId(deviceId, imageId);
                _emulatorDataRepository.SetLastScreen(deviceId, array);
            }

            if (ImagesHub.Devices.TryGetValue(deviceId, out var clientIds) && clientIds.Count > 0)
            {
                await _imageHub.Clients.Clients(clientIds.ToArray()).SendAsync("ImageMessage", imageId);
            }

            return Ok("THANKS");
        }

        [HttpGet("token")]
        public IActionResult GenerateToken()
        {
            var signingCredentials = new SigningCredentials(
                key: new SymmetricSecurityKey(Encoding.UTF8.GetBytes("qwertyuiopasdfghjklzxcvbnm123456")),
                algorithm: SecurityAlgorithms.HmacSha256);

            var jwtDate = DateTime.Now;

            var jwt = new JwtSecurityToken(
                audience: "jwt-test", // must match the audience in AddJwtBearer()
                issuer: "jwt-test", // must match the issuer in AddJwtBearer()

                // Add whatever claims you'd want the generated token to include
                claims: new List<Claim> { 
                    new Claim(ClaimTypes.Name, "anton@vayosoft.com"),
                },
                notBefore: jwtDate,
                expires: jwtDate.AddSeconds(3600), // Should be short lived. For logins, it's may be fine to use 24h

                // Provide a cryptographic key used to sign the token.
                // When dealing with symmetric keys then this must be
                // the same key used to validate the token.
                signingCredentials: signingCredentials
            );

            // Generate the actual token as a string
            string token = new JwtSecurityTokenHandler().WriteToken(jwt);

            // Return some agreed upon or documented structure.
            return Ok(new
            {
                jwt = token,
                // Even if the expiration time is already a part of the token, it's common to be 
                // part of the response body.
                unixTimeExpiresAt = new DateTimeOffset(jwtDate).ToUnixTimeMilliseconds()
            });
        }
    }
}