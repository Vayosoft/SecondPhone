using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace EmulatorHub.Tokens
{
    public static class TokenUtils
    {
        public static TokenResult GenerateToken(string key)
        {
            var signingCredentials = new SigningCredentials(
                key: new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                algorithm: SecurityAlgorithms.HmacSha256);

            var jwtDate = DateTime.Now;

            var jwt = new JwtSecurityToken(
                audience: "Vayosoft", // must match the audience in AddJwtBearer()
                issuer: "Vayosoft", // must match the issuer in AddJwtBearer()

                // Add whatever claims you'd want the generated token to include
                claims: new List<Claim> {
                    new(ClaimTypes.Name, "test_client@vayosoft.com"),
                },
                notBefore: jwtDate,
                expires: jwtDate.AddSeconds(300), // Should be short lived. For logins, it's may be fine to use 24h

                // Provide a cryptographic key used to sign the token.
                // When dealing with symmetric keys then this must be
                // the same key used to validate the token.
                signingCredentials: signingCredentials
            );

            // Generate the actual token as a string
            string token = new JwtSecurityTokenHandler().WriteToken(jwt);

            // Return some agreed upon or documented structure.
            return new TokenResult(token)
            {
                // Even if the expiration time is already a part of the token, it's common to be 
                // part of the response body.
                UnixTimeExpiresAt = new DateTimeOffset(jwtDate).ToUnixTimeMilliseconds()
            };
        }
    }
}
