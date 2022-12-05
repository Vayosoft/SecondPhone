using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EmulatorHub.API.IdentityProvider.Authentication
{
    /// <summary>
    /// https://referbruv.com/blog/implementing-custom-authentication-scheme-and-handler-in-aspnet-core-3x/
    /// </summary>
    public class CustomAuthenticationHandler : AuthenticationHandler<CustomSchemeOptions>
    {
        public CustomAuthenticationHandler(IOptionsMonitor<CustomSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            throw new NotImplementedException();
        }
    }
}
