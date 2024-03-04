using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AuthCodePKCEServerSide
{
    public interface IClaimExtractor
    {
        Task<IEnumerable<System.Security.Claims.Claim>> ExtractClaim(string token);
    }

    public class ClaimExtractor : IClaimExtractor
    {
        public Task<IEnumerable<Claim>> ExtractClaim(string token)
        {
            // Check if the token can be read
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                throw new ArgumentException("Invalid JWT token format.");
            }

            // Parse the JWT token
            var jwtToken = handler.ReadJwtToken(token);

            // Return the claims found in the token
            return Task.FromResult<IEnumerable<Claim>>(jwtToken.Claims);
        }
    }
}
