using System.Net;
using System.Text;
using EmulatorHub.API.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Vayosoft.Caching;
using Vayosoft.Commons.ValueObjects;
using Vayosoft.Identity;
using Vayosoft.Identity.Authentication;
using Vayosoft.Identity.Persistence;
using Vayosoft.SmsBrokers;
using Vayosoft.Utilities;
using Vayosoft.Web.Extensions;
using Vayosoft.Web.Identity.Authorization;
using Vayosoft.Web.Model.Authentication;

namespace EmulatorHub.API.Controllers
{
    [Route("api/account")]
    [ApiController]
    [PermissionAuthorization]
    public class AccountController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly IDistributedMemoryCache _cache;
        private readonly IHostEnvironment _env;
        private readonly SmsBrokerFactory _smsBrokerFactory;
        private readonly IUserRepository _userRepository;

        public AccountController(
            IDistributedMemoryCache cache,
            IHostEnvironment env,
            SmsBrokerFactory smsBrokerFactory,
            IUserRepository userRepository,
            IAuthenticationService authService)
        {
            _cache = cache;
            _env = env;
            _smsBrokerFactory = smsBrokerFactory;
            _userRepository = userRepository;
            _authService = authService;
        }

        [AllowAnonymous]
        [HttpPost("one-time-password")]
        public async Task<IActionResult> OneTimePassword([FromBody] OneTimePasswordRequest model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var key = $"one-time-password:{HttpContext.GetIpAddress()}";
            var timeout = TimeSpans.FifteenSeconds;

            var lastTime = _cache.Get<DateTime?>(key);
            if (lastTime != null && DateTime.UtcNow - lastTime.Value < timeout)
            {
                return StatusCode((int)HttpStatusCode.TooManyRequests, (timeout - (DateTime.UtcNow - lastTime)).Value.TotalMilliseconds);
            }

            var phoneNumber = new PhoneNumber(model.PhoneNumber);
            var user = await _userRepository.FindByNameAsync(phoneNumber, cancellationToken);
            if (user != null)
            {
                var password = GetFixedHash($"{phoneNumber}:{DateTime.UtcNow}", 4);
                _cache.Set($"{model.PhoneNumber}:{password}", user, TimeSpans.FiveMinutes);

                var smsBroker = _smsBrokerFactory.GetFor("Diafan");
                var message = $"Validation code: {password}";
                await smsBroker.SendAsync(phoneNumber, "system", WebUtility.HtmlDecode(message));
            }

            _cache.Set(key, DateTime.UtcNow, TimeSpan.FromMinutes(1));

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] OneTimePasswordLoginRequest model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!_cache.TryGetValue<UserEntity>($"{model.PhoneNumber}:{model.Password}", out var cachedUser) || cachedUser == null)
            {
                return NotFound(model.PhoneNumber);
            }

            var user = await _userRepository.FindByIdAsync(cachedUser.Id, cancellationToken);
            if (user == null)
            {
                return NotFound(cachedUser.Phone);
            }

            var authResult = await _authService.AuthenticateAsync(user, HttpContext.GetIpAddress(), cancellationToken);
            await HttpContext.Session.SetAsync("_roles", authResult.Roles);
            HttpContext.SetTokenCookie(authResult.RefreshToken, _env.IsProduction());
            var response = new AuthenticationResponse(
                authResult.User.Username,
                authResult.Token,
                authResult.TokenExpirationTime);

            return Ok(response);
        }

        private static int GetFixedHash(string s, int length)
        {
            var mustBeLessThan = Math.Pow(10, length); // 6 decimal digits

            uint hash = 0;
            // if you care this can be done much faster with unsafe 
            // using fixed char* reinterpreted as a byte*
            foreach (byte b in Encoding.Unicode.GetBytes(s))
            {
                hash += b;
                hash += (hash << 10);
                hash ^= (hash >> 6);
            }
            // final avalanche
            hash += (hash << 3);
            hash ^= (hash >> 11);
            hash += (hash << 15);
            // helpfully we only want positive integer < MUST_BE_LESS_THAN
            // so simple truncate cast is ok if not perfect
            return (int)(hash % mustBeLessThan);
        }
    }
}
