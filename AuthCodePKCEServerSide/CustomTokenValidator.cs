using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;
using System.Runtime.Caching; // Add this for MemoryCache

namespace AuthCodePKCEServerSide
{
    public interface ICustomTokenValidator
    {
        Task<bool> ValidateToken(string token, string oktaDomain);
    }

    public class CustomTokenValidator : ICustomTokenValidator
    {
        private static readonly MemoryCache JwksCache = MemoryCache.Default;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public async Task<bool> ValidateToken(string token, string oktaDomain)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var jsonWebKeySet = await GetJsonWebKeySetAsync(token,oktaDomain); // Use await here
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = jsonWebKeySet.Keys,
                ValidateIssuer = true,
                ValidIssuer = $"{oktaDomain}/oauth2/default",
                ValidateAudience = false,
                ValidateLifetime = true
            };

            try
            {
                tokenHandler.ValidateToken(token, parameters, out var validatedToken);
                return validatedToken != null;
            }
            catch
            {
                return false;
            }
        }

        private async Task<JsonWebKeySet> GetJsonWebKeySetAsync(string token, string oktaDomain)
        {
            // Decode the token to extract the user ID (or sub claim)
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

            // If unable to extract user ID, use a default or generic key
            if (string.IsNullOrEmpty(userId))
            {
                userId = "generic";
            }

            var cacheKey = $"JWKS-{oktaDomain}-{userId}";

            if (JwksCache[cacheKey] is JsonWebKeySet cachedJwks && cachedJwks != null)
            {
                return cachedJwks;
            }

            var httpClient = new HttpClient();
            var jwksUri = $"{oktaDomain}/oauth2/default/v1/keys";
            var response = await httpClient.GetAsync(jwksUri);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var jsonWebKeySet = new JsonWebKeySet(jsonResponse);

            JwksCache.Set(cacheKey, jsonWebKeySet, DateTimeOffset.Now.Add(CacheDuration));

            return jsonWebKeySet;
        }

    }
}
