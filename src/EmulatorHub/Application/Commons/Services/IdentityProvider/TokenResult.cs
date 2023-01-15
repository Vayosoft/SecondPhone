namespace EmulatorHub.Application.Commons.Services.IdentityProvider
{
    public class TokenResult
    {
        public TokenResult(string token)
        {
            Token = token;
        }

        public string Token { get; set; }
        public long UnixTimeExpiresAt { get; set; }
    }
}
